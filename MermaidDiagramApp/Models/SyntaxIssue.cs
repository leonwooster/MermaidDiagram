using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MermaidDiagramApp.Models
{
    public enum IssueSeverity
    {
        Error,
        Warning,
        Info
    }

    public enum IssueType
    {
        UnicodeDash,
        UnicodeArrow,
        SmartQuote,
        LineBreak,
        Other
    }

    public class SyntaxIssue : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public int LineNumber { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Description { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public IssueType Type { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string ReplacementText { get; set; } = string.Empty;
        public string UnicodeInfo { get; set; } = string.Empty;
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public Func<string, string> ProposeFix { get; set; } = code => code;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
