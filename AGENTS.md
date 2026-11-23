# AGENTS.md — Rastreador de hábitos con metas

Programación III (TDS-007) · ITLA · 2026-C-3. Instrucciones para cualquier agente de IA que
trabaje en este repositorio.

## 0. Contexto del proyecto en una página

Es el archivo de contexto que exige la Práctica 5 (sección 1.1). Lo que sigue lo resume; las
secciones 1 a 7 lo detallan.

| | |
|---|---|
| **Propósito** | API de un rastreador de hábitos con metas. Un usuario define hábitos con sus días de la semana, registra cada día si los cumplió y agrupa hábitos en metas con un objetivo de cumplimientos y una fecha límite (máquina de estados de la Meta). Lo rodea el Core del curso: Control de acceso, Gestión de permisos, Manejador de documentos, Notificaciones con cola de correos, Reportes y Auditoría. |
| **Lenguaje y framework** | C# sobre .NET 10 · ASP.NET Core Web API · Entity Framework Core 10 con SQL Server 2022 · xUnit para las pruebas · Docker Compose para ejecutar. |
| **Cómo correrlo** | Desde la raíz de un clon limpio: `cp .env.example .env` y después `docker compose up --build`. La API queda en `http://localhost:5003` y los correos se leen en Mailpit, `http://localhost:8025`. Detener: `docker compose down` (sin `-v`, conserva los datos). |
| **Cómo probarlo** | `dotnet test` desde la raíz. Toda la suite corre sin SQL Server, sin variables de entorno y sin levantar la API (bases EF Core en memoria). Debe pasar completa antes de cada commit. |
| **Peticiones de ejemplo** | `src/Api/RastreadorHabitos.Api/RastreadorHabitos.Api.http` y las secciones del `README.md`. |

**Convenciones de código** (siguen el código existente; ver también las secciones 4 y 6):

- Todo en español: clases, métodos, propiedades, mensajes al usuario y comentarios. Los métodos
  asíncronos terminan en `Async`.
- Cada pieza es su propio `.csproj` con las carpetas `Entities/`, `Data/`, `DTOs/`, `Services/`,
  `Reglas/`, `Configuracion/` y `Migrations/`. Cada servicio tiene su interfaz (`IXxxService`).
- La lógica de negocio vive en los servicios y las reglas de cada pieza, nunca en los
  controllers (RD-02). Un controller solo traduce la petición, toma el Id y el rol del usuario de
  la sesión y llama al servicio.
- Toda entrada externa se valida en el servicio, con un mensaje en español (RD-07). Los rechazos
  esperados lanzan la excepción propia de la pieza (`RechazoXxxException`) y los traduce
  `Errores/ManejadorErroresGlobal.cs`; cualquier otra excepción responde un 500 genérico (RD-08).
- La exigencia de rol de cada operación se declara solo en
  `src/Api/RastreadorHabitos.Api/Autorizacion/ExigenciasDeRol.cs` (RF-CA-05). Si falta una
  operación, la API no arranca.
- Las fechas y horas se guardan en UTC (`DateTime.UtcNow`, propiedades que terminan en `Utc`) y
  salen en JSON marcadas como UTC. «Hoy» y los rangos de fechas `AAAA-MM-DD` son días locales
  del servidor (`TimeProvider`, variable `TZ`) (RD-11).
- Una pieza habla con otra solo por su interfaz, o por un puerto que declara y que la Api
  conecta en `Adaptadores/`. Nunca agregues una referencia de proyecto fuera de la tabla de la
  sección 4.
- Las pruebas: una base en memoria con nombre único por prueba, las fases marcadas con
  `// Preparar`, `// Actuar` y `// Verificar`, un nombre que dice el comportamiento y un
  comentario con el requisito que verifican, por ejemplo `// [RF-NOT-05] …`.
- Commits atómicos: `tipo(alcance): descripción en imperativo [ID-DEL-REQUISITO]`.

