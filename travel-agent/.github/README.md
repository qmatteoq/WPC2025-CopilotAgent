# GitHub Actions for M365 Agent Deployment

This directory contains GitHub Actions workflows and documentation for automating the deployment of the Microsoft 365 Travel Agent.

## Quick Start

1. **Review the workflow**: `.github/workflows/deploy-m365-agent.yml`
2. **Read the setup guide**: `.github/DEPLOYMENT.md`
3. **Configure secrets**: Run `.github/scripts/setup-secrets.ps1` to see what secrets you need

## Files

- **workflows/deploy-m365-agent.yml**: Main CI/CD workflow
- **DEPLOYMENT.md**: Comprehensive setup and troubleshooting guide
- **scripts/setup-secrets.ps1**: Helper script to extract secret values from your environment

## Quick Setup Checklist

- [ ] Run the setup script: `.\\.github\scripts\setup-secrets.ps1`
- [ ] Create Azure Service Principal for GitHub Actions
- [ ] Add all 8 required secrets to GitHub repository settings
- [ ] Commit and push the workflow file
- [ ] Test the workflow by triggering it manually

## GitHub Secrets Required

1. `AZURE_CREDENTIALS` - Azure Service Principal JSON
2. `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID
3. `AZURE_RESOURCE_GROUP_NAME` - Azure resource group name
4. `AZURE_APP_SERVICE_NAME` - Azure App Service name
5. `AZURE_OPENAI_API_KEY` - Azure OpenAI API key
6. `AZURE_OPENAI_ENDPOINT` - Azure OpenAI endpoint URL
7. `AZURE_OPENAI_DEPLOYMENT_NAME` - Azure OpenAI deployment name
8. `AAD_APP_CLIENT_SECRET` - AAD application client secret

## Triggering Deployments

### Automatic
The workflow automatically runs when code is pushed to `main` branch in these directories:
- `TravelAgent/**`
- `M365Agent/**`
- `TravelAgent.ServiceDefaults/**`
- `Flights.Api/**`

### Manual
1. Go to the **Actions** tab in your GitHub repository
2. Select "Build and Deploy M365 Agent"
3. Click **Run workflow**
4. Select the environment (dev/prod)
5. Click **Run workflow** button

## Need Help?

- See `DEPLOYMENT.md` for detailed documentation
- Check the GitHub Actions logs for build/deployment issues
- Review Azure App Service logs for runtime issues
