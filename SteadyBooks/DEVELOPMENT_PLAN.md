# SteadyBooks Development Plan

## Overview
White-labeled, read-only financial dashboard for accountants to share with their QuickBooks Online clients.

**Tech Stack:**
- ASP.NET Core 10.0 (Razor Pages)
- PostgreSQL with Entity Framework Core
- ASP.NET Core Identity
- Polly for resilience
- QuickBooks Online API (OAuth 2.0, read-only scopes)

---

## Current State Assessment

### ? Completed
- [x] Project scaffolding (.NET 10, Razor Pages)
- [x] PostgreSQL database setup with EF Core
- [x] ASP.NET Core Identity integration
- [x] Custom ApplicationUser with FirmName and PrimaryContactEmail
- [x] Basic authentication pages (Login, Register, Logout, ForgotPassword)
- [x] Polly resilience patterns (retry, circuit breaker)
- [x] Repository pattern implementation
- [x] HttpClient service with resilience
- [x] **Marketing/Landing Page (Screen 1)** - Professional landing page with value prop and CTAs
- [x] **Updated Registration Page** - Accountant-focused with firm details
- [x] **Updated Login Page** - Improved styling and messaging
- [x] **Privacy Policy Page** - Comprehensive privacy policy for Intuit review
- [x] **Terms of Service Page** - Complete terms for Intuit review
- [x] **Updated Navigation** - Added logo, improved structure

### ?? In Progress
- [ ] None currently

### ?? Pending
Remaining screens from the implementation plan (screens 3-10)

---

## Data Model Requirements

### Phase 1: Core Entities

#### 1. Firm (extends ApplicationUser)
Already have ApplicationUser with:
- FirmName ?
- PrimaryContactEmail ?

Need to add:
- Logo (file path or blob storage URL)
- BrandColor (hex code)
- ContactPhone
- FooterMessage
- CreatedDate
- IsActive

#### 2. QuickBooksConnection
- Id (PK)
- FirmId (FK to ApplicationUser)
- CompanyId (QuickBooks Company ID)
- CompanyName
- AccessToken (encrypted)
- RefreshToken (encrypted)
- TokenExpiresAt
- RealmId
- Scopes
- ConnectionStatus (Connected/NeedsReconnect/Disconnected)
- LastSyncDate
- CreatedDate
- ModifiedDate

#### 3. ClientDashboard
- Id (PK)
- FirmId (FK to ApplicationUser)
- QuickBooksConnectionId (FK)
- DashboardName
- AccessLink (secure GUID)
- AccessPassword (hashed, nullable)
- IsActive
- CreatedDate
- ModifiedDate
- LastAccessedDate

#### 4. DashboardConfiguration
- Id (PK)
- ClientDashboardId (FK)
- DateRange (ThisMonth/LastMonth/YTD/Custom)
- ShowCashBalance (bool)
- ShowProfit (bool)
- ShowTaxesDue (bool)
- ShowOutstandingInvoices (bool)
- CashAccountMapping (JSON)
- TaxAccountMapping (JSON)
- CreatedDate
- ModifiedDate

#### 5. DashboardSnapshot
- Id (PK)
- ClientDashboardId (FK)
- SnapshotDate
- CashBalance (decimal)
- ProfitMTD (decimal)
- ProfitYTD (decimal)
- TaxesDue (decimal)
- OutstandingInvoices (decimal)
- RawDataJson (for audit/debugging)
- CreatedDate

---

## Screen Implementation Plan

### ?? Phase 1: Foundation (Week 1)

#### 1. Marketing / Landing Page ? PUBLIC
**Priority:** HIGH (Required for Intuit review)
**Route:** `/` or `/Home/Index`
**File:** `Pages/Index.cshtml` (update existing)

**Tasks:**
- [x] Update existing Index.cshtml with marketing content
- [x] Add hero section with value proposition
- [x] Add benefits section (read-only, white-labeled, client-safe)
- [x] Add screenshot placeholders
- [x] Add "Sign up as an accountant" CTA ? links to Register
- [x] Add basic CSS styling
- [x] Ensure no login required to view

**Acceptance Criteria:**
- Clear messaging for Intuit reviewers
- Explains read-only nature
- Links work to registration

---

#### 2. Accountant Sign-Up / Login ? PARTIALLY COMPLETE
**Priority:** HIGH
**Routes:** `/Identity/Account/Register`, `/Identity/Account/Login`
**Files:** Already exist in `Areas/Identity/Pages/Account/`

**Tasks:**
- [x] Update Register.cshtml to include FirmName field
- [x] Update Register.cshtml.cs to capture FirmName
- [x] Add email confirmation flow
- [x] Test password reset flow
- [x] Consider adding Google OAuth (optional for MVP)
- [x] Update UI to be accountant-focused

