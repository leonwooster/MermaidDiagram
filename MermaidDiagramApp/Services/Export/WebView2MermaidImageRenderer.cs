using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Svg.Skia;
using MermaidDiagramApp.Services.Logging;

namespace MermaidDiagramApp.Services.Export;

/// <summary>
/// Renders Mermaid diagrams to image files using WebView2 and Svg.Skia.
/// </summary>
public class WebView2MermaidImageRenderer : IMermaidImageRenderer
{
    private readonly IWebView2Wrapper _webView;
    private readonly ILogger _logger;

    public WebView2MermaidImageRenderer(IWebView2Wrapper webView, ILogger logger)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Renders a Mermaid diagram to an image file.
    /// </summary>
    /// <param name="mermaidCode">The Mermaid diagram code.</param>
    /// <param name="outputPath">The path where the image will be saved.</param>
    /// <param name="format">The desired image format.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The path to the rendered image file.</returns>
    public async Task<string> RenderToImageAsync(
        string mermaidCode,
        string outputPath,
        ImageFormat format,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mermaidCode))
        {
            throw new ArgumentException("Mermaid code cannot be null or empty", nameof(mermaidCode));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        try
        {
            _logger.LogDebug($"Rendering Mermaid diagram to {format} format");

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Render the Mermaid diagram in WebView2
            var renderScript = GenerateRenderScript(mermaidCode);
            
            try
            {
                await _webView.ExecuteScriptAsync(renderScript);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to execute Mermaid render script in WebView2", ex);
                throw new InvalidOperationException("Failed to render Mermaid diagram. The diagram syntax may be invalid or WebView2 is not available.", ex);
            }

            // Wait a bit for rendering to complete
            await Task.Delay(500, cancellationToken);

            // Check for cancellation again
            cancellationToken.ThrowIfCancellationRequested();

            // Get the SVG content from WebView2
            string? svgString = null;
            try
            {
                var svgJson = await _webView.ExecuteScriptAsync("getSvg()");
                svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve SVG content from WebView2", ex);
                throw new InvalidOperationException("Failed to retrieve rendered diagram. The diagram may contain syntax errors.", ex);
            }

            if (string.IsNullOrEmpty(svgString))
            {
                throw new InvalidOperationException("Failed to get SVG content from WebView2. The diagram may have failed to render due to syntax errors.");
            }

            _logger.LogDebug($"Retrieved SVG content ({svgString.Length} characters)");

            // Check for cancellation before conversion
            cancellationToken.ThrowIfCancellationRequested();

            // Convert based on format
            if (format == ImageFormat.SVG)
            {
                // Save SVG directly
                try
                {
                    await File.WriteAllTextAsync(outputPath, svgString, cancellationToken);
                    _logger.LogInformation($"Saved SVG to {outputPath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied when writing SVG file: {outputPath}", ex);
                }
                catch (IOException ex)
                {
                    throw new IOException($"I/O error when writing SVG file: {outputPath}", ex);
                }
            }
            else if (format == ImageFormat.PNG)
            {
                // Convert SVG to PNG using Svg.Skia
                try
                {
                    ConvertSvgToPng(svgString, outputPath);
                    _logger.LogInformation($"Converted and saved PNG to {outputPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to convert SVG to PNG", ex);
                    throw new InvalidOperationException($"Failed to convert diagram to PNG format: {ex.Message}", ex);
                }
            }
            else
            {
                throw new NotSupportedException($"Image format {format} is not supported");
            }

            return outputPath;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Mermaid rendering was cancelled");
            throw;
        }
        catch (NotSupportedException)
        {
            // Re-throw NotSupportedException as-is
            throw;
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException as-is (these are our custom error messages)
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw UnauthorizedAccessException as-is
            throw;
        }
        catch (IOException)
        {
            // Re-throw IOException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error rendering Mermaid diagram: {ex.Message}", ex);
            throw new InvalidOperationException($"Unexpected error rendering Mermaid diagram: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates the JavaScript command to render Mermaid in WebView2.
    /// </summary>
    private string GenerateRenderScript(string mermaidCode)
    {
        // Escape content for JavaScript
        var escapedContent = System.Text.Json.JsonSerializer.Serialize(mermaidCode);

        return $@"
            (async function() {{
                try {{
                    await window.renderMermaid({escapedContent}, 'light');
                }} catch (error) {{
                    console.error('Mermaid render error:', error);
                    window.chrome.webview.postMessage({{
                        type: 'renderError',
                        error: error.message
                    }});
                }}
            }})();
        ";
    }

    /// <summary>
    /// Converts SVG string to PNG file using Svg.Skia.
    /// </summary>
    private void ConvertSvgToPng(string svgString, string outputPath)
    {
        try
        {
            using var svg = new SKSvg();
            using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgString));

            var picture = svg.Load(svgStream);
            if (picture == null)
            {
                throw new InvalidOperationException("Failed to load SVG for conversion to PNG. The SVG content may be invalid.");
            }

            // Calculate dimensions with transparent background
            var dimensions = new SKSizeI(
                (int)Math.Ceiling(picture.CullRect.Width),
                (int)Math.Ceiling(picture.CullRect.Height)
            );

            // Validate dimensions
            if (dimensions.Width <= 0 || dimensions.Height <= 0)
            {
                throw new InvalidOperationException($"Invalid SVG dimensions ({dimensions.Width}x{dimensions.Height}). The SVG may be malformed.");
            }

            // Limit maximum dimensions to prevent memory issues
            const int maxDimension = 10000;
            if (dimensions.Width > maxDimension || dimensions.Height > maxDimension)
            {
                throw new InvalidOperationException($"SVG dimensions ({dimensions.Width}x{dimensions.Height}) exceed maximum allowed size ({maxDimension}x{maxDimension}).");
            }

            // Create bitmap with transparent background
            using var bitmap = new SKBitmap(new SKImageInfo(
                dimensions.Width,
                dimensions.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            ));

            if (bitmap == null)
            {
                throw new InvalidOperationException("Failed to create bitmap for PNG conversion.");
            }

            using var canvas = new SKCanvas(bitmap);

            // Clear with transparent background
            canvas.Clear(SKColors.Transparent);

            // Draw the SVG
            canvas.DrawPicture(picture);
            canvas.Flush();

            // Save as PNG
            try
            {
                using var fileStream = File.OpenWrite(outputPath);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                
                if (data == null)
                {
                    throw new InvalidOperationException("Failed to encode bitmap as PNG.");
                }
                
                data.SaveTo(fileStream);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when writing PNG file: {outputPath}", ex);
            }
            catch (IOException ex)
            {
                throw new IOException($"I/O error when writing PNG file: {outputPath}", ex);
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (IOException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert SVG to PNG: {ex.Message}", ex);
        }
    }
}
