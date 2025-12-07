# Implementation Plan: Markdown to Word Export

- [x] 1. Set up project dependencies and core interfaces





  - Install Markdig NuGet package for Markdown parsing
  - Install DocumentFormat.OpenXml NuGet package for Word generation
  - Create Export folder structure under Services
  - Define core interfaces: IMarkdownParser, IWordDocumentGenerator, IMermaidImageRenderer
  - _Requirements: 8.1, 8.2_

- [x] 1.1 Write property test for dependency installation



  - **Property 1: Package references are resolvable**
  - **Validates: Requirements 8.1, 8.2**

- [x] 2. Implement ImagePathResolver utility




  - Create ImagePathResolver class with path resolution logic
  - Implement ResolveImagePath method for relative and absolute paths
  - Implement IsValidImagePath validation method
  - Implement DetectImageFormat method for image type detection
  - _Requirements: 5.1, 5.2_

- [x] 2.1 Write property test for image path resolution


  - **Property 18: Image path resolution**
  - **Validates: Requirements 5.1, 5.2**

- [x] 2.2 Write unit tests for ImagePathResolver


  - Test relative path resolution
  - Test absolute path resolution
  - Test URL pass-through
  - Test invalid path handling
  - _Requirements: 5.1, 5.2_

- [x] 3. Implement Markdown parsing service





  - Create MarkdigMarkdownParser class implementing IMarkdownParser
  - Implement Parse method to convert Markdown to AST
  - Implement ExtractMermaidBlocks to identify Mermaid code blocks
  - Implement ExtractImageReferences to find image references
  - _Requirements: 1.3, 8.1_

- [x] 3.1 Write property test for Mermaid block extraction


  - **Property 2: Mermaid block identification completeness**
  - **Validates: Requirements 1.3**

- [x] 3.2 Write unit tests for MarkdigMarkdownParser


  - Test parsing various Markdown elements
  - Test Mermaid block extraction
  - Test image reference extraction
  - Test malformed Markdown handling
  - _Requirements: 1.3_

- [x] 4. Implement Mermaid image rendering service





  - Create WebView2MermaidImageRenderer class implementing IMermaidImageRenderer
  - Integrate with existing MermaidRenderer for diagram rendering
  - Implement RenderToImageAsync to convert Mermaid to PNG
  - Use existing Svg.Skia infrastructure for SVG to PNG conversion
  - Handle rendering errors gracefully
  - _Requirements: 4.1, 4.2, 8.3_

- [x] 4.1 Write property test for Mermaid rendering


  - **Property 14: Mermaid diagram rendering**
  - **Validates: Requirements 4.1**

- [x] 4.2 Write property test for diagram image format


  - **Property 15: Diagram image format**
  - **Validates: Requirements 4.2**

- [x] 4.3 Write unit tests for WebView2MermaidImageRenderer


  - Test valid Mermaid rendering
  - Test PNG format output
  - Test transparent background
  - Test error handling for invalid syntax
  - _Requirements: 4.1, 4.2_

- [-] 5. Implement Word document generator






  - Create OpenXmlWordDocumentGenerator class implementing IWordDocumentGenerator
  - Implement CreateDocument method to initialize DOCX file
  - Implement AddHeading method with H1-H6 support
  - Implement AddParagraph method with text formatting (bold, italic, code)
  - Implement AddImage method with sizing and aspect ratio preservation
  - Implement AddTable method with cell alignment
  - Implement AddList method for ordered and unordered lists with nesting
  - Implement AddCodeBlock method with monospace font and shading
  - Implement Save and Dispose methods
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 4.4, 5.4, 5.5, 6.1, 8.2_

- [x] 5.1 Write property test for heading preservation



  - **Property 7: Heading level preservation**
  - **Validates: Requirements 3.1**

- [x] 5.2 Write property test for text formatting

  - **Property 8: Text formatting preservation**
  - **Validates: Requirements 3.2, 3.3, 3.8**

- [x] 5.3 Write property test for list conversion

  - **Property 9: List structure conversion**
  - **Validates: Requirements 3.4, 3.5**

- [x] 5.4 Write property test for nested lists

  - **Property 10: Nested list hierarchy preservation**
  - **Validates: Requirements 3.6**

- [x] 5.5 Write property test for code block formatting

  - **Property 11: Code block formatting**
  - **Validates: Requirements 3.7**

- [x] 5.6 Write property test for blockquote styling

  - **Property 12: Blockquote styling**
  - **Validates: Requirements 3.9**

- [x] 5.7 Write property test for table structure

  - **Property 13: Table structure preservation**
  - **Validates: Requirements 3.10**

- [x] 5.8 Write property test for diagram scaling


  - **Property 16: Diagram scaling maintains aspect ratio**
  - **Validates: Requirements 4.4**

- [x] 5.9 Write property test for image format preservation



  - **Property 19: Image format preservation**
  - **Validates: Requirements 5.4**

- [ ] 5.10 Write property test for SVG conversion
  - **Property 20: SVG to PNG conversion**
  - **Validates: Requirements 5.5**

- [ ] 5.11 Write property test for hyperlink conversion
  - **Property 21: Hyperlink conversion**
  - **Validates: Requirements 6.1, 6.2, 6.3**

- [x] 5.12 Write unit tests for OpenXmlWordDocumentGenerator





  - Test document creation
  - Test all element types (headings, paragraphs, lists, tables, images, code blocks)
  - Test formatting preservation
  - Test image embedding
  - Test hyperlink creation
  - _Requirements: 3.1-3.10, 4.4, 5.4, 5.5, 6.1_

