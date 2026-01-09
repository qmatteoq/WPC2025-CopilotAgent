# GitHub Secrets Setup Helper
# This script helps you prepare the values needed for GitHub secrets
# DO NOT commit this file with actual secret values!

Write-Host "GitHub Secrets Configuration Helper" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# Check if running in the correct directory
if (-not (Test-Path "M365Agent/env/.env.dev")) {
    Write-Host "Error: Please run this script from the root of your project (travel-agent directory)" -ForegroundColor Red
    exit 1
}

Write-Host "Reading values from your existing environment files..." -ForegroundColor Yellow
Write-Host ""

# Read .env.dev file
$envDevContent = Get-Content "M365Agent/env/.env.dev" -Raw
$envDevUserContent = Get-Content "M365Agent/env/.env.dev.user" -Raw

# Parse environment variables
function Get-EnvValue {
    param(
        [string]$Content,
        [string]$Key
    )
    if ($Content -match "$Key=(.+)") {
        return $matches[1].Trim()
    }
    return $null
}

# Extract values
$subscriptionId = Get-EnvValue $envDevContent "AZURE_SUBSCRIPTION_ID"
$resourceGroup = Get-EnvValue $envDevContent "AZURE_RESOURCE_GROUP_NAME"
$botDomain = Get-EnvValue $envDevContent "BOT_DOMAIN"
$aadClientId = Get-EnvValue $envDevContent "AAD_APP_CLIENT_ID"

$azureOpenAIKey = Get-EnvValue $envDevUserContent "SECRET_AZURE_OPENAI_API_KEY"
$azureOpenAIEndpoint = Get-EnvValue $envDevUserContent "AZURE_OPENAI_ENDPOINT"
$azureOpenAIDeployment = Get-EnvValue $envDevUserContent "AZURE_OPENAI_DEPLOYMENT_NAME"

Write-Host "=== Step 1: Create Azure Service Principal ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Run this Azure CLI command to create a service principal:" -ForegroundColor Yellow
Write-Host ""
$spCommand = "az ad sp create-for-rbac --name `"github-actions-m365-agent`" --role contributor --scopes /subscriptions/$subscriptionId/resourceGroups/$resourceGroup --sdk-auth"
Write-Host $spCommand -ForegroundColor White
Write-Host ""
Write-Host "Copy the ENTIRE JSON output and use it as the AZURE_CREDENTIALS secret in GitHub." -ForegroundColor Yellow
Write-Host ""

Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Write-Host ""

Write-Host "=== Step 2: GitHub Secrets to Configure ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Go to your GitHub repository ? Settings ? Secrets and variables ? Actions" -ForegroundColor Yellow
Write-Host "Add the following secrets:" -ForegroundColor Yellow
Write-Host ""

$secrets = @{
    "AZURE_CREDENTIALS" = "(Use the JSON output from the az ad sp command above)"
    "AZURE_SUBSCRIPTION_ID" = $subscriptionId
    "AZURE_RESOURCE_GROUP_NAME" = $resourceGroup
    "AZURE_APP_SERVICE_NAME" = $botDomain.Split('.')[0]
    "AZURE_OPENAI_API_KEY" = $azureOpenAIKey
    "AZURE_OPENAI_ENDPOINT" = $azureOpenAIEndpoint
    "AZURE_OPENAI_DEPLOYMENT_NAME" = $azureOpenAIDeployment
    "AAD_APP_CLIENT_SECRET" = "(Get this from Azure Portal ? App Registrations ? Your App ? Certificates & secrets)"
}

foreach ($secret in $secrets.GetEnumerator()) {
    Write-Host "Secret Name: " -NoNewline -ForegroundColor Green
    Write-Host $secret.Key -ForegroundColor White
    Write-Host "Value: " -NoNewline -ForegroundColor Green
    
    if ($secret.Value -like "*(Get this from*" -or $secret.Value -like "*(Use the*") {
        Write-Host $secret.Value -ForegroundColor Yellow
    } else {
        Write-Host $secret.Value -ForegroundColor White
    }
    Write-Host ""
}

Write-Host "=== Step 3: Additional Information ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "AAD App Client ID: $aadClientId" -ForegroundColor White
Write-Host "You'll need to retrieve the client secret for this app from Azure Portal." -ForegroundColor Yellow
Write-Host ""
Write-Host "Steps to get AAD App Client Secret:" -ForegroundColor Yellow
Write-Host "1. Go to Azure Portal (https://portal.azure.com)" -ForegroundColor White
Write-Host "2. Navigate to: Azure Active Directory ? App registrations" -ForegroundColor White
Write-Host "3. Find your app with Client ID: $aadClientId" -ForegroundColor White
Write-Host "4. Go to: Certificates & secrets ? Client secrets" -ForegroundColor White
Write-Host "5. Either copy an existing secret or create a new one" -ForegroundColor White
Write-Host "6. Use that value for the AAD_APP_CLIENT_SECRET GitHub secret" -ForegroundColor White
Write-Host ""

Write-Host "=== Step 4: Verify Secrets ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "After adding all secrets, verify them in GitHub:" -ForegroundColor Yellow
Write-Host "GitHub Repository ? Settings ? Secrets and variables ? Actions" -ForegroundColor White
Write-Host "You should see all 8 secrets listed above." -ForegroundColor White
Write-Host ""

Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Add all the secrets to your GitHub repository" -ForegroundColor White
Write-Host "2. Commit and push the workflow file (.github/workflows/deploy-m365-agent.yml)" -ForegroundColor White
Write-Host "3. The workflow will run automatically on push to main branch" -ForegroundColor White
Write-Host "4. Or trigger it manually from the Actions tab" -ForegroundColor White
Write-Host ""
Write-Host "For detailed documentation, see: .github/DEPLOYMENT.md" -ForegroundColor Cyan
