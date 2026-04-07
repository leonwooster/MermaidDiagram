# Implementation Tasks

## Task 1: Service Layer — IZoomPanelService and ZoomPanelService
- [x] 1.1 Create `MermaidDiagramApp/Services/IZoomPanelService.cs` with `IsOpen`, `ZoomLevel`, `CurrentSvgContent` properties, `Open()`, `Close()`, `ZoomIn()`, `ZoomOut()`, `SetZoomLevel()`, `ApplyWheelDelta()` methods, and `StateChanged` event
- [x] 1.2 Create `MermaidDiagramApp/Models/ZoomPanelStateChangedEventArgs.cs` with `IsOpen`, `ZoomLevel`, `SvgContent` properties
- [x] 1.3 Create `MermaidDiagramApp/Services/ZoomPanelService.cs` implementing `IZoomPanelService` — zoom increment 0.25, range [0.25, 5.0], fires `StateChanged` on every state mutation
- [x] 1.4 Write property-based tests for `ZoomPanelService`: zoom bounds clamping, open/close state transitions, wheel delta direction, StateChanged event firing

## Task 2: Service Layer — IDiagramExportService and DiagramExportService
- [x] 2.1 Create `MermaidDiagramApp/Services/IDiagramExportService.cs` with `RasterizeSvgToPngAsync(string svgContent, float scale)` method
- [x] 2.2 Create `MermaidDiagramApp/Services/DiagramExportService.cs` implementing `IDiagramExportService` — uses Svg.Skia/SkiaSharp to rasterize SVG to PNG bytes, composes `IExportService.AddBackgroundToSvg()` for background
- [x] 2.3 Write unit tests for `DiagramExportService`: valid SVG produces non-empty PNG bytes, null/empty SVG returns empty array, scale parameter affects output dimensions

## Task 3: DI Registration
- [x] 3.1 Register `IZoomPanelService` → `ZoomPanelService` as singleton in `App.xaml.cs`
- [x] 3.2 Register `IDiagramExportService` → `DiagramExportService` as singleton in `App.xaml.cs`
- [x] 3.3 Register `ZoomPanelViewModel` as transient in `App.xaml.cs`
- [x] 3.4 Add `IZoomPanelService` and `IDiagramExportService` constructor parameters to `MainWindow` and store as fields in `MainWindow.xaml.cs`
- [x] 3.5 Update `App.xaml.cs` `OnLaunched` to resolve and pass the new services to `MainWindow`

## Task 4: ZoomPanelViewModel
- [x] 4.1 Create `MermaidDiagramApp/ViewModels/ZoomPanelViewModel.cs` with `IsOpen`, `ZoomLevelDisplay`, `CanZoomIn`, `CanZoomOut` bindable properties and `ZoomInCommand`, `ZoomOutCommand`, `CloseCommand` relay commands
- [x] 4.2 Subscribe to `IZoomPanelService.StateChanged` in constructor to update properties and raise `PropertyChanged`
- [x] 4.3 Add `RequestClose` callback delegate (Action) for layout restoration, invoked by `CloseCommand`
- [x] 4.4 Write property-based tests for `ZoomPanelViewModel`: `ZoomLevelDisplay` format matches "{level*100}%", `CanZoomIn`/`CanZoomOut` reflect bounds, commands delegate to service

## Task 5: ZoomPanel UserControl
- [x] 5.1 Create `MermaidDiagramApp/Assets/ZoomPanelHost.html` — minimal HTML with `setDiagram()`, `setZoom()`, `setTheme()` JS functions, mouse wheel forwarding, Escape key forwarding
- [x] 5.2 Add `ZoomPanelHost.html` as `Content` with `CopyToOutputDirectory=PreserveNewest` in `MermaidDiagramApp.csproj`
- [x] 5.3 Create `MermaidDiagramApp/Views/ZoomPanel.xaml` — UserControl with toolbar (zoom in, zoom out, zoom %, exit buttons) and WebView2
- [x] 5.4 Create `MermaidDiagramApp/Views/ZoomPanel.xaml.cs` — code-behind with `ViewModel` property, `InitializeWebViewAsync()`, `LoadDiagramAsync(string svgContent)`, `SetZoomLevel(double level)`, `SetTheme(string theme)` methods
- [x] 5.5 Handle `WebMessageReceived` in ZoomPanel code-behind for `zoomWheel` messages → route to `IZoomPanelService.ApplyWheelDelta()`
- [x] 5.6 Handle `WebMessageReceived` in ZoomPanel code-behind for `keypress/Escape` messages → route to `IZoomPanelService.Close()`

