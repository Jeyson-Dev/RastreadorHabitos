# Bitácora de la Asignación 3 — Auditoría de lo que el agente entregó en la Práctica 5

Programación III · ITLA · 2026-C-3. Sesión del 2026-11-23 con Claude Code (Claude Opus 5.5).

Se audita el código que quedó en `main` con la etiqueta `practica-5` (`a160350`): notificaciones,
cola de correos y reportes, que el agente construyó en la Práctica 5 a partir de
[`docs/criterios-notificaciones-reportes.md`](criterios-notificaciones-reportes.md).

## 1. Cómo se auditó

1. **Revisión independiente.** Dos revisores que no escribieron el código: los agentes
   `ecc:csharp-reviewer` (correctitud, criterios CN/CC/CR/CT, RD-02, 03, 07 y 11, casos no
   cubiertos) y `ecc:security-reviewer` (RD-06, 07, 08, 10, inyección y datos sensibles). Leyeron
   `git diff practica-4 practica-5 -- src` contra los criterios, los RF-NOT y RF-REP, los RD y la
   lista de casos de la sección 1.1 del enunciado. No modificaron nada ni abrieron el `.env`.
2. **Reproducción.** Cada candidato se reprodujo ejecutando el proyecto: un clon de `practica-5`
   levantado con `docker compose up --build` (proyecto `a3audit`, volúmenes propios), la API real
   en `http://localhost:5003`, SQL Server y Mailpit. Solo se declara hallazgo lo que se reprodujo.
3. **Corrección y prueba.** Un commit por hallazgo con su prueba de regresión xUnit, más un commit
   de preparación en el Hallazgo 4. Cada prueba se ejecutó sobre el código de producción del
   commit **anterior** a su corrección (con la carpeta `tests` del commit corregido) y sobre el
   commit corregido (sección 3).

### Candidatos de los revisores y decisión

| Candidato | Revisor | Decisión |
|---|---|---|
| Un destinatario vacío rompe el procesador de la cola | correctitud | **Hallazgo 1**: reproducido |
| Dos consultas simultáneas de una meta duplican `MetaCambioEstado` | correctitud | **Hallazgo 2**: reproducido |
| El reporte usa la diferencia horaria de hoy y una medianoche inexistente da 500 | correctitud | **Hallazgo 3**: reproducido con `TZ=America/New_York` y `TZ=America/Havana` |
| El comando `enviar-correos` no captura errores de base de datos e imprime la traza | seguridad | **Hallazgo 4**: reproducido con SQL Server detenido |
| Un fallo al notificar después de guardar deja el cambio aplicado y responde 500 | los dos | **Riesgo anotado, no hallazgo.** Es real en el código, pero solo ocurre si la base falla entre dos guardados seguidos; no se pudo provocar ejecutando el proyecto. Corregirlo bien exige un *outbox* transaccional entre tres piezas. Queda registrado aquí. |
| `ultimoError` copia el texto de la plataforma | seguridad | **Descartado.** En el contenedor el texto observado fue «No se pudo enviar por el servidor de correo: Resource temporarily unavailable.», sin host, IP ni credenciales. No se reprodujo una exposición. |
| Las fechas de las solicitudes salen sin `Z` | correctitud | **Fuera del alcance.** Es código de la Asignación 2 (Gestión de permisos), no de la Práctica 5. CT-01 solo exige notificaciones y cola, que sí la llevan. |
| Paginación, código de salida 1 con un error, destinatarios con coma, programados históricos, transición doble | correctitud | **Descartados.** Decisiones de diseño o riesgos que no se pueden provocar con la API actual (no hay forma de editar los días de un hábito, ni de registrar un día que no sea hoy). |
| Autorización (RD-06), inyección HTML y SQL, cuerpo de los correos, RD-10 | seguridad | **Sin defectos.** El revisor lo confirmó leyendo el código. |

## 2. Hallazgos

### Hallazgo 1 — Un correo con destinatario vacío rompe el procesador y bloquea la cola

