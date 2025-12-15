using Microsoft.Extensions.Options;
using SteadyBooks.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SteadyBooks.Services;

public interface IQuickBooksApiService
{
    Task<decimal> GetCashBalanceAsync(string accessToken, string realmId, DateTime asOfDate);
    Task<ProfitLossData> GetProfitLossAsync(string accessToken, string realmId, DateTime startDate, DateTime endDate);
    Task<decimal> GetTaxLiabilityAsync(string accessToken, string realmId);
    Task<decimal> GetOutstandingInvoicesAsync(string accessToken, string realmId);
    Task<CompanyInfoResponse?> GetCompanyInfoAsync(string accessToken, string realmId);
}

public class QuickBooksApiService : IQuickBooksApiService
{
    private readonly QuickBooksSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuickBooksApiService> _logger;

    public QuickBooksApiService(
        IOptions<QuickBooksSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<QuickBooksApiService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal> GetCashBalanceAsync(string accessToken, string realmId, DateTime asOfDate)
    {
        try
        {
            var client = CreateAuthenticatedClient(accessToken);
            
            // Query for bank and cash accounts
            var query = "SELECT * FROM Account WHERE AccountType IN ('Bank', 'Other Current Asset') AND AccountSubType IN ('CashOnHand', 'Checking', 'Savings')";
            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/query?query={Uri.EscapeDataString(query)}&minorversion=65";
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch cash accounts. Status: {Status}, Response: {Response}", 
                    response.StatusCode, json);
                return 0;
            }

            var doc = JsonDocument.Parse(json);
            decimal totalBalance = 0;

            if (doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) &&
                queryResponse.TryGetProperty("Account", out var accounts))
            {
                foreach (var account in accounts.EnumerateArray())
                {
                    if (account.TryGetProperty("CurrentBalance", out var balance))
                    {
                        totalBalance += balance.GetDecimal();
                    }
                }
            }

            _logger.LogInformation("Cash balance calculated: {Balance}", totalBalance);
            return totalBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cash balance from QuickBooks");
            return 0;
        }
    }

    public async Task<ProfitLossData> GetProfitLossAsync(string accessToken, string realmId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var client = CreateAuthenticatedClient(accessToken);
            
            var start = startDate.ToString("yyyy-MM-dd");
            var end = endDate.ToString("yyyy-MM-dd");
            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/reports/ProfitAndLoss?start_date={start}&end_date={end}&minorversion=65";
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch P&L. Status: {Status}, Response: {Response}", 
                    response.StatusCode, json);
                return new ProfitLossData();
            }

            var doc = JsonDocument.Parse(json);
            decimal revenue = 0;
            decimal expenses = 0;

            // Parse the P&L report structure
            if (doc.RootElement.TryGetProperty("Rows", out var rows) &&
                rows.TryGetProperty("Row", out var rowArray))
            {
                foreach (var section in rowArray.EnumerateArray())
                {
                    if (!section.TryGetProperty("Header", out var header)) continue;
                    
                    var headerText = header.TryGetProperty("ColData", out var colData) &&
                                   colData.EnumerateArray().Any() &&
                                   colData.EnumerateArray().First().TryGetProperty("value", out var value)
                        ? value.GetString() ?? ""
                        : "";

                    if (section.TryGetProperty("Summary", out var summary) &&
                        summary.TryGetProperty("ColData", out var summaryColData))
                    {
                        var summaryArray = summaryColData.EnumerateArray().ToList();
                        if (summaryArray.Count > 1 && summaryArray[1].TryGetProperty("value", out var amountValue))
                        {
                            if (decimal.TryParse(amountValue.GetString(), out var amount))
                            {
                                if (headerText.Contains("Income", StringComparison.OrdinalIgnoreCase) ||
                                    headerText.Contains("Revenue", StringComparison.OrdinalIgnoreCase))
                                {
                                    revenue += amount;
                                }
                                else if (headerText.Contains("Expense", StringComparison.OrdinalIgnoreCase) ||
                                        headerText.Contains("Cost", StringComparison.OrdinalIgnoreCase))
                                {
                                    expenses += Math.Abs(amount);
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("P&L calculated - Revenue: {Revenue}, Expenses: {Expenses}, Profit: {Profit}", 
                revenue, expenses, revenue - expenses);

            return new ProfitLossData
            {
                Revenue = revenue,
                Expenses = expenses,
                Profit = revenue - expenses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching P&L from QuickBooks");
            return new ProfitLossData();
        }
    }

    public async Task<decimal> GetTaxLiabilityAsync(string accessToken, string realmId)
    {
        try
        {
            var client = CreateAuthenticatedClient(accessToken);
            
            // Query for tax payable accounts
            var query = "SELECT * FROM Account WHERE AccountType = 'Other Current Liability' AND Name LIKE '%tax%'";
            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/query?query={Uri.EscapeDataString(query)}&minorversion=65";
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch tax accounts. Status: {Status}, Response: {Response}", 
                    response.StatusCode, json);
                return 0;
            }

            var doc = JsonDocument.Parse(json);
            decimal totalTax = 0;

            if (doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) &&
                queryResponse.TryGetProperty("Account", out var accounts))
            {
                foreach (var account in accounts.EnumerateArray())
                {
                    if (account.TryGetProperty("CurrentBalance", out var balance))
                    {
                        totalTax += Math.Abs(balance.GetDecimal());
                    }
                }
            }

            _logger.LogInformation("Tax liability calculated: {Tax}", totalTax);
            return totalTax;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tax liability from QuickBooks");
            return 0;
        }
    }

    public async Task<decimal> GetOutstandingInvoicesAsync(string accessToken, string realmId)
    {
        try
        {
            var client = CreateAuthenticatedClient(accessToken);
            
            // Query for unpaid invoices
            var query = "SELECT * FROM Invoice WHERE Balance != '0'";
            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/query?query={Uri.EscapeDataString(query)}&minorversion=65";
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch invoices. Status: {Status}, Response: {Response}", 
                    response.StatusCode, json);
                return 0;
            }

            var doc = JsonDocument.Parse(json);
            decimal totalOutstanding = 0;

            if (doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) &&
                queryResponse.TryGetProperty("Invoice", out var invoices))
            {
                foreach (var invoice in invoices.EnumerateArray())
                {
                    if (invoice.TryGetProperty("Balance", out var balance))
                    {
                        totalOutstanding += balance.GetDecimal();
                    }
                }
            }

            _logger.LogInformation("Outstanding invoices calculated: {Amount}", totalOutstanding);
            return totalOutstanding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching outstanding invoices from QuickBooks");
            return 0;
        }
    }

    public async Task<CompanyInfoResponse?> GetCompanyInfoAsync(string accessToken, string realmId)
    {
        try
        {
            var client = CreateAuthenticatedClient(accessToken);
            var url = $"{_settings.ApiBaseUrl}/v3/company/{realmId}/companyinfo/{realmId}?minorversion=65";
            
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch company info. Status: {Status}", response.StatusCode);
                return null;
            }

            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("CompanyInfo", out var companyInfo))
            {
                var name = companyInfo.TryGetProperty("CompanyName", out var nameElement) 
                    ? nameElement.GetString() ?? "" 
                    : "";
                
                return new CompanyInfoResponse { CompanyName = name };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company info from QuickBooks");
            return null;
        }
    }

    private HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

public class ProfitLossData
{
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

public class CompanyInfoResponse
{
    public string CompanyName { get; set; } = string.Empty;
}
