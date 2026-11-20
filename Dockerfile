# syntax=docker/dockerfile:1.7
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.403-noble
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.14-noble
FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY disaster-platform-web.csproj ./
RUN dotnet restore disaster-platform-web.csproj
COPY . .
RUN dotnet publish disaster-platform-web.csproj -c Release --no-restore -o /out /p:UseAppHost=false
FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080 8443
USER $APP_UID
ENTRYPOINT ["dotnet", "disaster-platform-web.dll"]
