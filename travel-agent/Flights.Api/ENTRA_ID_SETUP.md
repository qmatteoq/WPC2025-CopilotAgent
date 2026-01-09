# Microsoft Entra ID Authentication Setup Guide

This guide walks you through configuring Microsoft Entra ID (formerly Azure AD) authentication for the Flights API.

## Prerequisites

- An Azure subscription
- Access to Azure Portal (https://portal.azure.com)
- Azure CLI (optional, for command-line setup)

## Step 1: Register the Application in Azure Portal

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Microsoft Entra ID** (formerly Azure Active Directory)
3. Select **App registrations** from the left menu
4. Click **+ New registration**

### Registration Details

- **Name**: `Flights API` (or your preferred name)
- **Supported account types**: 
  - Select **Accounts in this organizational directory only** (for single tenant)
  - Or select **Accounts in any organizational directory** (for multi-tenant)
- **Redirect URI**: Leave blank for API-only applications
- Click **Register**

## Step 2: Configure API Permissions

1. In your app registration, go to **Expose an API**
2. Click **+ Add a scope**
3. Accept the default Application ID URI (e.g., `api://{client-id}`) or customize it
4. Click **Save and continue**

### Create API Scopes

Add the following scope:

- **Scope name**: `access_as_user`
- **Who can consent**: Admins and users
- **Admin consent display name**: Access Flights API
- **Admin consent description**: Allows the app to access the Flights API on behalf of the signed-in user
- **User consent display name**: Access Flights API
- **User consent description**: Allows the app to access the Flights API on your behalf
- **State**: Enabled
- Click **Add scope**

## Step 3: Get Configuration Values

From your app registration overview page, collect these values:

1. **Application (client) ID** - Copy this value
2. **Directory (tenant) ID** - Copy this value
3. **Domain** - Usually `yourdomain.onmicrosoft.com`

## Step 4: Update appsettings.json

Update the `appsettings.json` file in the Flights.Api project with your values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_DOMAIN.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "Audience": "api://YOUR_CLIENT_ID"
  }
}
```

Replace:
- `YOUR_DOMAIN` with your Azure AD domain
- `YOUR_TENANT_ID` with the Directory (tenant) ID
- `YOUR_CLIENT_ID` with the Application (client) ID

## Step 5: Create a Client Application (For Testing)

To test the API, you need a client application to obtain tokens:

### Option A: Use Azure CLI

```bash
# Login to Azure
az login

# Get an access token
az account get-access-token --resource api://YOUR_CLIENT_ID
```

### Option B: Register a Client App in Azure Portal (for MCP Inspector or other clients)

1. Go back to **App registrations** and create another app registration
2. **Name**: `Flights API Client` (or `MCP Inspector`)
3. **Supported account types**: Same as your API registration
4. **Redirect URI**: 
   - Select **Single-page application (SPA)** from the dropdown
   - For MCP Inspector, add the redirect URL provided by the tool (usually `http://localhost:3000/callback` or similar)
   - You can also add `http://localhost:3000` for local testing
5. Click **Register**

### Configure API Permissions

1. In the client app registration, go to **API permissions**
2. Click **+ Add a permission**
3. Select **My APIs** tab
4. Select **Flights API** (the API you registered earlier)
5. Select **Delegated permissions**
6. Check `access_as_user`
7. Click **Add permissions**
8. Click **Grant admin consent for [Your Organization]** (if you have admin rights, otherwise request admin consent)

### Optional: Get Client Secret (for confidential clients only)

**Note**: For MCP Inspector using the Authorization Code Flow with PKCE, you typically **don't need a client secret**. However, if you need one:

1. Go to **Certificates & secrets**
2. Click **+ New client secret**
3. Add a description and expiration
4. Click **Add**
5. **Copy the secret value immediately** (it won't be shown again)

### Configure MCP Inspector OAuth Settings

In the MCP Inspector OAuth 2.0 configuration, use these values:

- **Auth URL**: `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize`
- **Token URL**: `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token`
- **Client ID**: The Application (client) ID from your **client app** registration (not the API)
- **Client Secret**: Leave blank if using PKCE, otherwise use the secret from above
- **Redirect URL**: Must match exactly what you configured in Azure AD (e.g., `http://localhost:3000/callback`)
- **Scope**: `api://{API_CLIENT_ID}/access_as_user` (use the API's client ID here)

**Important**: The scope must be the **full scope URI** including the `api://` prefix and your **API's client ID** (not the client app's ID)

## Step 6: Test the API

### Using Postman or curl

1. First, get an access token using OAuth 2.0 authorization code flow or client credentials flow

For **Client Credentials Flow** (app-to-app):

```bash
curl -X POST https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id={CLIENT_ID}" \
  -d "client_secret={CLIENT_SECRET}" \
  -d "scope=api://{API_CLIENT_ID}/.default" \
  -d "grant_type=client_credentials"
```

2. Use the access token in your API requests:

```bash
curl -X GET https://localhost:7000/flights/search?origin=SEA \
  -H "Authorization: Bearer {ACCESS_TOKEN}"
```

## Step 7: Update Client Applications

Any client application (web app, mobile app, etc.) that calls this API needs to:

1. Be registered in Azure AD
2. Have API permissions to call the Flights API
3. Obtain an access token before making API calls
4. Include the token in the `Authorization: Bearer {token}` header

## Security Best Practices

1. **Use HTTPS in production** - Uncomment `app.UseHttpsRedirection()` in Program.cs
2. **Store secrets securely** - Use Azure Key Vault or User Secrets for local development
3. **Implement token validation** - The Microsoft.Identity.Web library handles this automatically
4. **Use scopes** - Implement fine-grained permissions based on different scopes
5. **Monitor and log** - Enable logging for authentication failures
6. **Regular key rotation** - Rotate client secrets and certificates regularly

## For Local Development

Use User Secrets to avoid committing sensitive data:

```bash
cd Flights.Api
dotnet user-secrets init
dotnet user-secrets set "AzureAd:Domain" "yourdomain.onmicrosoft.com"
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
```

## Common Issues

### 401 Unauthorized with MCP Inspector

If you're getting 401 Unauthorized when using MCP Inspector with OAuth 2.0 flow:

1. **Check the Redirect URI**:
   - The redirect URI in MCP Inspector must **exactly match** what's configured in Azure AD
   - In Azure AD, go to your client app ? **Authentication** ? Check the redirect URI
   - Make sure it's registered as a **Single-page application (SPA)** platform type

2. **Verify the Scope Format**:
   - The scope must be: `api://{API_CLIENT_ID}/access_as_user`
   - Use your **API's client ID** (not the client app's ID)
   - Example: `api://12345678-1234-1234-1234-123456789abc/access_as_user`