**Restricciones: lo que el agente nunca hace.**

- **Nunca lee ni escribe el archivo `.env`**, ni lo copia, ni lo muestra. Para saber qué
  variables existen, usa `.env.example`, que solo tiene valores de ejemplo.
- **Nunca escribe una credencial, una clave, una contraseña ni una cadena de conexión real** en
  el código, en `appsettings*.json`, en `launchSettings.json`, en el `Dockerfile`, en el
  `docker-compose.yml`, en la documentación ni en un mensaje de commit. Todo llega por
  variables de entorno (RD-10, RF-NOT-13).
- **No modifica sin autorización** lo ya entregado: Control de acceso, Gestión de permisos, el
  Manejador de documentos, la máquina de estados de la Meta (`EstadoMeta.cs`,
  `TransicionesMeta.cs`) ni las pruebas existentes. Si una tarea lo exige, lo consulta primero.
  Agregar una operación de solo lectura o un puerto nuevo se informa en el pull request.
- **No edita una migración ya aplicada**: todo cambio de esquema es una migración nueva.
- **No toca los enunciados de `docs/`** (son transcripciones literales), ni los tags ya
  entregados (`practica-1` a `practica-4`, `asignacion-2`, `practica-2-defecto-*`,
  `practica-3-defecto-*`), ni las ramas `defecto-N` y `p3-defecto-N`, ni reescribe la historia
  publicada.
- **Nunca hace commits directos a `main`**, ni `git push --force`, ni merge a `main`: cada
  cambio va en su rama y su pull request, y el merge y los tags los hace el usuario.
- No construye nada que ningún enunciado pida (sección 1).

**Nota sobre el historial de Notificaciones.** Los commits `5c8c2eb`, `72481d0`, `b0a26bc` y
`9f5befd` (1 y 2 de octubre, alcance `feat(notificaciones)`) son la **cola mínima de correos de
la Práctica 1** (sección 1.5 de su enunciado: encolar, enviador independiente, sin duplicados,
SMTP por variables de entorno). La Práctica 5 la reconoce en su sección 1.3. **No son el motor
de notificaciones**: este (entidad `Notificacion`, operación `notificar`, bandeja,
preferencias) se construye en la Práctica 5 con el alcance `motor-notificaciones`, siempre
después del commit que agrega este contexto y `docs/criterios-notificaciones-reportes.md`.

## 1. Fuentes de verdad

Es un proyecto largo que se construye por prácticas, todas sobre el mismo diseño.

- **Base de todo el proyecto: `Estructura.md`.** Es el diagrama de componentes de la
  semana 2: las seis piezas del Core (Control de acceso, Gestión de permisos, Notificaciones,
  Auditoría, Reportes, Manejador de documentos), el módulo de negocio (Hábitos y Metas) y
  cómo se relacionan. Solo contiene el diagrama: no trae modelo de entidades, máquina de
  estados ni reglas de negocio.
- **Cada práctica tiene su enunciado en `docs/`**, transcripción literal de su PDF. Para la
  pieza (o las partes) que una práctica define, su enunciado **se cumple estrictamente** y
  prevalece sobre todo lo demás, incluido este archivo; sobre la transcripción, prevalece el
  PDF.

