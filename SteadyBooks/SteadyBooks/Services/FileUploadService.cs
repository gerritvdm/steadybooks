namespace SteadyBooks.Services;

public interface IFileUploadService
{
    Task<string?> UploadLogoAsync(IFormFile file, string userId);
    Task DeleteLogoAsync(string? logoPath);
    bool IsValidLogoFile(IFormFile file);
}

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string[] _allowedExtensions = { ".png", ".jpg", ".jpeg", ".svg" };
    private const long MaxFileSize = 2 * 1024 * 1024; // 2MB

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public bool IsValidLogoFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    public async Task<string?> UploadLogoAsync(IFormFile file, string userId)
    {
        try
        {
            if (!IsValidLogoFile(file))
            {
                _logger.LogWarning("Invalid logo file upload attempt by user {UserId}", userId);
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/logos/{fileName}";
            _logger.LogInformation("Logo uploaded successfully for user {UserId}: {Path}", userId, relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for user {UserId}", userId);
            return null;
        }
    }

    public async Task DeleteLogoAsync(string? logoPath)
    {
        if (string.IsNullOrEmpty(logoPath))
            return;

        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, logoPath.TrimStart('/'));
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("Logo deleted: {Path}", logoPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting logo at path {Path}", logoPath);
        }
    }
}
