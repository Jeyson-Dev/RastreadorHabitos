# Criterios de aceptación — Notificaciones, cola de correos y reportes

Práctica 5, sección 1.1. Estos criterios se escribieron **antes** de delegar la construcción del
motor de notificaciones, la cola completa y los reportes. El trabajo se acepta solo si cumple
todos. Cada criterio es una condición comprobable («dado / cuando / entonces»), cita el
requisito que cubre y dice cómo se comprueba sin preguntarle a nadie.

## Convenciones para comprobar

- La API corre como dice la sección «Contenedor» del README (`http://localhost:5003`). Los
  correos se leen en Mailpit (`http://localhost:8025`) y las consultas SQL se ejecutan contra la
  base `RastreadorHabitos`.
- **A1** y **A2**: Administradores activos (registrados, activados y promovidos con
  `promover-administrador`). **E1** y **E2**: Estándar activos. Cada uno inicia sesión con
  `POST /api/sesion/iniciar` y usa su `token` en `Authorization: Bearer <token>`.
- «Activo» = cuenta activada por correo y no desactivada por un Administrador (el estado
  `Activo` de `GET /api/usuarios`).
- Las fechas `AAAA-MM-DD` de los filtros son días locales del servidor (`TZ`). «Hoy» es la fecha
  local del servidor.
- Todas las operaciones de este documento exigen sesión válida. Sin `Authorization`, o con un
  token cerrado o inventado, responden `401` y no devuelven datos (RD-06).

## 1. Notificaciones (RF-NOT-01 a 07)

Una notificación tiene destinatario, tipo, mensaje, si está leída y su fecha (entidad
`Notificacion`). Operaciones: `GET /api/notificaciones` (bandeja propia, filtro opcional
`?leida=true|false`), `POST /api/notificaciones/{id}/leida`,
`GET /api/notificaciones/preferencias` y `PUT /api/notificaciones/preferencias` con
`{ "correoActivo": true | false }`.

**CN-01 · RF-NOT-01 · Crear una solicitud notifica a los Administradores activos.**
Dado A1 y A2 activos y E1 y E2 Estándar, cuando E1 crea una solicitud
(`POST /api/solicitudes-permiso`), entonces A1 y A2 tienen cada uno exactamente una notificación
nueva, sin leer, de tipo `SolicitudPermisoCreada`, que nombra el permiso pedido. E1 y E2 no
tienen ninguna de ese tipo.
*Comprobación:* `GET /api/notificaciones` con el token de cada uno.

**CN-02 · RF-NOT-01 · Ningún Administrador inactivo la recibe.**
Dado A1 activo, A2 desactivado (`POST /api/usuarios/{id}/desactivar`) y un tercer
Administrador A3 todavía sin activar su cuenta, cuando E1 crea una solicitud, entonces solo A1
la recibe. En la tabla de notificaciones no hay ninguna para A2 ni para A3.
*Comprobación:* `SELECT DestinatarioId, Tipo FROM Notificaciones.Notificaciones;`.

**CN-03 · RF-NOT-01 · Sin ningún Administrador activo, la solicitud se crea igual.**
Dado que ningún usuario Administrador está activo, cuando E1 crea una solicitud, entonces
recibe `201`, la solicitud queda `Pendiente` y no se crea ninguna notificación ni se encola
ningún correo por ese evento.
*Comprobación:* la respuesta, y las tablas `Notificaciones.Notificaciones` y
`Notificaciones.CorreosEnCola` sin filas nuevas.

**CN-04 · RF-NOT-01, RF-GP-07 · Quien pide no se notifica a sí mismo.**
Dado A1 y A2 activos, cuando A1 crea una solicitud propia, entonces A2 recibe la notificación y
A1 no. A1 no puede resolverla (RF-GP-07), así que el aviso es solo para quien sí puede.
*Comprobación:* `GET /api/notificaciones` con el token de A1 y de A2.

**CN-05 · RF-NOT-02 · Aprobar notifica al solicitante.**
Dado una solicitud Pendiente de E1, cuando A1 la aprueba, entonces E1 tiene una notificación
nueva sin leer, de tipo `SolicitudPermisoAprobada`, cuyo mensaje dice el permiso y que fue
aprobada. Ningún otro usuario recibe esa notificación.
*Comprobación:* `GET /api/notificaciones` con el token de E1, A1 y E2.

