using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MermaidDiagramApp.Services.AI
{
    /// <summary>
    /// AI service implementation for Ollama local LLM provider
    /// </summary>
    public class OllamaAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiConfiguration _config;
        private readonly string _generatePromptTemplate;
        private readonly string _classifyPromptTemplate;
        private readonly string _validatePromptTemplate;

        public OllamaAiService(AiConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_config.BaseUrl),
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
            };

            // Prompt templates for different operations
            _generatePromptTemplate = """
            You are an expert at creating Mermaid diagrams. Generate a complete, syntactically correct Mermaid diagram based on the user's request.
            
            Guidelines:
            1. Always start with the appropriate diagram type declaration (e.g., 'flowchart TD', 'sequenceDiagram', etc.)
            2. Use proper Mermaid syntax
            3. Create clear, readable diagrams
            4. Use appropriate node shapes and connection styles
            5. Include meaningful labels
            
            User request: {0}
            
            Respond ONLY with the Mermaid code, nothing else.
            """;

            _classifyPromptTemplate = """
            Based on the user's request, determine the most appropriate Mermaid diagram type.
            
            Available diagram types:
            - flowchart: For process flows, workflows, decision trees, etc.
            - sequenceDiagram: For showing interactions between different entities over time
            - classDiagram: For showing class relationships and structures
            - stateDiagram: For showing state transitions
            - pie: For showing proportions
            - gantt: For showing project timelines
            - erDiagram: For entity relationship diagrams
            
            User request: {0}
            
            Respond with ONLY the diagram type name (e.g., 'flowchart', 'sequenceDiagram', etc.)
            """;

            _validatePromptTemplate = """
            You are an expert at validating Mermaid diagram syntax. Review the following Mermaid code and:
            1. Identify any syntax errors
            2. Suggest improvements for clarity and best practices
            3. Fix any errors if possible
            
            Mermaid code:
            {0}
            
            Respond with a JSON object containing:
            - "isValid": boolean indicating if the syntax is valid
            - "errors": array of error messages (empty if valid)
            - "suggestions": array of improvement suggestions
            - "fixedCode": the corrected Mermaid code (same as original if no errors)
            """;
        }

        public async Task<string> GenerateMermaidDiagramAsync(string prompt)
        {
            var fullPrompt = string.Format(_generatePromptTemplate, prompt);
            return await SendOllamaRequestAsync(fullPrompt);
        }

        public async Task<string> DetermineDiagramTypeAsync(string prompt)
        {
            var fullPrompt = string.Format(_classifyPromptTemplate, prompt);
            return await SendOllamaRequestAsync(fullPrompt);
        }

        public async Task<string> ValidateAndImproveMermaidAsync(string mermaidCode)
        {
            var fullPrompt = string.Format(_validatePromptTemplate, mermaidCode);
            return await SendOllamaRequestAsync(fullPrompt);
        }

        private async Task<string> SendOllamaRequestAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _config.ModelName,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = _config.Temperature,
                        num_predict = _config.MaxTokens
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/generate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                if (responseObject != null && responseObject.ContainsKey("response"))
                {
                    return responseObject["response"].ToString() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to communicate with Ollama: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
