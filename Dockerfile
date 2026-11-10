# Imagen de la aplicación (Práctica 4, sección 1.2). Dos etapas: el SDK compila y publica; la
# imagen final solo lleva el runtime de ASP.NET y lo publicado. Ningún secreto entra en la imagen:
# toda la configuración llega por variables de entorno desde .env (ver docker-compose.yml).

# --- Etapa 1: compilación ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /src

# Primero los proyectos, para que la restauración quede en caché mientras no cambien.
COPY src/Core/RastreadorHabitos.Core.Auditoria/RastreadorHabitos.Core.Auditoria.csproj src/Core/RastreadorHabitos.Core.Auditoria/
COPY src/Core/RastreadorHabitos.Core.ControlAcceso/RastreadorHabitos.Core.ControlAcceso.csproj src/Core/RastreadorHabitos.Core.ControlAcceso/
COPY src/Core/RastreadorHabitos.Core.Documentos/RastreadorHabitos.Core.Documentos.csproj src/Core/RastreadorHabitos.Core.Documentos/
COPY src/Core/RastreadorHabitos.Core.GestionPermisos/RastreadorHabitos.Core.GestionPermisos.csproj src/Core/RastreadorHabitos.Core.GestionPermisos/
COPY src/Core/RastreadorHabitos.Core.Notificaciones/RastreadorHabitos.Core.Notificaciones.csproj src/Core/RastreadorHabitos.Core.Notificaciones/
COPY src/Core/RastreadorHabitos.Core.Reportes/RastreadorHabitos.Core.Reportes.csproj src/Core/RastreadorHabitos.Core.Reportes/
COPY src/Negocio/RastreadorHabitos.Modulo.Habitos/RastreadorHabitos.Modulo.Habitos.csproj src/Negocio/RastreadorHabitos.Modulo.Habitos/
COPY src/Negocio/RastreadorHabitos.Modulo.Metas/RastreadorHabitos.Modulo.Metas.csproj src/Negocio/RastreadorHabitos.Modulo.Metas/
COPY src/Api/RastreadorHabitos.Api/RastreadorHabitos.Api.csproj src/Api/RastreadorHabitos.Api/
RUN dotnet restore src/Api/RastreadorHabitos.Api/RastreadorHabitos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/RastreadorHabitos.Api/RastreadorHabitos.Api.csproj \
    -c Release -o /app/publicado --no-restore /p:UseAppHost=false

# --- Etapa 2: ejecución ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=compilacion /app/publicado .

# Carpeta de los archivos de documentos: en la composición se monta encima un volumen con nombre
# [RF-DOC-05]. Se crea aquí con dueño el usuario sin privilegios de la imagen, para que el volumen
# nazca con permiso de escritura para la aplicación.
RUN mkdir -p /app/documentos && chown "$APP_UID" /app/documentos

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RastreadorHabitos.Api.dll"]
