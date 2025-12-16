# ?? Stripe Payment Integration - Setup Guide

## ?? Overview

Your SteadyBooks application now has complete Stripe payment integration for subscription management. This guide will help you set it up.

---

## ?? What's Been Implemented

### ? Features
1. **Subscription Plans**
   - Free Trial (3 dashboards, 30 days)
   - Professional ($29/month or $278/year)
   - Business ($79/month or $758/year)
   - 14-day trial on paid plans

2. **Payment Processing**
   - Stripe Checkout integration
   - Customer Portal for subscription management
   - Secure payment handling

3. **Webhook Integration**
   - Real-time subscription updates
   - Payment success/failure handling
   - Auto-cancellation handling

4. **Database**
   - Subscription tracking
   - User payment status
   - Billing history

---

## ?? Setup Steps

### Step 1: Create Stripe Account

1. Go to https://stripe.com
2. Click "Start now" and create an account
3. Complete business verification (required for live payments)

### Step 2: Get API Keys

1. Go to https://dashboard.stripe.com/test/apikeys
2. Copy your **Publishable key** (starts with `pk_test_`)
3. Copy your **Secret key** (starts with `sk_test_`)
4. Keep these secure!

### Step 3: Create Products and Prices in Stripe

#### Create Professional Plan
1. Go to https://dashboard.stripe.com/test/products
2. Click "Add product"
3. **Product Details:**
   - Name: `SteadyBooks Professional`
   - Description: `10 client dashboards, QuickBooks integration, white-label branding`
   
4. **Pricing:**
   - Click "Add pricing"
   - **Monthly Price:**
     - Type: Recurring
     - Price: `$29.00 USD`
     - Billing period: Monthly
     - Click "Add pricing"
     - **Copy the Price ID** (starts with `price_`)
   
   - **Yearly Price:**
     - Click "Add another price"
     - Type: Recurring
     - Price: `$278.00 USD` (save 20%)
     - Billing period: Yearly
     - Click "Add pricing"
     - **Copy the Price ID**

#### Create Business Plan
1. Click "Add product" again
2. **Product Details:**
   - Name: `SteadyBooks Business`
   - Description: `Unlimited dashboards, priority support, custom reports`
   
3. **Pricing:**
   - **Monthly Price:**
     - Type: Recurring
     - Price: `$79.00 USD`
     - Billing period: Monthly
     - **Copy the Price ID**
   
   - **Yearly Price:**
     - Type: Recurring
     - Price: `$758.00 USD` (save 20%)
     - Billing period: Yearly
     - **Copy the Price ID**

### Step 4: Update appsettings.json

Replace the placeholder values in `appsettings.json`:

```json
"Stripe": {
  "PublishableKey": "pk_test_YOUR_ACTUAL_KEY_HERE",
  "SecretKey": "sk_test_YOUR_ACTUAL_KEY_HERE",
  "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET_HERE",
  "ProfessionalMonthlyPriceId": "price_PROFESSIONAL_MONTHLY_ID",
  "ProfessionalYearlyPriceId": "price_PROFESSIONAL_YEARLY_ID",
  "BusinessMonthlyPriceId": "price_BUSINESS_MONTHLY_ID",
  "BusinessYearlyPriceId": "price_BUSINESS_YEARLY_ID"
}
```

### Step 5: Set Up Webhook Endpoint

#### Local Testing with Stripe CLI

1. **Install Stripe CLI:**
   - Windows: `choco install stripe-cli`
   - Mac: `brew install stripe/stripe-cli/stripe`
   - Or download from: https://github.com/stripe/stripe-cli/releases

2. **Login to Stripe CLI:**
   ```bash
   stripe login
   ```

3. **Forward webhooks to local development:**
   ```bash
   stripe listen --forward-to https://localhost:7189/api/StripeWebhook
   ```

4. **Copy the webhook secret** shown in the output (starts with `whsec_`)

5. **Update appsettings.Development.json:**
   ```json
   "Stripe": {
     "WebhookSecret": "whsec_YOUR_CLI_SECRET_HERE"
   }
   ```

#### Production Webhook Setup

