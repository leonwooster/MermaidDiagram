using System;

namespace MermaidDiagramApp.Models
{
    public class SyntaxIssue
    {
        public string Description { get; set; } = string.Empty;
        public Func<string, string> ProposeFix { get; set; } = code => code;
    }
}
