using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.AI
{
    /// <summary>
    /// Interface for AI services that can generate Mermaid diagrams from natural language prompts
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// Generates a Mermaid diagram from a natural language prompt
        /// </summary>
        /// <param name="prompt">The natural language prompt describing the desired diagram</param>
        /// <returns>The generated Mermaid diagram code</returns>
        Task<string> GenerateMermaidDiagramAsync(string prompt);

        /// <summary>
        /// Determines the most suitable diagram type for a given prompt
        /// </summary>
        /// <param name="prompt">The natural language prompt</param>
        /// <returns>The recommended diagram type</returns>
        Task<string> DetermineDiagramTypeAsync(string prompt);

        /// <summary>
        /// Validates Mermaid syntax and provides suggestions for improvement
        /// </summary>
        /// <param name="mermaidCode">The Mermaid code to validate</param>
        /// <returns>Validation result with suggestions</returns>
        Task<string> ValidateAndImproveMermaidAsync(string mermaidCode);
    }
}