**CN-06 · RF-NOT-02 · Rechazar notifica al solicitante con el motivo.**
Dado una solicitud Pendiente de E2, cuando A1 la rechaza con el motivo «No necesita
administrar usuarios.», entonces E2 tiene una notificación nueva sin leer, de tipo
`SolicitudPermisoRechazada`, cuyo mensaje dice que fue rechazada e incluye ese motivo, literal.
*Comprobación:* `GET /api/notificaciones` con el token de E2.

**CN-07 · RF-NOT-02 · Una resolución que no ocurre no notifica.**
Dado una solicitud ya Aplicada, cuando A1 intenta aprobarla otra vez (`409`), o intenta
rechazarla sin motivo (`400`), entonces el solicitante no recibe ninguna notificación nueva.
*Comprobación:* contar las notificaciones del solicitante antes y después.

**CN-08 · RF-NOT-03, RF-NEG-07 · Un cambio de estado de una meta notifica a su dueño.**
Dado una meta de E1 en `Pendiente` con un hábito programado hoy, cuando E1 registra hoy el
cumplimiento de ese hábito y consulta la meta (`GET /api/metas/{id}`), entonces la meta pasa a
`EnProgreso` y E1 tiene una notificación nueva sin leer, de tipo `MetaCambioEstado`, que nombra
la meta, su estado anterior y el nuevo. Ningún otro usuario la recibe. Consultar la meta otra
vez sin que cambie de estado no crea otra notificación.
*Comprobación:* `GET /api/notificaciones` con el token de E1 y de E2, antes y después.

**CN-09 · RF-NOT-03 · El vencimiento de una meta también notifica.**
Dado una meta de E1 en `EnProgreso` o `Pendiente`, cuando su fecha límite queda en el pasado
(consulta SQL del README) y E1 la consulta, entonces pasa a `Fallida` y E1 recibe una
notificación `MetaCambioEstado` con el estado nuevo `Fallida`. Lo mismo vale al completar
(`Completada`) y al reactivar (`EnProgreso`) una meta. Una transición rechazada (`409`) no
notifica.
*Comprobación:* `GET /api/notificaciones` con el token de E1.

**CN-10 · RF-NOT-04 · Cada uno ve solo las suyas y distingue leídas de no leídas.**
Dado que E1 y E2 tienen notificaciones, cuando E1 consulta `GET /api/notificaciones`, entonces
recibe solo las suyas (ninguna con otro destinatario), cada una con `leida: true` o `false`, y
el total `noLeidas` coincide con las que tienen `leida: false`. Con `?leida=false` recibe solo
las no leídas y con `?leida=true` solo las leídas. La ruta no recibe ningún Id de usuario: el
servidor usa el de la sesión.
*Comprobación:* la respuesta, comparada con
`SELECT DestinatarioId, Leida FROM Notificaciones.Notificaciones;`.

**CN-11 · RF-NOT-05 · Marcar como leída una notificación propia.**
Dado una notificación sin leer de E1, cuando E1 llama `POST /api/notificaciones/{id}/leida`,
entonces responde `200`, la notificación queda `leida: true` con su fecha de lectura y
`noLeidas` baja en uno. Marcarla otra vez responde `200` y no cambia nada.
*Comprobación:* `GET /api/notificaciones` antes y después.

**CN-12 · RF-NOT-05 · Marcar la notificación de otro se rechaza.**
Dado una notificación sin leer de E1, cuando E2 llama `POST /api/notificaciones/{id-de-E1}/leida`,
entonces responde `403` y la notificación de E1 sigue `leida: false`. Un Id que no existe, o
que no es un Id válido, responde `404` y no cambia nada.
*Comprobación:* la respuesta y `GET /api/notificaciones` con el token de E1.

