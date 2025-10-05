using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.Services
{
    /// <summary>
    /// Analyzes Mermaid diagram syntax and detects common issues
    /// </summary>
    public class MermaidSyntaxAnalyzer : ISyntaxAnalyzer
    {
        private readonly List<ISyntaxRule> _rules;

        public MermaidSyntaxAnalyzer()
        {
            _rules = new List<ISyntaxRule>
            {
                new UnicodeDashRule(),
                new UnicodeArrowRule(),
                new SmartQuoteRule(),
                new LineBreakRule(),
                new ParenthesesInLabelRule()
            };
        }

        public List<SyntaxIssue> Analyze(string code)
        {
            var issues = new List<SyntaxIssue>();

            foreach (var rule in _rules.Where(r => r.IsEnabled))
            {
                issues.AddRange(rule.Detect(code));
            }

            return issues.OrderBy(i => i.LineNumber).ThenBy(i => i.Column).ToList();
        }

        public List<ISyntaxRule> GetRules() => _rules;
    }

    /// <summary>
    /// Base class for syntax rules
    /// </summary>
    public abstract class SyntaxRuleBase : ISyntaxRule
    {
        public abstract string Name { get; }
        public bool IsEnabled { get; set; } = true;
        public IssueSeverity Severity { get; set; } = IssueSeverity.Error;

        public abstract List<SyntaxIssue> Detect(string code);

        protected (int line, int column) GetLineAndColumn(string code, int index)
        {
            var lines = code.Substring(0, index).Split('\n');
            return (lines.Length, lines[^1].Length + 1);
        }
    }

    /// <summary>
    /// Detects Unicode dash characters (en-dash, em-dash)
    /// </summary>
    public class UnicodeDashRule : SyntaxRuleBase
    {
        public override string Name => "Unicode Dash Detection";

        public override List<SyntaxIssue> Detect(string code)
        {
            var issues = new List<SyntaxIssue>();
            var pattern = @"[\u2013\u2014]"; // en-dash (–) and em-dash (—)

            foreach (Match match in Regex.Matches(code, pattern))
            {
                var (line, column) = GetLineAndColumn(code, match.Index);
                var unicodeChar = match.Value[0];
                var unicodeCode = $"U+{((int)unicodeChar):X4}";
                var charName = unicodeChar == '\u2013' ? "en-dash" : "em-dash";

                issues.Add(new SyntaxIssue
                {
                    LineNumber = line,
                    Column = column,
                    Length = match.Length,
                    Description = $"Unicode {charName} ({unicodeCode}) should be replaced with hyphen (-).",
                    Severity = Severity,
                    Type = IssueType.UnicodeDash,
                    OriginalText = match.Value,
                    ReplacementText = "-",
                    UnicodeInfo = $"{charName} ({unicodeCode})"
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Detects Unicode arrow characters
    /// </summary>
    public class UnicodeArrowRule : SyntaxRuleBase
    {
        public override string Name => "Unicode Arrow Detection";

        public override List<SyntaxIssue> Detect(string code)
        {
            var issues = new List<SyntaxIssue>();
            var pattern = @"[\u2192\u2190\u2194\u21D2\u21D0\u21D4]"; // →, ←, ↔, ⇒, ⇐, ⇔

            var arrowNames = new Dictionary<char, string>
            {
                ['\u2192'] = "rightwards arrow",
                ['\u2190'] = "leftwards arrow",
                ['\u2194'] = "left-right arrow",
                ['\u21D2'] = "rightwards double arrow",
                ['\u21D0'] = "leftwards double arrow",
                ['\u21D4'] = "left-right double arrow"
            };

            foreach (Match match in Regex.Matches(code, pattern))
            {
                var (line, column) = GetLineAndColumn(code, match.Index);
                var unicodeChar = match.Value[0];
                var unicodeCode = $"U+{((int)unicodeChar):X4}";
                var charName = arrowNames.GetValueOrDefault(unicodeChar, "arrow");

                issues.Add(new SyntaxIssue
                {
                    LineNumber = line,
                    Column = column,
                    Length = match.Length,
                    Description = $"Unicode {charName} ({unicodeCode}) is not compatible with Mermaid syntax.",
                    Severity = Severity,
                    Type = IssueType.UnicodeArrow,
                    OriginalText = match.Value,
                    ReplacementText = "to",
                    UnicodeInfo = $"{charName} ({unicodeCode})"
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Detects smart quotes (curly quotes)
    /// </summary>
    public class SmartQuoteRule : SyntaxRuleBase
    {
        public override string Name => "Smart Quote Detection";

        public override List<SyntaxIssue> Detect(string code)
        {
            var issues = new List<SyntaxIssue>();
            var pattern = @"[\u201C\u201D\u2018\u2019]"; // ", ", ', '

            var quoteNames = new Dictionary<char, (string name, string replacement)>
            {
                ['\u201C'] = ("left double quotation mark", "\""),
                ['\u201D'] = ("right double quotation mark", "\""),
                ['\u2018'] = ("left single quotation mark", "'"),
                ['\u2019'] = ("right single quotation mark", "'")
            };

            foreach (Match match in Regex.Matches(code, pattern))
            {
                var (line, column) = GetLineAndColumn(code, match.Index);
                var unicodeChar = match.Value[0];
                var unicodeCode = $"U+{((int)unicodeChar):X4}";
                var (charName, replacement) = quoteNames.GetValueOrDefault(unicodeChar, ("smart quote", "\""));

                issues.Add(new SyntaxIssue
                {
                    LineNumber = line,
                    Column = column,
                    Length = match.Length,
                    Description = $"Smart quote ({unicodeCode}) should be replaced with straight quote.",
                    Severity = Severity,
                    Type = IssueType.SmartQuote,
                    OriginalText = match.Value,
                    ReplacementText = replacement,
                    UnicodeInfo = $"{charName} ({unicodeCode})"
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Detects \n escape sequences in node labels that should be <br/> tags
    /// </summary>
    public class LineBreakRule : SyntaxRuleBase
    {
        public override string Name => "Line Break Detection";

        public override List<SyntaxIssue> Detect(string code)
        {
            var issues = new List<SyntaxIssue>();
            // Find all node labels first
            var labelPattern = @"\[([^\]]+)\]";

            foreach (Match labelMatch in Regex.Matches(code, labelPattern))
            {
                var labelContent = labelMatch.Groups[1].Value;
                var labelStartIndex = labelMatch.Index;

                // Find all \n within this label
                int searchIndex = 0;
                while ((searchIndex = labelContent.IndexOf("\\n", searchIndex)) != -1)
                {
                    var absoluteIndex = labelStartIndex + 1 + searchIndex; // +1 for the opening [
                    var (line, column) = GetLineAndColumn(code, absoluteIndex);

                    issues.Add(new SyntaxIssue
                    {
                        LineNumber = line,
                        Column = column,
                        Length = 2,
                        Description = "Escape sequence \\n in node label should be replaced with <br/> tag.",
                        Severity = Severity,
                        Type = IssueType.LineBreak,
                        OriginalText = "\\n",
                        ReplacementText = "<br/>",
                        UnicodeInfo = ""
                    });

                    searchIndex += 2; // Move past this \n
                }
            }

            return issues;
        }
    }

    /// <summary>
    /// Detects parentheses in node labels that can cause parse errors
    /// </summary>
    public class ParenthesesInLabelRule : SyntaxRuleBase
    {
        public override string Name => "Parentheses in Label Detection";

        public override List<SyntaxIssue> Detect(string code)
        {
            var issues = new List<SyntaxIssue>();
            // Match any opening parenthesis within square brackets (node labels)
            // We need to find all ( within [...] contexts
            var labelPattern = @"\[([^\]]+)\]";

            foreach (Match labelMatch in Regex.Matches(code, labelPattern))
            {
                var labelContent = labelMatch.Groups[1].Value;
                var labelStartIndex = labelMatch.Index;

                // Find all parentheses pairs within this label
                for (int i = 0; i < labelContent.Length; i++)
                {
                    if (labelContent[i] == '(')
                    {
                        // Find the closing parenthesis
                        int closeIndex = labelContent.IndexOf(')', i);
                        if (closeIndex > i)
                        {
                            var contentInParens = labelContent.Substring(i + 1, closeIndex - i - 1);
                            var absoluteIndex = labelStartIndex + 1 + i; // +1 for the opening [
                            var (line, column) = GetLineAndColumn(code, absoluteIndex);

                            issues.Add(new SyntaxIssue
                            {
                                LineNumber = line,
                                Column = column,
                                Length = contentInParens.Length + 2, // +2 for the parentheses
                                Description = $"Parentheses in node label may cause parse errors. Consider removing or using different delimiters.",
                                Severity = IssueSeverity.Warning,
                                Type = IssueType.Other,
                                OriginalText = $"({contentInParens})",
                                ReplacementText = contentInParens, // Remove parentheses
                                UnicodeInfo = ""
                            });

                            i = closeIndex; // Skip to after the closing parenthesis
                        }
                    }
                }
            }

            return issues;
        }
    }
}