| Práctica | Enunciado | Qué define | Estado |
|---|---|---|---|
| 1 | `docs/practica1-control-acceso.md` (`practica1-p3.pdf`) | Control de acceso completo (pieza 1) · cola mínima de correos (RF-NOT-08, 09, 12, 13) · estructura de la máquina de estados del negocio (sección 1.6) | Entregada (tag `practica-1`) |
| Asignación 2 | `docs/asignacion2-gestion-permisos.md` | Gestión de permisos (pieza 2): RF-GP-01 a 07 · suite de pruebas (RD-12) · bitácora `docs/bitacora-asignacion-2.md` | Entregada (tag `asignacion-2`) |
| 2 | `docs/practica2-suite-defectos.md` | Suite sobre Gestión de permisos (RF-GP-01 a 07, una prueba identificada por requisito) · seis defectos plantados de la variante A (tags `practica-2-defecto-1` a `6`, ramas `defecto-N`, nunca en `main`) · sección «Defectos» del README · revisión acumulada de Control de acceso | Entregada (tag `practica-2`) |
| 3 | `docs/practica3-modulo-negocio.md` | Módulo de negocio funcionando (RF-NEG-02 a 06 y 09) · suite de la máquina de estados de Meta · cuatro defectos plantados (tags `practica-3-defecto-1` a `4`) · prueba de integración con Gestión de permisos · revisión acumulada de los cinco frentes | Entregada (tag `practica-3`) |
| 4 | `docs/practica4-contenedor-documentos-api.md` | Manejador de documentos (pieza 3: RF-DOC-01 a 05) · proyecto en contenedores con un solo comando (`Dockerfile`, `.dockerignore`, `docker-compose.yml`) · volúmenes persistentes · API externa con su clave como secreto (`.env` no versionado, `.env.example`) · revisión acumulada de los seis frentes | Entregada (tag `practica-4`) |
| 5 | `docs/practica5-notificaciones-reportes.md` | Contexto del agente y criterios de aceptación antes de delegar (§1.1) · Notificaciones (pieza 4: RF-NOT-01 a 07) · cola de correos completa (RF-NOT-08 a 13) · Reportes con agregación (pieza 5: RF-REP-01 a 05) · revisión acumulada de los seis frentes | En curso (tag `practica-5`) |
| Asignación 3 | `docs/asignacion3-auditoria-delegacion.md` | Auditoría de lo construido en la Práctica 5 · al menos tres hallazgos con evidencia · un commit y una prueba de regresión por hallazgo · bitácora `docs/bitacora-asignacion-3.md` | Pendiente (tag `asignacion-3`) |
| Asignación 4 | `docs/asignacion4-historial-auditoria.md` | Auditoría (pieza 6: RF-AUD-01 a 07, RF-DOC-06) · pruebas de cada evento auditado · bitácora `docs/bitacora-asignacion-4.md` con el paralelo con git | Pendiente (tag `asignacion-4`) |

**Cuando llegue una práctica nueva:** agregar su transcripción literal a `docs/`, añadir su
fila a esta tabla y adaptar el proyecto a ella **sin romper lo que exigen las prácticas
anteriores**. A partir de la Práctica 2 se recalifica lo entregado en la Práctica 1
(registro con activación, inicio de sesión, rechazo por rol y recuperación de contraseña).

**Orden ante un conflicto:** enunciado de la práctica que define esa pieza →
`Estructura.md` → este archivo → el código existente.

**No inventes requisitos ni alcance:**
- No implementes en una pieza más de lo que pide la práctica que la define. Una pieza que
  todavía no tiene práctica (hoy, Auditoría, hasta la Asignación 4) no se construye por
  adelantado.
- Si un identificador (RF-CA-XX, RF-NOT-XX, RF-NEG-XX, RD-XX) aparece citado sin su texto
  —por ejemplo RD-04, RD-06 o RD-10 en la Práctica 1—, pide su texto antes de asumir qué
  exige.
- Si falta información para una parte (por ejemplo, una regla de negocio que ningún
  documento define), pregunta; no la deduzcas.

## 2. Práctica 1 — Control de acceso (cumplimiento estricto)

Resumen operativo; no reemplaza al enunciado. Ante cualquier duda, léelo completo.

**Alcance:** las cuatro funcionalidades de Control de acceso con sus requisitos
RF-CA-01 a RF-CA-22 (registro y activación, sesión, roles y administración, contraseñas),
cada una con sus criterios de aceptación tal como están en el enunciado.