**Acceptance Criteria:**
- Accountants can register with firm name
- Email confirmation works
- Password reset works
- Login/logout functional

---

### ?? Phase 2: Firm Setup & QuickBooks (Week 2)

#### 3. Firm Settings (Branding) ??  WIP
**Priority:** HIGH (Key selling feature)
**Route:** `/Firm/Settings`
**Files:** `Pages/Firm/Settings.cshtml`, `Pages/Firm/Settings.cshtml.cs`

**Tasks:**
- [ ] Create migration to add branding fields to ApplicationUser/Firm
- [ ] Create Firm/Settings page
- [ ] Add form fields:
  - Firm name (editable)
  - Logo upload (file upload ? store path)
  - Brand color picker (hex input)
  - Contact email
  - Contact phone
  - Footer message (textarea)
- [ ] Implement file upload for logo (wwwroot/uploads/logos/)
- [ ] Add validation
- [ ] Save to database
- [ ] Show preview of branding

**Acceptance Criteria:**
- Firm can upload logo
- Firm can set brand color
- Settings persist and load correctly
- Preview shows how client dashboard will look

---

#### 4. Connect QuickBooks (OAuth Screen) ??  WIP
**Priority:** HIGH (Critical integration)
**Route:** `/QuickBooks/Connect`
**Files:** 
- `Pages/QuickBooks/Connect.cshtml`
- `Pages/QuickBooks/Callback.cshtml`
- `Services/QuickBooksOAuthService.cs`

**Tasks:**
- [ ] Register app with Intuit Developer Portal
- [ ] Get Client ID and Client Secret
- [ ] Store credentials in appsettings.json / environment variables
- [ ] Create QuickBooksConnection table migration
- [ ] Create QuickBooksOAuthService
  - Generate OAuth URL with read-only scopes
  - Handle callback with authorization code
  - Exchange code for tokens
  - Store encrypted tokens in database
- [ ] Create Connect page with "Connect QuickBooks" button
- [ ] Create Callback page to handle OAuth return
- [ ] Add success/error messaging
- [ ] Add reconnect button
- [ ] Add disconnect button

**QuickBooks Scopes (Read-Only):**
- `com.intuit.quickbooks.accounting`

**Acceptance Criteria:**
- Clicking "Connect QuickBooks" redirects to Intuit OAuth
- After authorization, callback stores connection
- Can disconnect a connection
- Can reconnect if expired
- Tokens stored securely (encrypted)

---

### ?? Phase 3: Dashboard Management (Week 3)

#### 5. Client List / Dashboard List ??  WIP
**Priority:** HIGH (Main home screen after login)
**Route:** `/Dashboards` or `/Dashboards/Index`
**Files:** `Pages/Dashboards/Index.cshtml`, `Pages/Dashboards/Index.cshtml.cs`

**Tasks:**
- [ ] Create ClientDashboard table migration
- [ ] Create Dashboards/Index page
- [ ] Query all dashboards for logged-in firm
- [ ] Display table/cards with:
  - Dashboard name
  - Company name (from QuickBooks)
  - Status (Connected/Needs Reconnect)
  - Last synced date
- [ ] Add action buttons:
  - View Dashboard (client view)
  - Configure Dashboard
  - Invite Client
  - Archive
- [ ] Add "Create New Dashboard" button
- [ ] Handle empty state (no dashboards yet)

**Acceptance Criteria:**
- Lists all dashboards for firm
- Shows connection status
- Buttons navigate correctly
- Can create new dashboard

---

#### 6. Dashboard Configuration (Per Client) ??  WIP
**Priority:** HIGH
**Route:** `/Dashboards/Configure/{id}`
**Files:** `Pages/Dashboards/Configure.cshtml`, `Pages/Dashboards/Configure.cshtml.cs`

**Tasks:**
- [ ] Create DashboardConfiguration table migration
- [ ] Create Configure page
- [ ] Add form fields:
  - Dashboard name
  - Date range selector (This Month/Last Month/YTD)
  - Widget toggles (checkboxes):
    - Show Cash Balance
    - Show Profit
    - Show Taxes Due
    - Show Outstanding Invoices
  - Account mapping section:
    - Fetch accounts from QuickBooks API
    - Multi-select for Cash accounts
    - Multi-select for Tax Payable accounts
- [ ] Save configuration to database
- [ ] Add sensible defaults
- [ ] Add validation

**Acceptance Criteria:**
- Accountant can name dashboard
- Can toggle widgets on/off
- Can map QBO accounts to dashboard metrics
- Configuration saves and loads correctly

---

### ?? Phase 4: Client Access (Week 4)

#### 7. Client Invite / Access Screen ??  WIP
**Priority:** HIGH
**Route:** `/Dashboards/Invite/{id}`
**Files:** `Pages/Dashboards/Invite.cshtml`, `Pages/Dashboards/Invite.cshtml.cs`

