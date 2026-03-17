using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MermaidDiagramApp.Services.Logging;
using SkiaSharp;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Provides SVG background insertion, PNG scaling, and file-saving operations
/// for diagram export, extracted from MainWindow event handlers.
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger _logger;

    public ExportService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string AddBackgroundToSvg(string svgContent)
    {
        if (string.IsNullOrEmpty(svgContent))
        {
            return svgContent ?? string.Empty;
        }

        try
        {
            // Insert a background rectangle right after the opening <svg> tag
            var svgTagEndIndex = svgContent.IndexOf('>');
            if (svgTagEndIndex > 0)
            {
                // Extract width and height if available, otherwise use large default values
                var widthMatch = Regex.Match(svgContent, @"width=""([^""]+)""");
                var heightMatch = Regex.Match(svgContent, @"height=""([^""]+)""");

                string width = widthMatch.Success ? widthMatch.Groups[1].Value.Replace("px", "") : "1200";
                string height = heightMatch.Success ? heightMatch.Groups[1].Value.Replace("px", "") : "800";

                // Create a background rectangle
                var backgroundRect = $"<rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" fill=\"#222222\"/>";

                // Insert the background right after the opening <svg> tag
                return svgContent.Insert(svgTagEndIndex + 1, backgroundRect);
            }

            return svgContent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to add background to SVG: {ex.Message}", ex);
            return svgContent;
        }
    }


    /// <inheritdoc />
    public Task<byte[]> ScaleImageAsync(byte[] pngData, float scale)
    {
        if (pngData == null || pngData.Length == 0)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        if (Math.Abs(scale - 1.0f) < 0.01f)
        {
            // No scaling needed
            return Task.FromResult(pngData);
        }

        return Task.Run(() =>
        {
            try
            {
                using var originalBitmap = SKBitmap.Decode(pngData);
                if (originalBitmap == null)
                {
                    _logger.LogError("Failed to decode PNG data for scaling");
                    return pngData;
                }

                var newWidth = (int)(originalBitmap.Width * scale);
                var newHeight = (int)(originalBitmap.Height * scale);

                using var scaledBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                if (scaledBitmap == null)
                {
                    _logger.LogError("Failed to resize bitmap");
                    return pngData;
                }

                using var data = scaledBitmap.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to scale image: {ex.Message}", ex);
                return pngData;
            }
        });
    }

    /// <inheritdoc />
    public async Task SaveSvgAsync(string filePath, string svgContent)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        await File.WriteAllTextAsync(filePath, svgContent ?? string.Empty);
        _logger.Log(LogLevel.Information, $"SVG saved to {filePath}");
    }

    /// <inheritdoc />
    public async Task SavePngAsync(string filePath, byte[] pngData)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        await File.WriteAllBytesAsync(filePath, pngData ?? Array.Empty<byte>());
        _logger.Log(LogLevel.Information, $"PNG saved to {filePath}");
    }
}
