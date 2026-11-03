# Stage 1: build with the full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/CompanyEnrichment.Api -c Release -o /app

# Stage 2: run with the smaller ASP.NET runtime only
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Run as the built-in non-root user
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyEnrichment.Api.dll"]