## Task 6: MainWindow XAML Layout Changes
- [x] 6.1 Add `ZoomSplitterColumn` (Width="Auto") and `ZoomPanelColumn` (Width="0") to `MainGrid.ColumnDefinitions` in `MainWindow.xaml`
- [x] 6.2 Add `GridSplitter` (x:Name="ZoomSplitter", Grid.Column="9", Visibility="Collapsed") to `MainWindow.xaml`
- [x] 6.3 Add `Border` (x:Name="ZoomPanelBorder", Grid.Column="10", Visibility="Collapsed") containing `views:ZoomPanel` (x:Name="ZoomPanelControl") to `MainWindow.xaml`

## Task 7: MainWindow.ZoomPanel.cs Partial Class
- [x] 7.1 Create `MermaidDiagramApp/MainWindow.ZoomPanel.cs` partial class with `_savedEditorWidth` and `_savedPreviewWidth` fields
- [x] 7.2 Implement `InitializeZoomPanel()` — subscribe to `IZoomPanelService.StateChanged`, wire `ZoomPanelViewModel.RequestClose` callback
- [x] 7.3 Implement `ShowZoomPanel(string svgContent)` — save current column widths, set `ZoomPanelColumn.Width = new GridLength(1, GridUnitType.Star)`, show splitter and border, call `ZoomPanelControl.LoadDiagramAsync()`
- [x] 7.4 Implement `HideZoomPanel()` — set `ZoomPanelColumn.Width = new GridLength(0)`, hide splitter and border, restore saved column widths, navigate ZoomPanel WebView to `about:blank`
- [x] 7.5 Implement `OnZoomPanelStateChanged` handler — dispatch to UI thread, call `ShowZoomPanel`/`HideZoomPanel` based on state, call `ZoomPanelControl.SetZoomLevel()` on zoom changes
- [x] 7.6 Call `InitializeZoomPanel()` from `MainWindow` constructor
- [x] 7.7 Extend `MainWindow_PreviewKeyDown` in `MainWindow.xaml.cs` to close zoom panel on Escape when `_zoomPanelService.IsOpen`

## Task 8: WebMessage Router and PNG Export
- [x] 8.1 Add `diagramAction` message type handler in `MainWindow.WebView.cs` `WebMessageReceived` — parse `action` and `svgContent`, dispatch to `IZoomPanelService.Open()` or `ExportDiagramAsPngAsync()`
- [x] 8.2 Add `ExportDiagramAsPngAsync(string svgContent)` method in `MainWindow.Export.cs` — show FileSavePicker, call `IDiagramExportService.RasterizeSvgToPngAsync()`, call `IExportService.SavePngAsync()`

## Task 9: Hover Toolbar (JavaScript — UnifiedRenderer.html)
- [x] 9.1 Add `.diagram-wrapper` and `.diagram-hover-toolbar` CSS styles to UnifiedRenderer.html — dark and light theme variants, opacity transition on hover
- [x] 9.2 Implement `wrapDiagramWithToolbar(svgElement)` function — wraps SVG in `.diagram-wrapper`, injects toolbar with zoom and download buttons, sends `diagramAction` messages via `postMessage`
- [x] 9.3 Call `wrapDiagramWithToolbar()` in `renderMermaid()` after successful render on the container's SVG element
- [x] 9.4 Call `wrapDiagramWithToolbar()` in `renderMarkdown()` after each embedded Mermaid block renders successfully (inside the for loop)

## Task 10: Integration Testing and Build Verification
- [x] 10.1 Build the solution (`dotnet build MermaidDiagramApp/MermaidDiagramApp.sln -p:Platform=x64`) and fix any compilation errors
- [x] 10.2 Run existing test suite (`dotnet test MermaidDiagramApp.Tests/MermaidDiagramApp.Tests.csproj -p:Platform=x64`) and verify no regressions beyond pre-existing failures
- [x] 10.3 Verify DI container resolves all new services by checking `DiContainerPropertyTests` still pass