1. Go to https://dashboard.stripe.com/test/webhooks
2. Click "Add endpoint"
3. **Endpoint URL:** `https://yourdomain.com/api/StripeWebhook`
4. **Select events to listen to:**
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`

5. Click "Add endpoint"
6. **Copy the Signing secret** (starts with `whsec_`)
7. Update production `appsettings.json`

---

## ??? Apply Database Migration

Run this command to create the subscription tables:

```bash
cd C:\Projects\steadybooks\SteadyBooks\SteadyBooks
dotnet ef database update
```

This creates:
- `Subscriptions` table
- Adds Stripe fields to `AspNetUsers`

---

## ?? Testing the Integration

### Test the Checkout Flow

1. **Start your application:**
   ```bash
   dotnet run
   ```

2. **Start Stripe webhook forwarding** (in another terminal):
   ```bash
   stripe listen --forward-to https://localhost:7189/api/StripeWebhook
   ```

3. **Test subscription:**
   - Register a new account
   - Go to `/Account/Pricing`
   - Click "Choose Professional" or "Choose Business"
   - You'll be redirected to Stripe Checkout
   - Use test card: `4242 4242 4242 4242`
   - Any future expiration date
   - Any CVC
   - Complete checkout

4. **Verify webhook:**
   - Check the Stripe CLI terminal for webhook events
   - Check your application logs
   - Verify subscription in database

### Stripe Test Cards

| Card Number | Description |
|-------------|-------------|
| `4242 4242 4242 4242` | Success |
| `4000 0000 0000 0341` | Declined - Insufficient funds |
| `4000 0000 0000 9995` | Declined - Charge exceeds limit |
| `4000 0000 0000 0002` | Declined - Generic decline |

Full list: https://stripe.com/docs/testing

---

## ?? Pages Implemented

### 1. `/Account/Pricing`
- Displays all available plans
- Monthly/Yearly toggle
- Initiates Stripe Checkout

### 2. `/Account/CheckoutSuccess`
- Shown after successful payment
- Onboarding instructions
- Quick action buttons

### 3. `/Account/Settings`
- Displays current plan
- Usage statistics
- Upgrade/manage subscription button

### 4. Customer Portal (Stripe-hosted)
- Change payment method
- View invoices
- Cancel subscription
- Initiated from Settings page

---

## ?? Key Files Created

### Models
- `Models/Subscription.cs` - Subscription data model
- `Models/ApplicationUser.cs` - Updated with Stripe fields

### Services
- `Services/StripeService.cs` - Stripe API integration
- Interface: `IStripeService`
- Plan details and pricing logic

### Controllers
- `Controllers/StripeWebhookController.cs` - Webhook handler
- Processes Stripe events
- Updates database

### Pages
- `Pages/Account/Pricing.cshtml` - Plan selection
- `Pages/Account/Pricing.cshtml.cs` - Checkout logic
- `Pages/Account/CheckoutSuccess.cshtml` - Success page
- `Pages/Account/Settings.cshtml` - Updated with plan info

### Configuration
- `appsettings.json` - Stripe settings
- `Program.cs` - Service registration

---

## ?? Customization

### Change Plan Prices

1. Update prices in Stripe Dashboard
2. Update `StripeService.cs` `GetPlanDetails()` method:

```csharp
SubscriptionPlan.Professional when interval == BillingInterval.Month => new PlanDetails
{
    // ...
    Price = 29, // Change this
    // ...
}
```

### Add New Plan

1. Create product and price in Stripe Dashboard
2. Add to `SubscriptionPlan` enum in `Models/Subscription.cs`
3. Add Price ID to `appsettings.json`
4. Add case in `StripeService.GetPlanDetails()`
5. Add to Pricing page UI

### Customize Trial Period

In `StripeService.CreateCheckoutSessionAsync()`:

```csharp
SubscriptionData = new SessionSubscriptionDataOptions
{
    // ...
    TrialPeriodDays = 14 // Change this number
}
```

---

## ?? Security Best Practices

### Production Setup

1. **Never commit secrets:**
   ```bash
   # Use User Secrets in development
   dotnet user-secrets set "Stripe:SecretKey" "sk_live_..."
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
   ```

2. **Use environment variables in production:**
   - Azure: Application Settings
   - AWS: Secrets Manager
   - Docker: Environment variables

3. **Enable Stripe Radar:**
   - Fraud detection
   - Risk analysis
   - Block suspicious cards

4. **Verify webhook signatures:**
   - Already implemented in `StripeWebhookController`
   - Never skip signature verification

5. **Use HTTPS only:**
   - Enforce SSL in production
   - Stripe requires HTTPS for webhooks

---

## ?? Dashboard Limits by Plan

| Plan | Max Dashboards | Price/Month |
|------|----------------|-------------|
| Free Trial | 3 | $0 |
| Professional | 10 | $29 ($23/mo if annual) |
| Business | Unlimited | $79 ($63/mo if annual) |

To enforce limits, check in `DashboardsController`:

```csharp
var user = await _userManager.GetUserAsync(User);
var maxDashboards = GetMaxDashboardsForPlan(user.CurrentPlan);
var currentCount = await _context.ClientDashboards
    .CountAsync(d => d.FirmId == user.Id && d.Status == DashboardStatus.Active);

