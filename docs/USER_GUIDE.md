# User Guide

Welcome to the Mermaid Diagram App! This guide will walk you through its features and how to use them.

## 1. The Interface

The main window is split into two sections:

- **Code Editor (Left)**: This is where you write and edit your Mermaid diagram syntax.
- **Preview Panel (Right)**: This panel shows a live rendering of the diagram from the code editor.

You can resize these panels by dragging the vertical splitter between them.

## 2. Creating a New Diagram

You can start a new diagram in two ways:

- **Blank Diagram**: Simply start typing in the code editor.
- **From a Template**: Go to the `New` menu and select one of the available diagram templates (e.g., `Class Diagram`, `Sequence Diagram`). This will load a basic example into the editor to get you started.

## 3. Live Preview

As you type in the code editor, the preview panel will automatically update every half-second to reflect your changes. 

If there is an error in your Mermaid syntax, the preview panel will display a red error message from the Mermaid.js library, helping you to debug your diagram.

## 4. Opening and Saving Files

### Opening a File

1.  Go to `File` > `Open`.
2.  A file dialog will appear.
3.  Select a Mermaid diagram file (`.mmd` or `.md`).
4.  The content of the file will be loaded into the code editor, and the preview will update.

### Saving a File

1.  Go to `File` > `Save`.
2.  A file dialog will appear.
3.  Choose a location, enter a file name, and click `Save`.
4.  The current content of the code editor will be saved to the specified file.

## 5. Exporting Diagrams

You can export your diagram as an image in either SVG or PNG format.

### Exporting to SVG

1.  Go to `Export` > `Export as SVG`.
2.  A file dialog will appear.
3.  Choose a location, enter a file name, and click `Save`.
4.  The currently rendered diagram will be saved as a `.svg` file.

### Exporting to PNG

1.  Go to `Export` > `Export as PNG`.
2.  A file dialog will appear.
3.  Choose a location, enter a file name, and click `Save`.
4.  The currently rendered diagram will be saved as a `.png` file.
