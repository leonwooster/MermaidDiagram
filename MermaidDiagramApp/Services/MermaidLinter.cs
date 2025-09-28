using System;
using MermaidDiagramApp.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MermaidDiagramApp.Services
{
    public class MermaidLinter
    {
        public List<SyntaxIssue> Lint(string code, Version mermaidVersion)
        {
            var issues = new List<SyntaxIssue>();

            // Rule: Check for incompatible Font Awesome syntax
            if (mermaidVersion < new Version(10, 0, 0))
            {
                // Older versions expect fa(icon-name)
                var modernIconRegex = new Regex(@"`fa:fa-([a-zA-Z0-9-]+)`");
                if (modernIconRegex.IsMatch(code))
                {
                    issues.Add(new SyntaxIssue
                    {
                        Description = "You are using modern Font Awesome syntax (e.g., `fa:fa-icon`) with an older version of Mermaid.js.",
                        ProposeFix = (currentCode) => modernIconRegex.Replace(currentCode, "fa($1)")
                    });
                }
            }
            else
            {
                // Modern versions expect `fa:fa-icon-name`
                var legacyIconRegex = new Regex(@"fa\((fa-[a-zA-Z0-9-]+)\)");
                if (legacyIconRegex.IsMatch(code))
                {
                    issues.Add(new SyntaxIssue
                    {
                        Description = "You are using legacy Font Awesome syntax (e.g., fa(fa-icon)) with a modern version of Mermaid.js.",
                        ProposeFix = (currentCode) => legacyIconRegex.Replace(currentCode, "`fa:fa-$1`")
                    });
                }
            }

            return issues;
        }
    }
}
