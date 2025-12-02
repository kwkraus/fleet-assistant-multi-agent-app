# Azure DevOps Copilot MCP Setup

This project adds the Azure DevOps MCP server so Copilot coding agent sessions can inspect and manipulate Pipelines, Repos, Work Items, Wikis, and other Azure DevOps artifacts. The setup follows the GitHub guidance for Copilot MCP extensions (see the Azure DevOps example section in  https://docs.github.com/en/enterprise-cloud@latest/copilot/how-tos/use-copilot-agents/coding-agent/extend-coding-agent-with-mcp#example-azure-devops).

## Authentication workflow

- The repository now owns `.github/workflows/copilot-setup-steps.yml`. It runs on-demand, requests `id-token` + `contents` permissions, and logs in via `azure/login`. This ensures that the Copilot agent can mint OIDC tokens for Azure DevOps tools without human interaction.
- The workflow expects the repository/environment secrets described in the next section. It is executed automatically whenever Copilot coding agent starts the MCP servers.

## Copilot environment secrets

1. Create or update the GitHub Copilot environment named `copilot` (Settings → Copilot → Coding agent → Environments).
2. Add environment secrets for `AZURE_CLIENT_ID` and `AZURE_TENANT_ID` so the workflow can authenticate with the Azure login action.
3. If the Azure service principal is scoped to a specific subscription, grant the identity access to that subscription and to the target Azure DevOps organization/project per Microsoft docs (see "Use the Azure Login action with OpenID Connect" and "Add organization users and manage access").
4. Make sure the workflow runs in the `copilot` environment and that the identity has `id-token` permissions (`allow-no-subscriptions: true` is already configured so Azure resources are optional).

## MCP JSON configuration

After the workflow and secrets are set up, add the following JSON to the repo’s Copilot coding agent MCP configuration in the Settings UI. Replace `<your-azure-devops-org>` with your organization name.

```json
{
  "mcpServers": {
    "ado": {
      "type": "local",
      "command": "npx",
      "args": [
        "-y",
        "@azure-devops/mcp",
        "<your-azure-devops-org>",
        "-a",
        "azcli",
        "-d",
        "core",
        "work-items",
        "pipelines"
      ],
      "tools": [
        "mcp_ado_core_list_projects",
        "mcp_ado_repo_list_pull_requests_by_repo_or_project",
        "mcp_ado_wit_get_work_item",
        "mcp_ado_wit_list_work_item_comments",
        "mcp_ado_pipelines_list_runs",
        "mcp_ado_repo_get_repo_by_name_or_id",
        "mcp_ado_wiki_get_wiki"
      ]
    }
  }
}
```

This example limits the server to the `core`, `work-items`, and `pipelines` domains so the toolset stays focused on project metadata, work items, pipelines, and repository lookups. Consult the Azure DevOps MCP tool list (https://github.com/microsoft/azure-devops-mcp/blob/main/docs/TOOLSET.md) to add additional tools or domains such as `wiki`, `repositories`, `search`, or `test-plans`. Only enable the tools that the agent actually needs to keep interactions predictable and avoid exposing write hooks unless they are required.

If your Copilot configuration uses an MCP server elsewhere, you can merge this `ado` entry with the existing JSON. Be sure to include whichever `tools` arrays make sense, and keep the `type`/`command`/`args` structure for a local NPX-based server as shown above.

## Validation

- Create an issue and assign it to Copilot to trigger an agent session (see the GitHub doc's validation section). Copilot should now increase its tooling list with the Azure DevOps tools defined here.
- If a tool fails to start because it requires extra packages (for example tooling beyond Node.js), extend `copilot-setup-steps.yml` with additional install steps before the Azure login step.

## Additional notes

- Always check the `tools` list in https://github.com/microsoft/azure-devops-mcp/tree/main/docs/TOOLSET.md when a new request comes in so you know what data the MCP server can fetch.
- Keep the `copilot` environment secrets up to date whenever the Azure service principal rotates credentials yourself or via automation.
