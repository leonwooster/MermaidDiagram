using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Interface for analyzing Mermaid diagram syntax and detecting issues
    /// </summary>
    public interface ISyntaxAnalyzer
    {
        /// <summary>
        /// Analyzes the provided Mermaid code and returns a list of detected issues
        /// </summary>
        /// <param name="code">The Mermaid diagram code to analyze</param>
        /// <returns>List of detected syntax issues</returns>
        List<SyntaxIssue> Analyze(string code);
    }
}
