# Next Steps - Sprint 2 Preparation

## ?? Immediate Next Actions

### 1. Register with Intuit Developer Portal
**Priority:** HIGH - Required for QuickBooks integration

**Steps:**
1. Go to https://developer.intuit.com
2. Create a developer account (or log in)
3. Create a new app
   - Name: "SteadyBooks"
   - Type: "Accounting"
4. Configure OAuth 2.0 settings:
   - Redirect URI: `https://localhost:5001/QuickBooks/Callback` (dev)
   - Scopes: Select "Accounting" (read-only)
5. Save your credentials:
   - Client ID
   - Client Secret
   - Redirect URI

**Where to store:**
Add to `appsettings.json`:
```json
"QuickBooks": {
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET",
  "RedirectUri": "https://localhost:5001/QuickBooks/Callback",
  "Environment": "sandbox",
  "AuthorizationEndpoint": "https://appcenter.intuit.com/connect/oauth2",
  "TokenEndpoint": "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer"
}
```

**For production:** Use environment variables or Azure Key Vault

---

### 2. Create Database Migrations for Firm Branding
**Priority:** HIGH - Required for Screen 3

**Fields to add to ApplicationUser:**
```csharp
public string? LogoPath { get; set; }
public string? BrandColor { get; set; } // Hex color, default: #667eea
public string? ContactPhone { get; set; }
public string? FooterMessage { get; set; }
public DateTime CreatedDate { get; set; }
public bool IsActive { get; set; } = true;
```

**Commands:**
```bash
dotnet ef migrations add AddFirmBrandingFields
dotnet ef database update
```

---

### 3. Create QuickBooksConnection Entity
**Priority:** HIGH - Required for Screen 4

**Create file:** `Models/QuickBooksConnection.cs`
```csharp
public class QuickBooksConnection
{
    public int Id { get; set; }
    public string FirmId { get; set; } // FK to ApplicationUser
    public ApplicationUser Firm { get; set; }
    public string CompanyId { get; set; }
    public string CompanyName { get; set; }
    public string EncryptedAccessToken { get; set; }
    public string EncryptedRefreshToken { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public string RealmId { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public enum ConnectionStatus
{
    Connected,
    NeedsReconnect,
    Disconnected
}
```

**Commands:**
```bash
dotnet ef migrations add AddQuickBooksConnection
dotnet ef database update
```

---

### 4. Set Up File Storage for Logos
**Priority:** MEDIUM - Required for Screen 3

**Option A: Local Storage (Development)**
- Create folder: `wwwroot/uploads/logos/`
- Add to `.gitignore`: `wwwroot/uploads/logos/*`
- Store file paths in database

**Option B: Azure Blob Storage (Production)**
- Install: `dotnet add package Azure.Storage.Blobs`
- Configure connection string
- Implement upload service

**For MVP:** Start with Option A, migrate to B later

---

### 5. Test Email Confirmation Flow
**Priority:** MEDIUM - Required for production

**Current Status:** Identity configured but email not set up

**Options:**
1. **SMTP (Simple):** Gmail, Outlook, or company email
2. **SendGrid (Recommended):** Free tier, 100 emails/day
3. **Azure Communication Services**

**Configuration needed in `appsettings.json`:**
```json
"EmailSettings": {
  "SmtpServer": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SenderEmail": "noreply@steadybooks.com",
  "SenderName": "SteadyBooks",
  "ApiKey": "YOUR_SENDGRID_API_KEY"
}
```

**For now:** Can skip email confirmation in development

---

## ?? Sprint 2 Task Checklist

### Week 2 Goals
- [ ] Complete Screen 3: Firm Settings
- [ ] Complete Screen 4: QuickBooks OAuth

### Screen 3: Firm Settings - Task Breakdown

#### Database & Models
- [ ] Add branding fields to ApplicationUser
- [ ] Create migration for branding fields
- [ ] Apply migration to database

#### Pages & UI
- [ ] Create `Pages/Firm/Settings.cshtml`
- [ ] Create `Pages/Firm/Settings.cshtml.cs`
- [ ] Build firm settings form (name, email, phone, footer)
- [ ] Implement logo upload control
- [ ] Add color picker (HTML5 color input)
- [ ] Create branding preview section

#### Services
- [ ] Create `Services/FileUploadService.cs` for logo handling
- [ ] Add file validation (size, type)
- [ ] Implement file storage logic

#### Testing
- [ ] Test logo upload
- [ ] Test color selection
- [ ] Test form validation
- [ ] Test branding save/load
- [ ] Verify preview accuracy

