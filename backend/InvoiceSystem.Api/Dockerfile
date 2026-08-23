FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY backend/InvoiceSystem.Api/InvoiceSystem.Api.csproj backend/InvoiceSystem.Api/
COPY backend/InvoiceSystem.Application/InvoiceSystem.Application.csproj backend/InvoiceSystem.Application/
COPY backend/InvoiceSystem.Domain/InvoiceSystem.Domain.csproj backend/InvoiceSystem.Domain/
COPY backend/InvoiceSystem.Infrastructure/InvoiceSystem.Infrastructure.csproj backend/InvoiceSystem.Infrastructure/

RUN dotnet restore backend/InvoiceSystem.Api/InvoiceSystem.Api.csproj

COPY . .

RUN dotnet publish backend/InvoiceSystem.Api/InvoiceSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InvoiceSystem.Api.dll"]