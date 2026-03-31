FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/shared/ shared/
COPY backend/services/policy/ services/policy/

RUN dotnet restore services/policy/SmartSure.Policy.API/SmartSure.Policy.API.csproj

RUN dotnet publish services/policy/SmartSure.Policy.API/SmartSure.Policy.API.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartSure.Policy.API.dll"]
