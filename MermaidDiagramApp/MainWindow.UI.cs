// MainWindow.UI.cs
// Partial class for MainWindow containing new diagram templates, fullscreen/presentation
// mode toggling, keyboard shortcut wiring, and menu/toolbar click handlers.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using MermaidDiagramApp.Services;
using MermaidDiagramApp.Services.Logging;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp
{
    /// <summary>
    /// Partial class for MainWindow containing new diagram templates, fullscreen/presentation
    /// mode toggling, keyboard wiring, and menu/toolbar click handlers.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region New Diagram Handlers

        private void NewDiagram(string diagramType)
        {
            switch (diagramType)
            {
                case "classDiagram":
                    NewClassDiagram_Click(this, new RoutedEventArgs());
                    break;
                case "sequenceDiagram":
                    NewSequenceDiagram_Click(this, new RoutedEventArgs());
                    break;
                case "stateDiagram":
                    NewStateDiagram_Click(this, new RoutedEventArgs());
                    break;
                case "activityDiagram":
                    NewActivityDiagram_Click(this, new RoutedEventArgs());
                    break;
                case "flowchart":
                    NewFlowchart_Click(this, new RoutedEventArgs());
                    break;
                case "ganttChart":
                    NewGanttChart_Click(this, new RoutedEventArgs());
                    break;
                case "pieChart":
                    NewPieChart_Click(this, new RoutedEventArgs());
                    break;
                case "gitGraph":
                    NewGitGraph_Click(this, new RoutedEventArgs());
                    break;
                default:
                    _logger.LogWarning($"Unknown diagram type requested: {diagramType}");
                    break;
            }
        }

        private void NewClassDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "classDiagram\n    class Animal {\n        +String name\n        +int age\n        +void eat()\n    }";
            _ = UpdatePreview();
        }

        private void NewSequenceDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "sequenceDiagram\n    participant Alice\n    participant Bob\n    Alice->>Bob: Hello Bob, how are you?\n    Bob-->>Alice: I am good, thanks!";
            _ = UpdatePreview();
        }

        private void NewStateDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = "stateDiagram-v2\n    [*] --> Still\n    Still --> [*]\n    Still --> Moving\n    Moving --> Still\n    Moving --> Crash\n    Crash --> [*]";
            _ = UpdatePreview();
        }

        private void NewActivityDiagram_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                activityDiagram
                    start
                    :Initial Action;
                    if (condition) then (yes)
                        :Success;
                    else (no)
                        :Failure;
                    endif
                    :Another Action;
                    stop
                """;
            _ = UpdatePreview();
        }

        private void NewFlowchart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                graph TD
                    A[Start] --> B{Is it?};
                    B -->|Yes| C[OK];
                    C --> D[End];
                    B -->|No| E[Not OK];
                    E --> D[End];
                """;
            _ = UpdatePreview();
        }

        private void NewGanttChart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                gantt
                    title A Gantt Diagram
                    dateFormat  YYYY-MM-DD
                    section Section
                    A task           :a1, 2024-01-01, 30d
                    Another task     :after a1  , 20d
                    section Another
                    Task in sec      :2024-01-12  , 12d
                    another task      : 24d
                """;
            _ = UpdatePreview();
        }

        private void NewPieChart_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                pie
                    title Key elements in Product X
                    "Calcium" : 42.96
                    "Potassium" : 50.05
                    "Magnesium" : 10.01
                    "Iron" :  5
                """;
            _ = UpdatePreview();
        }

        private void NewGitGraph_Click(object sender, RoutedEventArgs e)
        {
            CodeEditor.Text = """
                gitGraph
                   commit
                   commit
                   branch develop
                   checkout develop
                   commit
                   commit
                   checkout main
                   merge develop
                   commit
                   commit
                """;
            _ = UpdatePreview();
        }

        #endregion

        #region Fullscreen and Presentation Mode

        private void ToggleFullScreen_Click(object sender, RoutedEventArgs e)
        {
            _isFullScreen = !_isFullScreen;
            if (_isFullScreen)
            {
                MainMenuBar.Visibility = Visibility.Collapsed;
                EditorColumn.Width = new GridLength(0);
                EditorPreviewSplitter.Visibility = Visibility.Collapsed;
                
                ToolboxColumn.Width = new GridLength(0);
                ToolboxPanel.Visibility = Visibility.Collapsed;
                ToolboxSplitter.Visibility = Visibility.Collapsed;
                BuilderColumn.Width = new GridLength(0);
                CanvasPanel.Visibility = Visibility.Collapsed;
                CanvasSplitter.Visibility = Visibility.Collapsed;
                BuilderPanel.Visibility = Visibility.Collapsed;
                PropertiesColumn.Width = new GridLength(0);
                PropertiesPanel.Visibility = Visibility.Collapsed;
                PropertiesSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainMenuBar.Visibility = Visibility.Visible;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitter.Visibility = Visibility.Visible;
                UpdateBuilderVisibility();
            }
        }

        private void PresentationMode_Click(object sender, RoutedEventArgs e)
        {
            var appWindow = GetAppWindowForCurrentWindow();

            if (_isPresentationMode)
            {
                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                MainMenuBar.Visibility = Visibility.Visible;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorPreviewSplitter.Visibility = Visibility.Visible;
                UpdateBuilderVisibility();
            }
            else
            {
                appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                MainMenuBar.Visibility = Visibility.Collapsed;
                EditorColumn.Width = new GridLength(0);
                EditorPreviewSplitter.Visibility = Visibility.Collapsed;
                
                ToolboxColumn.Width = new GridLength(0);
                ToolboxPanel.Visibility = Visibility.Collapsed;
                ToolboxSplitter.Visibility = Visibility.Collapsed;
                BuilderColumn.Width = new GridLength(0);
                CanvasPanel.Visibility = Visibility.Collapsed;
                CanvasSplitter.Visibility = Visibility.Collapsed;
                BuilderPanel.Visibility = Visibility.Collapsed;
                PropertiesColumn.Width = new GridLength(0);
                PropertiesPanel.Visibility = Visibility.Collapsed;
                PropertiesSplitter.Visibility = Visibility.Collapsed;
            }
            _isPresentationMode = !_isPresentationMode;
        }

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (_isFullScreen)
                {
                    ToggleFullScreen_Click(this, new RoutedEventArgs());
                }
                if (_isPresentationMode)
                {
                    PresentationMode_Click(this, new RoutedEventArgs());
                }
            }
        }

        #endregion

        #region Menu and Toolbar Handlers

        private async void MarkdownStyleSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentContentType != ContentType.Markdown && _currentContentType != ContentType.MarkdownWithMermaid)
                {
                    var infoDialog = new ContentDialog
                    {
                        Title = "Markdown Style Settings",
                        Content = "Note: These settings only apply to Markdown (.md) files. You are currently viewing a Mermaid diagram. Open a Markdown file to see the style changes.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await infoDialog.ShowAsync();
                }

                var dialog = new Views.MarkdownStyleSettingsDialog
                {
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    _styleSettingsService.ClearCache();
                    var settings = _styleSettingsService.LoadSettings();
                    _logger.LogDebug($"Loaded settings - FontSize: {settings.FontSize}, FontFamily: {settings.FontFamily}");
                    _logger.LogDebug($"Current content type: {_currentContentType}");
                    
                    var settingsJson = JsonSerializer.Serialize(new
                    {
                        fontSize = settings.FontSize,
                        fontFamily = settings.FontFamily,
                        lineHeight = settings.LineHeight,
                        maxContentWidth = settings.MaxContentWidth,
                        codeFontFamily = settings.CodeFontFamily,
                        codeFontSize = settings.CodeFontSize
                    });

                    _logger.LogDebug($"Settings JSON: {settingsJson}");

                    var updateScript = $"if (window.updateStyleSettings) {{ window.updateStyleSettings({settingsJson}); }}";
                    var scriptResult = await PreviewBrowser.ExecuteScriptAsync(updateScript);
                    _logger.LogDebug($"Script execution result: {scriptResult}");

                    _lastPreviewedCode = null;
                    await UpdatePreview();
                    _logger.LogDebug("Markdown style settings updated and applied");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error opening Markdown style settings: {ex.Message}", ex);
            }
        }

        private async void CheckSyntax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = CodeEditor.Text;
                
                if (string.IsNullOrWhiteSpace(code))
                {
                    var emptyDialog = new ContentDialog
                    {
                        Title = "No Code to Check",
                        Content = "The editor is empty. Please add some Mermaid diagram code first.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyDialog.ShowAsync();
                    return;
                }

                var analyzer = new MermaidSyntaxAnalyzer();
                var fixer = new MermaidSyntaxFixer();
                var currentCode = code;
                var totalFixesApplied = 0;

                while (true)
                {
                    var issues = analyzer.Analyze(currentCode);

                    var dialog = new SyntaxIssuesDialog
                    {
                        XamlRoot = this.Content.XamlRoot
                    };
                    dialog.LoadIssues(issues, currentCode);

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        var selectedIssues = dialog.ViewModel.Issues.Where(i => i.IsSelected).ToList();
                        
                        if (selectedIssues.Any())
                        {
                            var fixedCode = fixer.ApplyFixes(currentCode, selectedIssues);
                            totalFixesApplied += selectedIssues.Count;
                            CodeEditor.Text = fixedCode;
                            currentCode = fixedCode;
                            await UpdatePreview();

                            var remainingIssues = analyzer.Analyze(fixedCode);

                            if (remainingIssues.Count > 0)
                            {
                                var continueDialog = new ContentDialog
                                {
                                    Title = "More Issues Found",
                                    Content = $"Applied {selectedIssues.Count} fix(es). Found {remainingIssues.Count} remaining issue(s).\n\nDo you want to review and fix the remaining issues?",
                                    PrimaryButtonText = "Yes, Continue",
                                    CloseButtonText = "No, Done",
                                    DefaultButton = ContentDialogButton.Primary,
                                    XamlRoot = this.Content.XamlRoot
                                };

                                var continueResult = await continueDialog.ShowAsync();
                                if (continueResult != ContentDialogResult.Primary) break;
                            }
                            else
                            {
                                var successDialog = new ContentDialog
                                {
                                    Title = "All Issues Fixed!",
                                    Content = $"Successfully applied {totalFixesApplied} fix(es) in total.\n\nNo remaining syntax issues detected. Your diagram is clean!",
                                    CloseButtonText = "OK",
                                    XamlRoot = this.Content.XamlRoot
                                };
                                await successDialog.ShowAsync();
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking syntax: {ex.Message}", ex);
                
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"An error occurred while checking syntax: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _lastPreviewedCode = null;
                await UpdatePreview();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Manual refresh failed: {ex.Message}", ex);
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle preview key down events
        }

        private void AiSettings_Click(object sender, RoutedEventArgs e)
        {
            // Handle AI settings
        }

        private void AiPanelTool_Click(object sender, RoutedEventArgs e)
        {
            // Handle AI panel toggle
        }

        private void DismissKeyboardTip_Click(object sender, RoutedEventArgs e)
        {
            // Handle dismiss keyboard tip
        }

        #endregion
    }
}
