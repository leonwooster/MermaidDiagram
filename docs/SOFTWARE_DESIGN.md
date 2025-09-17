# Software Design Document

This document details the software architecture, components, and design decisions for the Mermaid Diagram App.

## 1. Introduction

The Mermaid Diagram App is a desktop application for Windows built using WinUI 3. It provides a simple, efficient environment for creating, viewing, and managing Mermaid diagrams. The core functionality includes a text editor for writing Mermaid syntax and a live preview panel that renders the diagram in real-time.

## 2. Architecture

The application follows a simple, view-centric architecture based on the standard WinUI 3 project structure.

- **Presentation Layer (View)**: Defined in `MainWindow.xaml`, this layer contains the UI layout, including the code editor, the preview panel, and menu controls. It uses a `Grid` with a `GridSplitter` to create a resizable two-pane layout.

- **Logic Layer (Code-Behind)**: Implemented in `MainWindow.xaml.cs`, this layer handles UI events, manages application state, and orchestrates the interaction between the code editor and the preview panel.

- **Rendering Engine**: A `WebView2` control hosts a local HTML file (`MermaidHost.html`) which uses the Mermaid.js library to parse Mermaid syntax and render it as an SVG image. This decouples the rendering logic from the main application code.

## 3. Components

### 3.1. `MainWindow.xaml` / `MainWindow.xaml.cs`

This is the primary component of the application.

- **`MainWindow.xaml`**: Defines the user interface structure, including:
  - `MenuBar`: For application commands (File, New, etc.).
  - `TextControlBox` (`CodeEditor`): A third-party control for text editing with syntax highlighting and line numbers.
  - `WebView2` (`PreviewBrowser`): Hosts the Mermaid rendering engine.
  - `GridSplitter`: Allows the user to resize the editor and preview panes.

- **`MainWindow.xaml.cs`**: Contains the application logic:
  - **Initialization**: Sets up the `WebView2` control, loads `MermaidHost.html`, and starts a `DispatcherTimer` to trigger live updates.
  - **Event Handlers**: Manages clicks for menu items like `Open`, `Save`, `Export`, and creating new diagrams from templates.
  - **Live Preview Logic**: The `Timer_Tick` event checks for changes in the `CodeEditor` and calls the `UpdatePreview` method.
  - **`UpdatePreview`**: Sends the current Mermaid code from the editor to the `WebView2` control via JavaScript interop (`ExecuteScriptAsync`).

### 3.2. `Assets/MermaidHost.html`

This is a self-contained HTML file that acts as a bridge between the C# application and the Mermaid.js library.

- **Structure**: Contains a single `div` (`mermaid-container`) to hold the rendered diagram.
- **Scripts**:
  - Includes `mermaid.min.js` from a CDN.
  - `renderDiagram(code)`: A JavaScript function called from C# to render the diagram. It takes the Mermaid syntax as a string, uses the `mermaid.render()` API to generate an SVG, and injects it into the container. It includes error handling to display parsing errors from Mermaid.js.
  - `getSvg()`: A function that returns the generated SVG content back to the C# code for export functionality.

### 3.3. File Picker Interoperability

Because the application is configured as **unpackaged**, it cannot use the standard `FileOpenPicker` and `FileSavePicker` APIs directly. These APIs require a window handle to be associated with them.

- **Solution**: A static helper class (`WinRT_InterOp`) and a COM interface (`IInitializeWithWindow`) are defined in `MainWindow.xaml.cs`.
- **`WinRT_InterOp.InitializeWithWindow()`**: This method retrieves the main window's handle (`HWND`) using `WinRT.Interop.WindowNative.GetWindowHandle()` and passes it to the picker instance by casting the picker to the `IInitializeWithWindow` interface. This is done for every `Open`, `Save`, and `Export` operation.

## 4. Data Flow: Diagram Rendering

1.  The user types Mermaid syntax into the `CodeEditor`.
2.  The `DispatcherTimer` fires every 500ms.
3.  The `Timer_Tick` handler detects a change in the text.
4.  `UpdatePreview()` is called, which serializes the editor text to a JSON string.
5.  `ExecuteScriptAsync` is called on the `PreviewBrowser` to invoke the JavaScript function `renderDiagram()` inside `MermaidHost.html`, passing the serialized code.
6.  `renderDiagram()` in JavaScript calls `mermaid.render()`.
7.  Mermaid.js parses the code and generates an SVG string.
8.  The SVG is injected into the `mermaid-container` `div`, making it visible to the user.

## 5. Dependencies

- **`Microsoft.WindowsAppSDK`**: The core SDK for building WinUI 3 applications.
- **`TextControlBox.WinUI.JuliusKirsch`**: A third-party code editor control.
- **`CommunityToolkit.WinUI.UI.Controls`**: Provides the `GridSplitter` control.
- **`Svg.Skia`**: Used for converting the generated SVG to a PNG file during the export process.
