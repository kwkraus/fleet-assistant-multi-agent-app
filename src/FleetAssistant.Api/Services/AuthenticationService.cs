using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace FleetAssistant.Api.Services;

/// <summary>
/// Service for handling API Key authentication and user context extraction
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates an API key and extracts user context
    /// </summary>
    /// <param name="authHeader">Authorization header value (X-API-Key or Authorization: Bearer)</param>
    /// <returns>User context if valid, null if invalid</returns>
    Task<UserContext?> ValidateApiKeyAndGetUserContextAsync(string? authHeader);

    /// <summary>
    /// Generates a new API key for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="name">Display name for the API key</param>
    /// <param name="scopes">Permissions for the API key</param>
    /// <returns>The generated API key (store this securely - it won't be shown again)</returns>
    Task<(string apiKey, ApiKeyInfo keyInfo)> GenerateApiKeyAsync(string tenantId, string name, List<string> scopes);
}

/// <summary>
/// Implementation of API Key authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    
    // For MVP, we'll use in-memory storage. In production, this would be in a database
    private static readonly Dictionary<string, ApiKeyInfo> _apiKeys = new();

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
        
        // Initialize with some development API keys if empty
        if (!_apiKeys.Any())
        {
            InitializeDevelopmentApiKeys();
        }
    }    public async Task<UserContext?> ValidateApiKeyAndGetUserContextAsync(string? authHeader)
    {
        try
        {
            var apiKey = ExtractApiKeyFromHeader(authHeader);
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Missing or invalid API key in request");
                return null;
            }

            var hashedApiKey = HashApiKey(apiKey);
            var keyInfo = _apiKeys.Values.FirstOrDefault(k => k.HashedApiKey == hashedApiKey && k.IsActive);
            
            if (keyInfo == null)
            {
                _logger.LogWarning("Invalid API key provided");
                return null;
            }

            // Check expiration
            if (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key used: {ApiKeyId}", keyInfo.ApiKeyId);
                return null;
            }

            // Update last used timestamp
            keyInfo.LastUsedAt = DateTime.UtcNow;

            var userContext = new UserContext
            {
                ApiKeyId = keyInfo.ApiKeyId,
                ApiKeyName = keyInfo.Name,
                TenantId = keyInfo.TenantId,
                Environment = keyInfo.Environment,
                Scopes = keyInfo.Scopes,
                CreatedAt = keyInfo.CreatedAt,
                LastUsedAt = keyInfo.LastUsedAt,
                Metadata = keyInfo.Metadata
            };

            _logger.LogInformation("Successfully authenticated API key {ApiKeyId} for tenant {TenantId}", 
                keyInfo.ApiKeyId, keyInfo.TenantId);

            return await Task.FromResult(userContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return null;
        }
    }

    public async Task<(string apiKey, ApiKeyInfo keyInfo)> GenerateApiKeyAsync(string tenantId, string name, List<string> scopes)
    {
        var apiKey = GenerateSecureApiKey();
        var hashedApiKey = HashApiKey(apiKey);
        var apiKeyId = Guid.NewGuid().ToString();

        var keyInfo = new ApiKeyInfo
        {
            ApiKeyId = apiKeyId,
            HashedApiKey = hashedApiKey,
            Name = name,
            TenantId = tenantId,
            Environment = "development", // Could be passed as parameter
            Scopes = scopes ?? new List<string> { "fleet:read", "fleet:query" },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _apiKeys[apiKeyId] = keyInfo;

        _logger.LogInformation("Generated new API key {ApiKeyId} for tenant {TenantId}", apiKeyId, tenantId);

        return await Task.FromResult((apiKey, keyInfo));
    }

    private string? ExtractApiKeyFromHeader(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
            return null;

        // Support both "Bearer {api-key}" and "ApiKey {api-key}" formats
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        
        if (authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("ApiKey ".Length).Trim();
        }

        // Also support direct API key (for X-API-Key header)
        return authHeader.Trim();
    }

    private string GenerateSecureApiKey()
    {
        // Generate a secure random API key: fa_live_xxxxxxxxxxxxxxxxxxxx (30 chars total)
        const string prefix = "fa_dev_";
        const int keyLength = 24;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[keyLength];
        rng.GetBytes(bytes);
        
        var result = new StringBuilder();
        foreach (var b in bytes)
        {
            result.Append(chars[b % chars.Length]);
        }
        
        return prefix + result.ToString();
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }

    private void InitializeDevelopmentApiKeys()
    {
        // Create some development API keys for testing
        var devApiKeys = new[]
        {
            new { TenantId = "contoso-fleet", Name = "Contoso Development Key", Scopes = new[] { "fleet:read", "fleet:query", "fleet:admin" } },
            new { TenantId = "acme-logistics", Name = "ACME Development Key", Scopes = new[] { "fleet:read", "fleet:query" } },
            new { TenantId = "northwind-transport", Name = "Northwind Development Key", Scopes = new[] { "fleet:read", "fleet:query" } }
        };

        foreach (var keyData in devApiKeys)
        {
            var (apiKey, keyInfo) = GenerateApiKeyAsync(keyData.TenantId, keyData.Name, keyData.Scopes.ToList()).Result;
            
            // For development, log the API keys so you can use them for testing
            _logger.LogInformation("Development API Key for {TenantId}: {ApiKey}", keyData.TenantId, apiKey);
        }
    }
}
