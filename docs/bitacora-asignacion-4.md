# Bitácora de la Asignación 4 — Historial de auditoría con el agente

Programación III · ITLA · 2026-C-3. Sesión del 2026-11-23 con Claude Code (Claude Opus 5.5).

## 1. Qué se le pidió al agente

La instrucción del usuario, literal en lo esencial: «Pieza 6 del Core: Core.Auditoria con
RegistroAuditoria (usuario que actuó, acción, entidad afectada, identificador, fecha y hora, valor
anterior, valor nuevo). RF-AUD-01 a 07. Se auditan: resolución de solicitudes de permiso, subida y
eliminación de documentos, cambio de rol, desactivación y reactivación de usuarios, y
restablecimiento de contraseña propio y forzado por un Administrador (indicando quién lo inició).
Nunca guardes contraseñas ni códigos de recuperación.» Y: «DECISIONES DE DISEÑO IMPORTANTES,
consúltame con 2 o 3 opciones y tu recomendación ANTES de implementarlas».

**Decisiones consultadas** (el usuario eligió en los cuatro casos la opción recomendada):

| Decisión | Elegida | Alternativas descartadas |
|---|---|---|
| Dónde se escribe el registro | En el **mismo `SaveChanges`** que el cambio: cada contexto auditado mapea la tabla `Auditoria.RegistrosAuditoria` sin crearla | Transacción compartida entre contextos (frágil); registrar después de guardar (puede quedar un cambio sin registro) |
| Valor anterior y nuevo | **JSON con solo los campos que cambian**, desde una lista blanca (nunca hash, token ni código) | Foto completa de la entidad; texto libre |
| RD-03 | **Auditoría genérica**, sin referencias a otros proyectos; las piezas la referencian a ella | Un puerto por pieza |
| Administrador inicial | Mantener el comando `promover-administrador` | Sembrarlo desde variables; que el primero sea Administrador |

## 2. Qué devolvió el agente

En cuatro ramas, con commits atómicos:

- **Pieza** (`db1de4c`, `fe6b548`, `f5ae8ee`): `RegistroAuditoria` sin ninguna operación de edición,
  `registrarEvento`, el interceptor `HistorialInmutable` que rechaza cualquier guardado que modifique
  o borre un registro, la migración inicial y el registro en la Api.
- **Eventos** (`3d6c464`, `06434eb`, `80cb18b`, `d8e717d`): Control de acceso, Gestión de permisos y
  Documentos escriben su registro en el mismo guardado que su cambio. Hay 7 pruebas, una por tipo
  de evento. Reactivar y forzar el restablecimiento reciben ahora el Id del Administrador.
- **Consulta** (`32d4ea4`, `dbd4872`, `59185f9`): `GET /api/auditoria` solo para el Administrador,
  con filtros en SQL y 13 pruebas.
- La suite pasó de 161 a **181 pruebas**, todas en verde.

**Verificación en vivo** (clon limpio de GitHub, `docker compose up --build`, volúmenes nuevos):
la base nace sin Administradores (0) y se crea uno con el comando. La migración
`20261123234531_InicialAuditoria` se aplicó sola. Los 11 eventos provocados quedaron registrados:
`RolCambiado` (por el sistema, al promover), `SolicitudAprobada` y `SolicitudRechazada`,
`DocumentoSubido` y `DocumentoEliminado`, dos `RolCambiado` por el Administrador,
`UsuarioDesactivado` y `UsuarioReactivado`, `RestablecimientoContrasenaForzado` e
`…Propio`. Ninguno contiene la contraseña ni el código. Los filtros dieron: `?usuarioId=` 3,
`?entidad=Documento` 2, `?entidad=SolicitudPermiso` 2, hoy 11 y un rango de 2020 0. Los tres
filtros inválidos dieron `400`, el Estándar `403`, sin sesión `401`, y `PUT`, `DELETE` y `PATCH`
`405`. Después de `docker compose down` sin `-v` y volver a levantar: **11 registros**.

## 3. Errores del agente detectados y corregidos (reales, de esta sesión)

**Error 1 — Una edición automática borró la cabecera de un método en tres archivos.** Para agregar
el mapeo del historial a los tres contextos, el agente usó un reemplazo con expresión regular en
Python. La referencia `\1` del reemplazo no insertó el grupo capturado: en `ControlAccesoDbContext`,
`GestionPermisosDbContext` y `DocumentosDbContext` desapareció la cabecera
`protected override void OnModelCreating(…) { base.OnModelCreating(…); HasDefaultSchema(…);` y
quedó suelto `modelBuilder.MapearHistorialAuditoria(…)`. **Cómo se detectó:** al revisar el
`git diff` antes de compilar se vio que el método había perdido su cabecera:

```text
-    protected override void OnModelCreating(ModelBuilder modelBuilder)
-    {
-        base.OnModelCreating(modelBuilder);
-
-        modelBuilder.HasDefaultSchema(Esquema);
+    // El historial de auditoría no se modifica ni se borra desde este contexto [RF-AUD-06].
+    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
+        optionsBuilder.ProtegerHistorialAuditoria();
+
+        // El registro de auditoría se escribe en el mismo guardado que el cambio; […]
+        modelBuilder.MapearHistorialAuditoria(excluirDeMigraciones: true);
```

