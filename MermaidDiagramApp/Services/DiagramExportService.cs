using System;
using System.IO;
using System.Threading.Tasks;
using MermaidDiagramApp.Services.Logging;
using SkiaSharp;
using Svg.Skia;

namespace MermaidDiagramApp.Services;

/// <summary>
/// Rasterizes SVG content to PNG bytes using Svg.Skia / SkiaSharp.
/// Composes <see cref="IExportService.AddBackgroundToSvg"/> for background insertion.
/// </summary>
public class DiagramExportService : IDiagramExportService
{
    private readonly IExportService _exportService;
    private readonly ILogger _logger;

    public DiagramExportService(IExportService exportService, ILogger logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<byte[]> RasterizeSvgToPngAsync(string svgContent, float scale = 2.0f)
    {
        if (string.IsNullOrEmpty(svgContent))
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        return Task.Run(() =>
        {
            try
            {
                // Add dark background before rasterization
                var svgWithBackground = _exportService.AddBackgroundToSvg(svgContent);

                using var svg = new SKSvg();
                using var svgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgWithBackground));

                if (svg.Load(svgStream) is { } picture)
                {
                    var width = (int)(picture.CullRect.Width * scale);
                    var height = (int)(picture.CullRect.Height * scale);

                    using var bitmap = new SKBitmap(new SKImageInfo(width, height));
                    using var canvas = new SKCanvas(bitmap);
                    canvas.Clear(SKColor.Parse("#222222"));

                    var matrix = SKMatrix.CreateScale(scale, scale);
                    canvas.DrawPicture(picture, ref matrix);
                    canvas.Flush();

                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    return data.ToArray();
                }

                _logger.Log(LogLevel.Warning, "SVG could not be loaded for rasterization");
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to rasterize SVG to PNG: {ex.Message}", ex);
                return Array.Empty<byte>();
            }
        });
    }
}
