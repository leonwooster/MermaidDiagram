# Implementation Plan: MainWindow Refactoring

- [ ] 1. Preparation and analysis
  - Create backup of MainWindow.xaml.cs
  - Analyze current file structure and identify all methods
  - Create method categorization map (which methods go to which partial class)
  - Verify all existing tests pass before refactoring
  - _Requirements: 3.1, 3.3_

- [ ] 2. Create partial class file structure
  - Create MainWindow.WebView.cs with file header and class declaration
  - Create MainWindow.FileOperations.cs with file header and class declaration
  - Create MainWindow.UI.cs with file header and class declaration
  - Create MainWindow.AI.cs with file header and class declaration
  - Create MainWindow.Builder.cs with file header and class declaration
  - Create MainWindow.Updates.cs with file header and class declaration
  - Verify all files compile with empty class bodies
  - _Requirements: 1.1, 5.1, 5.3, 6.1, 6.2, 6.3_

- [ ] 3. Refactor WebView functionality to MainWindow.WebView.cs
  - Move InitializeWebViewAsync method
  - Move UpdatePreview method
  - Move ExecuteRenderingScript method
  - Move OnRenderingStateChanged method
  - Move UpdateRenderModeIndicator method
  - Move zoom control methods (SetupCtrlWheelZoom, ApplyPreviewZoom, ZoomIn, ZoomOut, ZoomReset)
  - Move drag mode toggle methods
  - Move WebView message handling methods
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 1.2, 2.2, 4.1, 5.4_

- [ ] 3.1 Test WebView functionality
  - Verify Mermaid diagram rendering works
  - Verify Markdown rendering works
  - Verify zoom controls work
  - Verify drag mode toggle works
  - _Requirements: 3.1, 3.5_

- [ ] 4. Refactor file operations to MainWindow.FileOperations.cs
  - Move file open/save/close methods
  - Move file watching setup methods
  - Move window state management methods (RestoreWindowStateAsync, SaveWindowStateAsync)
  - Move file path handling methods
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 1.3, 2.3, 4.1, 5.4_

- [ ] 4.1 Test file operations
  - Verify file open works
  - Verify file save works
  - Verify file close works
  - Verify window state persistence works
  - _Requirements: 3.1, 3.5_

- [ ] 5. Refactor UI event handlers to MainWindow.UI.cs
  - Move menu item click handlers (New diagram types, Open, Save, Export, Exit)
  - Move dialog management methods
  - Move full screen toggle methods
  - Move presentation mode methods
  - Move keyboard shortcut registration (RegisterKeyboardShortcuts)
  - Move keyboard event handlers
  - Move status bar update methods
  - Move InfoBar management methods
  - Move syntax checking methods (CheckSyntax_Click, CheckForSyntaxIssues)
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 1.4, 2.4, 4.1, 5.4_

- [ ] 5.1 Test UI functionality
  - Verify all menu items work
  - Verify keyboard shortcuts work
  - Verify full screen mode works
  - Verify presentation mode works
  - Verify dialogs display correctly
  - _Requirements: 3.1, 3.5_

- [ ] 6. Refactor AI features to MainWindow.AI.cs
  - Move InitializeAiServices method
  - Move AI configuration loading/saving methods
  - Move floating AI prompt setup methods
  - Move PopOutFloatingPrompt method
  - Move OpenAiSettingsAndRefreshVmAsync method
  - Move ImportCodeToCanvasAsync method
  - Move AI settings dialog handler
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 1.5, 2.5, 4.1, 5.4_

- [ ] 6.1 Test AI functionality
  - Verify AI service initialization works
  - Verify AI prompt displays correctly
  - Verify AI diagram generation works
  - Verify AI settings dialog works
  - Verify pop-out window works
  - _Requirements: 3.1, 3.5_

- [ ] 7. Refactor visual builder to MainWindow.Builder.cs
  - Move UpdateBuilderVisibility method
  - Move DiagramBuilderViewModel_PropertyChanged handler
  - Move BuilderTool_Click handler
  - Move canvas setup and wiring methods
  - Move shape toolbox integration methods
  - Move properties panel wiring methods
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 1.5, 2.6, 4.1, 5.4_

- [ ] 7.1 Test visual builder functionality
  - Verify builder panel shows/hides correctly
  - Verify canvas operations work
  - Verify shape toolbox works
  - Verify properties panel works
  - Verify code generation from canvas works
  - _Requirements: 3.1, 3.5_

- [ ] 8. Refactor update functionality to MainWindow.Updates.cs
  - Move CheckForMermaidUpdatesAsync method
  - Move CheckForNewerVersionAsync method
  - Move CheckForNewerVersionInternalAsync method
  - Move UpdateMermaid_Click handler
  - Move version comparison logic
  - Move update InfoBar management
  - Add necessary using statements
  - Verify compilation after migration
  - _Requirements: 2.7, 4.1, 5.4_

- [ ] 8.1 Test update functionality
  - Verify update checking works
  - Verify version comparison works
  - Verify update notification displays
  - Verify update installation works
  - _Requirements: 3.1, 3.5_

- [ ] 9. Clean up MainWindow.xaml.cs core file
  - Remove all migrated methods
  - Keep only constructor, core fields, and basic initialization
  - Organize remaining code logically
  - Add clear section comments
  - Ensure file is under 300 lines
  - Verify compilation
  - _Requirements: 2.1, 4.1, 5.1, 5.2_

- [ ] 10. Add documentation and comments
  - Add file header comments to each partial class explaining its purpose
  - Add XML documentation comments to public methods
  - Add inline comments for complex logic
  - Create migration guide document
  - _Requirements: 5.1, 5.2, 5.3, 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 11. Verify file size constraints
  - Check MainWindow.xaml.cs is under 300 lines
  - Check MainWindow.WebView.cs is under 500 lines
  - Check MainWindow.FileOperations.cs is under 500 lines
  - Check MainWindow.UI.cs is under 500 lines
  - Check MainWindow.AI.cs is under 500 lines
  - Check MainWindow.Builder.cs is under 500 lines
  - Check MainWindow.Updates.cs is under 500 lines
  - _Requirements: 4.1, 4.2, 4.3_

- [ ] 12. Run comprehensive testing
  - Run all existing unit tests
  - Run all existing integration tests
  - Perform manual testing of all features
  - Verify no regressions
  - Document any issues found
  - _Requirements: 3.2, 3.3, 3.4, 3.5_

- [ ] 13. Code review and cleanup
  - Review all partial class files for consistency
  - Ensure no duplicate code exists
  - Verify all using statements are necessary
  - Check for any orphaned methods
  - Verify proper access modifiers
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [ ] 14. Final verification
  - Build solution in Debug configuration
  - Build solution in Release configuration
  - Run full test suite
  - Perform smoke testing of critical features
  - Verify application launches and functions correctly
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 15. Documentation and handoff
  - Update developer documentation with new file structure
  - Create migration guide for future development
  - Document where to add new functionality
  - Update code review guidelines
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
