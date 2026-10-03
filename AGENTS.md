# AGENTS.md — Rastreador de hábitos con metas

Programación III (TDS-007) · ITLA · 2026-C-3. Instrucciones para cualquier agente de IA que
trabaje en este repositorio.

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
| 1 | `docs/practica1-control-acceso.md` (`practica1-p3.pdf`) | Control de acceso completo (pieza 1) · cola mínima de correos (RF-NOT-08, 09, 12, 13) · estructura de la máquina de estados del negocio (sección 1.6) | En curso |

**Cuando llegue una práctica nueva:** agregar su transcripción literal a `docs/`, añadir su
fila a esta tabla y adaptar el proyecto a ella **sin romper lo que exigen las prácticas
anteriores**. A partir de la Práctica 2 se recalifica lo entregado en la Práctica 1
(registro con activación, inicio de sesión, rechazo por rol y recuperación de contraseña).

**Orden ante un conflicto:** enunciado de la práctica que define esa pieza →
`Estructura.md` → este archivo → el código existente.

**No inventes requisitos ni alcance:**
- No implementes en una pieza más de lo que pide la práctica que la define. Las piezas que
  todavía no tienen práctica (Gestión de permisos, Auditoría, Reportes, Documentos, el
  módulo de negocio y el resto de Notificaciones) no se construyen por adelantado.
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
- **No implementar todavía** reintentos, estado fallido, último error ni consulta de la cola
  por el Administrador: llegan en la semana 11, con la pieza 4.

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

Referencias actuales: Api → ControlAcceso, Notificaciones, Modulo.Metas · Tests → ControlAcceso.

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

**Módulo de negocio — la Meta y su máquina de estados** (definidas con el usuario para la
sección 1.6 de la Práctica 1; la tabla completa está en `docs/maquina-de-estados.md`):
- Una **Meta** agrupa uno o más hábitos de un usuario y se cumple al sumar un objetivo de
  cumplimientos (1 o más) antes de que termine el día de su fecha límite.
- Cuatro estados, declarados solo en `Entities/EstadoMeta.cs`: **Pendiente** (inicial),
  **En progreso**, **Completada** y **Fallida** (estas dos, terminales).
- Las transiciones se declaran solo en `Reglas/TransicionesMeta.cs`, y la entidad cambia de
  estado solo con `Meta.CambiarEstado`, que rechaza las no permitidas. Permitidas, todas
  ejecutadas por el sistema: Pendiente → En progreso (primer cumplimiento), En progreso →
  Completada (alcanza el objetivo a tiempo), En progreso → Fallida y Pendiente → Fallida
  (vence la fecha límite sin alcanzarlo). Prohibidas explícitas: Pendiente → Completada,
  En progreso → Pendiente, reabrir una meta terminada y cambiar su resultado.
- La Meta guarda solo el `UsuarioId` de su dueño, sin relación con las tablas de Control de
  acceso. `Habito`, `MetaHabito` e `HistorialEstadoMeta` (del diagrama) todavía no existen:
  se construyen cuando una práctica los pida.

**Pendiente: Docker.** El profesor anunció que el proyecto deberá correr con Docker en su
máquina, sin dar detalles todavía. No construir nada de Docker hasta que haya una indicación
concreta, pero evitar decisiones que lo dificulten: toda la configuración por variables de
entorno y nada atado a una máquina en particular. La autenticación de Windows en la cadena
de conexión (`Trusted_Connection`) no funciona dentro de un contenedor Linux; cuando llegue
Docker hará falta autenticación SQL, con las credenciales también por variable de entorno.

## 5. Flujo de trabajo acordado con el usuario

- Una rama `feature/<nombre-funcionalidad>` por funcionalidad, creada desde `main`
  actualizado.
- Avanza **un sub-paso a la vez**: propón el diseño o el cambio, espera la aprobación
  explícita del usuario y solo entonces impleméntalo. No generes código de un sub-paso no
  aprobado.
- Un commit por sub-paso aprobado. Convención del proyecto para el asunto:
  `tipo(alcance): descripción corta en imperativo [ID-DEL-REQUISITO]`, por ejemplo
  `feat(control-acceso): agrega hash de contraseña con sal [RF-CA-02, RD-05]`.
- Muestra siempre el comando git exacto antes de correrlo.
- Nunca publiques (`git push`) ni abras un pull request sin confirmación del usuario.
- Nunca hagas merge a `main` sin que el usuario lo confirme después de probar.
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
