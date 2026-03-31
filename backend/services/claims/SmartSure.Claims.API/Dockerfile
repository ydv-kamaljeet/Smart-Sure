FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/shared/ shared/
COPY backend/services/claims/ services/claims/

RUN dotnet restore services/claims/SmartSure.Claims.API/SmartSure.Claims.API.csproj

RUN dotnet publish services/claims/SmartSure.Claims.API/SmartSure.Claims.API.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartSure.Claims.API.dll"]