---

### Screen 4: QuickBooks OAuth - Task Breakdown

#### Setup & Configuration
- [ ] Register Intuit developer account
- [ ] Create QuickBooks app
- [ ] Add credentials to configuration
- [ ] Create QuickBooksConnection entity
- [ ] Create migration and apply

#### Services
- [ ] Create `Services/EncryptionService.cs`
  - Methods: Encrypt(string), Decrypt(string)
  - Use Data Protection API
- [ ] Create `Services/QuickBooksOAuthService.cs`
  - GenerateAuthorizationUrl()
  - ExchangeCodeForTokens()
  - RefreshAccessToken()
  - StoreConnection()

#### Pages & UI
- [ ] Create `Pages/QuickBooks/Connect.cshtml`
  - "Connect QuickBooks" button
  - Connection status display
  - Reconnect/Disconnect buttons
- [ ] Create `Pages/QuickBooks/Connect.cshtml.cs`
- [ ] Create `Pages/QuickBooks/Callback.cshtml`
  - Handle OAuth return
  - Display success/error
- [ ] Create `Pages/QuickBooks/Callback.cshtml.cs`

#### Integration
- [ ] Test OAuth flow in sandbox
- [ ] Test token storage (encrypted)
- [ ] Test reconnection logic
- [ ] Test disconnect functionality
- [ ] Handle error scenarios:
  - User cancels authorization
  - Invalid credentials
  - Network errors

---

## ?? Development Environment Setup

### Required Tools
- [x] .NET 10 SDK
- [x] PostgreSQL
- [x] Visual Studio 2022 or VS Code
- [ ] QuickBooks Online Sandbox account
- [ ] Intuit Developer account
- [ ] Email service account (optional for now)

### Nice to Have
- [ ] Postman (for API testing)
- [ ] Azure Storage Explorer (if using Blob storage)
- [ ] Seq (for log viewing) - already configured in code

---

## ?? Notes & Reminders

### Security Considerations
- Never commit QuickBooks credentials to Git
- Use User Secrets for development: `dotnet user-secrets set "QuickBooks:ClientId" "value"`
- Encrypt tokens before storing in database
- Use HTTPS for all OAuth redirects

### QuickBooks Sandbox
- Sandbox companies are free test environments
- Can create multiple companies for testing
- Data resets periodically
- Production OAuth requires app review by Intuit

### File Upload Best Practices
- Limit file size (2MB max for logos)
- Validate file types (.png, .jpg, .svg)
- Generate unique filenames (GUID)
- Store paths, not files, in database
- Implement file deletion when logo changes

---

## ?? Success Criteria for Sprint 2

### Screen 3: Firm Settings
? Accountant can:
- Edit firm name
- Upload/change logo
- Select brand color
- Add contact phone
- Set footer message
- See preview of branding

### Screen 4: QuickBooks OAuth
? Accountant can:
- Click "Connect QuickBooks"
- Authorize in Intuit OAuth flow
- Return to app with connection saved
- See connection status
- Reconnect if expired
- Disconnect when needed

? System:
- Stores encrypted tokens
- Validates connections
- Handles errors gracefully

---

## ?? Reference Documentation

### Must-Read Before Sprint 2
1. [QuickBooks OAuth 2.0 Guide](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization/oauth-2.0)
2. [ASP.NET Core File Upload](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
3. [Data Protection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction)

### Useful APIs
- [QuickBooks Online API Reference](https://developer.intuit.com/app/developer/qbo/docs/api/accounting/all-entities/account)
- [QuickBooks Sandbox](https://developer.intuit.com/app/developer/qbo/docs/develop/sandboxes)

---

## ?? Known Issues / Technical Debt
- None currently - Sprint 1 clean! ?

---

## ? Questions to Resolve

1. **Logo Storage:** Local or cloud? (Decision: Start local, move to Azure Blob later)
2. **Email Provider:** Which service? (Decision: SendGrid for MVP)
3. **Token Refresh:** Automatic or manual? (Decision: Automatic with Quartz job in Phase 5)
4. **Multiple QBO Connections:** One per firm or multiple? (Decision: One for MVP, multiple in post-MVP)

---

**Next Work Session:** Start with Firm Settings (Screen 3)  
**Estimated Time:** 4-6 hours for complete implementation  
**Dependencies:** Database migration, file upload strategy

---

**Ready to begin Sprint 2? Start with database migrations!** ??