**Correo saliente, versión mínima (sección 1.5):**
- El correo no se envía dentro de la operación que lo origina: se registra en la entidad
  `CorreoEnCola` y un proceso o comando independiente lo envía (RF-NOT-08, RF-NOT-09).
- La operación termina bien aunque el servidor de correo no responda; el correo queda
  pendiente.
- Ejecutar el enviador dos veces no duplica envíos (RF-NOT-12).
- Las credenciales SMTP se leen de variables de entorno (RF-NOT-13, RD-10).
- El correo se recibe de verdad: el profesor se registra con su propio correo.
- Los reintentos, el estado fallido, el último error y la consulta de la cola por el
  Administrador no eran de la Práctica 1: los pide la Práctica 5 (RF-NOT-10, RF-NOT-11), sin
  romper lo de arriba.

**Requisitos de diseño con texto en el enunciado:**
- RD-05: dos usuarios con la misma contraseña no comparten el valor almacenado (hash con sal).
- RD-07: un correo vacío o mal formado produce un rechazo controlado, no una excepción sin
  manejar. Toda contraseña que no cumpla RF-CA-14 se rechaza con mensaje controlado, en el
  registro y en todo cambio de contraseña.
- RD-08: ningún mensaje al usuario expone trazas ni consultas.
- RD-09: los usuarios registrados sobreviven a reiniciar la aplicación (persistencia real).
- Los registros de auditoría de RF-CA-08, RF-CA-13 y RF-CA-20 no se califican en esta
  práctica (semana 14, pieza 6).

**Máquina de estados del negocio (sección 1.6):** solo la estructura, sin pruebas
(llegan en la semana 8): entidad central con su atributo de estado, entre 3 y 5 estados
declarados en un solo lugar (RF-NEG-03), transiciones en un solo lugar (RD-04) con al menos
una prohibida (RF-NEG-04) y un estado terminal (RF-NEG-05), y `docs/maquina-de-estados.md`
con la tabla desde, hacia, quién la ejecuta, condición.

**Entrega:** `git tag practica-1` y `git push origin --tags`. Se evalúa el punto etiquetado.

## 3. Historial, pull requests y README

Lo exige la Práctica 1 (sección 1.7 y rúbrica) y se aplica a todo el proyecto, salvo que una
práctica posterior diga otra cosa.

- Commits atómicos con asunto en imperativo y el identificador del requisito que cubren.
- Cada funcionalidad en su rama, fusionada a `main` por un pull request con las cuatro
  secciones: **Qué cambia, Por qué, Cómo probarlo, Qué NO incluye**.
- **Nunca commits directos a `main`.** «Trabajo directo en `main` sin ramas ni pull
  requests» es el nivel 0 % de la rúbrica.
- Nada indebido en el historial: ni credenciales ni archivos generados (`bin/`, `obj/`,
  `.vs/`). El profesor revisa `git log -p` y `git ls-files`.
- README con las instrucciones exactas para ejecutar el proyecto, las variables de entorno
  que necesita (nombre y para qué, **nunca el valor**) y cómo provocar cada criterio de
  aceptación. Se sigue al pie de la letra al calificar; lo que el README no dice, no se
  busca.

## 4. Decisiones del proyecto (no vienen de ningún enunciado)

Se tomaron con el usuario sobre el diseño de la semana 2. Si alguna choca con el enunciado
de una práctica, prevalece el enunciado.

**Stack:** C# / ASP.NET Core (.NET 10) y SQL Server con EF Core. Planificado y aún no
implementado: credencial de sesión con JWT (Bearer token en el header `Authorization`) y un
frontend en HTML, CSS y JavaScript plano que consume la API con `fetch`. La Práctica 1
menciona una «interfaz» en RD-06 y en la rúbrica de Roles y administración.

**Arquitectura: monolito modular por proyectos.** Cada pieza es su propio `.csproj`, para que
el compilador impida dependencias indebidas. Nunca copies código de una pieza en otra ni
agregues una referencia de proyecto que no esté en esta tabla. Las referencias se agregan
cuando el código las necesita, no antes.

