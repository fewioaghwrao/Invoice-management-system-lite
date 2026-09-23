FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ ./backend/

RUN dotnet restore backend/InvoiceSystem.Batch/InvoiceSystem.Batch.csproj

RUN dotnet publish \
    backend/InvoiceSystem.Batch/InvoiceSystem.Batch.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InvoiceSystem.Batch.dll"]