**CN-13 · RF-NOT-06 · Toda notificación llega también por correo.**
Dado E1 con el canal de correo activo (el valor por defecto), cuando se genera cualquier
notificación para E1 (CN-05, CN-06, CN-08), entonces además de la notificación interna se
encola en `Notificaciones.CorreosEnCola` un correo `Pendiente` a la dirección de E1, con el
mismo mensaje, y tras el procesador de la cola el correo aparece en Mailpit. La operación que
generó la notificación no envía el correo (RF-NOT-08).
*Comprobación:* `SELECT Destinatario, Asunto, Estado FROM Notificaciones.CorreosEnCola;` y
Mailpit.

**CN-14 · RF-NOT-07 · Con el correo desactivado queda la interna y no se encola correo.**
Dado E1 con `PUT /api/notificaciones/preferencias` `{ "correoActivo": false }`, cuando se genera
una notificación para E1, entonces aparece en su bandeja interna y la cola de correos no tiene
ningún correo nuevo para E1. Con `{ "correoActivo": true }` las siguientes vuelven a encolarse.
La preferencia es por usuario: la de E1 no cambia lo que recibe E2.
*Comprobación:* `GET /api/notificaciones` y la consulta SQL de la cola, contando las filas de
E1 antes y después. `GET /api/notificaciones/preferencias` devuelve el valor guardado.

**CN-15 · RF-NOT-07, RD-07 · Una preferencia inválida se rechaza.**
Dado E1, cuando envía `PUT /api/notificaciones/preferencias` con `{}`, `{ "correoActivo": "no" }`
o un JSON mal formado, entonces responde `400` con un mensaje comprensible y la preferencia no
cambia.

**CN-16 · RD-06 · Sin sesión no hay notificaciones.**
Dado cualquier usuario, cuando se llama una de las cuatro operaciones de notificaciones sin
`Authorization`, entonces responde `401` y no devuelve ni cambia nada.

## 2. Cola de correos (RF-NOT-08 a 13)

Cada correo de la cola tiene destinatario, asunto, cuerpo, estado (`Pendiente`, `Enviado` o
`Fallido`), intentos, fecha de creación, fecha del último intento, fecha de envío y último error
(entidad `CorreoEnCola`). **Máximo de intentos: 3.** El procesador es el comando
`enviar-correos` (servicio `enviador` de la composición, cada 10 segundos). Consulta:
`GET /api/correos` (solo Administrador), con el filtro opcional `?estado=`.

**CC-01 · RF-NOT-08 · La operación termina aunque el servidor de correo no responda.**
Dado Mailpit detenido (`docker compose stop mailpit`), cuando un usuario se registra, o E1 crea
una solicitud, entonces la operación responde lo de siempre (`201`) en menos de un segundo, y
cada correo queda en la cola `Pendiente` con 0 intentos.
*Comprobación:* la respuesta y la consulta SQL de la cola.

**CC-02 · RF-NOT-09 · Un proceso independiente envía los pendientes.**
Dado correos `Pendiente`, cuando se ejecuta el procesador
(`docker compose exec app dotnet RastreadorHabitos.Api.dll enviar-correos`) sin repetir la
operación que los creó, entonces cada uno queda `Enviado`, con 1 intento y su fecha de envío, y
aparece en Mailpit.

**CC-03 · RF-NOT-10 · Un correo que falla se reintenta 3 veces y queda Fallido.**
Dado un correo `Pendiente` y el servidor de correo detenido, cuando el procesador se ejecuta tres
veces, entonces después de la primera y la segunda el correo sigue `Pendiente` con 1 y 2 intentos
y su último error. Después de la tercera queda `Fallido`, con 3 intentos, la fecha de su último
intento y su último error (un texto comprensible, sin traza de pila). Una cuarta ejecución no lo
vuelve a intentar: sigue con 3 intentos. El correo nunca se borra.
*Comprobación:* `GET /api/correos` como A1 después de cada ejecución.

**CC-04 · RF-NOT-10 · Un correo con destinatario inválido no bloquea a los demás.**
Dado en la cola un correo con un destinatario mal formado (por ejemplo `sin-arroba`, insertado
con SQL) y otro correo válido, con el servidor de correo disponible, cuando se ejecuta el
procesador, entonces el válido queda `Enviado` y el inválido suma un intento con su último
error. Al tercer intento queda `Fallido`.
*Comprobación:* `GET /api/correos` como A1.

