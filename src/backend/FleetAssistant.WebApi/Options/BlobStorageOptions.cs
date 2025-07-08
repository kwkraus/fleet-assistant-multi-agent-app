using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.WebApi.Services;

/// <summary>
/// Options for configuring Azure Blob Storage
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Azure Storage connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Default container name for fleet documents
    /// </summary>
    [Required]
    public string DefaultContainer { get; set; } = "fleet-documents";

    /// <summary>
    /// Maximum file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions
    /// </summary>
    public string[] AllowedExtensions { get; set; } = 
    {
        ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".csv"
    };
}
