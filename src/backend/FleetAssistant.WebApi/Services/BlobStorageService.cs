using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FleetAssistant.WebApi.Options;
using Microsoft.Extensions.Options;

namespace FleetAssistant.WebApi.Services;

/// <summary>
/// Implementation of blob storage service using Azure Blob Storage
/// </summary>
public class BlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<BlobStorageService> logger) : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
    private readonly BlobStorageOptions _options = options.Value;
    private readonly ILogger<BlobStorageService> _logger = logger;

    public async Task<StorageResult> UploadFileAsync(IFormFile file, string? containerName = null, string? blobName = null)
    {
        try
        {
            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            containerName ??= _options.DefaultContainer;
            blobName ??= GenerateUniqueBlobName(file.FileName);

            // Ensure container exists
            await CreateContainerIfNotExistsAsync(containerName);

            // Get blob client
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload file
            using var stream = file.OpenReadStream();
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(file.FileName)
                }
            };

            await blobClient.UploadAsync(stream, blobUploadOptions);

            var blobUrl = blobClient.Uri.ToString();

            _logger.LogInformation("Successfully uploaded file {FileName} to blob {BlobName} in container {ContainerName}",
                file.FileName, blobName, containerName);

            return StorageResult.SuccessResult(blobUrl, blobName, containerName, file.Length, GetContentType(file.FileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to blob storage", file.FileName);
            return StorageResult.ErrorResult($"Upload failed: {ex.Message}");
        }
    }

    public async Task<StorageResult> UploadStreamAsync(Stream stream, string fileName, string? containerName = null, string? blobName = null)
    {
        try
        {
            // Validate stream
            if (stream == null || stream.Length == 0)
            {
                return StorageResult.ErrorResult("Stream is null or empty");
            }

            if (stream.Length > _options.MaxFileSizeBytes)
            {
                return StorageResult.ErrorResult($"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB");
            }

            var extension = Path.GetExtension(fileName);
            if (!_options.AllowedExtensions.Contains(extension.ToLowerInvariant()))
            {
                return StorageResult.ErrorResult($"File extension {extension} is not allowed");
            }

            containerName ??= _options.DefaultContainer;
            blobName ??= GenerateUniqueBlobName(fileName);

            // Ensure container exists
            await CreateContainerIfNotExistsAsync(containerName);

            // Get blob client
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload stream
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(fileName)
                }
            };

            await blobClient.UploadAsync(stream, blobUploadOptions);

            var blobUrl = blobClient.Uri.ToString();

            _logger.LogInformation("Successfully uploaded stream for file {FileName} to blob {BlobName} in container {ContainerName}",
                fileName, blobName, containerName);

            return StorageResult.SuccessResult(blobUrl, blobName, containerName, stream.Length, GetContentType(fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading stream for file {FileName} to blob storage", fileName);
            return StorageResult.ErrorResult($"Upload failed: {ex.Message}");
        }
    }

    public async Task<Stream?> DownloadFileAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Blob {BlobName} not found in container {ContainerName}", blobName, containerName);
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync();

            _logger.LogInformation("Successfully downloaded blob {BlobName} from container {ContainerName}", blobName, containerName);

            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob {BlobName} from container {ContainerName}", blobName, containerName);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
            }
            else
            {
                _logger.LogWarning("Blob {BlobName} not found in container {ContainerName} for deletion", blobName, containerName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob {BlobName} from container {ContainerName}", blobName, containerName);
            return false;
        }
    }

    public string GetBlobUrl(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> BlobExistsAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if blob {BlobName} exists in container {ContainerName}", blobName, containerName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                _logger.LogWarning("Container {ContainerName} does not exist", containerName);
                return [];
            }

            var blobNames = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobNames.Add(blobItem.Name);
            }

            _logger.LogInformation("Listed {Count} blobs from container {ContainerName} with prefix {Prefix}",
                blobNames.Count, containerName, prefix ?? "none");

            return blobNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing blobs from container {ContainerName} with prefix {Prefix}",
                containerName, prefix ?? "none");
            return [];
        }
    }

    public async Task<bool> CreateContainerIfNotExistsAsync(string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var response = await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            if (response != null)
            {
                _logger.LogInformation("Created container {ContainerName}", containerName);
                return true;
            }

            return true; // Container already exists
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating container {ContainerName}", containerName);
            return false;
        }
    }

    private StorageResult ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return StorageResult.ErrorResult("File is null or empty");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            return StorageResult.ErrorResult($"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !_options.AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            return StorageResult.ErrorResult($"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}");
        }

        return StorageResult.SuccessResult("", "", "", file.Length, GetContentType(file.FileName));
    }

    private static string GenerateUniqueBlobName(string originalFileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}