**CC-05 · RF-NOT-10 · Sin SMTP configurado no se consumen intentos.**
Dado que faltan las variables `Smtp__*`, cuando se ejecuta el procesador, entonces informa que el
SMTP no está configurado, termina con código 1 y ningún correo cambia de estado ni suma intentos.

**CC-06 · RF-NOT-11 · El Administrador consulta la cola; el Estándar no.**
Dado correos en los tres estados, cuando A1 llama `GET /api/correos`, entonces recibe `200` con
cada correo y su `id`, destinatario, asunto, estado, intentos, fecha de creación, fecha del último
intento, fecha de envío y último error, del más reciente al más antiguo. Con `?estado=Fallido`
recibe solo los fallidos. Con un estado que no existe recibe `400`. La consulta no devuelve el
cuerpo de los correos, que lleva enlaces de activación y códigos de recuperación. Cuando E1
llama la misma operación, incluso construyendo la petición a mano, recibe `403` y ningún dato.
*Comprobación:* las dos respuestas.

**CC-07 · RF-NOT-12 · Ejecutar el procesador dos veces no duplica envíos.**
Dado correos `Pendiente` y el servidor disponible, cuando se ejecuta el procesador dos veces
seguidas, entonces la primera los envía y la segunda informa `Correos enviados: 0`. Cada correo
llega una sola vez a Mailpit y su fecha de envío no cambia. Un correo `Fallido` tampoco se
vuelve a enviar.
*Comprobación:* la salida de las dos ejecuciones y Mailpit.

**CC-08 · RF-NOT-12 · Dos procesadores al mismo tiempo no duplican envíos.**
Dado correos `Pendiente`, cuando se ejecuta el procesador a mano mientras el servicio `enviador`
también está procesando, entonces cada correo se envía una sola vez: solo un procesador trabaja
a la vez y el otro termina sin enviar nada.
*Comprobación:* Mailpit: un solo mensaje por correo de la cola.

**CC-09 · RF-NOT-13, RD-10 · Las credenciales SMTP solo salen de variables de entorno.**
Dado el repositorio, cuando se busca en `git ls-files` y en `git log -p --all`, entonces no hay
ningún usuario, contraseña ni servidor SMTP real: solo los nombres `Smtp__*` y los valores de
ejemplo de `.env.example`. La consulta de la cola tampoco los muestra.

## 3. Reportes con agregación (RF-REP-01 a 05)

Dos reportes, los dos con los filtros opcionales `?desde=AAAA-MM-DD&hasta=AAAA-MM-DD`. Los dos
días del rango están **incluidos**. Cada reporte responde su pregunta, el rango aplicado, el
alcance (todos los usuarios o solo los datos de quien consulta) y filas agrupadas
`{ "grupo", "valores" }`.

- **Del Core**, `GET /api/reportes/solicitudes-por-estado`: «¿Cuántas solicitudes de permiso
  hubo en cada estado, mes por mes?». Una fila por mes de creación (`AAAA-MM`) con la cantidad de
  cada estado y el total.
- **Del negocio**, `GET /api/reportes/cumplimiento-semanal`: «¿Qué porcentaje de los días
  programados de sus hábitos cumplió cada semana?». Una fila por semana (la fecha de su lunes)
  con los días programados, los días cumplidos y el porcentaje de cumplimiento.

**CR-01 · RF-REP-01, RF-REP-03 · El reporte del Core devuelve valores agrupados.**
Dado tres solicitudes creadas este mes (una Pendiente, una Aplicada y una Rechazada), cuando A1
consulta el reporte del Core sin filtros, entonces recibe una sola fila para el mes actual con
`Pendiente: 1`, `Aplicada: 1`, `Rechazada: 1`, `Aprobada: 0` y `Total: 3`: menos filas que las
solicitudes que resume, y ningún Id ni dato de una solicitud individual.

