using Azure.AI.Agents.Persistent;
using Azure.Identity;
using FleetAssistant.WebApi.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FleetAssistant.WebApi.HealthChecks
{
    public class FoundryHealthCheck : IHealthCheck
    {
        private readonly PersistentAgentsClient _agentClient;
        private readonly ILogger<FoundryHealthCheck> _logger;
        private readonly FoundryAgentOptions _options;

        public FoundryHealthCheck(
            ILogger<FoundryHealthCheck> logger,
            IOptions<FoundryAgentOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            // Validate required configuration
            if (string.IsNullOrEmpty(_options.AgentEndpoint))
                throw new InvalidOperationException("FoundryAgentService:AgentEndpoint configuration is required");

            if (string.IsNullOrEmpty(_options.AgentId))
                throw new InvalidOperationException("FoundryAgentService:AgentId configuration is required");

            // Initialize PersistentAgentClient with DefaultAzureCredential
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = false,
                ExcludeManagedIdentityCredential = false,
                ExcludeSharedTokenCacheCredential = false,
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeAzurePowerShellCredential = false,
                ExcludeInteractiveBrowserCredential = false
            });

            _agentClient = new PersistentAgentsClient(_options.AgentEndpoint, credential);
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // ...
            var agents = _agentClient.Administration.GetAgents(limit: 1, cancellationToken: cancellationToken).AsEnumerable<PersistentAgent>();

            if (agents == null)
            {
                return Task.FromResult(
                   new HealthCheckResult(
                       context.Registration.FailureStatus, "Connection to AI Foundry Agent Service failed: Unable to retrieve agents."));
            }
            return Task.FromResult(HealthCheckResult.Healthy("Connection to AI Foundry Agent Service succeeded"));

        }
    }
}
