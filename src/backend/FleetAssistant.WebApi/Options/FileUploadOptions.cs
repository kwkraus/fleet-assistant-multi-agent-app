namespace FleetAssistant.WebApi.Options;

/// <summary>
/// Configuration options for file uploads
/// </summary>
public class FileUploadOptions
{
    public const string SectionName = "FileUpload";
    
    /// <summary>
    /// Maximum file size in bytes (default: 3MB)
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 3 * 1024 * 1024; // 3MB

    /// <summary>
    /// Maximum number of files per request (default: 2)
    /// </summary>
    public int MaxFileCount { get; set; } = 2;

    /// <summary>
    /// Allowed file extensions
    /// </summary>
    public string[] AllowedFileTypes { get; set; } = 
    { 
        ".pdf", ".txt", ".docx", ".doc", 
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", 
        ".csv", ".xlsx", ".xls"
    };
}