```
src/
├── Core/
│   ├── RastreadorHabitos.Core.Auditoria/        → sin dependencias
│   ├── RastreadorHabitos.Core.Notificaciones/   → sin dependencias
│   ├── RastreadorHabitos.Core.ControlAcceso/    → puede referenciar Core.Auditoria
│   ├── RastreadorHabitos.Core.GestionPermisos/  → puede referenciar ControlAcceso, Notificaciones, Auditoria
│   ├── RastreadorHabitos.Core.Documentos/       → puede referenciar Core.Auditoria
│   └── RastreadorHabitos.Core.Reportes/         → puede referenciar Core.Auditoria
├── Negocio/
│   ├── RastreadorHabitos.Modulo.Habitos/        → puede referenciar ControlAcceso, Auditoria, Reportes, Notificaciones
│   └── RastreadorHabitos.Modulo.Metas/          → puede referenciar Modulo.Habitos, ControlAcceso, Auditoria, Reportes, Notificaciones
├── Api/
│   └── RastreadorHabitos.Api/                   → puede referenciar todos (única pieza con visión global)
└── tests/
    └── RastreadorHabitos.Tests/                 → referencia los proyectos que vaya cubriendo
```

Referencias actuales: Api → ControlAcceso, Notificaciones, GestionPermisos, Documentos, Reportes,
Modulo.Habitos, Modulo.Metas · Reportes no referencia a nadie: las métricas le llegan ya agregadas
por el puerto `IFuenteReporte`, que la Api conecta con `contarSolicitudesPorEstado` de Gestión de
permisos y con `IMetricasHabitos` del módulo de negocio · Modulo.Habitos → ControlAcceso · Modulo.Metas → Modulo.Habitos, Notificaciones (solo
`notificar`, para avisar los cambios de estado de una meta) ·
Tests → ControlAcceso, GestionPermisos, Modulo.Habitos, Modulo.Metas, Api (solo por los adaptadores
reales que usan las pruebas de integración y de notificaciones). Gestión de permisos no referencia a
ControlAcceso ni a Notificaciones: declara los puertos `IAplicadorPermiso` e `IAvisoSolicitudes`, y
la Api los conecta con Control de acceso (cambio de rol para `Administrador`, `aplicarPermiso` para
cualquier otro permiso; Administradores activos con `IDirectorioUsuarios`) y con `notificar`.
Notificaciones no referencia a nadie: el correo del destinatario le llega por el puerto
`IDirectorioDestinatarios`, que la Api conecta con Control de acceso. Documentos no referencia a
nadie: como Gestión de permisos, recibe de la Api el Id y el rol del usuario de la sesión. Las pruebas
viven en `tests/RastreadorHabitos.Tests/`, en la raíz.

**Fronteras entre piezas** (derivadas del diagrama de `Estructura.md`):
- El módulo de negocio (Hábitos y Metas) solo habla con Control de acceso para identidad y
  permisos (`usuarioActual()`, `tienePermiso()`); nunca llama directo a Gestión de permisos.
- Gestión de permisos nunca autentica ni decide identidad: tramita el estado de una solicitud
  y le pide a Control de acceso que aplique el permiso aprobado.
- Reportes nunca conoce entidades del módulo de negocio; solo recibe métricas ya calculadas y
  filtra por rol.
- Metas depende de Hábitos, nunca al revés. Ninguna pieza del Core referencia al negocio.
- La exigencia de rol por operación se declara en un único punto reutilizable (atributo o
  política de autorización), nunca repetida en cada controller (RF-CA-05).
- Control de acceso no referencia a Notificaciones: declara el puerto
  `ISolicitudCorreoSaliente` y la Api lo conecta con la cola de Notificaciones.