3. **Check Token Audience**:
   - After getting a token, decode it at [jwt.ms](https://jwt.ms)
   - Check the `aud` (audience) claim - it should match your API's client ID
   - Check the `scp` (scopes) claim - it should include `access_as_user`

4. **Grant Admin Consent**:
   - In Azure AD, go to your client app ? **API permissions**
   - Click **Grant admin consent for [Your Organization]**
   - This is required before users can use the app

5. **Enable Public Client Flow** (if needed):
   - In your client app registration, go to **Authentication**
   - Under **Advanced settings** ? **Allow public client flows** ? Set to **Yes**
   - This is needed for some OAuth flows

6. **Check CORS Settings**:
   - If testing from a browser, ensure the API's CORS policy allows your origin
   - The current configuration allows any origin, but verify it's working

### 401 Unauthorized (General)
- Verify the token is included in the Authorization header
- Check that the token hasn't expired
- Ensure the audience (`aud`) claim matches your API's client ID

### 403 Forbidden
- Check that the required scopes are present in the token
- Verify API permissions are granted in Azure AD

### Invalid Token
- Ensure the token is obtained for the correct audience
- Verify the tenant ID and client ID are correct

## Additional Resources

- [Microsoft Identity Platform Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web)
- [Azure AD Authentication Scenarios](https://docs.microsoft.com/en-us/azure/active-directory/develop/authentication-scenarios)

---

## Appendix: Complete MCP Inspector Setup Walkthrough

This section provides a complete step-by-step guide for configuring OAuth 2.0 with MCP Inspector.

### Prerequisites

1. You've completed Steps 1-4 above (API registered, scope created, configuration values obtained)
2. You have MCP Inspector running and accessible
3. You have the redirect URL from MCP Inspector (check the OAuth settings page)

### Step 1: Register Client Application

1. Go to [Azure Portal](https://portal.azure.com) ? **Microsoft Entra ID** ? **App registrations**
2. Click **+ New registration**
3. Configure:
   - **Name**: `MCP Inspector Client`
   - **Supported account types**: Same as your API (usually "Accounts in this organizational directory only")
   - **Redirect URI**: 
     - Platform: **Single-page application (SPA)**
     - URI: The redirect URL from MCP Inspector (e.g., `http://localhost:3000/callback`)
4. Click **Register**
5. **Save the Application (client) ID** - you'll need this for MCP Inspector

### Step 2: Configure Authentication Settings

1. In your new client app, go to **Authentication**
2. Under **Single-page application** section, verify your redirect URI is listed
3. Under **Advanced settings**:
   - **Allow public client flows**: Set to **Yes**
   - **Enable the following mobile and desktop flows**: Enable if needed
4. Click **Save**

### Step 3: Add API Permissions

1. In your client app, go to **API permissions**
2. Click **+ Add a permission**
3. Select **My APIs** tab
4. Find and click on **Flights API** (your API registration from earlier)
5. Select **Delegated permissions**
6. Check the box next to `access_as_user`
7. Click **Add permissions**
8. **Important**: Click **Grant admin consent for [Your Organization]**
   - This button appears at the top of the permissions list
   - You need admin privileges to do this
   - If you don't have admin privileges, request consent from your Azure AD admin

### Step 4: Configure MCP Inspector

1. Open MCP Inspector
2. Navigate to the OAuth 2.0 configuration section
3. Enter the following values:

   | Field | Value | Example |
   |-------|-------|---------|
   | **Authorization Endpoint** | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize` | `https://login.microsoftonline.com/12345678.../oauth2/v2.0/authorize` |
   | **Token Endpoint** | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token` | `https://login.microsoftonline.com/12345678.../oauth2/v2.0/token` |
   | **Client ID** | Client ID from your **MCP Inspector Client** app | `87654321-4321-4321-4321-123456789abc` |
   | **Client Secret** | Leave blank (using PKCE) | _(empty)_ |
   | **Redirect URI** | Must match Azure AD configuration | `http://localhost:3000/callback` |
   | **Scope** | `api://{API_CLIENT_ID}/access_as_user` | `api://12345678-1234-1234-1234-123456789abc/access_as_user` |

   **Critical**: For the **Scope** field, use your **API's client ID** (from the Flights API registration), not the MCP Inspector Client ID.

4. Save the OAuth configuration

### Step 5: Test the Authentication Flow

1. In MCP Inspector, configure the MCP server endpoint: `https://localhost:7277/mcp`
2. Click the **Authenticate** or **Login** button
3. You should be redirected to Microsoft login page
4. Sign in with your Azure AD credentials
5. If prompted, consent to the permissions (you may need admin consent)
6. You should be redirected back to MCP Inspector with a valid token
7. Try calling one of the MCP tools - it should work!

### Step 6: Verify Token (Optional but Recommended)

1. Copy the access token from MCP Inspector (usually visible in dev tools or OAuth debug section)
2. Go to [jwt.ms](https://jwt.ms) and paste the token
3. Verify the following claims:
   - `aud` (audience): Should match your **API's client ID**
   - `iss` (issuer): Should be `https://login.microsoftonline.com/{TENANT_ID}/v2.0`
   - `scp` (scopes): Should include `access_as_user`
   - `exp` (expiration): Should be in the future

### Troubleshooting This Setup

**"Invalid redirect URI" error during login**
- Ensure the redirect URI in Azure AD exactly matches what MCP Inspector is using
- Check for trailing slashes, http vs https, port numbers

**"AADSTS65001: The user or administrator has not consented" error**
- Go back to Step 3 and click "Grant admin consent"
- If you're not an admin, ask your Azure AD administrator to grant consent

**"401 Unauthorized" when calling MCP endpoint**
- Decode your token at jwt.ms and verify the `aud` claim matches your API's client ID
- Verify the scope in your token includes `access_as_user`
- Check that the API's `appsettings.json` has the correct `ClientId` configured

**Token obtained but still getting 401**
- Check that the token is being sent in the `Authorization: Bearer {token}` header
- Verify your API's authentication middleware is configured correctly
- Check the API logs for specific authentication failures

**"AADSTS500011: The resource principal named api://..." error**
- This means the scope URI is incorrect
- Double-check you're using the **API's client ID** in the scope, not the client app's ID
- Verify the scope format: `api://{API_CLIENT_ID}/access_as_user`
