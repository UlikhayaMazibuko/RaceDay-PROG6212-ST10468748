# The SDK image builds the app and then only the smaller runtime image is used to run it
# Build it from the Part3 folder: docker build -t raceday-web ./RaceDay.Web

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore is done first so Docker can reuse it if only the code changes
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The app runs on port 8080 inside the container
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

# Runs as a normal user instead of root
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