**Datos y configuración:**
- Una sola base de datos; cada pieza en su propio esquema SQL (`ControlAcceso`,
  `Notificaciones`, `Metas`) con su propio historial de migraciones. Las migraciones viven en el
  proyecto de cada pieza y se aplican al arrancar la Api.
- La cadena de conexión también llega por variable de entorno
  (`ConnectionStrings__RastreadorHabitos`); sin ella la Api no arranca. Es más estricto que
  la Práctica 1, que lo exige para las credenciales SMTP.
- Los enlaces de los correos se arman con `App__UrlBase` (opcional, con valor por defecto) y
  nunca con la cabecera `Host` de la petición.
- Errores: un único manejador global; los rechazos de negocio usan una excepción propia de
  cada pieza, y cualquier otra excepción responde un 500 genérico.

**Módulo de negocio — la Meta y su máquina de estados** (en la Práctica 3 se adoptó, con el
usuario, la máquina de `docs/diseno-componentes-detalle.md`; la tabla exacta de lo implementado
está en `docs/maquina-de-estados.md`):
- Una **Meta** agrupa uno o más hábitos de un usuario y su avance es la cantidad de días
  cumplidos de esos hábitos entre su fecha de inicio y su fecha límite (no se guarda).
- Cuatro estados, declarados solo en `Entities/EstadoMeta.cs`: **Pendiente** (inicial),
  **En progreso**, **Completada** (único terminal) y **Fallida**.
- Las transiciones, con quién las ejecuta (Sistema o Dueño) y su condición, se declaran y se
  resuelven solo en `Reglas/TransicionesMeta.cs`; la entidad cambia de estado solo con
  `Meta.CambiarEstado`. Las del sistema se aplican al consultar la meta. La fila 1 de permitidas
  es Pendiente → En progreso y la 1 de prohibidas es Pendiente → Completada: los defectos de la
  Práctica 3 se plantan sobre ese orden.
- Límite de hábitos: 10, o 20 con el permiso `LIMITE_HABITOS_AMPLIADO`, que Hábitos consulta solo
  a Control de acceso (`tienePermiso`).
- Hábitos y Metas guardan solo el `UsuarioId` del dueño, sin relación con las tablas de Control
  de acceso. Todavía no existen `CategoriaHabito`, `HistorialEstadoMeta`, archivar, rachas ni
  hábitos cuantitativos: se construyen cuando una práctica los pida.

**Docker (Práctica 4).** El proyecto corre con `docker compose up --build` tras copiar
`.env.example` a `.env`. La composición tiene cuatro servicios: `app`, `sqlserver`, `enviador`
(el procesador de la cola en bucle) y `mailpit`. Los datos van en los volúmenes con nombre
`datos-sqlserver` y `documentos`, que sobreviven a `docker compose down` sin `-v`. Toda la
configuración llega por variables de entorno. Una variable nueva se agrega a `.env.example`
con un valor de ejemplo (nunca uno real), a `docker-compose.yml` como `${VARIABLE}` y a la
tabla «Variables de entorno» del README.

## 5. Flujo de trabajo acordado con el usuario

- Una rama `feature/<nombre-funcionalidad>` por funcionalidad, creada desde `main`
  actualizado.
- Hasta la Práctica 4, se avanzó **un sub-paso a la vez**: proponer, esperar la aprobación
  explícita del usuario y solo entonces implementar.
- Desde la Práctica 5 el usuario **delega bloques completos**. Antes de cada bloque, el agente
  explica en 3 a 5 líneas qué va a hacer y en qué requisito se basa. Al terminarlo, informa qué
  hizo y cómo lo verificó. Consulta antes de actuar solo ante una decisión importante: un
  conflicto entre un enunciado y el código, un cambio en algo ya entregado, el riesgo de romper
  una práctica anterior, un dato que solo tiene el usuario o dos caminos con consecuencias
  distintas. En cada consulta presenta opciones con pros y contras y su recomendación. Lo
  demás lo resuelve con la opción más simple que cumpla y lo anota en «Supuestos» del pull
  request.
