using System.Text.Json;

namespace SteadyBooks.Services;

public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest content, CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest content, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string url, CancellationToken cancellationToken = default);
}

public class HttpClientService : IHttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IResiliencePipelineService _resiliencePipelineService;
    private readonly ILogger<HttpClientService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        IResiliencePipelineService resiliencePipelineService,
        ILogger<HttpClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _resiliencePipelineService = resiliencePipelineService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetHttpPipeline<T>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            using var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("Making GET request to {Url}", url);
            
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url, 
        TRequest content, 
        CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetHttpPipeline<TResponse>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            using var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("Making POST request to {Url}", url);
            
            var jsonContent = JsonSerializer.Serialize(content, _jsonOptions);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(url, httpContent, ct);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
        }, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url, 
        TRequest content, 
        CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetHttpPipeline<TResponse>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            using var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("Making PUT request to {Url}", url);
            
            var jsonContent = JsonSerializer.Serialize(content, _jsonOptions);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync(url, httpContent, ct);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetHttpPipeline();
        
        await pipeline.ExecuteAsync(async ct =>
        {
            using var client = _httpClientFactory.CreateClient();
            _logger.LogInformation("Making DELETE request to {Url}", url);
            
            var response = await client.DeleteAsync(url, ct);
            response.EnsureSuccessStatusCode();
        }, cancellationToken);

        return true;
    }
}