**CR-02 · RF-REP-02, RF-REP-03 · El reporte del negocio responde una pregunta del dominio.**
Dado E1 con dos hábitos programados todos los días, creados hoy, y el cumplimiento de hoy
registrado en uno (`cumplido: true`) y en el otro no, cuando E1 consulta el reporte del negocio
con `desde` y `hasta` iguales a hoy, entonces recibe una fila para la semana de hoy con
`diasProgramados: 2`, `diasCumplidos: 1` y `porcentajeCumplimiento: 50`: una fila que resume dos
registros diarios. No es un conteo de filas: cruza los días programados con los cumplidos.

**CR-03 · RF-REP-03 · La agregación la hace el motor de datos.**
Dado cualquiera de los dos reportes, cuando se revisa su código, entonces el conteo por grupo
lo resuelve la consulta en SQL Server (`GROUP BY`), no un listado completo de registros sumado
en memoria ni en la interfaz. Los días programados del reporte del negocio salen del calendario
de cada hábito, no de sus registros.
*Comprobación:* el código de la consulta y una prueba automática del reporte.

**CR-04 · RF-REP-04 · Un Estándar solo obtiene sus propios datos.**
Dado solicitudes y cumplimientos de E1 y de E2, cuando E1 consulta cada reporte, entonces los
valores cuentan solo sus solicitudes y sus hábitos (el alcance dice «solo sus datos»), y ningún
parámetro de la petición (por ejemplo `?usuarioId=` con el Id de E2) cambia eso. Cuando A1
consulta los mismos reportes, los valores incluyen los de todos los usuarios.
*Comprobación:* comparar las respuestas de E1, E2 y A1 sobre los mismos datos.

**CR-05 · RF-REP-05 · Dos rangos distintos dan resultados distintos.**
Dado los datos de CR-01 y CR-02, creados hoy, cuando se consulta cada reporte con
`desde=hasta=<hoy>` y con `desde=hasta=<ayer>`, entonces el primero incluye los datos de hoy y el
segundo no (reporte del Core: sin filas; reporte del negocio: sin cumplimientos de hoy).

**CR-06 · RF-REP-05 · El último día del rango está incluido.**
Dado una solicitud creada hoy a cualquier hora, incluso a las 23:59 locales, cuando se consulta
el reporte del Core con `hasta=<hoy>`, entonces la solicitud se cuenta. Con `hasta=<ayer>` no se
cuenta. El primer día (`desde`) también está incluido. Lo mismo vale para el cumplimiento de hoy
en el reporte del negocio.

**CR-07 · RF-REP-05 · Un rango sin datos no es un error.**
Dado un rango donde no hay datos (por ejemplo `desde=2020-01-01&hasta=2020-01-31`), cuando se
consulta cada reporte, entonces responde `200` con la lista de filas vacía y el rango aplicado.

**CR-08 · RF-REP-05, RD-07 · Un rango inválido se rechaza de forma controlada.**
Dado cualquier usuario con sesión, cuando consulta un reporte con `desde` posterior a `hasta`,
o con una fecha que no tiene el formato `AAAA-MM-DD` o no existe (`2026-02-30`, `30/11/2026`,
`hoy`), entonces responde `400` con un mensaje que dice qué corregir, sin traza ni consulta
(RD-08).

**CR-09 · RD-06 · Sin sesión no hay reportes.**
Dado cualquier reporte, cuando se consulta sin `Authorization`, entonces responde `401`.

## 4. Transversales

**CT-01 · RD-11 · Fechas con el mismo criterio.** Toda fecha y hora de las notificaciones y de
la cola se guarda en UTC y sale en JSON marcada como UTC (termina en `Z`), igual que el resto del
sistema. Una notificación y su correo, creados en la misma operación, tienen la misma hora.

**CT-02 · RD-08 · Ningún error expone detalles internos.** Ningún mensaje de las operaciones de
este documento, ni el último error de un correo, muestra una traza de pila, una ruta de archivo
ni una consulta SQL.

**CT-03 · RD-03 · El Core no depende del negocio.** Notificaciones y Reportes no referencian a
los proyectos de Hábitos ni de Metas. Las métricas del negocio le llegan a Reportes ya
calculadas, a través de un puerto que conecta la Api.

**CT-04 · RD-09 · Persisten fuera del proceso.** Las notificaciones, las preferencias y la cola
sobreviven a `docker compose down` (sin `-v`) y a volver a levantar.

