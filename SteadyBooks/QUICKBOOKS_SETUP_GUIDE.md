# QuickBooks OAuth Setup Guide ??

## ? Infrastructure Ready!

I've set up the foundation for QuickBooks OAuth integration. Here's what's been created:

### Files Created:
1. ? `Models/QuickBooksConnection.cs` - Database model
2. ? `Models/QuickBooksSettings.cs` - Configuration model
3. ? Updated `ApplicationDbContext.cs` - Database relationships
4. ? Updated `appsettings.json` - Configuration placeholders
5. ? Updated `appsettings.Development.json` - Dev configuration

---

## ?? STEP 1: Get Your Intuit Credentials

### Go to Intuit Developer Portal:
**URL:** https://developer.intuit.com

### Registration Checklist:
- [ ] Sign in / Create account
- [ ] Click "Create an app"
- [ ] Select "QuickBooks Online and Payments"
- [ ] Name your app: `SteadyBooks`
- [ ] Description: `White-labeled financial dashboards for accountants`
- [ ] Click Create

### Get Your Credentials:
- [ ] Go to "Keys & credentials" tab
- [ ] Copy **Client ID** (looks like: `ABxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)
- [ ] Copy **Client Secret** (long string)

### Configure Redirect URIs:
- [ ] In "Keys & credentials", find "Redirect URIs"
- [ ] Add: `https://localhost:7189/QuickBooks/Callback`
- [ ] Add: `http://localhost:5271/QuickBooks/Callback`
- [ ] Click Save

### Select Scopes:
- [ ] Go to "Scopes" section
- [ ] Check: ?? **Accounting** (`com.intuit.quickbooks.accounting`)
- [ ] Save

---

## ?? STEP 2: Add Your Credentials to the App

Once you have your **Client ID** and **Client Secret**, you need to add them to two files:

### File 1: `appsettings.Development.json`
```json
"QuickBooks": {
  "ClientId": "PASTE_YOUR_CLIENT_ID_HERE",
  "ClientSecret": "PASTE_YOUR_CLIENT_SECRET_HERE",
  "Environment": "Sandbox",
  "RedirectUri": "https://localhost:7189/QuickBooks/Callback",
  "Scopes": "com.intuit.quickbooks.accounting"
}
```

### File 2: `appsettings.json`
```json
"QuickBooks": {
  "ClientId": "PASTE_YOUR_CLIENT_ID_HERE",
  "ClientSecret": "PASTE_YOUR_CLIENT_SECRET_HERE",
  "Environment": "Sandbox",
  "RedirectUri": "https://localhost:7189/QuickBooks/Callback",
  "Scopes": "com.intuit.quickbooks.accounting"
}
```

**Important:** 
- Replace `YOUR_CLIENT_ID_HERE` with your actual Client ID
- Replace `YOUR_CLIENT_SECRET_HERE` with your actual Client Secret
- Keep `Environment` as `"Sandbox"` for testing

---

## ?? STEP 3: Once You Have Credentials

**Tell me when you have your credentials ready**, and I'll:

1. ? Create the database migration
2. ? Build the OAuth service
3. ? Create the Connect QuickBooks page
4. ? Build the OAuth callback handler
5. ? Add connection status display
6. ? Implement token refresh logic
7. ? Update dashboard configuration to show QuickBooks connection

---

## ?? What QuickBooks Integration Will Enable:

Once connected, your dashboards will be able to:

### ? Pull Real Financial Data:
- Cash balance from bank accounts
- Profit & Loss calculations
- Tax liability estimates
- Outstanding invoices
- Revenue and expenses

### ? Keep Data Fresh:
- Auto-refresh tokens (valid for 100 days)
- Sync data on demand
- Background sync every 24 hours

### ? Account Mapping:
- Map QuickBooks accounts to dashboard metrics
- Customize which accounts contribute to each metric
- Filter by account types

---

## ?? Security Features:

- ? OAuth 2.0 (industry standard)
- ? Tokens encrypted in database
- ? Automatic token refresh
- ? Secure HTTPS redirect
- ? Per-dashboard connection (multi-tenant safe)

---

## ?? Quick Reference:

### Intuit URLs:
- **Developer Portal:** https://developer.intuit.com
- **Documentation:** https://developer.intuit.com/app/developer/qbo/docs/get-started
- **OAuth Guide:** https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization

### OAuth Flow:
```
1. User clicks "Connect QuickBooks"
2. Redirect to Intuit authorization page
3. User logs into QuickBooks
4. User grants permission
5. Intuit redirects back to your app
6. Your app exchanges code for tokens
7. Tokens stored (encrypted)
8. Dashboard now connected!
```

---

## ?? Estimated Time:

- **Intuit Registration:** 5-10 minutes
- **Add Credentials:** 2 minutes
- **Build OAuth Pages:** 30-40 minutes (I'll do this)
- **Test Connection:** 5 minutes

**Total:** ~45-60 minutes to complete Screen 4!

---

## ?? Current Status:

? Database models created  
? Configuration structure ready  
? Settings files prepared  
? **Waiting for your Intuit credentials**  
? Then I'll build the OAuth flow  
? Then I'll build the connection UI  

---

## ?? Need Help?

If you get stuck during registration:
- Check: https://developer.intuit.com/app/developer/qbo/docs/get-started
- Common issues:
  - Wrong redirect URI (must match exactly)
  - Missing scopes (need Accounting scope)
  - Sandbox vs Production confusion (use Sandbox)

---

## ?? Bonus: Test Company

Intuit provides test companies in Sandbox mode:
- You don't need a real QuickBooks subscription
- They provide sample data
- Perfect for testing!

---

**Next Steps:**
1. ?? Register at https://developer.intuit.com
2. ?? Get your Client ID and Client Secret
3. ?? Add them to `appsettings.Development.json`
4. ?? Tell me "I have the credentials"
5. ?? I'll build the OAuth pages
6. ?? We test the connection!

---

**Ready to start? Go register now!** ??

Once you have your credentials, just paste them and say "credentials ready" - I'll build the rest!