if (currentCount >= maxDashboards)
{
    return BadRequest("Dashboard limit reached. Please upgrade your plan.");
}
```

---

## ?? Troubleshooting

### Webhook not receiving events
- Check Stripe CLI is running: `stripe listen`
- Verify webhook URL is correct
- Check firewall settings
- Review webhook logs in Stripe Dashboard

### Checkout session not creating
- Verify API keys are correct
- Check price IDs match Stripe Dashboard
- Review application logs
- Test in Stripe test mode first

### Subscription not updating
- Check webhook secret is correct
- Verify events are being received
- Check database connection
- Review webhook handler logs

### Payment failing
- Use Stripe test cards
- Check card details format
- Verify billing address required
- Review Stripe Dashboard logs

---

## ?? Going Live

### Pre-Launch Checklist

- [ ] Switch to live API keys
- [ ] Create live products and prices in Stripe
- [ ] Update price IDs in configuration
- [ ] Set up live webhook endpoint
- [ ] Test with real credit card (your own)
- [ ] Verify webhook signature in production
- [ ] Enable Stripe Radar
- [ ] Set up email notifications
- [ ] Configure tax collection (if required)
- [ ] Review Terms of Service
- [ ] Enable SCA (Strong Customer Authentication) for EU

### Monitoring

1. **Stripe Dashboard:**
   - Monitor payments
   - View failed charges
   - Check customer disputes

2. **Application Logs:**
   - Webhook processing
   - Subscription updates
   - Payment failures

3. **Database:**
   - Subscription status
   - User plan distribution
   - Revenue metrics

---

## ?? Next Steps

### Recommended Enhancements

1. **Email Notifications:**
   - Payment confirmation
   - Subscription renewal
   - Payment failure
   - Trial ending reminder

2. **Usage Analytics:**
   - Revenue dashboard
   - Subscription growth charts
   - Churn analysis
   - Popular plans

3. **Proration:**
   - Handle mid-cycle upgrades
   - Credit unused time
   - Charge differences

4. **Coupons & Promotions:**
   - Create discount codes in Stripe
   - Apply at checkout
   - Track redemption

5. **Invoice Management:**
   - Download PDF invoices
   - Email invoices
   - Custom branding

---

## ?? Resources

- **Stripe Documentation:** https://stripe.com/docs
- **Stripe.NET Library:** https://github.com/stripe/stripe-dotnet
- **Testing:** https://stripe.com/docs/testing
- **Webhooks:** https://stripe.com/docs/webhooks
- **Checkout:** https://stripe.com/docs/payments/checkout
- **Subscriptions:** https://stripe.com/docs/billing/subscriptions/overview

---

## ?? Support

If you encounter issues:
1. Check Stripe Dashboard logs
2. Review application logs
3. Test with Stripe CLI
4. Consult Stripe documentation
5. Contact Stripe support: https://support.stripe.com

---

**Your payment system is ready!** ??

Follow the setup steps above to start accepting payments.

**Estimated Setup Time:** 30-45 minutes
