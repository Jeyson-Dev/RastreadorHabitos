# Cómo funciona el Rastreador de hábitos con metas — contrato para el frontend

Programación III · ITLA · 2026-C-3. El backend tal como queda tras la Asignación 4. La API corre en
`http://localhost:5003` (`docker compose up --build`); los correos se leen en Mailpit,
`http://localhost:8025`.

## 1. Qué hace y para quién

Sirve para **crear hábitos** (por ejemplo «Leer 20 minutos» lunes, miércoles y viernes),
**registrar cada día si se cumplieron** y agruparlos en **metas** con un objetivo y una fecha límite
(«Leer 20 veces antes del 30 de noviembre»). La meta avanza sola con los cumplimientos y pasa por
una máquina de estados. Alrededor está el Core del curso: cuentas y sesión, permisos que se piden y
se aprueban, documentos, notificaciones con correo, reportes y auditoría.

| Rol | Qué puede hacer |
|---|---|
| **Estándar** (todo usuario nace así) | Sus hábitos, registros diarios, metas, plan de hoy, documentos, notificaciones y preferencias; pedir permisos; reportes con **solo sus datos** |
| **Administrador** | Lo anterior, más: listar usuarios, cambiar roles, desactivar y reactivar, forzar restablecimientos de contraseña, aprobar o rechazar solicitudes, ver documentos de todos, la cola de correos, reportes de **todos** y el **historial de auditoría** |

El primer Administrador se crea con
`docker compose exec app dotnet RastreadorHabitos.Api.dll promover-administrador <correo>`.

## 2. Flujo completo

1. **Registro y activación.** `POST /api/cuentas/registro` → el correo de activación sale por la
   cola con un enlace a la página `/activar?token=…` del frontend, que llama a
   `GET /api/cuentas/activar?token=…`. Sin activar, iniciar sesión da `403`.
2. **Sesión.** `POST /api/sesion/iniciar` devuelve `{ token, expiraUtc }` (JWT, 8 horas). Se manda en
   todas las peticiones: `Authorization: Bearer <token>`. `GET /api/sesion/usuario` da el `rol`
   (para mostrar u ocultar opciones; el servidor igual lo verifica). `POST /api/sesion/cerrar`
   invalida el token. Cinco fallos seguidos bloquean 15 minutos (`423`).
3. **Contraseña.** Recuperar: `POST /api/contrasena/recuperar` → código por correo →
   `POST /api/contrasena/restablecer`. Con sesión: `POST /api/contrasena/cambiar`. Todo cambio cierra
   todas las sesiones: el frontend vuelve al inicio de sesión.
4. **Hábitos.** `POST /api/habitos` con sus días (`1` lunes … `7` domingo). Hasta **10**, o **20** con
   el permiso `LIMITE_HABITOS_AMPLIADO`; al llegar al límite responde `403` y el frontend ofrece pedir
   el permiso.
5. **Registro diario.** `POST /api/habitos/{id}/registros` con `{ cumplido }`: solo **hoy** (la fecha
   la pone el servidor) y solo si hoy es un día programado (si no, `409`). Se corrige el mismo día.
6. **Plan de hoy.** `GET /api/habitos/hoy?ciudad=…` cruza los hábitos de hoy con el pronóstico de
   WeatherAPI; sin clave o sin servicio, `503` controlado.
7. **Metas.** `POST /api/metas` nace **Pendiente**. Avance = días cumplidos de sus hábitos entre la
   creación y la fecha límite. Al consultar, el sistema la mueve: primer cumplimiento → **En
   progreso**; vence sin el objetivo → **Fallida**. El dueño la cierra con `…/completar` (avance ≥
   objetivo → **Completada**, terminal) o reactiva una Fallida con `…/reactivar`. Pendiente →
   Completada está **prohibida** (`409`).
8. **Permisos.** `POST /api/solicitudes-permiso` (`Administrador` o `LIMITE_HABITOS_AMPLIADO`): los
   Administradores activos reciben una notificación; uno la aprueba (se aplica sola) o la rechaza con
   un motivo, y el solicitante recibe la notificación. Nadie resuelve la suya.
