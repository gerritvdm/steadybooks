# SteadyBooks

> White-labeled, read-only financial dashboards for accountants to share with their QuickBooks Online clients.

## ?? Project Overview

**SteadyBooks** empowers accountants and accounting firms to provide their clients with transparent, easy-to-understand financial insights—without compromising data security or workflow efficiency.

### Key Features
- ? **100% Read-Only Access** to QuickBooks Online
- ?? **White-Labeled Dashboards** with your firm's branding
- ?? **Client-Safe** secure links (no login credentials to manage)
- ? **Quick Setup** - Connect QuickBooks and go live in minutes
- ?? **Simplified Metrics** - Cash, Profit, Taxes, Invoices

---

## ??? Tech Stack

- **Framework:** ASP.NET Core 10.0 (Razor Pages)
- **Database:** PostgreSQL with Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Resilience:** Polly (retry, circuit breaker)
- **Integrations:** QuickBooks Online API (OAuth 2.0)
- **Logging:** Serilog (Console, File, Seq)
- **Scheduling:** Quartz.NET (for background jobs)

---

## ?? Project Structure

```
SteadyBooks/
??? Areas/
?   ??? Identity/          # Authentication pages (Login, Register, etc.)
??? Data/
?   ??? ApplicationDbContext.cs
??? Models/
?   ??? ApplicationUser.cs # Extended Identity user with Firm fields
??? Pages/
?   ??? Index.cshtml       # Marketing/Landing page
?   ??? Privacy.cshtml     # Privacy policy
?   ??? Terms.cshtml       # Terms of service
?   ??? Firm/              # Firm settings (upcoming)
?   ??? Dashboards/        # Dashboard management (upcoming)
?   ??? QuickBooks/        # QBO integration (upcoming)
??? Services/
?   ??? HttpClientService.cs
?   ??? Repository.cs
?   ??? ResiliencePipelineService.cs
??? Migrations/            # EF Core migrations
??? wwwroot/
?   ??? css/site.css       # Custom styling
??? Program.cs
```

---

## ?? Getting Started

### Prerequisites
- .NET 10 SDK
- PostgreSQL 16+
- QuickBooks Online Developer Account (for OAuth)
- IDE: Visual Studio 2022+ or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/gerritvdm/steadybooks.git
   cd steadybooks/SteadyBooks
   ```

2. **Configure database connection**
   
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=steadybooks;Username=your_user;Password=your_password"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Navigate to** `https://localhost:5001`

---

## ?? Development Status

### ? Completed (Sprint 1)
- [x] Project setup and configuration
- [x] PostgreSQL + EF Core integration
- [x] ASP.NET Core Identity with custom ApplicationUser
- [x] Marketing/Landing page
- [x] Accountant registration and login
- [x] Privacy Policy and Terms of Service
- [x] Responsive UI with Bootstrap 5

### ?? In Progress
- [ ] Firm Settings (branding customization)
- [ ] QuickBooks OAuth integration

### ?? Planned (MVP)
- [ ] Dashboard management
- [ ] Client invite system
- [ ] Read-only client dashboard view
- [ ] QuickBooks data sync
- [ ] Financial metric calculations
- [ ] Account/billing page (light)

See [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) for complete roadmap.

---

## ?? Security & Privacy

### Read-Only Guarantee
SteadyBooks uses **read-only OAuth scopes** for QuickBooks Online. We:
- ? Display financial data
- ? Cannot modify transactions
- ? Cannot create/delete records
- ? Cannot access banking credentials

### Data Protection
- Encrypted token storage
- Secure client dashboard links (GUID-based)
- Optional password protection for dashboards
- HTTPS-only communication
- CSRF protection on all forms

See [Privacy Policy](SteadyBooks/Pages/Privacy.cshtml) for details.

---

## ?? Testing

### Run Tests
```bash
dotnet test
```

### Build Project
```bash
dotnet build
```

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName
```

---

## ?? Deployment

### Environment Variables (Production)
```bash
ConnectionStrings__DefaultConnection=<postgres-connection-string>
QuickBooks__ClientId=<your-qbo-client-id>
QuickBooks__ClientSecret=<your-qbo-client-secret>
QuickBooks__RedirectUri=<your-callback-url>
ASPNETCORE_ENVIRONMENT=Production
```

### Recommended Hosting
- Azure App Service
- AWS Elastic Beanstalk
- Docker containers
- Any .NET 10 compatible host

---

## ?? Resources

### Documentation
- [QuickBooks Online API](https://developer.intuit.com/app/developer/qbo/docs/get-started)
- [ASP.NET Core Razor Pages](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Polly Resilience](https://www.thepollyproject.org/)

### Development Guides
- [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) - Complete project roadmap
- [SPRINT_1_SUMMARY.md](SPRINT_1_SUMMARY.md) - Sprint 1 completion report

---

## ?? Contributing

This is a private project. Contributions are managed internally.

### Code Style
- Follow Microsoft C# coding conventions
- Use meaningful variable/method names
- Add XML comments to public APIs
- Keep Razor Pages focused and single-purpose

---

## ?? License

Proprietary - All rights reserved.

---

## ?? Support

### For Accountants Using SteadyBooks
- Email: support@steadybooks.com
- Website: (Coming soon)

### For Development Issues
- Check [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)
- Review existing documentation
- Contact project maintainer

---

## ??? Roadmap

### Q1 2025 - MVP Launch
- Complete Phase 1-6 (see DEVELOPMENT_PLAN.md)
- Submit to Intuit for app review
- Launch beta with select accounting firms

### Q2 2025 - Post-MVP
- Stripe billing integration
- Automated data refresh (background jobs)
- Chart/trend visualizations
- PDF export functionality
- Mobile responsiveness improvements

### Q3 2025 - Scale
- Multiple users per firm
- Advanced dashboard templates
- Client authentication (optional)
- Accountant notification system
- API for third-party integrations

---

## ?? Project Goals

1. ? **Trust:** Accountants trust us with client data
2. ? **Simplicity:** Clients understand their finances
3. ? **Security:** Read-only = zero risk
4. ? **Efficiency:** Less time explaining, more time advising
5. ? **Professionalism:** White-labeled = your brand

---

**Built with ?? for accountants who value transparency and security.**

---

## ?? Version History

### v0.1.0 - Sprint 1 Complete (January 2025)
- Initial project setup
- Marketing presence
- Authentication system
- Legal pages (Privacy/Terms)

---

**Last Updated:** January 2025  
**Status:** ?? Active Development - Sprint 2 Starting
