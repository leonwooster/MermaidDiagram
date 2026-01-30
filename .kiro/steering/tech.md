# Technology Stack

## Framework & Platform

- **Target Framework**: .NET 8.0 (net8.0-windows10.0.19041.0)
- **UI Framework**: WinUI 3 (Windows App SDK 1.7)
- **Minimum Windows Version**: Windows 10 version 1809 (10.0.17763.0)
- **Platforms**: x86, x64, ARM64
- **Packaging**: MSIX with self-contained Windows App SDK

## Core Dependencies

### UI & Rendering
- **Microsoft.WindowsAppSDK** (1.7.250606001) - WinUI 3 framework
- **WebView2** - Hosts Mermaid.js and markdown-it.js rendering engines
- **TextControlBox.WinUI.JuliusKirsch** (1.1.5) - Code editor with syntax highlighting
- **CommunityToolkit.WinUI.UI.Controls** (7.1.2) - GridSplitter and other controls

### Document Processing
- **Markdig** (0.44.0) - Markdown parsing for export
- **DocumentFormat.OpenXml** (3.3.0) - Word document generation
- **Svg.Skia** (1.0.0.4) + **SkiaSharp** (2.88.8) - SVG to PNG conversion

### Testing
- **xUnit** (2.9.2) - Unit testing framework
- **FsCheck.Xunit** (3.3.2) - Property-based testing
- **Moq** (4.20.72) - Mocking framework
- **coverlet.collector** (6.0.2) - Code coverage

## JavaScript Libraries (Bundled in Assets)

- **mermaid.min.js** (v10.9.0+) - Diagram rendering
- **markdown-it.js** - Markdown to HTML conversion
- **highlight.js** - Code syntax highlighting in Markdown

## Build System

### Build Commands

```powershell
# Build the solution
dotnet build MermaidDiagramApp/MermaidDiagramApp.sln

# Build for specific platform
dotnet build -c Debug -r win-x64

# Build release version
dotnet build -c Release

# Run tests
dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Project Structure

- **MermaidDiagramApp.csproj** - Main application project
- **MermaidDiagramApp.Tests.csproj** - Test project
- **Package.appxmanifest** - MSIX packaging configuration

### Build Configuration

- **Debug**: No ReadyToRun, no trimming, full debugging symbols
- **Release**: ReadyToRun enabled, trimming enabled for smaller package size
- **Nullable**: Enabled project-wide
- **ImplicitUsings**: Enabled in test project

## Development Tools

- **Visual Studio 2022** (recommended) with Windows App SDK workload
- **.NET 8.0 SDK** (required)
- **Windows 10/11 SDK** (10.0.26100.4948 or later)

## Logging

Custom logging system with rolling file logs:
- **Location**: `%LocalAppData%\Packages\[AppId]\LocalState\Logs\app.log`
- **Max File Size**: 2 MB
- **Retained Files**: 5
- **Log Levels**: Debug, Information, Warning, Error

## Common Development Tasks

```powershell
# View application logs
.\ViewLogs.ps1

# Find specific log entries
.\FindLogs.ps1

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```
