using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Svg.Skia;
using MermaidDiagramApp.Services.Logging;
using Windows.Storage.Streams;

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
            _logger.LogDebug($"Starting Mermaid rendering to {format} format");
            _logger.LogDebug($"Mermaid code length: {mermaidCode.Length} characters");
            _logger.LogDebug($"Output path: {outputPath}");
            _logger.LogDebug($"Mermaid code preview: {(mermaidCode.Length > 100 ? mermaidCode.Substring(0, 100) + "..." : mermaidCode)}");
            
            // Also output to console for immediate debugging
            Console.WriteLine($"[MERMAID DEBUG] Starting rendering to {format} format");
            Console.WriteLine($"[MERMAID DEBUG] Code length: {mermaidCode.Length} characters");
            Console.WriteLine($"[MERMAID DEBUG] Output path: {outputPath}");
            Console.WriteLine($"[MERMAID DEBUG] Code preview: {(mermaidCode.Length > 100 ? mermaidCode.Substring(0, 100) + "..." : mermaidCode)}");

            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Render the Mermaid diagram in WebView2
            var renderScript = GenerateRenderScript(mermaidCode);
            _logger.LogDebug($"Generated render script: {renderScript.Substring(0, Math.Min(200, renderScript.Length))}...");
            
            try
            {
                _logger.LogDebug("Executing render script in WebView2...");
                await _webView.ExecuteScriptAsync(renderScript);
                _logger.LogDebug("Render script executed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to execute Mermaid render script in WebView2", ex);
                throw new InvalidOperationException("Failed to render Mermaid diagram. The diagram syntax may be invalid or WebView2 is not available.", ex);
            }

            // Wait a bit for rendering to complete
            _logger.LogDebug("Waiting for rendering to complete...");
            await Task.Delay(1000, cancellationToken); // Increased delay to ensure rendering completes

            // Check for cancellation again
            cancellationToken.ThrowIfCancellationRequested();

            // Get the SVG content from WebView2
            string? svgString = null;
            try
            {
                _logger.LogDebug("Retrieving SVG content from WebView2...");
                
                // First try to get the stored SVG from our export rendering
                var exportSvgJson = await _webView.ExecuteScriptAsync("window._exportSvg || ''");
                svgString = System.Text.Json.JsonSerializer.Deserialize<string>(exportSvgJson);
                _logger.LogDebug($"Export SVG result: {(string.IsNullOrEmpty(svgString) ? "empty" : $"{svgString.Length} characters")}");
                
                // If that didn't work, try the regular getSvg() function
                if (string.IsNullOrEmpty(svgString))
                {
                    _logger.LogDebug("Export SVG empty, trying regular getSvg()...");
                    var svgJson = await _webView.ExecuteScriptAsync("getSvg()");
                    svgString = System.Text.Json.JsonSerializer.Deserialize<string>(svgJson);
                    _logger.LogDebug($"Regular getSvg() result: {(string.IsNullOrEmpty(svgString) ? "empty" : $"{svgString.Length} characters")}");
                }
                
                // Clean up the stored export SVG
                await _webView.ExecuteScriptAsync("delete window._exportSvg;");
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
                // Smart detection: If SVG contains foreignObject, skip Svg.Skia and use screenshot directly
                if (svgString.Contains("<foreignObject", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("SVG contains foreignObject elements, using WebView2 screenshot for best quality");
                    Console.WriteLine("[MERMAID DEBUG] Detected foreignObject in SVG, using screenshot method");
                    
                    try
                    {
                        await CaptureWebView2Screenshot(outputPath, cancellationToken);
                        _logger.LogInformation($"Captured WebView2 screenshot to {outputPath}");
                    }
                    catch (Exception screenshotEx)
                    {
                        _logger.LogError($"WebView2 screenshot failed: {screenshotEx.Message}");
                        throw new InvalidOperationException($"WebView2 screenshot capture failed: {screenshotEx.Message}", screenshotEx);
                    }
                }
                else
                {
                    // Try SVG to PNG conversion for pure SVG (no foreignObject)
                    try
                    {
                        ConvertSvgToPng(svgString, outputPath);
                        _logger.LogInformation($"Converted and saved PNG to {outputPath}");
                    }
                    catch (Exception svgEx)
                    {
                        _logger.LogWarning($"SVG to PNG conversion failed, trying WebView2 screenshot fallback: {svgEx.Message}");
                        
                        // Fallback: Use WebView2 screenshot
                        try
                        {
                            await CaptureWebView2Screenshot(outputPath, cancellationToken);
                            _logger.LogInformation($"Captured WebView2 screenshot to {outputPath}");
                        }
                        catch (Exception screenshotEx)
                        {
                            _logger.LogError($"WebView2 screenshot fallback also failed: {screenshotEx.Message}");
                            throw new InvalidOperationException($"Both SVG conversion and WebView2 screenshot failed. SVG error: {svgEx.Message}, Screenshot error: {screenshotEx.Message}", svgEx);
                        }
                    }
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
                    console.log('Starting Mermaid export rendering...');
                    console.log('Available functions:', {{
                        renderMermaid: typeof window.renderMermaid,
                        renderMermaidForExport: typeof window.renderMermaidForExport,
                        getSvg: typeof window.getSvg,
                        mermaid: typeof window.mermaid
                    }});
                    
                    // Use the export-specific rendering function if available
                    if (window.renderMermaidForExport) {{
                        console.log('Using renderMermaidForExport function');
                        await window.renderMermaidForExport({escapedContent}, 'light');
                        console.log('renderMermaidForExport completed');
                    }} else if (window.renderMermaid) {{
                        console.log('Using fallback renderMermaid function');
                        await window.renderMermaid({escapedContent}, 'light');
                        console.log('renderMermaid completed');
                    }} else {{
                        throw new Error('No Mermaid rendering functions available');
                    }}
                    
                    // Check if SVG was generated
                    const svg = window._exportSvg || (window.getSvg ? window.getSvg() : '');
                    console.log('Generated SVG length:', svg.length);
                    if (svg.length > 0) {{
                        console.log('SVG preview:', svg.substring(0, 200) + '...');
                    }}
                    
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
            // First attempt: Try with sanitized SVG
            var sanitizedSvg = SanitizeSvgForXmlParsing(svgString);
            
            if (TryConvertSvgToPngDirect(sanitizedSvg, outputPath))
            {
                _logger.LogDebug("SVG converted successfully using sanitized version");
                return;
            }

            // Second attempt: Try with original SVG
            if (TryConvertSvgToPngDirect(svgString, outputPath))
            {
                _logger.LogDebug("SVG converted successfully using original version");
                return;
            }

            // Third attempt: Use WebView2 screenshot as fallback
            _logger.LogWarning("Direct SVG conversion failed, attempting WebView2 screenshot fallback");
            throw new InvalidOperationException("SVG conversion failed - this will trigger WebView2 screenshot fallback in the calling method");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to convert SVG to PNG: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to convert SVG directly to PNG using Svg.Skia.
    /// </summary>
    private bool TryConvertSvgToPngDirect(string svgString, string outputPath)
    {
        try
        {
            using var svg = new SKSvg();
            using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgString));

            var picture = svg.Load(svgStream);
            if (picture == null)
            {
                _logger.LogDebug("Failed to load SVG - picture is null");
                return false;
            }

            // Calculate dimensions with transparent background
            var dimensions = new SKSizeI(
                (int)Math.Ceiling(picture.CullRect.Width),
                (int)Math.Ceiling(picture.CullRect.Height)
            );

            // Validate dimensions
            if (dimensions.Width <= 0 || dimensions.Height <= 0)
            {
                _logger.LogDebug($"Invalid SVG dimensions: {dimensions.Width}x{dimensions.Height}");
                return false;
            }

            // Limit maximum dimensions to prevent memory issues
            const int maxDimension = 10000;
            if (dimensions.Width > maxDimension || dimensions.Height > maxDimension)
            {
                _logger.LogDebug($"SVG dimensions too large: {dimensions.Width}x{dimensions.Height}");
                return false;
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
                _logger.LogDebug("Failed to create bitmap");
                return false;
            }

            using var canvas = new SKCanvas(bitmap);

            // Clear with transparent background
            canvas.Clear(SKColors.Transparent);

            // Draw the SVG
            canvas.DrawPicture(picture);
            canvas.Flush();

            // Save as PNG
            using var fileStream = File.OpenWrite(outputPath);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            
            if (data == null)
            {
                _logger.LogDebug("Failed to encode bitmap as PNG");
                return false;
            }
            
            data.SaveTo(fileStream);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Direct SVG conversion failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sanitizes SVG content to fix common XML parsing issues from Mermaid.js output.
    /// </summary>
    private string SanitizeSvgForXmlParsing(string svgString)
    {
        if (string.IsNullOrEmpty(svgString))
        {
            return svgString;
        }

        try
        {
            _logger.LogDebug("Starting SVG sanitization for XML parsing");
            
            // Fix self-closing br tags that aren't properly XML formatted
            // Convert <br> to <br/> and <br /> to <br/>
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString, 
                @"<br\s*(?!/)>", 
                "<br/>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Fix other common self-closing tags
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString, 
                @"<(hr|img|input|area|base|col|embed|link|meta|param|source|track|wbr)\s*(?![^>]*/)([^>]*)>", 
                "<$1$2/>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // More aggressive approach: Remove all HTML-style content within foreignObject elements
            // This is where Mermaid.js often puts problematic HTML content
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString,
                @"<foreignObject[^>]*>.*?</foreignObject>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Remove or fix problematic HTML elements that cause XML parsing issues
            // Remove div, p, span elements that contain nested content
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString,
                @"<(div|p|span)[^>]*>.*?</\1>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Remove any remaining unclosed HTML tags
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString,
                @"<(div|p|span|br|hr)[^>]*(?<!/)>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Clean up any malformed XML attributes
            svgString = System.Text.RegularExpressions.Regex.Replace(
                svgString,
                @"\s+xmlns:xlink=""[^""]*""\s*xmlns:xlink=""[^""]*""",
                @" xmlns:xlink=""http://www.w3.org/1999/xlink""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Ensure proper XML namespace declarations
            if (!svgString.Contains("xmlns=\"http://www.w3.org/2000/svg\""))
            {
                svgString = svgString.Replace("<svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"");
            }

            _logger.LogDebug("SVG sanitization completed successfully");
            return svgString;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to sanitize SVG, using original: {ex.Message}");
            return svgString; // Return original if sanitization fails
        }
    }

    /// <summary>
    /// Captures a screenshot from WebView2 as a fallback when SVG conversion fails.
    /// </summary>
    private async Task CaptureWebView2Screenshot(string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Attempting WebView2 screenshot capture");
            
            // Use WebView2's built-in screenshot capability
            using var stream = new MemoryStream();
            
            // Get the CoreWebView2 from our wrapper
            var coreWebView2 = ((CoreWebView2Wrapper)_webView).CoreWebView2;
            
            await coreWebView2.CapturePreviewAsync(
                Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, 
                stream.AsRandomAccessStream());
            
            stream.Position = 0;
            
            // Save the screenshot to the output file
            using var fileStream = File.Create(outputPath);
            await stream.CopyToAsync(fileStream, cancellationToken);
            
            _logger.LogDebug($"WebView2 screenshot saved to {outputPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to capture WebView2 screenshot: {ex.Message}", ex);
            throw new InvalidOperationException($"WebView2 screenshot capture failed: {ex.Message}", ex);
        }
    }
}
