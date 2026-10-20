# Bitácora de sesión con el agente — Asignación 2

Programación III · ITLA · 2026-C-3 · Sesión del 20 de octubre de 2026 con Claude Code
(modelo Claude Opus 5.5) sobre este repositorio.

## Tarea delegada

Le delegué al agente dos tareas de esta asignación:

1. Construir la pieza **Gestión de permisos** (RF-GP-01 a RF-GP-07) dentro de
   `src/Core/RastreadorHabitos.Core.GestionPermisos`, conectada a Control de acceso sin romper
   la Práctica 1.
2. **Proponer los casos de prueba y redactar las pruebas** de la suite (sección 1.2): transición
   válida, transición prohibida, preparar–actuar–verificar e independencia entre pruebas.

## Qué le pedí

En orden, durante la sesión:

1. Que leyera `AGENTS.md`, el enunciado de la Práctica 1, el de esta asignación
   (`docs/asignacion2-gestion-permisos.md`), el README y el código, **sin escribir código**, y me
   devolviera un resumen, qué parte de Control de acceso se iba a reutilizar y qué faltaba o era
   ambiguo. Después, que creara la rama `chore/docs-asignacion2` y commiteara solo el enunciado.
2. Que propusiera, sin código, la entidad `SolicitudPermiso`, dónde viven su DbContext y su
   migración, y cómo obtener la identidad del solicitante sin que Gestión de permisos autentique.
3. Luego cambié el modo de trabajo: que avanzara solo, con el enunciado como **única fuente de
   verdad** («ni más ni menos»), anotando como «Supuestos» lo que el documento no define; una
   rama por funcionalidad, commits atómicos `tipo(alcance): descripción [ID]`, y antes de
   avisarme: `dotnet build` y `dotnet test` en verde, el comando de «Pruebas» en un clon limpio,
   recorrer la sección 4 y la rúbrica, y comprobar que Control de acceso no se rompió.
4. A mitad del trabajo le recordé hacer un commit por cada cambio lógico en cuanto compile, y
   separar con `git add` selectivo lo que estuviera junto.

## Qué me devolvió

- El diagnóstico: Control de acceso ya tenía roles, la exigencia de rol en un punto único
  (`ExigenciasDeRol.cs`), el rol leído de la base en cada petición y el «usuario actual» como
  método privado de un controller; **no existía** ningún `tienePermiso()`/`aplicarPermiso()`.
  Señaló como ambigüedad principal qué es un «permiso» en RF-GP-05.
- La pieza construida en dos ramas apiladas, con 24 commits atómicos (11 y 13), más el commit
  del enunciado en `chore/docs-asignacion2`:
  - `feature/gp-crear-y-consultar`: entidad y estados, DbContext en el esquema
    `GestionPermisos`, migración, aplicación de la migración al arrancar, servicio de creación
    y listado de pendientes, traducción de sus rechazos en el manejador global, endpoints con
    su exigencia de rol, primeras pruebas y README.
  - `feature/gp-resolver-solicitudes`: tabla de transiciones en un solo punto
    (`Reglas/TransicionesSolicitud.cs`) y un único método que cambia el estado
    (`SolicitudPermiso.CambiarEstado`); aprobar, marcar aplicada, rechazar con motivo y la regla
    de no resolver la propia; el puerto `IAplicadorPermiso`, que la Api conecta con el cambio de
    rol que Control de acceso ya tenía (sin modificar esa pieza); endpoints, pruebas y README.
- La suite: 4 archivos en `tests/RastreadorHabitos.Tests/GestionPermisos/`, con 14 métodos de
  prueba (24 casos contando los `Theory`). Cada uno usa su propia base EF Core en memoria y
  marca sus fases con `// Preparar`, `// Actuar` y `// Verificar`.

## Casos en que el agente se equivocó

### 1. Una prueba que fallaba a veces (dependía de los Guid generados)

**Qué hizo mal.** En la primera versión de
`CreacionSolicitudTests.ListarPendientes_DevuelveLasSolicitudesPendientesDeTodosLosSolicitantes`
la verificación comparaba la lista esperada **en el orden de creación** contra la lista real
**ordenada por Guid**, con una expresión condicional innecesaria:

```csharp
Assert.Equal([deUno.Id, deOtro.Id], pendientes.Select(s => s.Id).Order().ToArray().Length == 2
    ? new[] { deUno.Id, deOtro.Id }.Order().ToArray()
    : []);
```

Como los Guid son aleatorios, la prueba pasaba o fallaba según cuál saliera menor. Eso
incumple la sección 1.2 («pasan en cualquier orden y cada una por separado»).

**Cómo se detectó.** El agente no corrió la prueba una sola vez, sino cuatro seguidas. En la
primera falló y en las otras tres pasó:

```
[FAIL] RastreadorHabitos.Tests.GestionPermisos.CreacionSolicitudTests.ListarPendientes_DevuelveLasSolicitudesPendientesDeTodosLosSolicitantes
   Assert.Equal() Failure: Collections differ
Expected: [dca3745d-ab7e-44de-b8f0-4053808e1935, cf53f651-8152-4e3d-9cce-1aaaedc8739b]
Actual:   [cf53f651-8152-4e3d-9cce-1aaaedc8739b, dca3745d-ab7e-44de-b8f0-4053808e1935]
Failed!  - Failed: 1, Passed: 4, Skipped: 0, Total: 5
--
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5
```