**CT-05 · Lo anterior sigue funcionando.** La suite completa (`dotnet test`) pasa, y los seis
frentes de la revisión acumulada de la Práctica 5 (sección 1.5) siguen funcionando, incluido el
correo de activación saliendo por la cola.

## 5. Criterios agregados después de la auditoría (Asignación 3)

Los criterios de arriba son los que se escribieron antes de delegar (Práctica 5). La auditoría de
la Asignación 3 encontró cuatro defectos que esos criterios no detectaban porque no cubrían el
caso: una entrada vacía (no solo mal formada), dos peticiones simultáneas, una zona horaria con
horario de verano y los comandos de consola. Estos criterios cierran esos huecos; cada uno cita el
hallazgo que lo originó (ver [`docs/bitacora-asignacion-3.md`](bitacora-asignacion-3.md)).

**Regla para todo criterio nuevo** (aplicada a los de esta sección):
- Un criterio de **entrada inválida** enumera, como mínimo, el valor ausente, el vacío, el de
  solo espacios y el mal formado.
- Un criterio de **«una sola vez»** (una notificación, un envío) incluye el caso de dos
  peticiones o dos procesos **al mismo tiempo**.
- Un criterio de **fechas o rangos** se comprueba también con una zona horaria con horario de
  verano, no solo con la del servidor.
- Un criterio de **RD-08** cubre también lo que ven los comandos de consola, no solo la API.

**CC-10 · RF-NOT-10 · Un error inesperado de un correo no detiene la cola (Hallazgo 1).**
Dado en la cola un correo `Pendiente` con el destinatario vacío (`''`) o solo con espacios y,
creado después, un correo válido, cuando se ejecuta `enviar-correos`, entonces el comando termina
normalmente (código 0 o 1, nunca una excepción sin manejar), el correo vacío suma un intento con
el último error «La dirección del destinatario no es válida.» y el válido sale. Al tercer intento
el vacío queda `Fallido`.
*Comprobación:* insertar los dos correos con SQL, ejecutar el comando y `GET /api/correos` como
Administrador.

**CN-17 · RF-NOT-03 · Consultas simultáneas no duplican el aviso de una meta (Hallazgo 2).**
Dado una meta `Pendiente` con el cumplimiento de hoy registrado, cuando llegan 8 consultas
`GET /api/metas/{id}` al mismo tiempo, entonces la meta queda `EnProgreso` y su dueño tiene
exactamente **una** notificación `MetaCambioEstado` y un solo correo encolado por ese cambio. Lo
mismo vale si se completa la meta mientras otra consulta la está pasando de estado: una de las dos
operaciones gana y la otra no avisa (una acción del dueño en conflicto responde `409`).
*Comprobación:* 8 peticiones en paralelo y `GET /api/notificaciones`.

**CR-10 · RF-REP-05, RD-07 · Los reportes respetan el horario de verano de la zona (Hallazgo 3).**
Dado el servidor con una zona con horario de verano, cuando se consultan los reportes, entonces:
- con `TZ=America/New_York`, una solicitud creada el 1 de agosto a las 00:30 locales (04:30 UTC)
  sale en el grupo `2026-08` al consultar `?desde=2026-08-01&hasta=2026-08-01`, y no en
  `2026-07`;
- con `TZ=America/Havana`, donde el reloj salta de las 00:00 a la 01:00 el 8 de marzo de 2026,
  `?desde=2026-03-08` y `?hasta=2026-03-07` responden `200` (el día empieza en su primer instante
  válido), nunca `500`.
*Comprobación:* levantar la app con esa `TZ` y consultar `GET /api/reportes/solicitudes-por-estado`.

**CT-06 · RD-08 · Los comandos de consola no muestran trazas (Hallazgo 4).**
Dado SQL Server detenido (`docker compose stop sqlserver`), cuando se ejecuta
`enviar-correos` o `promover-administrador`, entonces la consola muestra solo «No se pudo
completar el comando: la base de datos no está disponible o no respondió. …» y el comando termina
con código 1: ninguna línea de traza (`at …`), ni el texto de la excepción, ni la cadena de
conexión.
*Comprobación:* la salida y el código de salida del comando.
