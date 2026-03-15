---
inclusion: always
---

# Tech Stack & Build

## Platform
- **Framework**: WinUI 3 (Windows App SDK 1.7)
- **Runtime**: .NET 8 (`net8.0-windows10.0.19041.0`)
- **Min OS**: Windows 10 1809 (build 17763)
- **Language**: C# with nullable reference types enabled
- **Packaging**: MSIX (self-contained)
- **Platforms**: x86, x64, ARM64

## Key Dependencies
| Package | Purpose |
|---|---|
| Microsoft.WindowsAppSDK 1.7 | WinUI 3 framework |
| WebView2 (via WinUI) | Diagram preview rendering (Mermaid.js + markdown-it) |
| Markdig 0.44 | Server-side Markdown parsing for Word export |
| DocumentFormat.OpenXml 3.3 | Word document (.docx) generation |
| Svg.Skia 1.0 + SkiaSharp 2.88 | SVG-to-PNG rasterization for image export |
| TextControlBox.WinUI 1.1.5 | Code editor control with syntax highlighting |
| CommunityToolkit.WinUI.UI.Controls 7.1.2 | Additional WinUI controls |

## JavaScript Libraries (Bundled in Assets)
- **mermaid.min.js** (v10.9.0+) - Diagram rendering
- **markdown-it.js** - Markdown to HTML conversion
- **highlight.js** - Code syntax highlighting in Markdown

## Test Stack
| Package | Purpose |
|---|---|
| xUnit 2.9 | Test framework |
| FsCheck.Xunit 3.3 | Property-based testing |
| Moq 4.20 | Mocking |
| coverlet 6.0 | Code coverage |

## Common Commands

Solution file is at `MermaidDiagramApp/MermaidDiagramApp.sln`.

```powershell
# Build (from repo root)
dotnet build MermaidDiagramApp/MermaidDiagramApp.sln

# Build specific platform
dotnet build MermaidDiagramApp/MermaidDiagramApp.sln -p:Platform=x64

# Build release version
dotnet build MermaidDiagramApp/MermaidDiagramApp.sln -c Release

# Run tests
dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj

# Run tests with specific platform
dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj -p:Platform=x64

# Run tests with coverage
dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj --collect:"XPlat Code Coverage"

# Publish (Release, self-contained)
dotnet publish MermaidDiagramApp/MermaidDiagramApp.csproj -c Release -r win-x64

# Clean build artifacts
dotnet clean MermaidDiagramApp/MermaidDiagramApp.sln

# Restore NuGet packages
dotnet restore MermaidDiagramApp/MermaidDiagramApp.sln
```

## Build Configuration
- **Debug**: No ReadyToRun, no trimming, full debugging symbols
- **Release**: ReadyToRun enabled, trimming enabled for smaller package size
- **Nullable**: Enabled project-wide
- **ImplicitUsings**: Enabled in test project

## Development Tools
- **Visual Studio 2022** (recommended) with Windows App SDK workload
- **.NET 8.0 SDK** (required)
- **Windows 10/11 SDK** (10.0.26100.4948 or later)

## WebView2 Assets
The preview pane uses a bundled HTML page (`Assets/UnifiedRenderer.html`) loaded via virtual host mapping (`https://appassets/`). Mermaid.js is bundled locally in `Assets/mermaid.min.js`. Communication between C# and WebView2 uses `WebMessageReceived` / `ExecuteScriptAsync` with JSON messages.

## Logging
Custom logging system with rolling file logs:
- **Location**: `%LocalAppData%\Packages\[AppId]\LocalState\Logs\app.log`
- **Max File Size**: 2 MB
- **Retained Files**: 5
- **Log Levels**: Debug, Information, Warning, Error

```powershell
# View application logs
.\ViewLogs.ps1

# Find specific log entries
.\FindLogs.ps1
```
