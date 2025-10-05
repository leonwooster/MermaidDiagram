using System.Collections.Generic;
using System.Linq;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Applies fixes to detected syntax issues
    /// </summary>
    public class MermaidSyntaxFixer : ISyntaxFixer
    {
        public string ApplyFixes(string code, List<SyntaxIssue> issues)
        {
            // Only process selected issues
            var selectedIssues = issues.Where(i => i.IsSelected).ToList();
            
            if (!selectedIssues.Any())
            {
                return code;
            }

            // Calculate absolute positions once from the original code
            var issuesWithPositions = selectedIssues
                .Select(i => new
                {
                    Issue = i,
                    AbsoluteIndex = GetAbsoluteIndex(code, i.LineNumber, i.Column)
                })
                .Where(x => x.AbsoluteIndex >= 0)
                .OrderByDescending(x => x.AbsoluteIndex) // Process from end to start
                .ToList();

            var result = code;

            foreach (var item in issuesWithPositions)
            {
                var issue = item.Issue;
                var index = item.AbsoluteIndex;
                
                if (index >= 0 && index < result.Length)
                {
                    // Verify the text at this position matches what we expect
                    if (index + issue.OriginalText.Length <= result.Length)
                    {
                        var actualText = result.Substring(index, issue.OriginalText.Length);
                        if (actualText == issue.OriginalText)
                        {
                            result = result.Remove(index, issue.OriginalText.Length)
                                          .Insert(index, issue.ReplacementText);
                        }
                    }
                }
            }

            return result;
        }

        private int GetAbsoluteIndex(string code, int lineNumber, int column)
        {
            var lines = code.Split('\n');
            
            if (lineNumber < 1 || lineNumber > lines.Length)
            {
                return -1;
            }

            var index = 0;
            for (int i = 0; i < lineNumber - 1; i++)
            {
                index += lines[i].Length + 1; // +1 for newline
            }

            index += column - 1;

            return index;
        }
    }
}
