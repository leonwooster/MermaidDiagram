using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MermaidDiagramApp.Models;

namespace MermaidDiagramApp.ViewModels
{
    public class SyntaxIssuesViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SyntaxIssue> _issues = new();
        private SyntaxIssue? _selectedIssue;
        private string _originalCode = string.Empty;
        private bool _selectAll = true;

        public ObservableCollection<SyntaxIssue> Issues
        {
            get => _issues;
            set
            {
                _issues = value;
                OnPropertyChanged();
                UpdateCounts();
            }
        }

        public SyntaxIssue? SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                _selectedIssue = value;
                OnPropertyChanged();
            }
        }

        public string OriginalCode
        {
            get => _originalCode;
            set
            {
                _originalCode = value;
                OnPropertyChanged();
            }
        }

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                _selectAll = value;
                OnPropertyChanged();
                UpdateAllSelections(value);
            }
        }

        public int TotalIssuesCount => Issues.Count;
        public int SelectedIssuesCount => Issues.Count(i => i.IsSelected);
        public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
        public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
        public int InfoCount => Issues.Count(i => i.Severity == IssueSeverity.Info);

        public Dictionary<IssueType, int> IssueTypeCounts
        {
            get
            {
                return Issues.GroupBy(i => i.Type)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadIssues(List<SyntaxIssue> issues, string originalCode)
        {
            OriginalCode = originalCode;
            Issues = new ObservableCollection<SyntaxIssue>(issues);
            
            // Subscribe to property changes on each issue
            foreach (var issue in Issues)
            {
                // We need to track when IsSelected changes
                var propertyChangedHandler = new PropertyChangedEventHandler((s, e) =>
                {
                    if (e.PropertyName == nameof(SyntaxIssue.IsSelected))
                    {
                        UpdateCounts();
                    }
                });
            }
            
            UpdateCounts();
        }

        public void SelectAllIssues()
        {
            UpdateAllSelections(true);
        }

        public void DeselectAllIssues()
        {
            UpdateAllSelections(false);
        }

        public void SelectIssuesByType(IssueType type)
        {
            foreach (var issue in Issues.Where(i => i.Type == type))
            {
                issue.IsSelected = true;
            }
            UpdateCounts();
        }

        private void UpdateAllSelections(bool selected)
        {
            foreach (var issue in Issues)
            {
                issue.IsSelected = selected;
            }
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(TotalIssuesCount));
            OnPropertyChanged(nameof(SelectedIssuesCount));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(InfoCount));
            OnPropertyChanged(nameof(IssueTypeCounts));
        }

        public string GetPreviewText(SyntaxIssue issue)
        {
            if (string.IsNullOrEmpty(OriginalCode))
                return string.Empty;

            var lines = OriginalCode.Split('\n');
            if (issue.LineNumber < 1 || issue.LineNumber > lines.Length)
                return string.Empty;

            var line = lines[issue.LineNumber - 1];
            
            // Show context: the line with the issue highlighted
            return line;
        }

        public string GetFixedPreviewText(SyntaxIssue issue)
        {
            if (string.IsNullOrEmpty(OriginalCode))
                return string.Empty;

            var lines = OriginalCode.Split('\n');
            if (issue.LineNumber < 1 || issue.LineNumber > lines.Length)
                return string.Empty;

            var line = lines[issue.LineNumber - 1];
            
            // Replace the issue in the line
            if (issue.Column > 0 && issue.Column <= line.Length + 1)
            {
                var startIndex = issue.Column - 1;
                if (startIndex + issue.OriginalText.Length <= line.Length)
                {
                    var before = line.Substring(0, startIndex);
                    var after = line.Substring(startIndex + issue.OriginalText.Length);
                    return before + issue.ReplacementText + after;
                }
            }

            return line;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
