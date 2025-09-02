using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MermaidEditor
{
    public sealed partial class MainWindow : Window
    {
        private string _currentFilePath = string.Empty;
        private bool _isModified = false;
        private const string DefaultHtml = @"
<!DOCTYPE html>
<html>
<head>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid@10.6.1/dist/mermaid.min.js""></script>
    <style>
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 20px; 
            background-color: #f8f9fa;
        }
        .mermaid { 
            text-align: center; 
            background-color: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .error { 
            color: #dc3545; 
            background-color: #f8d7da; 
            border: 1px solid #f5c6cb;
            padding: 15px;
            border-radius: 4px;
            margin: 10px 0;
        }
    </style>
</head>
<body>
    <div id='diagram' class='mermaid'>
        graph TD
            A[Welcome to Mermaid Editor] --> B[Start creating UML diagrams]
            B --> C[Class Diagrams]
            B --> D[Sequence Diagrams]
            B --> E[State Diagrams]
    </div>
    <script>
        mermaid.initialize({ startOnLoad: true, theme: 'default' });
        
        function updateDiagram(code) {
            const element = document.getElementById('diagram');
            if (!code.trim()) {
                element.innerHTML = '<div class=""error"">Enter Mermaid code to see the diagram</div>';
                return;
            }
            
            try {
                element.innerHTML = code;
                element.className = 'mermaid';
                mermaid.init(undefined, element);
            } catch (error) {
                element.innerHTML = '<div class=""error"">Error: ' + error.message + '</div>';
            }
        }
    </script>
</body>
</html>";

        public MainWindow()
        {
            this.InitializeComponent();
            InitializePreview();
            LoadDefaultTemplate();
        }

        private void InitializePreview()
        {
            try
            {
                DiagramPreview.Text = "Ready to preview Mermaid diagrams";
                StatusText.Text = "Editor initialized successfully";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error initializing preview: {ex.Message}";
            }
        }

        private void LoadDefaultTemplate()
        {
            CodeEditor.Text = @"graph TD
    A[Welcome to Mermaid Editor] --> B[Start creating UML diagrams]
    B --> C[Class Diagrams]
    B --> D[Sequence Diagrams]
    B --> E[State Diagrams]";
            
            StatusText.Text = "Ready - UML-focused Mermaid Editor";
            UpdateCharacterCount();
        }

        private async void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isModified = true;
            UpdateCharacterCount();
            await UpdatePreview();
        }

        private Task UpdatePreview()
        {
            try
            {
                string code = CodeEditor.Text;
                DiagramPreview.Text = $"Preview of Mermaid code:\n\n{code}";
                StatusText.Text = "Preview updated (WebView2 implementation pending)";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Preview error: {ex.Message}";
            }
            return Task.CompletedTask;
        }

        private void UpdateCharacterCount()
        {
            int charCount = CodeEditor.Text.Length;
            int lineCount = CodeEditor.Text.Split('\n').Length;
            
            CharCountText.Text = $"{charCount} characters";
            LineColumnText.Text = $"Ln {lineCount}";
        }

        // File Menu Handlers
        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                CodeEditor.Text = string.Empty;
                _currentFilePath = string.Empty;
                _isModified = false;
                StatusText.Text = "New file created";
            }
        }

        private void NewClassDiagram_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                CodeEditor.Text = @"classDiagram
    class Animal {
        +String name
        +int age
        +makeSound()
    }
    
    class Dog {
        +String breed
        +bark()
    }
    
    class Cat {
        +boolean indoor
        +meow()
    }
    
    Animal <|-- Dog
    Animal <|-- Cat";
                
                _currentFilePath = string.Empty;
                _isModified = false;
                StatusText.Text = "Class diagram template loaded";
            }
        }

        private void NewSequenceDiagram_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                CodeEditor.Text = @"sequenceDiagram
    participant User
    participant System
    participant Database
    
    User->>System: Login Request
    System->>Database: Validate Credentials
    Database-->>System: Validation Result
    System-->>User: Login Response
    
    Note over User,Database: Authentication Flow";
                
                _currentFilePath = string.Empty;
                _isModified = false;
                StatusText.Text = "Sequence diagram template loaded";
            }
        }

        private void NewStateDiagram_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                CodeEditor.Text = @"stateDiagram-v2
    [*] --> Idle
    
    Idle --> Processing : start()
    Processing --> Completed : finish()
    Processing --> Error : error()
    
    Completed --> [*]
    Error --> Idle : reset()
    
    note right of Processing
        This is a processing state
    end note";
                
                _currentFilePath = string.Empty;
                _isModified = false;
                StatusText.Text = "State diagram template loaded";
            }
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".mmd");
                picker.FileTypeFilter.Add(".txt");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // Get the current window handle for WinUI3
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    string content = await FileIO.ReadTextAsync(file);
                    CodeEditor.Text = content;
                    _currentFilePath = file.Path;
                    _isModified = false;
                    StatusText.Text = $"Opened: {file.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error opening file: {ex.Message}";
            }
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsFile_Click(sender, e);
            }
            else
            {
                await SaveToFile(_currentFilePath);
            }
        }

        private async void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("Mermaid Diagram", new[] { ".mmd" });
                picker.FileTypeChoices.Add("Text File", new[] { ".txt" });
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = "diagram";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, CodeEditor.Text);
                    _currentFilePath = file.Path;
                    _isModified = false;
                    StatusText.Text = $"Saved: {file.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error saving file: {ex.Message}";
            }
        }

        private async Task SaveToFile(string filePath)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, CodeEditor.Text);
                _isModified = false;
                StatusText.Text = $"Saved: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error saving: {ex.Message}";
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                this.Close();
            }
        }

        // Edit Menu Handlers
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            // TextBox doesn't have built-in undo in WinUI3, would need custom implementation
            StatusText.Text = "Undo functionality - to be implemented";
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            // TextBox doesn't have built-in redo in WinUI3, would need custom implementation
            StatusText.Text = "Redo functionality - to be implemented";
        }

        // View Menu Handlers
        private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            await UpdatePreview();
            StatusText.Text = "Preview refreshed";
        }

        private void DiagramPreview_NavigationCompleted(object sender, object e)
        {
            StatusText.Text = "Preview loaded successfully";
        }

        private bool CheckUnsavedChanges()
        {
            if (_isModified)
            {
                // In a full implementation, show a dialog asking to save changes
                // For now, just return true to proceed
                return true;
            }
            return true;
        }
    }
}
