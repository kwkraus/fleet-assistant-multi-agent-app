namespace FleetAssistant.WebApi.Services.Interfaces;

/// <summary>
/// Represents the result of a blob storage operation
/// </summary>
public class StorageResult
{
    public bool Success { get; set; }
    public string? BlobUrl { get; set; }
    public string? BlobName { get; set; }
    public string? ContainerName { get; set; }
    public string? ErrorMessage { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ContentType { get; set; }

    public static StorageResult SuccessResult(string blobUrl, string blobName, string containerName, long fileSizeBytes, string contentType)
    {
        return new StorageResult
        {
            Success = true,
            BlobUrl = blobUrl,
            BlobName = blobName,
            ContainerName = containerName,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType
        };
    }

    public static StorageResult ErrorResult(string errorMessage)
    {
        return new StorageResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Interface for blob storage operations
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file to blob storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="containerName">Optional container name (uses default if not specified)</param>
    /// <param name="blobName">Optional blob name (generates unique name if not specified)</param>
    /// <returns>Result of the upload operation</returns>
    Task<StorageResult> UploadFileAsync(IFormFile file, string? containerName = null, string? blobName = null);

    /// <summary>
    /// Uploads a stream to blob storage
    /// </summary>
    /// <param name="stream">The stream to upload</param>
    /// <param name="fileName">Original file name for content type detection</param>
    /// <param name="containerName">Optional container name (uses default if not specified)</param>
    /// <param name="blobName">Optional blob name (generates unique name if not specified)</param>
    /// <returns>Result of the upload operation</returns>
    Task<StorageResult> UploadStreamAsync(Stream stream, string fileName, string? containerName = null, string? blobName = null);

    /// <summary>
    /// Downloads a file from blob storage
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>Stream containing the file data</returns>
    Task<Stream?> DownloadFileAsync(string containerName, string blobName);

    /// <summary>
    /// Deletes a file from blob storage
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(string containerName, string blobName);

    /// <summary>
    /// Gets the URL for a blob
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>URL to the blob</returns>
    string GetBlobUrl(string containerName, string blobName);

    /// <summary>
    /// Checks if a blob exists
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <returns>True if the blob exists</returns>
    Task<bool> BlobExistsAsync(string containerName, string blobName);

    /// <summary>
    /// Lists all blobs in a container
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="prefix">Optional prefix to filter blobs</param>
    /// <returns>List of blob names</returns>
    Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null);

    /// <summary>
    /// Creates a container if it doesn't exist
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <returns>True if created or already exists</returns>
    Task<bool> CreateContainerIfNotExistsAsync(string containerName);
}
