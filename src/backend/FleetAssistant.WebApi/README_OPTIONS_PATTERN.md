# FoundryAgentService Configuration Options

## Overview

The `FoundryAgentService` has been refactored to use the .NET Options pattern for dependency injection of configuration settings. This provides better type safety, validation, and maintainability.

## Configuration Class

The `FoundryAgentOptions` class is located in `Services/FoundryAgentOptions.cs` and contains all configuration options for the Azure AI Foundry Agent Service:

```csharp
public class FoundryAgentOptions
{
    public const string SectionName = "FoundryAgentService";
    
    [Required]
    public string AgentId { get; set; } = string.Empty;
    
    [Required]
    public string AgentEndpoint { get; set; } = string.Empty;
    
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public int StreamingDelayMs { get; set; } = 10;
    public int RunPollingDelayMs { get; set; } = 100;
}
```

## Configuration Files

### appsettings.json
```json
{
  "FoundryAgentService": {
    "AgentId": "YOUR_AGENT_ID_HERE",
    "AgentEndpoint": "YOUR_FOUNDRY_AGENT_ENDPOINT_HERE",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "StreamingDelayMs": 10,
    "RunPollingDelayMs": 100
  }
}
```

### appsettings.Development.json
```json
{
  "FoundryAgentService": {
    "AgentId": "YOUR_AGENT_ID_HERE",
    "AgentEndpoint": "YOUR_FOUNDRY_AGENT_ENDPOINT_HERE",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "StreamingDelayMs": 5,
    "RunPollingDelayMs": 50
  }
}
```

## Dependency Injection Setup

The configuration is registered in `Program.cs`:

```csharp
// Configure FoundryAgentOptions
builder.Services.Configure<FoundryAgentOptions>(
    builder.Configuration.GetSection(FoundryAgentOptions.SectionName));

// Register the service
builder.Services.AddSingleton<IAgentServiceClient, FoundryAgentService>();
```

## Service Usage

The `FoundryAgentService` constructor now accepts `IOptions<FoundryAgentOptions>`:

```csharp
public FoundryAgentService(
    ILogger<FoundryAgentService> logger,
    IOptions<FoundryAgentOptions> options)
{
    _logger = logger;
    _options = options.Value;
    // ... initialization
}
```

## Benefits

1. **Type Safety**: Configuration is strongly typed
2. **Validation**: Uses Data Annotations for required fields
3. **Testability**: Easy to mock and test with different configurations
4. **Environment-specific**: Different values for Development vs Production
5. **Maintainability**: Centralized configuration management
6. **Performance Tuning**: Configurable delays and timeouts

## Configuration Options Explained

- **AgentId**: The unique identifier for your Azure AI Foundry agent
- **AgentEndpoint**: The endpoint URL for your Azure AI Foundry service
- **TimeoutSeconds**: Maximum time to wait for agent operations (default: 30s)
- **RetryCount**: Number of retry attempts for failed operations (default: 3)
- **StreamingDelayMs**: Delay between streaming response chunks (default: 10ms, 5ms in dev)
- **RunPollingDelayMs**: Delay between polling agent run status (default: 100ms, 50ms in dev)

The development environment uses faster polling and streaming for better debugging experience.