- Los criterios de aceptación de lo que se delega se escriben y se commitean **antes** del
  código (`docs/criterios-notificaciones-reportes.md`); el trabajo se acepta solo si los cumple.
- Un commit por sub-paso aprobado. Convención del proyecto para el asunto:
  `tipo(alcance): descripción corta en imperativo [ID-DEL-REQUISITO]`, por ejemplo
  `feat(control-acceso): agrega hash de contraseña con sal [RF-CA-02, RD-05]`.
- Muestra siempre el comando git exacto antes de correrlo.
- Publicar la rama del bloque (`git push -u origin <rama>`, nunca forzado) está autorizado desde
  la Práctica 5. El agente entrega el texto de cada pull request (Qué cambia, Por qué, Cómo
  probarlo, Qué NO incluye, Supuestos). **El merge a `main` y los tags los hace el usuario.**
- Nunca commitees `bin/`, `obj/`, `.vs/` ni archivos con secretos.

## 6. Antes de crear una clase nueva

Revisa cómo está organizada la carpeta de esa pieza y sigue el patrón que ya exista
(`Entities/`, `Data/`, `DTOs/`, `Services/`, `Reglas/`, `Configuracion/`, `Migrations/` en
el Core y en el módulo de negocio; `Controllers/`, `Adaptadores/`, `Autenticacion/`,
`Autorizacion/`, `Errores/` en la Api). Si la pieza todavía no tiene
ninguna clase de ese tipo, pregunta antes de decidir la carpeta.

## 7. Documentos del repositorio

- `Estructura.md`: diagrama de componentes de la semana 2, base de todo el proyecto.
- `docs/practica1-control-acceso.md`: enunciado de la Práctica 1 (transcripción literal).
- `docs/maquina-de-estados.md`: estados y tabla de transiciones de la Meta (sección 1.6 de la
  Práctica 1).
- `docs/bitacora-asignacion-1.md`: bitácora de una entrega anterior; no es requisito de
  ninguna práctica.
- `docs/asignacion2-gestion-permisos.md`: enunciado de la Asignación 2 (Gestión de permisos).
- `docs/bitacora-asignacion-2.md`: bitácora con el agente que exige la sección 1.3 de la
  Asignación 2.
- `docs/practica2-suite-defectos.md`: enunciado de la Práctica 2 (suite que detecta defectos)
  y el extracto de los defectos publicados de la variante A.
- `docs/practica3-modulo-negocio.md`: enunciado de la Práctica 3 (módulo de negocio, suite de su
  máquina de estados, defectos plantados e integración con Gestión de permisos).
- `docs/practica4-contenedor-documentos-api.md`: enunciado de la Práctica 4 (Manejador de
  documentos, contenedor con un solo comando, volúmenes persistentes y API externa con su clave
  como secreto).
- `docs/practica5-notificaciones-reportes.md`: enunciado de la Práctica 5 (contexto y criterios
  antes de delegar, notificaciones, cola de correos completa y reportes con agregación).
- `docs/criterios-notificaciones-reportes.md`: criterios de aceptación de las notificaciones, la
  cola y los reportes, escritos antes de construirlos (Práctica 5, sección 1.1).
- `docs/asignacion3-auditoria-delegacion.md`: enunciado de la Asignación 3 (auditoría de lo
  delegado en la Práctica 5, correcciones con su prueba y bitácora).
- `docs/asignacion4-historial-auditoria.md`: enunciado de la Asignación 4 (historial de
  auditoría, sus pruebas y la bitácora con el paralelo con git).
- `docs/requerimientos-core.md`: Requerimientos del Core (RD-XX, RF-XX y entidades mínimas), igual
  para todos los proyectos.
- `docs/diseno-componentes-detalle.md`: detalle del diseño de componentes (fronteras, interfaces,
  entidades del módulo, máquina de estados de Meta y reglas de negocio).
