# RastreadorHabitos

Rastreador de hábitos con metas — Programación III · TDS-007 · ITLA · 2026-C-3

## Estado de la Práctica 1 — Control de acceso

| Funcionalidad | Requisitos | Estado |
|---|---|---|
| Registro y activación | RF-CA-01, 02, 14, 15, 16, 17 · RF-NOT-08, 09, 12, 13 · RD-05, 07, 08, 09, 10 | ✅ Implementado |
| Sesión | RF-CA-03, 07, 18, 19 | ✅ Implementado |
| Roles y administración | RF-CA-04, 05, 06, 08, 20, 21 · RD-06 | ✅ Implementado |
| Contraseñas | RF-CA-09 a 13, 22 | Pendiente |

Enunciado completo: [`docs/practica1-control-acceso.md`](docs/practica1-control-acceso.md).

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

## Pruebas unitarias

```bash
dotnet test
```

## Arquitectura

Monolito modular por proyectos sobre el diagrama de componentes de
[`Estructura.md`](Estructura.md): cada pieza del Core y del módulo de negocio es su propio
`.csproj`, de forma que una referencia de proyecto indebida no compila. Control de acceso no
conoce a Notificaciones: declara la interfaz `ISolicitudCorreoSaliente` y la Api la conecta
con la cola. Cada pieza guarda sus tablas en su propio esquema SQL (`ControlAcceso`,
`Notificaciones`). Detalles en [`AGENTS.md`](AGENTS.md).