**Tasks:**
- [ ] Create Invite page
- [ ] Generate secure access link (GUID-based)
  - Example: `/Dashboard/View/{guid}`
- [ ] Display copyable link
- [ ] Add optional password protection
- [ ] Create email template for invitation
- [ ] Implement email sending (SMTP or SendGrid)
- [ ] Add "Send Invite" button
- [ ] Add "Regenerate Link" button
- [ ] Add "Revoke Access" button (mark dashboard inactive)
- [ ] Track invitation sent date

**Acceptance Criteria:**
- Generates unique, unguessable link
- Can copy link to clipboard
- Can send email invitation
- Can regenerate link
- Can revoke access

---

#### 8. Client Financial Dashboard (Read-Only) ?? PUBLIC  WIP
**Priority:** HIGH (Core product value)
**Route:** `/Dashboard/View/{accessGuid}`
**Files:** `Pages/Dashboard/View.cshtml`, `Pages/Dashboard/View.cshtml.cs`

**Tasks:**
- [ ] Create DashboardSnapshot table migration
- [ ] Create View page (no authentication required)
- [ ] Validate access link (check GUID exists and is active)
- [ ] Check password if required
- [ ] Load firm branding:
  - Logo
  - Brand color
  - Footer message
- [ ] Load dashboard configuration
- [ ] Display metric cards (based on config):
  - Cash Balance
  - Profit (MTD/YTD)
  - Taxes Due
  - Outstanding Invoices
- [ ] Add "Questions? Contact your accountant" CTA
- [ ] Add "Last updated" timestamp
- [ ] Apply firm branding throughout
- [ ] Ensure completely read-only (no forms, no edits)
- [ ] Track last accessed date

**Rules:**
- No authentication required
- No data editing
- No drill-downs in MVP
- No exports in MVP

**Acceptance Criteria:**
- Client can view dashboard with secure link
- Displays firm branding
- Shows configured metrics
- Completely read-only
- Mobile responsive

---

### ?? Phase 5: Data Sync & Monitoring (Week 5)

#### 9. Data Refresh / Sync Status ??  WIP
**Priority:** MEDIUM
**Route:** Integrated into Dashboard List and Configuration pages
**Files:** `Services/QuickBooksSyncService.cs`

**Tasks:**
- [ ] Create QuickBooksSyncService
- [ ] Implement methods to fetch data from QuickBooks API:
  - Get account balances (for cash)
  - Get profit & loss report
  - Get balance sheet (for tax payable)
  - Get invoice aging (for outstanding invoices)
- [ ] Add "Last updated" timestamp to dashboard list
- [ ] Add "Refresh Now" button (accountant only)
- [ ] Implement manual refresh logic:
  - Fetch fresh data from QBO
  - Calculate metrics based on configuration
  - Store in DashboardSnapshot
- [ ] Add error handling and display:
  - QBO unavailable
  - Token expired (needs reconnect)
  - API rate limit
- [ ] Consider background job (Quartz) for auto-refresh
- [ ] Add sync status indicator (Success/Error/In Progress)

**Acceptance Criteria:**
- Displays last sync time
- Manual refresh works
- Error states are clear
- Accountant can see sync status

---

### ?? Phase 6: Billing & Admin (Week 6)

#### 10. Account Settings / Billing (Very Light) ??
**Priority:** LOW (Foundation for future)
**Route:** `/Account/Billing`
**Files:** `Pages/Account/Billing.cshtml`, `Pages/Account/Billing.cshtml.cs`

**Tasks:**
- [ ] Create Billing page
- [ ] Display current plan (hardcoded "Free Trial" for MVP)
- [ ] Count and display active dashboards
- [ ] Add placeholder for "Upgrade" button
- [ ] Add "Cancel Account" option (soft delete)
- [ ] Add confirmation modal for account cancellation
- [ ] Store cancellation date
- [ ] Keep data for 30 days after cancellation

**Future:**
- Integrate Stripe
- Define pricing tiers
- Implement usage-based billing

**Acceptance Criteria:**
- Shows current plan info
- Displays active dashboard count
- Upgrade CTA present (even if non-functional)
- Account cancellation works

---

## Additional Technical Tasks

### Security
- [ ] Encrypt QuickBooks tokens at rest
- [ ] Implement secure password for client dashboards
- [ ] Add rate limiting for public dashboard views
- [ ] Add CSRF protection to all forms
- [ ] Implement audit logging for sensitive actions

### Services to Create
- [ ] `QuickBooksOAuthService` - Handle OAuth flow
- [ ] `QuickBooksApiService` - Fetch financial data
- [ ] `QuickBooksSyncService` - Orchestrate data refresh
- [ ] `EmailService` - Send invitations
- [ ] `BrandingService` - Apply firm branding to views
- [ ] `DashboardService` - Business logic for dashboard operations
- [ ] `EncryptionService` - Encrypt/decrypt tokens

