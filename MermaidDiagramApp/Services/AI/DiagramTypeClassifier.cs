using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MermaidDiagramApp.Services.AI
{
    /// <summary>
    /// Service for classifying diagram types based on user prompts
    /// </summary>
    public class DiagramTypeClassifier
    {
        private readonly IAiService _aiService;
        private readonly Dictionary<string, string[]> _diagramTypeKeywords;

        public DiagramTypeClassifier(IAiService aiService)
        {
            _aiService = aiService;
            
            // Initialize keyword mappings for heuristic-based classification
            _diagramTypeKeywords = new Dictionary<string, string[]>
            {
                {
                    "flowchart", new[]
                    {
                        "process", "workflow", "flow", "steps", "procedure", "algorithm", 
                        "decision", "branch", "if", "then", "else", "loop", "iteration",
                        "sequence", "order", "hierarchy", "structure", "organization"
                    }
                },
                {
                    "sequenceDiagram", new[]
                    {
                        "sequence", "interaction", "message", "communication", "protocol",
                        "request", "response", "call", "api", "function", "method",
                        "user", "system", "client", "server", "component"
                    }
                },
                {
                    "classDiagram", new[]
                    {
                        "class", "object", "inheritance", "interface", "implementation",
                        "association", "composition", "aggregation", "uml", "design",
                        "architecture", "structure", "relationship", "property", "method"
                    }
                },
                {
                    "stateDiagram", new[]
                    {
                        "state", "transition", "status", "condition", "event", "trigger",
                        "machine", "finite", "automaton", "phase", "stage", "mode"
                    }
                },
                {
                    "pie", new[]
                    {
                        "pie", "chart", "proportion", "percentage", "distribution",
                        "share", "part", "portion", "ratio", "statistics", "data"
                    }
                },
                {
                    "gantt", new[]
                    {
                        "gantt", "timeline", "schedule", "project", "task", "milestone",
                        "duration", "start", "end", "dependency", "progress", "planning"
                    }
                },
                {
                    "erDiagram", new[]
                    {
                        "entity", "relationship", "database", "table", "schema",
                        "foreign", "primary", "key", "cardinality", "erd", "model"
                    }
                }
            };
        }

        /// <summary>
        /// Determines the most suitable diagram type for a given prompt
        /// </summary>
        /// <param name="prompt">The user's natural language prompt</param>
        /// <returns>The recommended diagram type</returns>
        public async Task<string> ClassifyDiagramTypeAsync(string prompt)
        {
            // First try AI-based classification
            try
            {
                var aiResult = await _aiService.DetermineDiagramTypeAsync(prompt);
                if (!string.IsNullOrWhiteSpace(aiResult))
                {
                    // Validate that the AI result is a supported diagram type
                    if (_diagramTypeKeywords.ContainsKey(aiResult.Trim()))
                    {
                        return aiResult.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception but continue with heuristic approach
                System.Diagnostics.Debug.WriteLine($"AI classification failed: {ex.Message}");
            }

            // Fallback to heuristic-based classification
            return ClassifyDiagramTypeHeuristically(prompt);
        }

        /// <summary>
        /// Classifies diagram type using keyword matching heuristics
        /// </summary>
        /// <param name="prompt">The user's natural language prompt</param>
        /// <returns>The recommended diagram type</returns>
        private string ClassifyDiagramTypeHeuristically(string prompt)
        {
            var lowerPrompt = prompt.ToLowerInvariant();
            var scores = new Dictionary<string, int>();

            // Calculate keyword match scores for each diagram type
            foreach (var kvp in _diagramTypeKeywords)
            {
                var diagramType = kvp.Key;
                var keywords = kvp.Value;
                var score = keywords.Count(keyword => lowerPrompt.Contains(keyword));
                scores[diagramType] = score;
            }

            // Return the diagram type with the highest score
            var bestMatch = scores.OrderByDescending(kvp => kvp.Value).First();
            
            // If no keywords matched, default to flowchart as it's the most versatile
            return bestMatch.Value > 0 ? bestMatch.Key : "flowchart";
        }

        /// <summary>
        /// Gets all supported diagram types
        /// </summary>
        /// <returns>List of supported diagram types</returns>
        public IEnumerable<string> GetSupportedDiagramTypes()
        {
            return _diagramTypeKeywords.Keys;
        }

        /// <summary>
        /// Gets example prompts for a specific diagram type
        /// </summary>
        /// <param name="diagramType">The diagram type</param>
        /// <returns>Example prompts for the diagram type</returns>
        public IEnumerable<string> GetExamplePrompts(string diagramType)
        {
            return diagramType.ToLowerInvariant() switch
            {
                "flowchart" => new[]
                {
                    "Create a flowchart for user login process",
                    "Show the steps for processing an online order",
                    "Diagram the workflow for handling customer complaints"
                },
                "sequencediagram" => new[]
                {
                    "Show interactions between user, web server, and database for login",
                    "Create sequence diagram for REST API call processing",
                    "Diagram the communication flow in a microservices architecture"
                },
                "classdiagram" => new[]
                {
                    "Design a class diagram for a simple e-commerce system",
                    "Show class relationships for a banking application",
                    "Create UML class diagram for user management system"
                },
                "statediagram" => new[]
                {
                    "Model the states of an order in an e-commerce system",
                    "Show user account status transitions",
                    "Diagram the lifecycle of a ticket in a support system"
                },
                "pie" => new[]
                {
                    "Show market share distribution among competitors",
                    "Display budget allocation across departments",
                    "Create pie chart for survey results distribution"
                },
                "gantt" => new[]
                {
                    "Create project timeline for software development",
                    "Show construction project schedule with milestones",
                    "Diagram product launch timeline with dependencies"
                },
                "erdiagram" => new[]
                {
                    "Design database schema for a library management system",
                    "Create ER diagram for online shopping cart",
                    "Show entity relationships for a university system"
                },
                _ => new[] { $"Generate a {diagramType} for [your specific use case]" }
            };
        }
    }
}