El mismo guion también agregó un BOM a `RastreadorHabitos.Core.Documentos.csproj`, que no lo tenía.
**Corrección:** se descartaron los tres archivos con `git checkout --`, sin commit de por medio, y
se editaron con la herramienta de edición exacta. Después, el agente cambió de método: los
reemplazos restantes se hicieron con un guion que solo acepta un texto que aparezca exactamente una
vez y conserva la codificación del archivo. También se quitó el BOM agregado.

**Error 2 — Leyó mal qué ediciones se habían aplicado y duplicó código.** En un lote de seis
ediciones, el hook de la sesión rechazó tres. El agente supuso que se habían rechazado las de
`OnModelCreating` y las reintentó, pero en Gestión de permisos y Documentos se habían rechazado las
de los `using` y las de `OnModelCreating` **sí** se habían aplicado. **Cómo se detectó:** al
compilar.

```text
GestionPermisosDbContext.cs(22,29): error CS0111: Type 'GestionPermisosDbContext' already defines a
member called 'OnConfiguring' with the same parameter types
DocumentosDbContext.cs(22,29): error CS0111: Type 'DocumentosDbContext' already defines a member
called 'OnConfiguring' with the same parameter types
```

Al quitar el duplicado apareció la otra mitad del error: `error CS1061: 'ModelBuilder' does not
contain a definition for 'MapearHistorialAuditoria'`, porque faltaban los `using`. **Corrección:**
se quitó el bloque duplicado (comprobando que hubiera exactamente dos copias), se agregaron los
`using` y se volvió a compilar. Ninguno de los dos errores llegó a un commit.

**Qué cambia en la forma de delegar:** las ediciones mecánicas en varios archivos se verifican con
`git diff` antes de compilar, y después de un lote rechazado en parte se vuelve a leer el archivo en
lugar de suponer qué cambió.

## 4. Paralelo entre un registro de auditoría y un commit de git

Registro real de la verificación en vivo (cambio de rol) y el commit que agregó esa auditoría:

- **Registro:** `usuarioId` = el Administrador, `accion` = `RolCambiado`, `entidad` = `Usuario`,
  `entidadId` = el Id del usuario, `fechaUtc` = 2026-11-23 (UTC), `valorAnterior` =
  `{"rol":"Estandar"}`, `valorNuevo` = `{"rol":"Administrador"}`.
- **Commit:** `3d6c464` — «feat(control-acceso): audita el cambio de rol, la desactivación, la
  reactivación y el restablecimiento de contraseña…».

| Commit de git | Registro de auditoría | Observación |
|---|---|---|
| Autor (`Jeyson Wascar Adames Echavarria`, y el coautor del agente) | `usuarioId` (quien actuó) | El commit guarda nombre y correo; el registro, solo el Id, que se resuelve con `GET /api/usuarios`. El commit distingue autor y quien lo confirmó (*committer*); el registro tiene un solo actor, y usa `000…000` para el sistema |
| Mensaje (asunto y cuerpo, escritos a mano) | `accion` (+ `entidad`) | La acción es un nombre fijo de un catálogo (`RolCambiado`), no un texto libre; el mensaje explica el porqué, y el registro no |
| Fecha (`2026-11-23`, con zona horaria del autor) | `fechaUtc` | Las dos son el instante del cambio; el registro siempre está en UTC (RD-11) |
| Identificador (`3d6c464…`, hash SHA-1 del contenido) | `id` (GUID aleatorio) | El hash del commit depende de su contenido y de su padre, así que alterarlo cambia el hash; el GUID no prueba nada sobre el contenido. La inmutabilidad del registro la da la aplicación (no hay edición ni borrado, y el interceptor lo impide) |
| Diff (líneas antes y después, de todos los archivos) | `valorAnterior` → `valorNuevo` | El diff guarda todo el cambio; el registro, solo los campos de la lista blanca, y a propósito nunca la contraseña, su hash ni el código |
| Qué cosa cambió: los archivos del diff | `entidad` + `entidadId` | Un commit puede tocar muchos archivos; un registro describe una sola entidad |
| Padre(s): la historia es una cadena | (no tiene) | El historial de auditoría es una lista ordenada por fecha; no enlaza un registro con el anterior |
| (no tiene) | Consulta filtrable con permiso | Un commit lo ve cualquiera con el repositorio; el historial lo consulta solo el Administrador, filtrando por usuario, entidad y fechas |

**Qué guarda uno que el otro no:** git guarda el porqué (el mensaje), el contenido completo, la
cadena de padres que hace detectable cualquier alteración y la diferencia entre autor y confirmador.
El registro guarda el estado de negocio antes y después en un formato que se puede consultar, quién
lo hizo dentro de la aplicación (no quién escribió el código) y una regla de acceso: solo el
Administrador lo ve.