| Campo | |
|---|---|
| **Hallazgo** | 1 · Un correo con destinatario vacío rompe el procesador y bloquea la cola · **Gravedad media** · Notificaciones (cola) · Incumple **CC-04** («un correo con destinatario inválido no bloquea a los demás») y **RF-NOT-10** |
| **Qué se pidió** | Criterio CC-04, escrito antes de delegar: «Dado en la cola un correo con un destinatario mal formado (por ejemplo `sin-arroba`, insertado con SQL) y otro correo válido […] el válido queda `Enviado` y el inválido suma un intento con su último error. Al tercer intento queda `Fallido`.» |
| **Qué entregó el agente** | `TransporteSmtp` (commit `f2deab8`, Práctica 5) solo capturaba `FormatException` al crear el `MailMessage`, y `EnviadorColaCorreos` solo esperaba `FalloEnvioCorreoException`. Con un destinatario vacío, `MailMessage` lanza `ArgumentException`. **Evidencia** (API real sobre `practica-5`: un correo con `Destinatario = ''` y uno válido creado después, insertados con SQL): `enviar-correos` terminó con **código 134** dos veces seguidas: «Unhandled exception. System.ArgumentException: The value cannot be an empty string. (Parameter 'to') at … MailMessage..ctor … at RastreadorHabitos.Core.Notificaciones.Services.TransporteSmtp.EnviarAsync». La cola quedó `(vacío)|Pendiente|0` y `h1-370ccc@prueba.local|Pendiente|0`: ninguno sumó intentos y el válido **nunca llegó a Mailpit** («0 veces»). |
| **Por qué pasó** | **Criterio incompleto.** CC-04 daba un solo ejemplo de destinatario inválido, el mal formado (`sin-arroba`), y la prueba del agente (`ColaCorreosTests`) usaba un transporte falso que ya lanzaba la excepción esperada. Nadie probó el valor vacío ni la frontera real con `MailMessage`. Además, la instrucción no exigía tratar cualquier error del transporte como fallo del correo. |
| **Corrección** | La hizo **el agente** (Claude Code) con la instrucción del usuario: «Un commit por hallazgo que cite su número, con su prueba de regresión que falle antes y pase después […] corrige la causa, no el síntoma (busca si el mismo error se repitió en otro lugar)». Se corrigió en los dos lugares donde estaba la causa: el transporte trata `ArgumentException` como destinatario inválido, y el procesador trata cualquier otro error del transporte como un intento fallido de ese correo, sin guardar su detalle. Commit **`a9094a4`**. Prueba: `tests/RastreadorHabitos.Tests/Notificaciones/Hallazgo1DestinatarioVacioTests.cs`. Después, en vivo: «Correos enviados: 1. Con error en este intento: 1 […] Último error: La dirección del destinatario no es válida.», el vacío con 2 intentos tras dos ejecuciones y el válido que había quedado bloqueado, ya enviado. |
| **Qué cambias** | En `AGENTS.md`, regla 1 de «Reglas que salen de la auditoría»: en una frontera con un sistema externo, capturar cualquier error por elemento, y probar siempre el valor vacío y el de solo espacios. En los criterios: **CC-10** y la regla de «entrada inválida» de la sección 5. |

### Hallazgo 2 — Consultas simultáneas de una meta duplican la notificación de cambio de estado

