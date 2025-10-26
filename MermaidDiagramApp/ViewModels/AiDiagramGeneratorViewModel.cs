using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using MermaidDiagramApp.Services.AI;

namespace MermaidDiagramApp.ViewModels
{
    /// <summary>
    /// ViewModel for the AI Diagram Generator panel
    /// </summary>
    public class AiDiagramGeneratorViewModel : INotifyPropertyChanged
    {
        private readonly IAiService _aiService;
        private string _userPrompt = string.Empty;
        private string _generatedCode = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isGenerating = false;
        private string _selectedProvider = "OpenAI";
        private string _selectedModel = "gpt-3.5-turbo";
        private double _temperature = 0.7;
        private ObservableCollection<string> _providers;
        private ObservableCollection<string> _models;

        public AiDiagramGeneratorViewModel(IAiService aiService)
        {
            _aiService = aiService;
            _providers = new ObservableCollection<string> { "OpenAI", "Azure", "Ollama" };
            _models = new ObservableCollection<string> 
            { 
                "gpt-3.5-turbo", 
                "gpt-4", 
                "llama2", 
                "mistral", 
                "codellama" 
            };
        }

        public string UserPrompt
        {
            get => _userPrompt;
            set => SetProperty(ref _userPrompt, value);
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set => SetProperty(ref _generatedCode, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
        }

        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (SetProperty(ref _selectedProvider, value))
                {
                    UpdateModelsForProvider(value);
                }
            }
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => SetProperty(ref _selectedModel, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public ObservableCollection<string> Providers => _providers;

        public ObservableCollection<string> Models => _models;

        public async Task GenerateDiagramAsync()
        {
            if (string.IsNullOrWhiteSpace(UserPrompt))
            {
                StatusMessage = "Please enter a prompt first.";
                return;
            }

            try
            {
                IsGenerating = true;
                StatusMessage = "Generating diagram...";

                GeneratedCode = await _aiService.GenerateMermaidDiagramAsync(UserPrompt);
                StatusMessage = "Diagram generated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                GeneratedCode = string.Empty;
            }
            finally
            {
                IsGenerating = false;
            }
        }

        public async Task DetermineDiagramTypeAsync()
        {
            if (string.IsNullOrWhiteSpace(UserPrompt))
            {
                StatusMessage = "Please enter a prompt first.";
                return;
            }

            try
            {
                IsGenerating = true;
                StatusMessage = "Analyzing prompt...";

                var diagramType = await _aiService.DetermineDiagramTypeAsync(UserPrompt);
                StatusMessage = $"Suggested diagram type: {diagramType}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void UpdateModelsForProvider(string provider)
        {
            _models.Clear();
            
            switch (provider.ToLower())
            {
                case "openai":
                    _models.Add("gpt-3.5-turbo");
                    _models.Add("gpt-4");
                    _models.Add("gpt-4-turbo");
                    break;
                case "azure":
                    _models.Add("gpt-35-turbo");
                    _models.Add("gpt-4");
                    _models.Add("gpt-4-turbo");
                    break;
                case "ollama":
                    _models.Add("llama2");
                    _models.Add("mistral");
                    _models.Add("codellama");
                    _models.Add("neural-chat");
                    break;
            }
            
            if (_models.Count > 0)
            {
                SelectedModel = _models[0];
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