### Configuration
- [ ] Add QuickBooks API settings to appsettings.json
- [ ] Add SMTP/SendGrid settings for email
- [ ] Add file storage settings for logo uploads
- [ ] Set up environment variables for secrets
- [ ] Configure CORS if needed for API calls

### Testing
- [ ] Unit tests for services
- [ ] Integration tests for QuickBooks API
- [ ] End-to-end test: Create firm ? Connect QBO ? Create dashboard ? View as client
- [ ] Test OAuth flow edge cases
- [ ] Test token refresh logic

### Deployment
- [ ] Set up PostgreSQL database in production
- [ ] Configure environment variables
- [ ] Set up SSL certificate
- [ ] Deploy to hosting (Azure, AWS, or other)
- [ ] Set up logging and monitoring (Serilog already configured)
- [ ] Create backup strategy

### Intuit Review Preparation
- [ ] Ensure landing page clearly explains read-only nature
- [ ] Document all API scopes used
- [ ] Create privacy policy page
- [ ] Create terms of service page
- [ ] Prepare demo video
- [ ] Complete Intuit security questionnaire

---

## Sprint Breakdown

### Sprint 1: Foundation (Week 1)
- Landing page
- Update registration
- Email confirmation

### Sprint 2: Firm & QuickBooks (Week 2)
- Firm settings/branding
- QuickBooks OAuth integration
- Data models

### Sprint 3: Dashboard Management (Week 3)
- Client dashboard list
- Dashboard configuration
- Create dashboard flow

### Sprint 4: Client Experience (Week 4)
- Client invite system
- Client view (read-only dashboard)
- Branding application

### Sprint 5: Data & Sync (Week 5)
- QuickBooks API integration
- Data sync service
- Snapshot storage
- Manual refresh

### Sprint 6: Polish & Launch Prep (Week 6)
- Billing page (light)
- Error handling improvements
- Testing
- Documentation
- Intuit review preparation

---

## Success Metrics

### MVP Launch Criteria
- [ ] Accountant can register and login
- [ ] Accountant can set firm branding
- [ ] Accountant can connect to QuickBooks Online
- [ ] Accountant can create a client dashboard
- [ ] Accountant can configure dashboard widgets and metrics
- [ ] Accountant can send secure link to client
- [ ] Client can view dashboard without login
- [ ] Dashboard displays firm branding
- [ ] Dashboard shows accurate financial metrics
- [ ] Data refreshes (at least manually)
- [ ] Landing page ready for Intuit review

### Post-MVP Enhancements
- Automated data refresh (background jobs)
- Stripe integration for billing
- Multiple dashboard templates
- Chart/trend visualizations
- Export to PDF
- Client authentication (optional login)
- Multiple users per firm
- Accountant notifications (data issues, client views)
- Mobile app

---

## Notes & Decisions

### Why Razor Pages?
- Simpler than MVC for page-centric workflows
- Better for this app's structure (mostly CRUD + forms)
- Easier to understand for small team
- .NET 10 support

### Why PostgreSQL?
- Open source and cost-effective
- Great for relational data
- Excellent EF Core support
- Scales well

### Why Read-Only?
- Reduces risk for accountants
- Simplifies Intuit review process
- Fewer permission requirements
- Builds trust with clients

### Key Risks
1. **QuickBooks API Rate Limits**: Need caching and smart sync
2. **Token Expiry**: Must handle refresh gracefully
3. **Intuit Review**: Must clearly communicate read-only nature
4. **Account Mapping Complexity**: May need smarter defaults
5. **Multi-tenancy**: Ensure data isolation between firms

---

## Resources

### Documentation
- [QuickBooks Online API Docs](https://developer.intuit.com/app/developer/qbo/docs/get-started)
- [OAuth 2.0 for QuickBooks](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization/oauth-2.0)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Razor Pages](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)

### Tools Needed
- Intuit Developer Account
- QuickBooks Online Sandbox (for testing)
- PostgreSQL database (dev and production)
- Email service (SMTP or SendGrid)
- File storage (local or Azure Blob Storage)

---

## Change Log

| Date | Change | Updated By |
|------|--------|------------|
| 2025-01-XX | Initial planning document created | Copilot |
| 2025-01-XX | **Sprint 1 Phase 1 COMPLETED**: Landing page, updated registration/login, Privacy Policy, Terms of Service, navigation improvements | Copilot |
|  |  |  |

---

**Last Updated:** 2025-01-XX
**Next Review:** Sprint 2 - Firm Settings & QuickBooks Integration
**Next Task:** Create Firm Settings page with branding customization
