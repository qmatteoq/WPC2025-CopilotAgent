namespace TravelAgent
{
    public class ConfigOptions
    {
        public required AzureConfigOptions Azure { get; set; }
    }

    /// <summary>
    /// Options for Azure OpenAI and Azure Content Safety
    /// </summary>
    public class AzureConfigOptions
    {
        public required string OpenAIApiKey { get; set; }
        public required string OpenAIEndpoint { get; set; }
        public required string OpenAIDeploymentName { get; set; }
    }
}