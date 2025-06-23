using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FleetAssistant.Api.Services;

/// <summary>
/// Service for handling JWT authentication and user context extraction
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates a JWT token and extracts user context
    /// </summary>
    /// <param name="authHeader">Authorization header value</param>
    /// <returns>User context if valid, null if invalid</returns>
    Task<UserContext?> ValidateTokenAndGetUserContextAsync(string? authHeader);
}

/// <summary>
/// Implementation of JWT authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
    }

    public async Task<UserContext?> ValidateTokenAndGetUserContextAsync(string? authHeader)
    {
        try
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Missing or invalid authorization header");
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Empty JWT token");
                return null;
            }

            // For development/testing, we'll implement a basic token parser
            // In production, this would validate against a proper OIDC provider
            var userContext = await ParseTokenAsync(token);
            
            if (userContext == null)
            {
                _logger.LogWarning("Failed to parse user context from token");
                return null;
            }

            _logger.LogInformation("Successfully authenticated user {UserId} for tenant {TenantId}", 
                userContext.UserId, userContext.TenantId);

            return userContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    private async Task<UserContext?> ParseTokenAsync(string token)
    {
        try
        {
            // For development, we'll create a simple token parser
            // In production, this would use proper JWT validation with issuer verification
            var jwtHandler = new JwtSecurityTokenHandler();
            
            if (!jwtHandler.CanReadToken(token))
            {
                _logger.LogWarning("Token is not a valid JWT");
                return null;
            }

            var jwtToken = jwtHandler.ReadJwtToken(token);
            
            // For development/demo purposes, we'll accept any well-formed JWT
            // In production, you would validate signature, issuer, audience, etc.
            var userContext = new UserContext
            {
                UserId = GetClaimValue(jwtToken, ClaimTypes.NameIdentifier) ?? GetClaimValue(jwtToken, "sub") ?? "dev-user",
                Email = GetClaimValue(jwtToken, ClaimTypes.Email) ?? GetClaimValue(jwtToken, "email") ?? "dev@example.com",
                TenantId = GetClaimValue(jwtToken, "tenant_id") ?? "dev-tenant",
                AuthorizedTenantIds = ParseTenantIds(jwtToken),
                Roles = ParseRoles(jwtToken),
                Claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value)
            };

            return await Task.FromResult(userContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JWT token");
            return null;
        }
    }

    private string? GetClaimValue(JwtSecurityToken token, string claimType)
    {
        return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    private List<string> ParseTenantIds(JwtSecurityToken token)
    {
        var tenantClaims = token.Claims.Where(c => c.Type == "tenant_id" || c.Type == "tenants").ToList();
        
        if (tenantClaims.Any())
        {
            return tenantClaims.Select(c => c.Value).Distinct().ToList();
        }

        // Default for development
        return new List<string> { "dev-tenant" };
    }

    private List<string> ParseRoles(JwtSecurityToken token)
    {
        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "roles").ToList();
        
        if (roleClaims.Any())
        {
            return roleClaims.Select(c => c.Value).Distinct().ToList();
        }

        // Default for development
        return new List<string> { "user" };
    }
}