| Campo | |
|---|---|
| **Hallazgo** | 2 · Consultas simultáneas de una meta duplican la notificación `MetaCambioEstado` · **Gravedad media** · Notificaciones · Incumple **CN-08** («Consultar la meta otra vez sin que cambie de estado no crea otra notificación») y **RF-NOT-03** |
| **Qué se pidió** | Criterio CN-08: «cuando E1 registra hoy el cumplimiento de ese hábito y consulta la meta, entonces la meta pasa a `EnProgreso` y E1 tiene una notificación nueva […]. Consultar la meta otra vez sin que cambie de estado no crea otra notificación.» |
| **Qué entregó el agente** | `MetasService` (commit `1ed9ccd`, Práctica 5) compara el estado antes y después de aplicar las transiciones del sistema y avisa si cambió. Pero `Meta.Estado` no era token de concurrencia: dos peticiones que leen `Pendiente` guardan las dos y avisan las dos. **Evidencia** (API real sobre `practica-5`: 5 metas `Pendiente` con el cumplimiento de hoy registrado y 8 `GET /api/metas/{id}` simultáneos por meta): «avisos MetaCambioEstado por meta (debería ser 1 cada una): {'Meta 4': 7, 'Meta 3': 7, 'Meta 2': 7, 'Meta 1': 6, 'Meta 0': 5}». Cada aviso además encoló su correo. |
| **Por qué pasó** | **Criterio ausente.** CN-08 comprobaba consultas seguidas, no simultáneas. La revisión de la Práctica 5 verificó el criterio tal como estaba escrito, con una petición a la vez, y no notó que Gestión de permisos sí tenía token de concurrencia y Metas no. |
| **Corrección** | La hizo **el agente** con la misma instrucción del usuario (arriba). `Meta.Estado` pasa a ser token de concurrencia, como el estado de las solicitudes. Si al guardar una transición del sistema otra petición ya la aplicó, se toma el estado guardado y **no se avisa otra vez**. Completar o reactivar en conflicto responde `409`. La migración `EstadoMetaComoTokenDeConcurrencia` no cambia columnas y la máquina de estados no cambia. Commit **`30094f5`**. Prueba: `tests/RastreadorHabitos.Tests/Notificaciones/Hallazgo2ConsultasSimultaneasMetaTests.cs`. Después, en vivo, con la misma prueba de 8 consultas simultáneas: «{'Meta 4': 1, 'Meta 3': 1, 'Meta 2': 1, 'Meta 1': 1, 'Meta 0': 1}». |
| **Qué cambias** | En `AGENTS.md`, regla 2: todo cambio de estado que notifica usa token de concurrencia y avisa solo si su guardado ganó, y se prueba con dos peticiones simultáneas. En los criterios: **CN-17** y la regla de «una sola vez» de la sección 5. |

### Hallazgo 3 — Con una zona con horario de verano, el reporte agrupa en el mes equivocado y una medianoche inexistente da 500

| Campo | |
|---|---|
| **Hallazgo** | 3 · Con una `TZ` con horario de verano, el reporte del Core agrupa en el mes equivocado y una medianoche inexistente da 500 · **Gravedad baja** (con la `TZ` por defecto, `America/Santo_Domingo`, no ocurre; pero `TZ` es una variable documentada en el README) · Reportes · Incumple **RF-REP-05**, **CR-06** (el día del rango) y **RD-07** (500 en lugar de una respuesta controlada) |
| **Qué se pidió** | RF-REP-05 y el criterio CR-06: «Dado una solicitud creada hoy a cualquier hora […] con `hasta=<hoy>` la solicitud se cuenta.» CR-08: un rango inválido se rechaza de forma controlada, «sin traza». |
| **Qué entregó el agente** | `FuenteSolicitudesPorEstado` (commit `bf49e58`) agrupaba por mes con la diferencia con UTC **de hoy** para todas las fechas (`zona.GetUtcOffset(_reloj.GetUtcNow())`, usada en `ContarSolicitudesPorEstadoAsync`, commit `41e788a`). `RangoFechas` (commit `f051502`) convertía la medianoche local con `TimeZoneInfo.ConvertTimeToUtc` sin contemplar que puede no existir. **Evidencia** (API real sobre `practica-5`): con `TZ=America/New_York` (hoy EST, −05:00) y una solicitud del 2026-08-01 a las 00:30 locales (04:30 UTC, horario de verano), `?desde=2026-08-01&hasta=2026-08-01` → `200 [{'grupo': '2026-07', … 'Total': 1}]`, y `?desde=2026-07-01&hasta=2026-07-31` → `200 []`: el reporte se contradice con su propio filtro. Con `TZ=America/Havana`, `?desde=2026-03-08` y `?hasta=2026-03-07` → **`500`** «Ocurrió un error interno.»; en el log: «System.ArgumentException: The supplied DateTime represents an invalid time.» |
| **Por qué pasó** | **Criterio ausente y revisión insuficiente.** Los criterios y las pruebas de la Práctica 5 usaron solo la zona del servidor (UTC−4 fija, sin horario de verano). El propio código llevaba un comentario que aceptaba el supuesto («sin horario de verano en America/Santo_Domingo») sin que nada lo comprobara, aunque la `TZ` es configurable. |
| **Corrección** | La hizo **el agente** con la misma instrucción del usuario. Cada mes local se convierte a su propio intervalo UTC, con las reglas de la zona para ese mes. Gestión de permisos cuenta por estado con `GROUP BY Estado` dentro de cada intervalo, sin traer solicitudes. `RangoFechas.InicioDelDiaUtc` toma el primer instante válido del día. Busqué si el error se repetía: la conversión de UTC a local de Hábitos ya era correcta. Commit **`1bb3606`**. Prueba: `tests/RastreadorHabitos.Tests/Reportes/Hallazgo3ZonaHorariaReportesTests.cs`, con zonas construidas en la prueba. Después, en vivo: con Nueva York → `'grupo': '2026-08'`; con La Habana → `?desde=2026-03-08` `200` y `?hasta=2026-03-07` `200 []`. |
| **Qué cambias** | En `AGENTS.md`, regla 3: nunca una diferencia horaria fija para otras fechas, conversiones locales que manejan horas inexistentes y pruebas con una zona con horario de verano. En los criterios: **CR-10** y la regla de fechas de la sección 5. |