9. **Notificaciones.** Bandeja propia con leídas y no leídas, marcar como leída y apagar el correo.
   Avisan: solicitud creada, aprobada o rechazada, y cada cambio de estado de una meta.
10. **Documentos.** Subir (`multipart/form-data`, campo `archivo`), listar, descargar y eliminar
    (lógico). Cada uno ve los suyos; el Administrador, todos.
11. **Reportes.** Solicitudes por estado y mes, y cumplimiento semanal (% de días programados
    cumplidos), con `?desde=&hasta=`.
12. **Auditoría.** Resoluciones de solicitudes, subidas y eliminaciones de documentos, cambios de rol,
    desactivaciones, reactivaciones y restablecimientos de contraseña dejan un registro que solo el
    Administrador consulta.

## 3. Convenciones

- JSON en `camelCase`. Éxito con mensaje: `{ "mensaje" }`; rechazo: `{ "error" }`, comprensible para
  mostrar tal cual (nunca trazas).
- `200`/`201` bien · `400` dato inválido · `401` sin sesión o token inválido (ir al inicio de sesión)
  · `403` sin permiso o regla que lo impide · `404` no existe o es de otro usuario · `409` conflicto de
  estado · `423` bloqueada · `503` servicio externo caído.
- Instantes en UTC con `Z`; días `AAAA-MM-DD` en hora local del servidor; ids GUID.

## 4. Inventario de endpoints

Rol: **—** sin sesión · **S** cualquier usuario con sesión · **A** solo Administrador.

