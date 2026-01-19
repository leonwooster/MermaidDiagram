using Xunit;
using Moq;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Tests.Services;

public class MermaidTextOptimizerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly MermaidTextOptimizer _optimizer;

    public MermaidTextOptimizerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _optimizer = new MermaidTextOptimizer(_mockLogger.Object);
    }

    [Fact]
    public void OptimizeDiagram_WithShortLabels_ReturnsUnchanged()
    {
        // Arrange
        var diagram = @"graph TB
    A[""Short""]
    B[""Also Short""]";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Equal(diagram, result);
    }

    [Fact]
    public void OptimizeDiagram_WithLongSubgraphTitle_AddsLineBreaks()
    {
        // Arrange
        var diagram = @"graph TB
    subgraph APIServer[""API Server (Port 8000) @designsai/api""]
        node1[""Test""]
    end";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Contains("<br/>", result);
        Assert.Contains("API Server", result);
    }

    [Fact]
    public void OptimizeDiagram_WithLongNodeLabel_AddsLineBreaks()
    {
        // Arrange
        var diagram = @"graph TB
    node1[""This is a very long node label that should be split into multiple lines""]";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Contains("<br/>", result);
        Assert.DoesNotContain("This is a very long node label that should be split into multiple lines", result);
    }

    [Fact]
    public void OptimizeDiagram_WithExistingLineBreaks_DoesNotModify()
    {
        // Arrange
        var diagram = @"graph TB
    node1[""Already<br/>Optimized""]";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Equal(diagram, result);
    }

    [Fact]
    public void OptimizeDiagram_WithMultipleLongLabels_OptimizesAll()
    {
        // Arrange
        var diagram = @"graph TB
    subgraph Backend[""Backend Services Layer with very long title""]
        node1[""This is a very long node label that needs optimization""]
        node2[""Another extremely long label that should be split""]
    end";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        var lineBreakCount = result.Split("<br/>").Length - 1;
        Assert.True(lineBreakCount >= 3, $"Expected at least 3 line breaks, found {lineBreakCount}");
    }

    [Fact]
    public void OptimizeDiagram_WithEmptyContent_ReturnsEmpty()
    {
        // Arrange
        var diagram = "";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void OptimizeDiagram_WithNullContent_ReturnsNull()
    {
        // Arrange
        string? diagram = null;

        // Act
        var result = _optimizer.OptimizeDiagram(diagram!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NeedsOptimization_WithLongLabels_ReturnsTrue()
    {
        // Arrange
        var diagram = @"graph TB
    node1[""This is a very long node label that should be optimized for better display""]";

        // Act
        var result = _optimizer.NeedsOptimization(diagram);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsOptimization_WithShortLabels_ReturnsFalse()
    {
        // Arrange
        var diagram = @"graph TB
    A[""Short""]
    B[""Also Short""]";

        // Act
        var result = _optimizer.NeedsOptimization(diagram);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsOptimization_WithOptimizedLabels_ReturnsFalse()
    {
        // Arrange
        var diagram = @"graph TB
    node1[""Already<br/>Optimized<br/>Label""]";

        // Act
        var result = _optimizer.NeedsOptimization(diagram);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OptimizeDiagram_WithParenthesesNodeStyle_OptimizesCorrectly()
    {
        // Arrange
        var diagram = @"graph TB
    node1(""This is a very long node label in parentheses that needs optimization"")";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Contains("<br/>", result);
        Assert.Contains("(\"", result);
        Assert.Contains("\")", result);
    }

    [Fact]
    public void OptimizeDiagram_WithRealWorldExample_OptimizesCorrectly()
    {
        // Arrange - Real example from the user's diagram
        var diagram = @"graph TB
    subgraph BackendServices[""Backend Services Layer""]
        subgraph APIServer[""API Server (Port 8000) @designsai/api""]
            api_framework[""Framework: Express.js + TypeScript""]
        end
        
        subgraph JobProcessor[""Job Processor Service (Port 3003) @designsai/job-processor""]
            job_purpose[""Purpose: Asynchronous AI job processing""]
        end
    end";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Contains("<br/>", result);
        
        // Verify the long titles were split
        Assert.DoesNotContain("API Server (Port 8000) @designsai/api", result);
        Assert.DoesNotContain("Job Processor Service (Port 3003) @designsai/job-processor", result);
        
        // Verify structure is maintained
        Assert.Contains("subgraph APIServer", result);
        Assert.Contains("subgraph JobProcessor", result);
    }

    [Fact]
    public void OptimizeDiagram_PreservesIndentation()
    {
        // Arrange
        var diagram = @"graph TB
    subgraph Backend[""Backend Services Layer with a very long title that needs optimization""]
        node1[""Test""]
    end";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        // Check that indentation is preserved
        Assert.Contains("    subgraph Backend", result);
        Assert.Contains("        node1", result);
    }

    [Fact]
    public void OptimizeDiagram_WithPortNumber_BreaksBeforePort()
    {
        // Arrange
        var diagram = @"graph TB
    subgraph APIServer[""API Server (Port 8000) @designsai/api""]
        node1[""Test""]
    end";

        // Act
        var result = _optimizer.OptimizeDiagram(diagram);

        // Assert
        Assert.Contains("<br/>", result);
        
        // Port number should be on its own line
        Assert.Contains("(Port 8000)", result);
        
        // Should not have the original long single line
        Assert.DoesNotContain("API Server (Port 8000) @designsai/api", result);
    }
}