### Hallazgo 4 — Los comandos de consola imprimen la traza completa si la base no responde

| Campo | |
|---|---|
| **Hallazgo** | 4 · Los comandos `enviar-correos` y `promover-administrador` imprimen la traza completa si la base no responde · **Gravedad baja** · Notificaciones (procesador de la cola) · Incumple **RD-08** y **CT-02** («Ningún mensaje […] muestra una traza de pila […] ni una consulta») |
| **Qué se pidió** | CT-02 (RD-08) y la instrucción de la Práctica 5: «errores sin trazas (RD-08)». |
| **Qué entregó el agente** | El comando `enviar-correos` se reescribió en la Práctica 5 (commit `f2deab8`) sin capturar errores, igual que en la cola mínima de la Práctica 1. Las migraciones que corren antes del comando escribían el error con su traza y la volvían a lanzar. **Evidencia** (API real sobre `practica-5`, `docker compose stop sqlserver` y `enviar-correos`): «código 134; 73 líneas; 55 líneas de traza («at ...»)», que empezaban con «crit: RastreadorHabitos.Api[0] No se pudieron aplicar las migraciones. […] Microsoft.Data.SqlClient.SqlException (0x80131904): A network-related or instance-specific error occurred while establishing a connection to SQL Server.» y seguían con «at System.Net.Dns.GetHostEntryOrAddressesCore(…)». El Hallazgo 1 mostró lo mismo: su excepción también salió con traza. |
| **Por qué pasó** | **Criterio ambiguo e instrucción incompleta.** CT-02 hablaba de «las operaciones de este documento» y del «último error de un correo», y se entendió como solo la API. Nadie ejecutó el comando con la base caída. |
| **Corrección** | La hizo **el agente** con la misma instrucción del usuario. Primero un commit de preparación sin cambio de comportamiento, que saca los comandos a `Comandos/ComandosConsola.cs` para poder probarlos sin levantar la app (**`594c115`**). Después la corrección: `EjecutarAsync` captura cualquier error y escribe solo «No se pudo completar el comando: la base de datos no está disponible o no respondió. …» con código 1, en los dos comandos (**`39dfe60`**). La API no cambia: su error de arranque sigue en el log del servidor. Prueba: `tests/RastreadorHabitos.Tests/Notificaciones/Hallazgo4ComandosSinTrazasTests.cs`. **Error del agente durante la corrección:** la primera versión de la prueba usaba la constante `ComandosConsola.MensajeFallo`, que solo existe en el commit corregido, así que sobre el commit anterior no habría compilado y no demostraba nada. Se detectó al preparar la demostración de la sección 3 y se cambió por una comprobación del texto antes de publicar el commit. Después, en vivo, con la base detenida: «código 1; 1 líneas; 0 líneas de traza». |
| **Qué cambias** | En `AGENTS.md`, regla 4: RD-08 también en la consola, con los comandos en `ComandosConsola`. En los criterios: **CT-06** y la regla de RD-08 de la sección 5. |

## 3. Falla antes, pasa después

Para cada hallazgo, en un `git worktree` aparte: se ejecutó la prueba del hallazgo sobre el código
de producción del commit **anterior** a la corrección (con la carpeta `tests` del commit
corregido), y después sobre el commit corregido. Comando:
`dotnet test --filter "FullyQualifiedName~HallazgoN"`. Salida real, recortada:

