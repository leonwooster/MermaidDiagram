using System.Collections.Generic;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Interface for applying fixes to detected syntax issues
    /// </summary>
    public interface ISyntaxFixer
    {
        /// <summary>
        /// Applies fixes for the selected issues to the code
        /// </summary>
        /// <param name="code">The original code</param>
        /// <param name="issues">The issues to fix (only selected ones will be applied)</param>
        /// <returns>The fixed code</returns>
        string ApplyFixes(string code, List<SyntaxIssue> issues);
    }
}
