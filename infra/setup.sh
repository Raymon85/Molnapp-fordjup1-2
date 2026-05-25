#!/usr/bin/env bash
# CloudSoft Recruitment Portal — Azure Infrastructure Setup
# Usage: ./infra/setup.sh
# Prerequisites: az CLI logged in, subscription set

set -euo pipefail

# ── Variables ────────────────────────────────────────────────────────────────
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-cloudsoft-recruitment}"
LOCATION="${LOCATION:-swedencentral}"
ACR_NAME="${ACR_NAME:-acrcloudsoft$RANDOM}"
CONTAINERAPPS_ENV="${CONTAINERAPPS_ENV:-cae-cloudsoft}"
CONTAINER_APP_NAME="${CONTAINER_APP_NAME:-ca-recruitment-portal}"
SQL_SERVER_NAME="${SQL_SERVER_NAME:-sql-cloudsoft-$RANDOM}"
SQL_DB_NAME="${SQL_DB_NAME:-RecruitmentPortal}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-sqladmin}"
STORAGE_ACCOUNT="${STORAGE_ACCOUNT:-stcloudsoft$RANDOM}"
BLOB_CONTAINER="${BLOB_CONTAINER:-cv-uploads}"
LOG_ANALYTICS_WORKSPACE="${LOG_ANALYTICS_WORKSPACE:-law-cloudsoft}"
APP_INSIGHTS_NAME="${APP_INSIGHTS_NAME:-ai-cloudsoft}"

echo "==> Creating resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"

# ── Container Registry ────────────────────────────────────────────────────────
echo "==> Creating Azure Container Registry: $ACR_NAME"
az acr create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$ACR_NAME" \
  --sku Basic \
  --admin-enabled false

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
echo "ACR login server: $ACR_LOGIN_SERVER"

# ── Log Analytics + Application Insights ─────────────────────────────────────
echo "==> Creating Log Analytics workspace"
az monitor log-analytics workspace create \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
  --location "$LOCATION"

LAW_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
  --query customerId -o tsv)

LAW_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
  --query primarySharedKey -o tsv)

echo "==> Creating Application Insights"
az monitor app-insights component create \
  --app "$APP_INSIGHTS_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --workspace "$LOG_ANALYTICS_WORKSPACE"

AI_CONN_STRING=$(az monitor app-insights component show \
  --app "$APP_INSIGHTS_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query connectionString -o tsv)

# ── SQL Server ────────────────────────────────────────────────────────────────
echo "==> Creating Azure SQL Server"
SQL_ADMIN_PASSWORD=$(openssl rand -base64 20)
az sql server create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$SQL_SERVER_NAME" \
  --location "$LOCATION" \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASSWORD"

az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

az sql db create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name "$SQL_DB_NAME" \
  --service-objective Basic

SQL_CONN_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Database=${SQL_DB_NAME};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;"

# ── Storage Account ───────────────────────────────────────────────────────────
echo "==> Creating Storage Account"
az storage account create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$STORAGE_ACCOUNT" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --allow-blob-public-access false \
  --https-only true

STORAGE_URI="https://${STORAGE_ACCOUNT}.blob.core.windows.net"

az storage container create \
  --account-name "$STORAGE_ACCOUNT" \
  --name "$BLOB_CONTAINER" \
  --auth-mode login

# ── Container Apps Environment ────────────────────────────────────────────────
echo "==> Creating Container Apps Environment"
az containerapp env create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINERAPPS_ENV" \
  --location "$LOCATION" \
  --logs-workspace-id "$LAW_ID" \
  --logs-workspace-key "$LAW_KEY"

# ── Container App (initial deploy with placeholder) ──────────────────────────
echo "==> Creating Container App"
az containerapp create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER_APP_NAME" \
  --environment "$CONTAINERAPPS_ENV" \
  --image "mcr.microsoft.com/dotnet/samples:aspnetapp" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --system-assigned \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ConnectionStrings__DefaultConnection=secretref:sql-connection-string" \
    "BlobStorage__AccountUri=${STORAGE_URI}" \
    "BlobStorage__ContainerName=${BLOB_CONTAINER}" \
    "ApplicationInsights__ConnectionString=${AI_CONN_STRING}" \
    "ApiKey=secretref:api-key" \
    "Seed__AdminEmail=secretref:seed-admin-email" \
    "Seed__AdminPassword=secretref:seed-admin-password" \
  --secrets \
    "sql-connection-string=${SQL_CONN_STRING}" \
    "api-key=$(openssl rand -hex 32)" \
    "seed-admin-email=admin@cloudsoft.com" \
    "seed-admin-password=$(openssl rand -base64 16)@A1" \
  --readiness-probe-path "/healthz" \
  --readiness-probe-period-seconds 15 \
  --readiness-probe-failure-threshold 3

# ── Assign Managed Identity roles ────────────────────────────────────────────
APP_PRINCIPAL_ID=$(az containerapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER_APP_NAME" \
  --query identity.principalId -o tsv)

STORAGE_ID=$(az storage account show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$STORAGE_ACCOUNT" \
  --query id -o tsv)

echo "==> Assigning Storage Blob Data Contributor role to Container App"
az role assignment create \
  --assignee "$APP_PRINCIPAL_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "$STORAGE_ID"

# ── Grant ACR pull access to Container App ─────────────────────────────────
ACR_ID=$(az acr show --name "$ACR_NAME" --query id -o tsv)
az role assignment create \
  --assignee "$APP_PRINCIPAL_ID" \
  --role "AcrPull" \
  --scope "$ACR_ID"

# ── Output ────────────────────────────────────────────────────────────────────
APP_URL=$(az containerapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$CONTAINER_APP_NAME" \
  --query properties.configuration.ingress.fqdn -o tsv)

echo ""
echo "=== Setup Complete ==="
echo "Resource Group:    $RESOURCE_GROUP"
echo "ACR Login Server:  $ACR_LOGIN_SERVER"
echo "Container App URL: https://$APP_URL"
echo "Storage Account:   $STORAGE_ACCOUNT"
echo ""
echo "GitHub Actions secrets to set:"
echo "  AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID  (from federated credential)"
echo "  ACR_LOGIN_SERVER = $ACR_LOGIN_SERVER"
echo "  RESOURCE_GROUP   = $RESOURCE_GROUP"
echo "  CONTAINER_APP    = $CONTAINER_APP_NAME"
