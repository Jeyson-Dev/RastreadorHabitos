# RastreadorHabitos

Rastreador de hábitos con metas — Programación III · TDS-007 · ITLA · 2026-C-3

## Estado de la Práctica 1 — Control de acceso

| Funcionalidad | Requisitos | Estado |
|---|---|---|
| Registro y activación | RF-CA-01, 02, 14, 15, 16, 17 · RF-NOT-08, 09, 12, 13 · RD-05, 07, 08, 09, 10 | ✅ Implementado |
| Sesión | RF-CA-03, 07, 18, 19 | ✅ Implementado |
| Roles y administración | RF-CA-04, 05, 06, 08, 20, 21 · RD-06 | ✅ Implementado |
| Contraseñas | RF-CA-09 a 13, 22 | ✅ Implementado |
| Máquina de estados de negocio (`Meta`) | RF-NEG-03, 04, 05 · RD-04 | ✅ Estructura declarada (las pruebas llegan en la semana 8) |

Enunciado completo: [`docs/practica1-control-acceso.md`](docs/practica1-control-acceso.md).

## Estado de la Asignación 2 — Gestión de permisos

| Funcionalidad | Requisitos | Estado |
|---|---|---|
| Crear y consultar solicitudes | RF-GP-01, 02 · RD-06, 07, 09 | ✅ Implementado |
| Resolver solicitudes (aprobar, aplicar y rechazar) | RF-GP-03, 04, 05, 06, 07 · RD-04, 06, 07, 09 | ✅ Implementado |
| Suite de pruebas | RD-12 · RF-GP-03, 04, 06 | ✅ [Pruebas](#pruebas) |

Enunciado completo: [`docs/asignacion2-gestion-permisos.md`](docs/asignacion2-gestion-permisos.md).
Cómo provocar cada criterio: [Gestión de permisos](#gestión-de-permisos--asignación-2).

## Stack

- C# / ASP.NET Core Web API (.NET 10)
- SQL Server con Entity Framework Core

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download) o superior.
- SQL Server (local o una instancia accesible) con permiso para crear bases de datos.
- Un servidor SMTP para que los correos lleguen de verdad. Sirve una cuenta de Gmail con una
  [contraseña de aplicación](https://myaccount.google.com/apppasswords) (requiere tener
  activada la verificación en dos pasos), o un servidor de pruebas como
  [smtp4dev](https://github.com/rnwood/smtp4dev).

## Clonar el repositorio

```bash
git clone https://github.com/Jeyson-Dev/RastreadorHabitos.git
cd RastreadorHabitos
```

## Variables de entorno

El repositorio no contiene cadenas de conexión ni credenciales: todo llega por variables de
entorno. Aquí se documenta el nombre y el propósito de cada una, nunca su valor.

| Variable | Obligatoria | Para qué sirve |
|---|---|---|
| `ConnectionStrings__RastreadorHabitos` | Sí | Cadena de conexión a SQL Server. La base de datos se crea sola al arrancar. Sin esta variable la aplicación no arranca y lo indica en la consola. |
| `Jwt__Clave` | Sí (para la API) | Clave secreta, de 32 caracteres o más, con la que se firman las credenciales de sesión. Sin ella, o si es más corta, la API no arranca. El enviador de correos no la necesita. |
| `App__UrlBase` | No | URL pública de la aplicación, sin barra final, con la que se arman los enlaces de activación de los correos. Si no se configura, se usa `http://localhost:5003`, que es la dirección con la que arranca la aplicación. |
| `Smtp__Host` | Para enviar correos | Servidor SMTP (por ejemplo `smtp.gmail.com`). |
| `Smtp__Puerto` | Para enviar correos | Puerto del servidor SMTP (por ejemplo `587`). |
| `Smtp__Remitente` | Para enviar correos | Dirección que aparece como remitente. Con Gmail debe ser la misma cuenta. |
| `Smtp__Usuario` | Si el servidor pide autenticación | Usuario de la cuenta SMTP. |
| `Smtp__Contrasena` | Si el servidor pide autenticación | Contraseña de la cuenta SMTP (con Gmail, la contraseña de aplicación). |
| `Smtp__UsarSsl` | No (por defecto `true`) | Usa STARTTLS. Ponerla en `false` solo con servidores de prueba sin cifrado, como smtp4dev. |

Las variables `Smtp__*` solo las usa el enviador de correos (ver más abajo). La API funciona
sin ellas: los correos quedan pendientes en la cola hasta que se ejecute el enviador con un
servidor SMTP disponible.

Para generar un valor aleatorio para `Jwt__Clave` en PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### Opción A — PowerShell (para la ventana actual)

Reemplazar lo que está entre `< >`. Las variables duran mientras la ventana esté abierta, así
que la API y el enviador deben ejecutarse desde ventanas donde estén configuradas.

```powershell
$env:ConnectionStrings__RastreadorHabitos = "Server=<servidor\instancia>;Database=RastreadorHabitos;Trusted_Connection=True;TrustServerCertificate=True"
$env:Jwt__Clave = "<clave-aleatoria-de-32-caracteres-o-mas>"
$env:Smtp__Host = "smtp.gmail.com"
$env:Smtp__Puerto = "587"
$env:Smtp__Usuario = "<tu-correo@gmail.com>"
$env:Smtp__Contrasena = "<tu-contraseña-de-aplicación>"
$env:Smtp__Remitente = "<tu-correo@gmail.com>"
```

### Opción B — Visual Studio

Visual Studio no ve las variables de una ventana de PowerShell. Hay que guardarlas en el
usuario de Windows (una sola vez) y **cerrar y volver a abrir Visual Studio**:

```powershell
[Environment]::SetEnvironmentVariable('ConnectionStrings__RastreadorHabitos', 'Server=<servidor\instancia>;Database=RastreadorHabitos;Trusted_Connection=True;TrustServerCertificate=True', 'User')
[Environment]::SetEnvironmentVariable('Jwt__Clave', '<clave-aleatoria-de-32-caracteres-o-mas>', 'User')
[Environment]::SetEnvironmentVariable('Smtp__Host', 'smtp.gmail.com', 'User')
[Environment]::SetEnvironmentVariable('Smtp__Puerto', '587', 'User')
[Environment]::SetEnvironmentVariable('Smtp__Usuario', '<tu-correo@gmail.com>', 'User')
[Environment]::SetEnvironmentVariable('Smtp__Contrasena', '<tu-contraseña-de-aplicación>', 'User')
[Environment]::SetEnvironmentVariable('Smtp__Remitente', '<tu-correo@gmail.com>', 'User')
```

No configurarlas en las propiedades de depuración de Visual Studio: se guardan en
`Properties/launchSettings.json`, que forma parte del repositorio.

## Ejecutar la API

```bash
dotnet run --project src/Api/RastreadorHabitos.Api
```

En Visual Studio: abrir `RastreadorHabitos.slnx`, elegir el perfil **`http`** y ejecutar.

- Al arrancar se aplican automáticamente las migraciones: no hace falta `dotnet ef`.
- La API queda en `http://localhost:5003`.
- Peticiones de ejemplo: `src/Api/RastreadorHabitos.Api/RastreadorHabitos.Api.http`.

## Enviar los correos pendientes (enviador de la cola)

Las operaciones que generan correos (registro y reenvío del enlace) **no los envían**: los
registran en la tabla `Notificaciones.CorreosEnCola` en estado `Pendiente` y responden de
inmediato, aunque el servidor SMTP no esté disponible (RF-NOT-08).

Los envía un **comando independiente** (RF-NOT-09). Se ejecuta desde la raíz del
repositorio, en una ventana con las variables configuradas; puede ejecutarse mientras la API
está corriendo:

```bash
dotnet run --no-build --project src/Api/RastreadorHabitos.Api -- enviar-correos
```

(`--no-build` usa la compilación existente; si la API nunca se compiló, ejecutar antes
`dotnet build`.)

El comando envía todos los correos `Pendiente` por SMTP, marca cada uno como `Enviado` en
cuanto sale y termina mostrando un resumen:

| Salida | Significado | Código de salida |
|---|---|---|
| `Correos enviados: N. Pendientes: 0.` | Se enviaron todos los pendientes | 0 |
| `Correos enviados: 0. Pendientes: 0.` | No había nada que enviar | 0 |
| `SMTP sin configurar (variables Smtp__*): N correo(s) siguen pendientes.` | Faltan las variables SMTP | 1 |
| `No se pudo enviar: <motivo>. Correos enviados: X. Pendientes: Y.` | El servidor SMTP no respondió o rechazó el envío; los no enviados siguen pendientes | 1 |

**Ejecutarlo dos veces no duplica envíos (RF-NOT-12):** el enviador solo toma correos en
estado `Pendiente`, y cada correo se marca `Enviado` inmediatamente después de salir.

Consultar la cola:

```sql
SELECT Destinatario, Estado, FechaCreacionUtc, FechaEnvioUtc
FROM Notificaciones.CorreosEnCola ORDER BY FechaCreacionUtc;
```

## Crear el primer Administrador

Todo usuario nace con el rol Estándar (RF-CA-04). Para convertir en Administrador a un usuario
ya registrado se usa este comando, desde la raíz del repositorio y en una ventana con
`ConnectionStrings__RastreadorHabitos` configurada (no necesita `Jwt__Clave` ni las `Smtp__*`;
puede ejecutarse con la API corriendo):

```bash
dotnet run --no-build --project src/Api/RastreadorHabitos.Api -- promover-administrador <correo>
```

| Salida | Código de salida |
|---|---|
| `El usuario <correo> ahora es Administrador.` | 0 |
| `No existe un usuario registrado con ese correo.` | 1 |

El cambio se aplica de inmediato: una sesión ya abierta de ese usuario obtiene los permisos
de Administrador en su siguiente petición, sin volver a iniciar sesión. A partir de ahí, ese
Administrador puede cambiar el rol de los demás desde la API.

## Endpoints

| Método | Ruta | Cuerpo | Respuesta |
|---|---|---|---|
| `POST` | `/api/cuentas/registro` | `{ "email", "contrasena", "nombreCompleto" }` | `201` registrado · `400` dato inválido · `409` correo ya registrado |
| `GET` | `/api/cuentas/activar?token=…` | — | `200` cuenta activada · `400` enlace inválido, usado o vencido |
| `POST` | `/api/cuentas/reenviar-activacion` | `{ "email" }` | `200` siempre la misma respuesta · `400` correo vacío o mal formado |
| `POST` | `/api/sesion/iniciar` | `{ "email", "contrasena" }` | `200 { "token", "expiraUtc" }` · `401` credenciales incorrectas · `403` cuenta no activa o desactivada · `423` cuenta bloqueada |
| `GET` | `/api/sesion/usuario` | — (requiere sesión) | `200 { "email", "nombreCompleto", "rol" }` · `401` sin sesión válida |
| `POST` | `/api/sesion/cerrar` | — (requiere sesión) | `200` sesión cerrada · `401` sin sesión válida |
| `GET` | `/api/usuarios` | — (Administrador) | `200` lista con `id`, `email`, `nombreCompleto`, `rol` y `estado` |
| `PUT` | `/api/usuarios/{id}/rol` | `{ "rol": "Administrador" \| "Estandar" }` (Administrador) | `200` rol actualizado · `400` rol inválido · `403` es el propio rol · `404` usuario inexistente |
| `POST` | `/api/usuarios/{id}/desactivar` | — (Administrador) | `200` desactivado y sus sesiones cerradas · `403` es uno mismo · `404` usuario inexistente |
| `POST` | `/api/usuarios/{id}/reactivar` | — (Administrador) | `200` reactivado · `404` usuario inexistente |
| `POST` | `/api/usuarios/{id}/restablecer-contrasena` | — (Administrador) | `200` contraseña anterior inutilizada, sesiones cerradas y código enviado por la cola · `404` usuario inexistente |
| `POST` | `/api/contrasena/recuperar` | `{ "email" }` | `200` siempre la misma respuesta · `400` correo vacío o mal formado |
| `POST` | `/api/contrasena/restablecer` | `{ "email", "codigo", "contrasenaNueva" }` | `200` contraseña cambiada y sesiones cerradas · `400` código inválido, usado o vencido, o contraseña que no cumple la política |
| `POST` | `/api/contrasena/cambiar` | `{ "contrasenaActual", "contrasenaNueva" }` (requiere sesión) | `200` contraseña cambiada y todas las sesiones cerradas · `400` contraseña actual incorrecta o nueva que no cumple la política |

El código de recuperación tiene 8 caracteres (por ejemplo `K7Q2-M9XP`), es de un solo uso,
vence a los 30 minutos y llega por la cola de correos (hay que ejecutar el enviador). Se
acepta con o sin guion y en minúsculas. Pedir un código nuevo invalida el anterior.

Quién puede invocar cada operación (Anónimo, Autenticado o Administrador) está declarado en
un único punto del código: `src/Api/RastreadorHabitos.Api/Autorizacion/ExigenciasDeRol.cs`
(RF-CA-05). Si una operación no figura en esa tabla, la API no arranca. Un usuario Estándar que
invoca una operación de Administrador recibe `403` «No tiene permiso para realizar esta
operación.», aunque construya la petición a mano (RF-CA-06, RD-06).

Las respuestas son JSON: `{ "mensaje": "…" }` en los éxitos y `{ "error": "…" }` en los
rechazos. Ningún error expone trazas ni detalles de la base de datos (RD-08).

Los endpoints que requieren sesión se llaman con el header
`Authorization: Bearer <token>`, usando el `token` que devuelve `/api/sesion/iniciar`. La
sesión dura 8 horas, salvo que se cierre antes.

## Cómo provocar cada criterio de aceptación — Registro y activación

En el orden de la sección 4 del enunciado. Las peticiones pueden hacerse con Postman, `curl`
o el archivo `.http`; las consultas SQL se ejecutan contra la base `RastreadorHabitos`.

**1. Registrarse con el propio correo (RF-CA-01, RF-CA-15, RF-NOT-08, RF-NOT-09).**
`POST /api/cuentas/registro` con un correo real y una contraseña válida (por ejemplo
`Habitos2026`) → `201`. Ejecutar el enviador → `Correos enviados: 1`. Llega el correo con el
enlace de activación, de un solo uso y con vencimiento de 24 horas. Mientras no se abra, el
usuario queda con `CorreoConfirmado = 0`:

```sql
SELECT Email, CorreoConfirmado, CuentaHabilitada FROM ControlAcceso.Usuarios;
```

**2. Intentar iniciar sesión antes de activar (RF-CA-15).** `POST /api/sesion/iniciar` con
el correo y la contraseña correctos → `403` «La cuenta no está activa. Revise su correo para
activarla.»

**3. Abrir el enlace recibido (RF-CA-16).** → «Cuenta activada. Ya puede iniciar sesión.» El
usuario pasa a `CorreoConfirmado = 1`. Iniciar sesión ahora → `200` con el `token`.

**4. Abrir el enlace por segunda vez (RF-CA-16).** → `400` «El enlace de activación no es
válido, ya fue usado o venció.» El estado no cambia.

**5. Registrar el mismo correo otra vez (RF-CA-01).** → `409` «Ya existe una cuenta
registrada con este correo.» También con mayúsculas o espacios alrededor.

**6. Contraseña de 5 caracteres y correo mal formado (RF-CA-14, RD-07).**
- Contraseña `abc12` → `400` «La contraseña debe tener al menos 8 caracteres e incluir letras y números.»
- Correo `correo-sin-arroba` → `400` «El correo no tiene un formato válido.»
- Correo vacío → `400` «El correo es obligatorio.»
- JSON mal formado o cuerpo vacío → `400` «La solicitud no tiene un formato válido.»

**7. Leer el almacenamiento (RF-CA-02, RD-05).** Registrar dos usuarios con la misma
contraseña. La contraseña no aparece y los dos valores almacenados (BCrypt, con sal propia)
son distintos. Los tokens de activación tampoco se guardan en claro, solo su hash SHA-256:

```sql
SELECT Email, PasswordHash FROM ControlAcceso.Usuarios;
SELECT UsuarioId, TokenHash, FechaExpiracionUtc, FechaUsoUtc, FechaRevocacionUtc FROM ControlAcceso.TokensUsuario;
```

**8. Apagar el servidor SMTP y ejecutar el enviador dos veces (RF-NOT-08, RF-NOT-12).**
1. Con un servidor SMTP inalcanzable (por ejemplo `Smtp__Host` o `Smtp__Puerto` apuntando a
   algo que no responde) o sin las variables `Smtp__*`, registrar un usuario → `201`
   igualmente, y el correo queda `Pendiente`.
2. Ejecutar el enviador → informa que no pudo enviar; el correo sigue `Pendiente`.
3. Con el servidor SMTP disponible, ejecutar el enviador → `Correos enviados: 1`.
4. Ejecutarlo otra vez → `Correos enviados: 0`. El correo llega una sola vez y su
   `FechaEnvioUtc` no cambia.

**9. Reiniciar la aplicación (RD-09).** Detener y volver a arrancar la API: los usuarios
siguen en `ControlAcceso.Usuarios`.

**10. Credenciales fuera del repositorio (RD-10, RF-NOT-13).** `git ls-files` y
`git log -p` no contienen cadenas de conexión ni credenciales SMTP.

**Además:**

- **Enlace vencido (RF-CA-16).** Registrar otro usuario, vencer su enlace con la consulta de
  abajo y abrirlo → `400`; el usuario sigue sin confirmar.

  ```sql
  UPDATE t SET FechaExpiracionUtc = DATEADD(DAY, -1, SYSUTCDATETIME())
  FROM ControlAcceso.TokensUsuario t JOIN ControlAcceso.Usuarios u ON u.Id = t.UsuarioId
  WHERE u.Email = '<correo-del-usuario>';
  ```

- **Reenvío del enlace (RF-CA-17).** `POST /api/cuentas/reenviar-activacion` con un correo
  registrado pendiente, con uno inexistente y con uno ya activado: las tres respuestas son
  idénticas (`200`, mismo mensaje). Solo el pendiente genera un correo nuevo (ejecutar el
  enviador), y el enlace anterior deja de servir.

## Cómo provocar cada criterio de aceptación — Sesión

En el orden de la sección 4 del enunciado. Se necesita un usuario ya activado (ver
Registro y activación).

**1. Contraseña incorrecta y correo inexistente (RF-CA-03).** `POST /api/sesion/iniciar` con
el correo del usuario y una contraseña incorrecta, y después con un correo que no existe: las
dos respuestas son idénticas, `401` «Correo o contraseña incorrectos.», sin revelar cuál de
los dos datos falló. Con los datos correctos → `200` con `token` y `expiraUtc`.

**2. Fallar cinco veces seguidas y luego usar la contraseña correcta (RF-CA-19).** Cinco
intentos con contraseña incorrecta → `401` cada uno. El sexto, **con la contraseña
correcta** → `423` «La cuenta está bloqueada temporalmente por intentos fallidos. Intente de
nuevo en 15 minutos.» El bloqueo dura 15 minutos:

```sql
SELECT Email, IntentosFallidosConsecutivos, BloqueadoHastaUtc FROM ControlAcceso.Usuarios;
```

Un inicio de sesión correcto pone el contador en cero: con 1 a 4 fallos seguidos de un
inicio de sesión correcto, `IntentosFallidosConsecutivos` vuelve a `0`. Para no esperar los
15 minutos al probar, se puede desbloquear con
`UPDATE ControlAcceso.Usuarios SET BloqueadoHastaUtc = NULL WHERE Email = '<correo>';`.

**3. Consulta del usuario autenticado (RF-CA-07).** `GET /api/sesion/usuario`:
- con `Authorization: Bearer <token>` → `200` con el correo, el nombre y el rol;
- sin el header, o con un token inventado o modificado → `401` «Se requiere una sesión válida.»

**4. Cerrar sesión y volver a usar la credencial cerrada (RF-CA-18).**
`POST /api/sesion/cerrar` con `Authorization: Bearer <token>` → `200` «Sesión cerrada.»
Usar ese mismo token después (en `GET /api/sesion/usuario` o al cerrar otra vez) → `401`.
Las demás sesiones del usuario siguen abiertas.

## Cómo provocar cada criterio de aceptación — Roles y administración

En el orden de la sección 4 del enunciado. Se necesitan dos usuarios activados: uno que se
convierte en Administrador con el comando `promover-administrador` (ver arriba) y otro que se
queda como Estándar. Cada uno inicia sesión y usa su `token`; los `id` salen del listado.

**0. Dos roles, un rol por usuario (RF-CA-04).** Todo usuario nace Estándar y tiene
exactamente un rol (`ControlAcceso.Usuarios.RolId`, obligatorio):

```sql
SELECT u.Email, r.Nombre AS Rol FROM ControlAcceso.Usuarios u JOIN ControlAcceso.Roles r ON r.Id = u.RolId;
```

**1. Con sesión de Estándar, invocar una operación de Administrador construyendo la petición a
mano (RF-CA-06, RD-06).** `GET /api/usuarios` (o cualquier `/api/usuarios/...`) con el token
del Estándar → `403` «No tiene permiso para realizar esta operación.» Sin token → `401`.

**2. Intentar cambiar el propio rol (RF-CA-08).** Con el token del Estándar,
`PUT /api/usuarios/{su-id}/rol` con `{ "rol": "Administrador" }` → `403`. Un Estándar no puede
cambiar ningún rol, ni el propio.

**3. Como Administrador, listar usuarios (RF-CA-21).** `GET /api/usuarios` → `200` con cada
usuario, su rol y su estado (`Activo`, `Pendiente de activación` o `Desactivado`). El listado
nunca incluye hashes ni tokens.

**4. Como Administrador, cambiar un rol (RF-CA-08).** `PUT /api/usuarios/{id}/rol` con
`{ "rol": "Administrador" }` → `200`. Ese usuario tiene los permisos nuevos en su siguiente
petición, sin volver a iniciar sesión; si se le devuelve a `Estandar`, los pierde igual de
rápido. Un Administrador que intenta cambiar su propio rol → `403` «Un Administrador no puede
cambiar su propio rol.»

**5. Como Administrador, desactivar un usuario con sesión abierta y probar esa sesión
(RF-CA-20).** Con el Estándar con sesión iniciada, `POST /api/usuarios/{id}/desactivar` →
`200`. El token que tenía abierto → `401` en su siguiente petición, e iniciar sesión de nuevo
→ `403` «La cuenta está desactivada.» `POST /api/usuarios/{id}/reactivar` → `200`: puede
volver a iniciar sesión, pero sus credenciales anteriores siguen sin servir.

**6. Intentar desactivarse a sí mismo (RF-CA-20).** `POST /api/usuarios/{su-propio-id}/desactivar`
con el token del Administrador → `403` «Un Administrador no puede desactivarse a sí mismo.»

## Cómo provocar cada criterio de aceptación — Contraseñas

En el orden de la sección 4 del enunciado. Se necesita un usuario activado; los códigos llegan
por la cola, así que después de cada solicitud hay que ejecutar el enviador.

**1. Pedir recuperación con un correo inexistente y con uno existente (RF-CA-09).**
`POST /api/contrasena/recuperar` con los dos correos: las dos respuestas son idénticas
(`200`, mismo mensaje). Solo el existente recibe un correo. Un correo mal formado → `400`.

**2. Usar el código, volver a usarlo (RF-CA-10, RF-CA-11).** Antes, iniciar sesión con la
contraseña actual y guardar ese `token` (credencial emitida antes del cambio).
`POST /api/contrasena/restablecer` con el correo, el código recibido y una contraseña nueva
→ `200`. Repetir con el mismo código → `400` «El código no es válido, ya fue usado o venció.»,
y la contraseña no cambia. Un código vencido también se rechaza:

```sql
UPDATE t SET FechaExpiracionUtc = DATEADD(MINUTE, -1, SYSUTCDATETIME())
FROM ControlAcceso.TokensUsuario t JOIN ControlAcceso.Usuarios u ON u.Id = t.UsuarioId
WHERE u.Email = '<correo>' AND t.Tipo = 'RecuperacionContrasena';
```

**3. Iniciar sesión con la contraseña vieja y con la nueva (RF-CA-11).** La vieja → `401`;
la nueva → `200`.

**4. Probar una credencial emitida antes del cambio (RF-CA-12).** El `token` guardado en el
paso 2 → `401` en `GET /api/sesion/usuario`. Todas las sesiones abiertas antes del cambio
quedaron cerradas:

```sql
SELECT FechaInicioUtc, FechaCierreUtc FROM ControlAcceso.SesionesUsuario s
JOIN ControlAcceso.Usuarios u ON u.Id = s.UsuarioId WHERE u.Email = '<correo>';
```

**5. Forzar el restablecimiento de un usuario como Administrador (RF-CA-13).**
`POST /api/usuarios/{id}/restablecer-contrasena` con el token de un Administrador → `200`. La
contraseña anterior del usuario deja de servir (`401`), sus sesiones abiertas se cierran y
recibe por la cola un correo con el código para definir una nueva (usarlo como en el paso 2).
Con el token de un Estándar → `403`.

**6. Cambiar la contraseña con sesión indicando una actual incorrecta (RF-CA-22).**
`POST /api/contrasena/cambiar` con `Authorization: Bearer <token>`:
- contraseña actual incorrecta → `400` «La contraseña actual es incorrecta.»;
- contraseña nueva que no cumple la política → `400` (RF-CA-14);
- datos correctos → `200`: todas las sesiones, incluida la que hizo el cambio, dejan de ser
  válidas (RF-CA-12), y hay que iniciar sesión con la contraseña nueva.

## Máquina de estados de negocio — `Meta`

La entidad central del módulo de negocio es **Meta**: agrupa uno o más hábitos y se cumple al
sumar un objetivo de cumplimientos antes de su fecha límite. Tiene cuatro estados:
**Pendiente**, **En progreso**, **Completada** y **Fallida**; los dos últimos son terminales.

| Qué localizar | Dónde |
|---|---|
| Los estados, declarados en un solo lugar (RF-NEG-03) | `src/Negocio/RastreadorHabitos.Modulo.Metas/Entities/EstadoMeta.cs` |
| Las transiciones permitidas, en un solo lugar (RD-04), con las prohibidas explícitas (RF-NEG-04) y los estados terminales (RF-NEG-05) | `src/Negocio/RastreadorHabitos.Modulo.Metas/Reglas/TransicionesMeta.cs` |
| La entidad con su atributo de estado | `src/Negocio/RastreadorHabitos.Modulo.Metas/Entities/Meta.cs` |
| La tabla de transiciones (desde, hacia, quién la ejecuta, condición) | [`docs/maquina-de-estados.md`](docs/maquina-de-estados.md) |

La entidad existe en el modelo de datos: la API crea al arrancar la tabla `Metas.Metas`, con
la columna `Estado`:

```sql
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'Metas' AND TABLE_NAME = 'Metas';
```

Por ahora es solo la estructura que pide la sección 1.6: todavía no hay endpoints de metas
ni pruebas de la máquina de estados (llegan en la semana 8).

## Gestión de permisos — Asignación 2

Un usuario pide un permiso con una **solicitud**, que un Administrador resuelve. El único
permiso que se puede pedir es `Administrador`: Control de acceso solo tiene roles, así que al
aplicarse el solicitante puede ejecutar las operaciones reservadas a ese rol.

**Arranque de la pieza.** No necesita nada aparte: se ejecuta la API como se indica en
[Ejecutar la API](#ejecutar-la-api), con las mismas variables de entorno. Al arrancar se crea
la tabla `GestionPermisos.SolicitudesPermiso`. Quién puede invocar cada operación está en la
misma tabla única de la Práctica 1, `Autorizacion/ExigenciasDeRol.cs`, y se verifica en el
servidor (RD-06).

### Endpoints

Todos requieren el header `Authorization: Bearer <token>` (el `token` de `/api/sesion/iniciar`).

| Método | Ruta | Cuerpo | Quién | Respuesta |
|---|---|---|---|---|
| `POST` | `/api/solicitudes-permiso` | `{ "permiso": "Administrador" }` | Cualquier usuario con sesión | `201` la solicitud creada, con `estado: "Pendiente"` · `400` permiso vacío o inválido |
| `GET` | `/api/solicitudes-permiso/pendientes` | — | Cualquier usuario con sesión | `200` lista de las solicitudes `Pendiente`: un Administrador recibe las de todos los usuarios; un Estándar, solo las suyas |
| `POST` | `/api/solicitudes-permiso/{id}/aprobar` | — | Administrador | `200` la solicitud con `estado: "Aplicada"`, `resueltaPorId` y `fechaResolucionUtc` · `403` Estándar, o es su propia solicitud · `404` no existe · `409` transición no permitida, o ya la resolvió otra operación al mismo tiempo |
| `POST` | `/api/solicitudes-permiso/{id}/rechazar` | `{ "motivo": "…" }` | Administrador | `200` la solicitud con `estado: "Rechazada"` y `motivoRechazo` · `400` sin motivo · `403` Estándar, o es su propia solicitud · `404` no existe · `409` transición no permitida, o ya la resolvió otra operación al mismo tiempo |

Las transiciones válidas son **Pendiente → Aprobada → Aplicada** y **Pendiente → Rechazada**, y
están declaradas en un solo lugar del código (RD-04):

| Qué localizar | Dónde |
|---|---|
| Los estados | `src/Core/RastreadorHabitos.Core.GestionPermisos/Entities/EstadoSolicitud.cs` |
| La tabla de transiciones permitidas | `src/Core/RastreadorHabitos.Core.GestionPermisos/Reglas/TransicionesSolicitud.cs` |
| Dónde cambia el estado: `Aprobar`, que solo aprueba una solicitud Pendiente (condición explícita), y `CambiarEstado`, que resuelve contra la tabla `MarcarAplicada` y `Rechazar` | `src/Core/RastreadorHabitos.Core.GestionPermisos/Entities/SolicitudPermiso.cs` |
| La consulta de pendientes según quién consulta (RF-GP-02) | `src/Core/RastreadorHabitos.Core.GestionPermisos/Services/SolicitudesPermisoService.cs`, `ListarPendientesAsync` |

Aprobar hace las dos transiciones en la misma operación: Pendiente → Aprobada, le pide a Control
de acceso que aplique el permiso (cambia el rol del solicitante a Administrador) y Aprobada →
Aplicada. Si la aplicación falla, no se guarda nada y la solicitud sigue Pendiente.

Dos resoluciones simultáneas de la misma solicitud (dos aprobaciones, o aprobar y rechazar a
la vez) nunca pasan las dos: el estado es token de concurrencia y aprobar retiene la solicitud
en una transacción mientras aplica el permiso. La que llega segunda recibe `409` «La solicitud
ya fue resuelta por otra operación.» y no cambia nada (RF-GP-06).

Cada solicitud se devuelve como
`{ "id", "solicitanteId", "permiso", "estado", "fechaCreacionUtc", "resueltaPorId", "fechaResolucionUtc", "motivoRechazo" }`.

### Preparación

Se necesitan usuarios activados (ver [Registro y activación](#cómo-provocar-cada-criterio-de-aceptación--registro-y-activación)):

- **A**: Administrador, convertido con el comando `promover-administrador` (ver
  [Crear el primer Administrador](#crear-el-primer-administrador)).
- **E1**: Estándar.
- **E2**: Estándar (para la consulta de RF-GP-02 y el rechazo).

Cada uno inicia sesión con `POST /api/sesion/iniciar` y usa su `token`. El `id` de cada
usuario sale de `GET /api/usuarios` con el token de A.

Para ver lo guardado:

```sql
SELECT Id, SolicitanteId, Permiso, Estado, FechaCreacionUtc, ResueltaPorId, FechaResolucionUtc, MotivoRechazo
FROM GestionPermisos.SolicitudesPermiso ORDER BY FechaCreacionUtc;
```

### Cómo provocar cada criterio de aceptación — Gestión de permisos

**RF-GP-01 — Un Estándar crea una solicitud; nace Pendiente.** Con el token de E1,
`POST /api/solicitudes-permiso` con `{ "permiso": "Administrador" }` → `201` con
`"estado": "Pendiente"` y `"solicitanteId"` igual al id de E1. Guardar su `id` como **S1**.

**RF-GP-02 — Un Administrador consulta las pendientes; un Estándar no ve las de otros.** Con el
token de E2, crear otra solicitud (`POST /api/solicitudes-permiso`, `{ "permiso": "Administrador" }`)
y guardar su `id` como **S2** (se usa en RF-GP-04). Después,
`GET /api/solicitudes-permiso/pendientes`:
- con el token de A → `200`; la lista incluye S1 y S2;
- con el token de E1 → `200`; la lista incluye S1 y **no** S2: un Estándar solo ve las suyas;
- con el token de E2 → `200`; la lista incluye S2 y no S1;
- sin token → `401` «Se requiere una sesión válida.»

Ninguna ruta recibe el id de un solicitante: el servidor decide qué devolver según el usuario de la
sesión y su rol vigente, así que un Estándar no tiene forma de pedir las solicitudes de otros.

**RD-07 — Entrada inválida, rechazo controlado.** `POST /api/solicitudes-permiso`:
- `{ "permiso": "" }`, `{}` o `{ "permiso": "SuperUsuario" }` → `400` «El permiso solicitado
  debe ser Administrador.»
- JSON mal formado o cuerpo vacío → `400` «La solicitud no tiene un formato válido.»

Ninguna respuesta expone trazas ni detalles de la base de datos.

**RF-GP-04 — Rechazar indicando el motivo; sin motivo no se completa.** Con el token de A y la
solicitud S2 de E2 (creada en RF-GP-02):
- `POST /api/solicitudes-permiso/S2/rechazar` con `{}`, `{ "motivo": "" }` o `{ "motivo": "   " }` → `400`
  «El motivo del rechazo es obligatorio.» S2 sigue `Pendiente` (consulta SQL de arriba).
- `POST /api/solicitudes-permiso/S2/rechazar` con
  `{ "motivo": "No necesita administrar usuarios." }` → `200` con `"estado": "Rechazada"`, el
  `motivoRechazo` escrito, y `resueltaPorId` y `fechaResolucionUtc` de A.

**RF-GP-03 — Aprobar; queda registrado quién aprobó y cuándo.** Con el token de A,
`POST /api/solicitudes-permiso/S1/aprobar` → `200` con `"resueltaPorId"` igual al id de A y
`"fechaResolucionUtc"` con la fecha y hora (UTC) de la aprobación. En la base quedan las
columnas `ResueltaPorId` y `FechaResolucionUtc`.

**RF-GP-05 — Al aprobar, el permiso se aplica solo.** Hacer en este orden, alrededor del paso
anterior:
1. **Antes** de aprobar S1, con el token de E1, `GET /api/usuarios` (operación de
   Administrador) → `403`.
2. Aprobar S1 como en RF-GP-03 → `200` con `"estado": "Aplicada"`: ya no queda en Aprobada.
3. **Después**, con el **mismo token** de E1, sin cerrar ni volver a iniciar sesión,
   `GET /api/usuarios` → `200`. E1 ya puede ejecutar lo que pidió; su rol en
   `GET /api/sesion/usuario` es `Administrador`. No hubo ninguna intervención manual.

**RF-GP-06 — Solo las transiciones de la tabla.** Con el token de A:
- **Aprobar dos veces la misma solicitud:** `POST /api/solicitudes-permiso/S1/aprobar` otra
  vez → `409` «La solicitud está en estado Aplicada y no puede pasar a Aprobada.» S1 sigue
  `Aplicada` y su `ResueltaPorId` no cambia (consulta SQL).
- **De Rechazada a Aplicada:** `POST /api/solicitudes-permiso/S2/aprobar` (S2 está Rechazada;
  aprobarla es el único camino hacia Aplicada) → `409` «La solicitud está en estado Rechazada y
  no puede pasar a Aprobada.» S2 sigue `Rechazada` y E2 sigue siendo Estándar.
- Rechazar una solicitud ya resuelta, por ejemplo `POST /api/solicitudes-permiso/S1/rechazar`
  con un motivo → `409`.

**RF-GP-07 — Nadie resuelve su propia solicitud.** Con el token de A, crear una solicitud
propia (`POST /api/solicitudes-permiso`, `{ "permiso": "Administrador" }`) y guardar su `id`
como **S3**. Después, con el mismo token de A:
- `POST /api/solicitudes-permiso/S3/aprobar` → `403` «Nadie puede resolver su propia solicitud.»
- `POST /api/solicitudes-permiso/S3/rechazar` con `{ "motivo": "propia" }` → `403`, el mismo
  mensaje.

S3 sigue `Pendiente`. Lo mismo vale para cualquier Administrador, incluido E1 tras RF-GP-05.

**RD-06 — Autorización en el servidor.** Con el token de E2 (Estándar), construir a mano
`POST /api/solicitudes-permiso/S3/aprobar` → `403` «No tiene permiso para realizar esta
operación.»

**RD-09 — Las solicitudes sobreviven a reiniciar la aplicación.** Detener la API (`Ctrl+C`) y
volver a arrancarla. Iniciar sesión otra vez como A (la clave `Jwt__Clave` puede haber
cambiado) y:
- `GET /api/solicitudes-permiso/pendientes` → S3 sigue ahí, en `Pendiente`;
- la consulta SQL muestra S1 `Aplicada`, S2 `Rechazada` con su motivo y S3 `Pendiente`;
- `POST /api/solicitudes-permiso/S1/aprobar` → `409`: el estado guardado se respeta.

## Pruebas

Un solo comando, desde la raíz de un clon limpio del repositorio, ejecuta toda la suite. No
necesita SQL Server, variables de entorno ni levantar la API:

```bash
dotnet test
```

La suite de Gestión de permisos está en `tests/RastreadorHabitos.Tests/GestionPermisos/`. Ejercita
la pieza directamente, sin levantar la aplicación (RD-12): cada prueba crea su propia base EF
Core en memoria con un nombre único, así que las pruebas son independientes y pasan en
cualquier orden y por separado. Cada prueba marca sus fases con los comentarios `// Preparar`,
`// Actuar` y `// Verificar`, y su nombre dice qué comportamiento verifica.

| Archivo | Qué verifica |
|---|---|
| `CreacionSolicitudTests.cs` | La solicitud nace Pendiente (RF-GP-01), el rechazo de una entrada inválida (RD-07) y el listado de pendientes, sin las ya resueltas (RF-GP-02) |
| `TransicionValidaTests.cs` | **Transiciones válidas.** Aprobar deja la solicitud Aplicada y registra quién aprobó y cuándo (RF-GP-03); rechazar la deja Rechazada y registra el motivo (RF-GP-04); aprobar pide aplicar el permiso al solicitante (RF-GP-05) |
| `TransicionProhibidaTests.cs` | **Transiciones prohibidas.** Aprobar dos veces, aprobar una Rechazada y pasar de Rechazada a Aplicada se rechazan y el estado no cambia (RF-GP-06); rechazar sin motivo no se completa (RF-GP-04); ninguna pareja fuera de la tabla está permitida (RD-04) |
| `ResolucionPropiaTests.cs` | Un Administrador no aprueba ni rechaza su propia solicitud, y esta sigue Pendiente (RF-GP-07) |
| `ResolucionSimultaneaTests.cs` | Aprobar o rechazar una solicitud que otra operación aprobó después de leerla se rechaza y el estado no cambia (RF-GP-06) |

Control de acceso queda fuera de las pruebas de esta pieza: `AplicadorPermisoFalso` toma su
lugar y registra qué permiso se pidió aplicar y a quién.

Para ejecutar solo la suite de Gestión de permisos:

```bash
dotnet test --filter "FullyQualifiedName~GestionPermisos"
```

## Arquitectura

Monolito modular por proyectos sobre el diagrama de componentes de
[`Estructura.md`](Estructura.md): cada pieza del Core y del módulo de negocio es su propio
`.csproj`, de forma que una referencia de proyecto indebida no compila. Control de acceso no
conoce a Notificaciones: declara la interfaz `ISolicitudCorreoSaliente` y la Api la conecta
con la cola. Cada pieza guarda sus tablas en su propio esquema SQL (`ControlAcceso`,
`Notificaciones`, `Metas`). Detalles en [`AGENTS.md`](AGENTS.md).