- [x] 6. Implement export orchestration service





  - Create MarkdownToWordExportService class
  - Implement ExportToWordAsync method to orchestrate workflow
  - Integrate MarkdownParser, MermaidImageRenderer, and WordDocumentGenerator
  - Implement progress reporting using IProgress<ExportProgress>
  - Implement cancellation support using CancellationToken
  - Implement temporary file management and cleanup
  - Handle all error scenarios gracefully
  - _Requirements: 1.2, 2.2, 2.3, 2.4, 2.5, 4.3, 4.5, 5.3, 7.3, 7.4_

- [x] 6.1 Write property test for file loading


  - **Property 1: Markdown file loading preserves content**
  - **Validates: Requirements 1.2**

- [x] 6.2 Write property test for export file creation


  - **Property 4: Export creates file at specified path**
  - **Validates: Requirements 2.2**

- [x] 6.3 Write property test for success notification


  - **Property 5: Successful export shows notification**
  - **Validates: Requirements 2.3**

- [x] 6.4 Write property test for progress indicator


  - **Property 6: Export progress indicator visibility**
  - **Validates: Requirements 2.5**

- [x] 6.5 Write property test for diagram position preservation


  - **Property 17: Multiple diagram position preservation**
  - **Validates: Requirements 4.5**

- [x] 6.6 Write property test for cancellation


  - **Property 22: Cancellation stops processing**
  - **Validates: Requirements 7.3**

- [x] 6.7 Write property test for cleanup


  - **Property 23: Cancellation cleanup**
  - **Validates: Requirements 7.4**

- [x] 6.8 Write unit tests for MarkdownToWordExportService


  - Test workflow orchestration
  - Test progress reporting
  - Test cancellation handling
  - Test temporary file cleanup
  - Test error handling
  - _Requirements: 2.2, 2.3, 2.4, 2.5, 7.3, 7.4_

- [x] 7. Checkpoint - Ensure all core services are working




  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement ViewModel for export workflow





  - Create MarkdownToWordViewModel class
  - Implement INotifyPropertyChanged for property bindings
  - Create properties: MarkdownFilePath, OutputPath, IsExporting, ProgressPercentage, ProgressMessage
  - Implement OpenMarkdownFileCommand using RelayCommand pattern
  - Implement ExportToWordCommand using RelayCommand pattern
  - Implement CancelExportCommand using RelayCommand pattern
  - Implement CanExport computed property based on file loaded state
  - Wire up commands to MarkdownToWordExportService
  - _Requirements: 1.1, 1.5, 2.1, 9.2_

- [x] 8.1 Write property test for command state


  - **Property 3: Command state reflects file loading**
  - **Validates: Requirements 1.5, 9.2**

- [x] 8.2 Write unit tests for MarkdownToWordViewModel


  - Test property change notifications
  - Test command execution
  - Test command can-execute logic
  - Test progress updates
  - _Requirements: 1.5, 2.1, 9.2_

- [x] 9. Add UI elements to MainWindow





  - Add "Open Markdown File" menu item to File menu
  - Add "Export to Word" menu item to File menu
  - Create file picker for opening Markdown files with .md and .markdown filters
  - Create save file picker for Word export with .docx filter
  - Add progress dialog for export operation with cancel button
  - Add success/error notification dialogs
  - Wire up menu items to ViewModel commands
  - _Requirements: 1.1, 1.4, 2.1, 2.3, 2.4, 2.5, 9.1_

- [x] 9.1 Write property test for window title


  - **Property 24: Window title reflects loaded file**
  - **Validates: Requirements 9.3**

- [x] 10. Implement file loading workflow





  - Handle "Open Markdown File" command in MainWindow
  - Load file content using File.ReadAllTextAsync
  - Validate UTF-8 encoding and handle errors
  - Update window title with file name
  - Enable "Export to Word" command
  - Display error dialog for invalid files
  - _Requirements: 1.2, 1.4, 1.5, 9.3_

- [x] 11. Implement export workflow





  - Handle "Export to Word" command in MainWindow
  - Show save file dialog
  - Create progress dialog
  - Call MarkdownToWordExportService.ExportToWordAsync
  - Update progress dialog with export progress
  - Handle cancellation requests
  - Show success notification on completion
  - Show error dialog on failure
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 7.3_

- [x] 12. Implement error handling and edge cases





  - Add try-catch blocks for all file operations
  - Handle missing image files with placeholder text
  - Handle Mermaid syntax errors with error messages in document
  - Handle file system errors (access denied, disk full)
  - Implement logging for all errors
  - Ensure temporary files are always cleaned up
  - _Requirements: 1.4, 2.4, 4.3, 5.3_

- [x] 12.1 Write integration tests for error scenarios


  - Test missing image handling
  - Test Mermaid syntax error handling
  - Test file system error handling
  - Test temporary file cleanup on error
  - _Requirements: 4.3, 5.3_

- [x] 13. Checkpoint - Ensure all tests pass





  - Ensure all tests pass, ask the user if questions arise.

- [x] 14. Integration testing






  - Test end-to-end export with real Markdown files
  - Test with various Markdown elements (headings, lists, tables, images, code)
  - Test with multiple Mermaid diagrams
  - Test with various image formats (PNG, JPG, GIF, SVG)
  - Test with large files (>1MB)
  - Test cancellation during export
  - Verify exported Word documents open correctly in Microsoft Word
  - _Requirements: All_

- [x] 15. Performance testing and optimization





  - Test export performance with various file sizes
  - Profile memory usage during export
  - Optimize rendering pipeline if needed
  - Test with 50+ Mermaid diagrams
  - Test with 100+ images
  - _Requirements: 7.1_

- [x] 16. Final checkpoint - Complete feature verification









  - Ensure all tests pass, ask the user if questions arise.


