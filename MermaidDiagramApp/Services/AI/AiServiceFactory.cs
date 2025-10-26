using System;

namespace MermaidDiagramApp.Services.AI
{
    /// <summary>
    /// Factory for creating AI service instances based on configuration
    /// </summary>
    public static class AiServiceFactory
    {
        /// <summary>
        /// Creates an AI service instance based on the provided configuration
        /// </summary>
        /// <param name="config">AI configuration settings</param>
        /// <returns>Configured AI service instance</returns>
        public static IAiService CreateAiService(AiConfiguration config)
        {
            return config.ProviderType.ToLower() switch
            {
                "ollama" => new OllamaAiService(config),
                "openai" => new OpenAiService(config),
                "azure" => new OpenAiService(config),
                _ => throw new ArgumentException($"Unsupported AI provider: {config.ProviderType}")
            };
        }
    }
}
