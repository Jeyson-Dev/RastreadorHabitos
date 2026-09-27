# RastreadorHabitos

Rastreador de hábitos con metas — Programación III · TDS-007 · ITLA · 2026-C-3

## Stack
- Backend: C# / ASP.NET Core (.NET)
- Frontend: HTML, CSS y JavaScript
- Base de datos: SQL Server

## Requisitos previos
- .NET SDK (8.0 o superior)
- SQL Server (local o instancia accesible)
- Visual Studio 2022 (recomendado) o VS Code

## Clonar el repositorio

```bash
git clone https://github.com/Jeyson-Dev/RastreadorHabitos.git
cd RastreadorHabitos
```

## Restaurar dependencias

```bash
dotnet restore
```

## Configurar la base de datos

1. Copia `appsettings.json` (o crea `appsettings.Development.json`) y ajusta la cadena de conexión a tu instancia de SQL Server.
2. Aplica las migraciones (si el proyecto ya las tiene):

```bash
dotnet ef database update
```

## Ejecutar el proyecto

```bash
dotnet build
dotnet run --project RastreadorHabitos
```

La aplicación quedará disponible en la URL que indique la consola (por defecto `https://localhost:5001` o similar).

## Estructura del proyecto

Ver [`docs/estructura-proyecto.md`](docs/estructura-proyecto.md) para el diagrama de componentes y la descripción de cada módulo.