**Cómo se corrigió.** Se ordenan las dos listas de la misma forma antes de comparar. Ya no
importa el orden en que se crean ni en que se devuelven:

```csharp
Assert.Equal(new[] { deUno.Id, deOtro.Id }.Order(), pendientes.Select(s => s.Id).Order());
```

Después de corregirla, la prueba pasó 8 de 8 ejecuciones seguidas antes del commit `b73c760`.
El error nunca llegó al historial.

### 2. Una rama creada siguiendo a `origin/main`

**Qué hizo mal.** Para la rama del enunciado ejecutó
`git switch -c chore/docs-asignacion2 origin/main`, y Git respondió:

```
branch 'chore/docs-asignacion2' set up to track 'origin/main'.
```

La rama quedó siguiendo a `main` remoto. Un `git status` la compararía con `main` y un
`git pull` traería `main` dentro de ella.

**Cómo se detectó.** El agente leyó esa línea en la salida del comando y me lo informó en la
misma respuesta, como un error suyo.

**Cómo se corrigió.** Con `git branch --unset-upstream chore/docs-asignacion2`. Las ramas
siguientes se crearon desde el `main` local (`git switch main`, `git pull --ff-only origin main`,
`git switch -c feature/...`), que no deja configurado ese seguimiento. Se publicaron con
`git push -u origin <rama>`, que las vincula a su rama remota del mismo nombre.

### 3. Un BOM agregado a `Program.cs` por un script de edición

**Qué hizo mal.** Para registrar el DbContext editó `Program.cs` con un script de Python que
escribía el archivo con la codificación `utf-8-sig`. Esa codificación le agregó un BOM al
inicio, que el archivo original no tenía.

**Cómo se detectó.** El cambio debía ser solo de líneas agregadas, pero `git diff --stat`
mostró una línea borrada (`6 insertions(+), 1 deletion(-)`). Al revisar el diff con `cat -A`,
la primera línea aparecía cambiada solo por el BOM:

```
-using Microsoft.AspNetCore.Mvc;$
+M-oM-;M-?using Microsoft.AspNetCore.Mvc;$
```

**Cómo se corrigió.** Quitó el BOM (`sed -i '1s/^\xEF\xBB\xBF//'`) antes de commitear. El diff
quedó en `4 ++++` y el commit `fcaaebe` solo agrega líneas. De ahí en adelante editó los
archivos existentes con la herramienta de edición en vez de scripts.

## Otras incidencias de la sesión

- **Intento bloqueado.** Como `gh` no está instalado, el agente intentó leer la credencial de
  GitHub que guarda Git para abrir los pull requests por la API. El clasificador de seguridad
  de Claude Code bloqueó el intento («Credential Exploration»). El agente no buscó otra vía:
  siguió con el resto del trabajo y dejó los pull requests para que yo los abra.
- **Verificación de que las pruebas detectan fallos.** Para comprobar que las pruebas de
  transición prohibida verifican lo que dicen, el agente agregó temporalmente
  `(Rechazada, Aplicada)` a la tabla de transiciones. Fallaron exactamente las dos pruebas que
  debían fallar:

  ```
  TransicionProhibidaTests.TransicionFueraDeLaTabla_NoEstaPermitida(desde: Rechazada, hacia: Aplicada) [FAIL]
  TransicionProhibidaTests.MarcarAplicada_SolicitudRechazada_SeRechazaYElEstadoNoCambia [FAIL]
  Failed!  - Failed: 2, Passed: 19, Skipped: 0, Total: 21
  ```

  Después revirtió el cambio con `git checkout`, sin commitearlo.

## Evidencia de verificación

- **Suite.** `dotnet test` en un clon limpio de `feature/gp-resolver-solicitudes`, sin variables
  de entorno: `Passed! - Failed: 0, Passed: 47, Skipped: 0, Total: 47` (23 pruebas de la
  Práctica 1 y 24 casos de Gestión de permisos). Cada una de las 14 pruebas de Gestión de
  permisos, ejecutada sola con `--filter`, también pasa.
- **Punta a punta.** Contra la API real con SQL Server, en la base de prueba
  `RastreadorHabitos_E2E`, un script recorrió RF-GP-01 a RF-GP-07 y RD-07 (32 comprobaciones,
  todas OK). Después se reinició la API con otra `Jwt__Clave` y se verificó RD-09: S1 `Aplicada`,
  S2 `Rechazada`, S3 `Pendiente`, y aprobar S1 de nuevo responde `409`.
- **Regresión de la Práctica 1.** Con el código nuevo y los correos reales por la cola,
  entregados a smtp4dev, se probaron los cuatro puntos que se recalifican: registro con
  activación, inicio de sesión, rechazo por rol y recuperación de contraseña. Las 21
  comprobaciones salieron OK.