```text
===== Hallazgo 1 — antes: a160350 (anterior a la corrección, con las pruebas de a9094a4)
    Hallazgo1DestinatarioVacioTests.TransporteSmtp_ConDestinatarioVacio_LoInformaComoFalloDelCorreo [FAIL]
    Hallazgo1DestinatarioVacioTests.Procesar_ConUnDestinatarioVacioPrimero_SumaSuIntentoYEnviaElSiguiente [FAIL]
   Assert.IsType() Failure: Value is not the exact type
Expected: typeof(RastreadorHabitos.Core.Notificaciones.Services.FalloEnvioCorreoException)
Actual:   typeof(System.ArgumentException)
   System.ArgumentException : The value cannot be an empty string. (Parameter 'to')
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2
===== Hallazgo 1 — despues: a9094a4 (corrección)
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2

===== Hallazgo 2 — antes: a9094a4 (anterior a la corrección, con las pruebas de 30094f5)
    Hallazgo2ConsultasSimultaneasMetaTests.ConsultarMeta_QueOtraConsultaYaCambio_NoDuplicaLaNotificacion [FAIL]
   Assert.Single() Failure: The collection contained 2 items
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1
===== Hallazgo 2 — despues: 30094f5 (corrección)
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1

===== Hallazgo 3 — antes: 30094f5 (anterior a la corrección, con las pruebas de 1bb3606)
    Hallazgo3ZonaHorariaReportesTests.SolicitudesPorEstado_ConHorarioDeVerano_AgrupaEnElMesLocalDeCadaFecha [FAIL]
    Hallazgo3ZonaHorariaReportesTests.SolicitudesPorEstado_ConUnaMedianocheQueNoExiste_NoFallaYFiltraElDia [FAIL]
   Assert.Equal() Failure: Strings differ
Expected: "2026-08"
Actual:   "2026-07"
   Assert.Null() Failure: Value is not null
Actual:   System.ArgumentException: The supplied DateTime represents an invalid time. […]
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2
===== Hallazgo 3 — despues: 1bb3606 (corrección)
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2

===== Hallazgo 4 — antes: 594c115 (anterior a la corrección, con las pruebas de 39dfe60)
    Hallazgo4ComandosSinTrazasTests.EnviarCorreos_ConLaBaseCaidaDuranteElEnvio_InformaUnMensajeSinTrazaYTerminaConUno [FAIL]
    Hallazgo4ComandosSinTrazasTests.Comando_ConLaBaseCaidaAlMigrar_InformaUnMensajeSinTrazaYTerminaConUno [FAIL]
   Assert.Null() Failure: Value is not null
Actual:   System.InvalidOperationException: A network-related or instance-specific error occurred while establishing a connection to SQL Server. Server=sqlserver,1433;Password=secreto
Failed!  - Failed:     2, Passed:     0, Skipped:     0, Total:     2
===== Hallazgo 4 — despues: 39dfe60 (corrección)
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2
```

(`Password=secreto` es un texto inventado dentro de la prueba, para comprobar que no se muestra.)

Para repetirlo a mano (ver la sección «Pruebas» del README):

```bash
git checkout <commit-anterior>          # por ejemplo, el padre de a9094a4
git checkout <commit-de-la-corrección> -- tests
dotnet test --filter "FullyQualifiedName~Hallazgo1"   # falla
git checkout <commit-de-la-corrección>
dotnet test --filter "FullyQualifiedName~Hallazgo1"   # pasa
```

## 4. Lo anterior sigue funcionando

Sobre la punta de las correcciones (`39dfe60`), con `docker compose up --build`:

- `dotnet test`: **161 pruebas, todas en verde** (154 de antes y 7 de regresión).
- Los **seis frentes** de la revisión acumulada de la Práctica 5 pasaron: activación por la cola,
  sesión y rechazo `403`, recuperación de contraseña, Gestión de permisos con las transiciones
  fuera de la tabla rechazadas, módulo de negocio con la transición prohibida rechazada, y
  documento que sobrevive a `docker compose down` sin `-v`.
- La migración nueva de Metas se aplicó sola al arrancar sobre una base con datos.
