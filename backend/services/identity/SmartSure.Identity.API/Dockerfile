FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/shared/ shared/
COPY backend/services/identity/ services/identity/

RUN dotnet restore services/identity/SmartSure.Identity.API/SmartSure.Identity.API.csproj

RUN dotnet publish services/identity/SmartSure.Identity.API/SmartSure.Identity.API.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartSure.Identity.API.dll"]
