using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Interface for individual syntax detection rules
    /// </summary>
    public interface ISyntaxRule
    {
        /// <summary>
        /// Gets the name of the rule
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether the rule is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the severity level for issues detected by this rule
        /// </summary>
        IssueSeverity Severity { get; set; }

        /// <summary>
        /// Detects issues in the provided code according to this rule
        /// </summary>
        /// <param name="code">The code to analyze</param>
        /// <returns>List of detected issues</returns>
        List<SyntaxIssue> Detect(string code);
    }
}