| Método y ruta | Rol | Cuerpo de ejemplo | Respuestas |
|---|---|---|---|
| `GET /api/estado` | — | — | `200` estado de la API (la raíz `/` queda para el frontend) |
| `POST /api/cuentas/registro` | — | `{ "email": "ana@correo.com", "contrasena": "Habitos2026", "nombreCompleto": "Ana Pérez" }` | `201` · `400` · `409` correo ya registrado |
| `GET /api/cuentas/activar?token=…` | — | — | `200` · `400` enlace inválido o vencido |
| `POST /api/cuentas/reenviar-activacion` | — | `{ "email" }` | `200` siempre igual · `400` |
| `POST /api/sesion/iniciar` | — | `{ "email", "contrasena" }` | `200 { token, expiraUtc }` · `401` · `403` · `423` |
| `GET /api/sesion/usuario` | S | — | `200 { email, nombreCompleto, rol }` |
| `POST /api/sesion/cerrar` | S | — | `200` |
| `POST /api/contrasena/recuperar` | — | `{ "email" }` | `200` siempre igual · `400` |
| `POST /api/contrasena/restablecer` | — | `{ "email", "codigo": "K7Q2-M9XP", "contrasenaNueva": "Nueva2026x" }` | `200` · `400` |
| `POST /api/contrasena/cambiar` | S | `{ "contrasenaActual", "contrasenaNueva" }` | `200` (cierra sesiones) · `400` |
| `GET /api/usuarios` | A | — | `200 [ { id, email, nombreCompleto, rol, estado } ]` |
| `PUT /api/usuarios/{id}/rol` | A | `{ "rol": "Administrador" \| "Estandar" }` | `200` · `400` · `403` propio · `404` |
| `POST /api/usuarios/{id}/desactivar` · `…/reactivar` | A | — | `200` · `403` uno mismo · `404` |
| `POST /api/usuarios/{id}/restablecer-contrasena` | A | — | `200` · `404` |
| `POST /api/solicitudes-permiso` | S | `{ "permiso": "LIMITE_HABITOS_AMPLIADO" }` | `201` con `estado: "Pendiente"` · `400` |
| `GET /api/solicitudes-permiso/pendientes` | S | — | `200` (Administrador: todas; Estándar: las suyas) |
| `POST /api/solicitudes-permiso/{id}/aprobar` | A | — | `200` `Aplicada` · `403` · `404` · `409` |
| `POST /api/solicitudes-permiso/{id}/rechazar` | A | `{ "motivo": "No corresponde." }` | `200` `Rechazada` · `400` · `403` · `404` · `409` |
| `POST /api/habitos` | S | `{ "nombre": "Leer", "descripcion": "Antes de dormir", "dias": [1, 3, 5] }` | `201 { id, nombre, descripcion, dias, fechaCreacionUtc }` · `400` · `403` límite |
| `GET /api/habitos` | S | — | `200` sus hábitos |
| `POST /api/habitos/{id}/registros` | S | `{ "cumplido": true }` | `200 { habitoId, fecha, cumplido }` · `400` · `404` · `409` |
| `GET /api/habitos/hoy?ciudad=Santo Domingo` | S | — | `200 { fecha, ciudad, pronostico, favorableAlAireLibre, recomendacion, pendientes, habitos }` · `400` · `503` |
| `POST /api/metas` | S | `{ "nombre": "Leer 20 veces", "objetivoCumplimientos": 20, "fechaLimite": "2026-12-15", "habitoIds": ["<id>"] }` | `201 { id, nombre, objetivoCumplimientos, avance, fechaInicio, fechaLimite, estado, habitoIds, fechaCreacionUtc }` · `400` |
| `GET /api/metas` · `GET /api/metas/{id}` | S | — | `200` con avance y estado al día · `404` |
| `POST /api/metas/{id}/completar` | S | — | `200` · `404` · `409` |
| `POST /api/metas/{id}/reactivar` | S | `{ "nuevaFechaLimite": "2026-12-31" }` | `200` · `400` · `404` · `409` |
| `POST /api/documentos` | S | `multipart/form-data`, campo `archivo` | `201 { id, nombre, tipo, tamanoBytes, fechaSubidaUtc, subidoPorId }` · `400` |
| `GET /api/documentos` | S | — | `200` |
| `GET /api/documentos/{id}/descarga` | S | — | `200` el archivo · `403` · `404` |
| `DELETE /api/documentos/{id}` | S | — | `200` · `403` · `404` |
| `GET /api/notificaciones?leida=false` | S | — | `200 { noLeidas, notificaciones: [ { id, tipo, mensaje, leida, fechaUtc, fechaLecturaUtc } ] }` |
| `POST /api/notificaciones/{id}/leida` | S | — | `200` · `403` ajena · `404` |
| `GET` · `PUT /api/notificaciones/preferencias` | S | `{ "correoActivo": false }` | `200 { correoActivo }` · `400` |
| `GET /api/correos?estado=Fallido` | A | — | `200 [ { id, destinatario, asunto, estado, intentos, fechaCreacionUtc, fechaUltimoIntentoUtc, fechaEnvioUtc, ultimoError } ]` · `400` |
| `GET /api/reportes/solicitudes-por-estado?desde=…&hasta=…` | S | — | `200 { reporte, pregunta, desde, hasta, alcance, filas: [ { grupo: "2026-11", valores: { Pendiente, Aprobada, Aplicada, Rechazada, Total } } ] }` · `400` |
| `GET /api/reportes/cumplimiento-semanal?desde=…&hasta=…` | S | — | `200`, `grupo` = lunes de la semana; `valores`: `diasProgramados`, `diasCumplidos`, `porcentajeCumplimiento` · `400` |
| `GET /api/auditoria?usuarioId=&entidad=&desde=&hasta=` | A | — | `200 { total, registros: [ { id, usuarioId, accion, entidad, entidadId, fechaUtc, valorAnterior, valorNuevo } ] }` · `400` |

Estados de la meta: `Pendiente`, `EnProgreso`, `Completada` (terminal), `Fallida`. Tipos de
notificación: `SolicitudPermisoCreada`, `SolicitudPermisoAprobada`, `SolicitudPermisoRechazada`,
`MetaCambioEstado`.

## 5. Pantallas sugeridas

Inicio de sesión, registro y activación · recuperar contraseña · tablero de hoy (plan y registro) ·
mis hábitos · mis metas con barra de avance · notificaciones con contador · documentos · reportes con
selector de fechas · y solo para el Administrador: usuarios, solicitudes pendientes, cola de correos
y auditoría.
