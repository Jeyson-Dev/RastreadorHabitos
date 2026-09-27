# Rastreador de hábitos con metas: diseño de componentes

PROGRAMACIÓN III · TDS-007 · ITLA · 2026-C-3

Backend: C# / ASP.NET Core (.NET). Frontend: HTML, CSS y JavaScript. Base de datos: SQL Server.

## 1. Diagrama de componentes

```mermaid
flowchart LR
  subgraph NEGOCIO["MÓDULO DE NEGOCIO"]
    MOD["<b>Módulo Rastreador de Hábitos</b><br/><i>[C# Service]</i><br/>Hábitos diarios, rachas y metas con progreso<br/><br/><b>Entidades:</b><br/>CategoriaHabito, Habito, HabitoDia,<br/>RegistroDiario, Meta, MetaHabito,<br/>HistorialEstadoMeta<br/><br/><b>Estados de Meta:</b><br/>Pendiente → En progreso →<br/>Completada / Fallida"]
  end

  subgraph CORE["CORE · Servicios transversales"]
    DOC["<b>Manejador de documentos</b><br/><i>[C# Service]</i><br/>Subir, listar, borrado lógico<br/>(no lo usa este módulo)"]
    PERM["<b>Gestión de permisos</b><br/><i>[C# Service]</i><br/>Solicitud → Aprobación"]
    REP["<b>Reportes</b><br/><i>[C# Service]</i><br/>Agregación genérica, filtros por rol"]
    AUD["<b>Auditoría</b><br/><i>[C# Service]</i><br/>Quién hizo qué y cuándo"]
    NOTI["<b>Notificaciones</b><br/><i>[C# Service]</i><br/>Avisos internos por evento"]
    ACC["<b>Control de acceso</b><br/><i>[C# Service]</i><br/>Autentica, valida rol y permisos"]
  end

  MOD -->|"Consulta quién es, su rol y sus permisos"| ACC
  PERM -->|"Aplica el permiso aprobado"| ACC
  PERM -->|"Avisa al solicitante"| NOTI
  PERM -.->|"Registra transacciones de permisos"| AUD
  MOD -->|"Entrega métricas ya calculadas"| REP
  DOC -.->|"Registra subidas y borrados"| AUD
  MOD -->|"Dispara avisos de rachas y vencimientos"| NOTI
  MOD -.->|"Registra cumplimientos y cambios de hábitos"| AUD
  MOD ~~~ DOC
  MOD ~~~ PERM

  classDef core fill:#E6F2F0,stroke:#1F7A70,color:#0F3D3A
  classDef sinuso fill:#F1F3F3,stroke:#8A9694,stroke-dasharray: 4 3,color:#5B6664
  classDef negocio fill:#FCE9E1,stroke:#C0603A,color:#5A2A16
  class PERM,REP,AUD,NOTI,ACC core
  class DOC sinuso
  class MOD negocio

  style CORE fill:#F3F8F7,stroke:#9CC8C2,color:#0F3D3A
  style NEGOCIO fill:#FDF3EF,stroke:#C0603A,stroke-dasharray: 5 5,color:#5A2A16
```