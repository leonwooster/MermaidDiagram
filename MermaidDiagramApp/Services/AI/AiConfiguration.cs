namespace MermaidDiagramApp.Services.AI
{
    /// <summary>
    /// Configuration settings for AI services
    /// </summary>
    public class AiConfiguration
    {
        /// <summary>
        /// The type of AI provider to use (OpenAI, Azure, Ollama, etc.)
        /// </summary>
        public string ProviderType { get; set; } = "OpenAI";

        /// <summary>
        /// API key for cloud-based providers
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for local providers like Ollama
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Model name to use for generation
        /// </summary>
        public string ModelName { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// Temperature setting for generation (0.0 to 1.0)
        /// </summary>
        public double Temperature { get; set; } = 0.7;
    }
}
