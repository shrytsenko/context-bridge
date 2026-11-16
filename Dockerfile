FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ContextBridge.slnx ./
COPY src/ContextBridge.Api/ContextBridge.Api.csproj src/ContextBridge.Api/
COPY src/ContextBridge.Application/ContextBridge.Application.csproj src/ContextBridge.Application/
COPY src/ContextBridge.Domain/ContextBridge.Domain.csproj src/ContextBridge.Domain/
COPY src/ContextBridge.Infrastructure/ContextBridge.Infrastructure.csproj src/ContextBridge.Infrastructure/
RUN dotnet restore src/ContextBridge.Api/ContextBridge.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/ContextBridge.Api/ContextBridge.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ContextBridge.Api.dll"]
