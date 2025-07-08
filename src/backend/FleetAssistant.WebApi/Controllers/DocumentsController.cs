using FleetAssistant.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// Controller for handling document uploads and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IBlobStorageService blobStorageService,
        ILogger<DocumentsController> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single document
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="category">Optional category for organizing documents (e.g., "maintenance", "insurance", "vehicle")</param>
    /// <param name="vehicleId">Optional vehicle ID to associate the document with</param>
    /// <returns>Upload result with document URL and metadata</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<object>> UploadDocument(
        IFormFile file,
        [FromForm] string? category = null,
        [FromForm] int? vehicleId = null)
    {
        try
        {
            if (file == null)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Determine container name based on category
            var containerName = GetContainerName(category);

            // Generate blob name with optional vehicle prefix
            var blobName = GenerateBlobName(file.FileName, vehicleId, category);

            var result = await _blobStorageService.UploadFileAsync(file, containerName, blobName);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            var response = new
            {
                success = true,
                document = new
                {
                    url = result.BlobUrl,
                    fileName = file.FileName,
                    blobName = result.BlobName,
                    containerName = result.ContainerName,
                    fileSizeBytes = result.FileSizeBytes,
                    contentType = result.ContentType,
                    category = category,
                    vehicleId = vehicleId,
                    uploadedAt = DateTime.UtcNow
                }
            };

            _logger.LogInformation("Document uploaded successfully: {FileName} for vehicle {VehicleId} in category {Category}",
                file.FileName, vehicleId, category);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName}", file?.FileName);
            return StatusCode(500, new { error = "Internal server error during upload" });
        }
    }

    /// <summary>
    /// Upload multiple documents
    /// </summary>
    /// <param name="files">The files to upload</param>
    /// <param name="category">Optional category for organizing documents</param>
    /// <param name="vehicleId">Optional vehicle ID to associate the documents with</param>
    /// <returns>Upload results for all files</returns>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB total limit
    public async Task<ActionResult<object>> UploadMultipleDocuments(
        IFormFileCollection files,
        [FromForm] string? category = null,
        [FromForm] int? vehicleId = null)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { error = "No files provided" });
            }

            var results = new List<object>();
            var containerName = GetContainerName(category);

            foreach (var file in files)
            {
                try
                {
                    var blobName = GenerateBlobName(file.FileName, vehicleId, category);
                    var result = await _blobStorageService.UploadFileAsync(file, containerName, blobName);

                    var fileResult = new
                    {
                        fileName = file.FileName,
                        success = result.Success,
                        url = result.BlobUrl,
                        blobName = result.BlobName,
                        fileSizeBytes = result.FileSizeBytes,
                        contentType = result.ContentType,
                        error = result.ErrorMessage
                    };

                    results.Add(fileResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                    results.Add(new
                    {
                        fileName = file.FileName,
                        success = false,
                        error = $"Upload failed: {ex.Message}"
                    });
                }
            }

            var response = new
            {
                totalFiles = files.Count,
                successfulUploads = results.Count(r => ((dynamic)r).success),
                failedUploads = results.Count(r => !((dynamic)r).success),
                category = category,
                vehicleId = vehicleId,
                uploadedAt = DateTime.UtcNow,
                results = results
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple documents");
            return StatusCode(500, new { error = "Internal server error during multiple upload" });
        }
    }

    /// <summary>
    /// Download a document
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>File content</returns>
    [HttpGet("download/{containerName}/{blobName}")]
    public async Task<IActionResult> DownloadDocument(string containerName, string blobName)
    {
        try
        {
            var stream = await _blobStorageService.DownloadFileAsync(containerName, blobName);

            if (stream == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            var contentType = GetContentTypeFromBlobName(blobName);
            var fileName = GetOriginalFileNameFromBlobName(blobName);

            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {BlobName} from container {ContainerName}", blobName, containerName);
            return StatusCode(500, new { error = "Internal server error during download" });
        }
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{containerName}/{blobName}")]
    public async Task<ActionResult<object>> DeleteDocument(string containerName, string blobName)
    {
        try
        {
            var success = await _blobStorageService.DeleteFileAsync(containerName, blobName);

            if (!success)
            {
                return NotFound(new { error = "Document not found or could not be deleted" });
            }

            return Ok(new
            {
                success = true,
                message = "Document deleted successfully",
                containerName = containerName,
                blobName = blobName,
                deletedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {BlobName} from container {ContainerName}", blobName, containerName);
            return StatusCode(500, new { error = "Internal server error during deletion" });
        }
    }

    /// <summary>
    /// List documents in a container
    /// </summary>
    /// <param name="containerName">Container name (optional, uses default if not specified)</param>
    /// <param name="prefix">Optional prefix to filter documents</param>
    /// <returns>List of document names</returns>
    [HttpGet("list")]
    public async Task<ActionResult<object>> ListDocuments(
        [FromQuery] string? containerName = null,
        [FromQuery] string? prefix = null)
    {
        try
        {
            containerName ??= "fleet-documents";

            var blobNames = await _blobStorageService.ListBlobsAsync(containerName, prefix);

            var documents = blobNames.Select(blobName => new
            {
                blobName = blobName,
                url = _blobStorageService.GetBlobUrl(containerName, blobName),
                originalFileName = GetOriginalFileNameFromBlobName(blobName),
                category = ExtractCategoryFromBlobName(blobName),
                vehicleId = ExtractVehicleIdFromBlobName(blobName)
            });

            return Ok(new
            {
                containerName = containerName,
                prefix = prefix,
                documentCount = documents.Count(),
                documents = documents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents from container {ContainerName}", containerName);
            return StatusCode(500, new { error = "Internal server error during listing" });
        }
    }

    /// <summary>
    /// Check if a document exists
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>Existence result</returns>
    [HttpHead("{containerName}/{blobName}")]
    public async Task<IActionResult> DocumentExists(string containerName, string blobName)
    {
        try
        {
            var exists = await _blobStorageService.BlobExistsAsync(containerName, blobName);

            if (exists)
            {
                return Ok();
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document {BlobName} exists in container {ContainerName}", blobName, containerName);
            return StatusCode(500);
        }
    }

    private static string GetContainerName(string? category)
    {
        return category?.ToLowerInvariant() switch
        {
            "maintenance" => "maintenance-documents",
            "insurance" => "insurance-documents",
            "vehicle" => "vehicle-documents",
            "fuel" => "fuel-documents",
            "financial" => "financial-documents",
            _ => "fleet-documents"
        };
    }

    private static string GenerateBlobName(string originalFileName, int? vehicleId, string? category)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var prefix = "";
        if (vehicleId.HasValue)
        {
            prefix += $"vehicle_{vehicleId.Value}_";
        }
        if (!string.IsNullOrEmpty(category))
        {
            prefix += $"{category}_";
        }

        return $"{prefix}{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    private static string GetContentTypeFromBlobName(string blobName)
    {
        var extension = Path.GetExtension(blobName).ToLowerInvariant();

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

    private static string GetOriginalFileNameFromBlobName(string blobName)
    {
        // Try to extract original filename from generated blob name
        // Format: [prefix_]originalName_timestamp_uniqueId.ext
        var parts = blobName.Split('_');
        if (parts.Length >= 3)
        {
            // Find the part that looks like a timestamp (yyyyMMdd_HHmmss)
            for (int i = 0; i < parts.Length - 2; i++)
            {
                if (parts[i].Length == 8 && parts[i + 1].Length == 6)
                {
                    // Found timestamp, take everything before it
                    var originalParts = parts.Take(i).ToArray();
                    var remainingPart = string.Join("_", originalParts);
                    var extension = Path.GetExtension(blobName);
                    return remainingPart + extension;
                }
            }
        }

        // Fallback to blob name
        return blobName;
    }

    private static string? ExtractCategoryFromBlobName(string blobName)
    {
        var knownCategories = new[] { "maintenance", "insurance", "vehicle", "fuel", "financial" };

        foreach (var category in knownCategories)
        {
            if (blobName.StartsWith($"{category}_", StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return null;
    }

    private static int? ExtractVehicleIdFromBlobName(string blobName)
    {
        if (blobName.StartsWith("vehicle_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = blobName.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var vehicleId))
            {
                return vehicleId;
            }
        }

        return null;
    }
}
