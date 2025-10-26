using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MermaidDiagramApp.Services.AI;

namespace MermaidDiagramApp.Views
{
    /// <summary>
    /// Dialog for configuring AI service settings
    /// </summary>
    public sealed partial class AiSettingsDialog : ContentDialog
    {
        private AiConfiguration _config;
        private Dictionary<string, List<string>> _providerModels;

        public AiSettingsDialog(AiConfiguration config)
        {
            this.InitializeComponent();
            _config = config;
            _providerModels = new Dictionary<string, List<string>>();
            InitializeProviderModels();
            LoadSettings();
        }

        private void InitializeProviderModels()
        {
            _providerModels = new Dictionary<string, List<string>>
            {
                {
                    "OpenAI", new List<string>
                    {
                        "gpt-3.5-turbo",
                        "gpt-4",
                        "gpt-4-turbo",
                        "gpt-4o"
                    }
                },
                {
                    "Azure", new List<string>
                    {
                        "gpt-35-turbo",
                        "gpt-4",
                        "gpt-4-turbo"
                    }
                },
                {
                    "Ollama", new List<string>
                    {
                        "llama2",
                        "llama3",
                        "mistral",
                        "mixtral",
                        "codellama",
                        "neural-chat",
                        "starling-lm"
                    }
                }
            };

            // Populate provider combo box
            ProviderComboBox.Items.Clear();
            foreach (var provider in _providerModels.Keys)
            {
                ProviderComboBox.Items.Add(provider);
            }
        }

        private void LoadSettings()
        {
            // Load current settings into UI controls
            ProviderComboBox.SelectedItem = _config.ProviderType;
            ApiKeyPasswordBox.Password = _config.ApiKey;
            BaseUrlTextBox.Text = _config.BaseUrl;
            TemperatureSlider.Value = _config.Temperature;
            MaxTokensNumberBox.Value = _config.MaxTokens;
            TimeoutNumberBox.Value = _config.TimeoutSeconds;

            // Update model list based on selected provider
            UpdateModelList(_config.ProviderType);
            ModelComboBox.SelectedItem = _config.ModelName;

            // If BaseUrl is empty, auto-fill a sensible default for the selected provider
            if (string.IsNullOrWhiteSpace(BaseUrlTextBox.Text))
            {
                SetDefaultBaseUrlForProvider(_config.ProviderType, onlyIfEmpty: false);
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem is string provider)
            {
                UpdateModelList(provider);

                // Update BaseUrl and placeholder for the selected provider
                SetDefaultBaseUrlForProvider(provider, onlyIfEmpty: false);
            }
        }

        private void SetDefaultBaseUrlForProvider(string provider, bool onlyIfEmpty)
        {
            var current = BaseUrlTextBox.Text?.Trim() ?? string.Empty;
            if (onlyIfEmpty && !string.IsNullOrEmpty(current))
            {
                return;
            }

            switch (provider)
            {
                case "OpenAI":
                    BaseUrlTextBox.Text = "https://api.openai.com";
                    BaseUrlTextBox.PlaceholderText = "https://api.openai.com";
                    break;
                case "Azure":
                    // Note: ModelName must be your Azure OpenAI deployment name
                    BaseUrlTextBox.Text = "https://<your-resource-name>.openai.azure.com";
                    BaseUrlTextBox.PlaceholderText = "https://{resource}.openai.azure.com";
                    break;
                case "Ollama":
                    BaseUrlTextBox.Text = "http://localhost:11434";
                    BaseUrlTextBox.PlaceholderText = "http://localhost:11434";
                    break;
                default:
                    BaseUrlTextBox.Text = current; // leave unchanged
                    break;
            }
        }

        private void UpdateModelList(string provider)
        {
            ModelComboBox.Items.Clear();
            
            if (_providerModels.TryGetValue(provider, out var models))
            {
                foreach (var model in models)
                {
                    ModelComboBox.Items.Add(model);
                }
                
                if (models.Count > 0)
                {
                    ModelComboBox.SelectedIndex = 0;
                }
            }
        }

        public AiConfiguration GetUpdatedConfiguration()
        {
            return new AiConfiguration
            {
                ProviderType = ProviderComboBox.SelectedItem?.ToString() ?? "OpenAI",
                ApiKey = ApiKeyPasswordBox.Password,
                BaseUrl = BaseUrlTextBox.Text,
                ModelName = ModelComboBox.SelectedItem?.ToString() ?? "gpt-3.5-turbo",
                Temperature = TemperatureSlider.Value,
                MaxTokens = (int)MaxTokensNumberBox.Value,
                TimeoutSeconds = (int)TimeoutNumberBox.Value
            };
        }
    }
}
