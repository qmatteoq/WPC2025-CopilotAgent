# MCP Inspector OAuth 2.0 - Quick Reference Card

## Common Issues & Solutions

### ?? Getting 401 Unauthorized?

#### Check #1: Redirect URI Mismatch
- **Azure AD**: Go to your **client app** ? **Authentication** ? Check redirect URI
- **MCP Inspector**: Must match exactly (including http/https, port, path)
- **Platform Type**: Must be **Single-page application (SPA)** in Azure AD

#### Check #2: Wrong Scope Format
- **Correct**: `api://{API_CLIENT_ID}/access_as_user`
- **Common Mistake**: Using the client app's ID instead of the API's ID
- **How to verify**: 
  1. Get your token from MCP Inspector
  2. Go to [jwt.ms](https://jwt.ms) and paste it
  3. Check the `aud` claim - it should be your API's client ID
  4. Check the `scp` claim - it should include `access_as_user`

#### Check #3: Missing Admin Consent
- **Azure AD**: Client app ? **API permissions** ? Click **"Grant admin consent"**
- **Status**: All permissions should show green checkmark under "Status"

#### Check #4: Public Client Flow Disabled
- **Azure AD**: Client app ? **Authentication** ? **Advanced settings**
- **Allow public client flows**: Should be **Yes**

---

## Configuration Checklist

### In Azure Portal

#### API App Registration (Flights API)
- ? App registered
- ? "Expose an API" configured
- ? Scope `access_as_user` created
- ? Application ID URI set (usually `api://{client-id}`)
- ? Copy Client ID, Tenant ID, Domain

#### Client App Registration (MCP Inspector Client)
- ? App registered with correct redirect URI
- ? Redirect URI configured as **SPA** platform type
- ? Public client flows enabled
- ? API permission added for Flights API ? `access_as_user`
- ? **Admin consent granted** ?? CRITICAL
- ? Copy Client ID

### In appsettings.json (Flights API)
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_API_CLIENT_ID",  ?? API's Client ID
    "Audience": "api://YOUR_API_CLIENT_ID"
  }
}
```

### In MCP Inspector OAuth Configuration

| Setting | Value |
|---------|-------|
| **Auth URL** | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize` |
| **Token URL** | `https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token` |
| **Client ID** | Client ID from **MCP Inspector Client** app ?? |
| **Client Secret** | _(Leave blank for PKCE)_ |
| **Redirect URI** | Must match Azure AD exactly |
| **Scope** | `api://{API_CLIENT_ID}/access_as_user` ?? Use **API's Client ID** |

---

## Quick Test Procedure

### 1. Verify Token (Before Testing API)
```bash
# After authenticating in MCP Inspector, copy the access token
# Go to https://jwt.ms and paste it
# Verify these claims:
```
- ? `aud`: Should be your **API's client ID**
- ? `iss`: Should be `https://login.microsoftonline.com/{TENANT_ID}/v2.0`
- ? `scp`: Should include `access_as_user`
- ? `exp`: Should be in the future (not expired)

### 2. Test with curl (Alternative)
```bash
# Get token manually
curl -X POST https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id={CLIENT_ID}" \
  -d "scope=api://{API_CLIENT_ID}/access_as_user" \
  -d "grant_type=client_credentials" \
  -d "client_secret={CLIENT_SECRET}"

# Test API endpoint
curl -X GET https://localhost:7277/flights/search?origin=SEA \
  -H "Authorization: Bearer {ACCESS_TOKEN}"
```

### 3. Check API Logs
If you're getting 401, check the API console output for authentication details.

---

## Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `AADSTS65001` | User/admin has not consented | Grant admin consent in Azure AD |
| `AADSTS500011` | Resource principal not found | Wrong scope format - check API client ID |
| `AADSTS7000218` | Invalid client secret | Remove client secret (use PKCE) or create new one |
| `Invalid redirect URI` | Redirect URI mismatch | Match exactly in Azure AD and MCP Inspector |
| `401 Unauthorized` at API | Token audience mismatch | Verify `aud` claim at jwt.ms |
| `403 Forbidden` | Missing scope in token | Check `scp` claim, re-grant permissions |

---

## Key Differences: API vs Client

| Aspect | API App (Flights API) | Client App (MCP Inspector) |
|--------|----------------------|---------------------------|
| **Purpose** | Protects your API resources | Calls your API on behalf of users |
| **Client ID Usage** | Used in `appsettings.json` and scope URI | Used in MCP Inspector config |
| **Permissions** | Exposes API scopes | Requests API permissions |
| **Redirect URI** | Not needed | Required (must match exactly) |
| **Client Secret** | Not needed | Optional (PKCE recommended) |

---

## Need More Help?

1. **Decode your token**: Go to [jwt.ms](https://jwt.ms) - this shows exactly what's in your token
2. **Check API logs**: Look for authentication failures in console output
3. **Verify Azure AD config**: 
   - API app ? Expose an API ? Verify scope exists
   - Client app ? API permissions ? Verify consent granted
4. **Check full documentation**: See `ENTRA_ID_SETUP.md` for detailed walkthrough

---

## The Most Common Mistake ??

**Using the wrong Client ID in the scope!**

```
? WRONG:  api://{MCP_INSPECTOR_CLIENT_ID}/access_as_user
? CORRECT: api://{API_CLIENT_ID}/access_as_user
```

The scope should always reference the **API you're trying to access** (Flights API), not the client app making the request!
