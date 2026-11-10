# Multi-stage build: the large SDK image compiles the site, and only the small ASP.NET
# runtime image ships. Build from the Part3 folder:
#   docker build -t raceday-web ./RaceDay.Web

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on its own layer, so packages are cached until the project file changes.
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The site listens on port 8080 inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

# Run as the non-root user built into the official .NET images.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
