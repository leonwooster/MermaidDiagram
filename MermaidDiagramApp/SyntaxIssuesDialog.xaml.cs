using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.ViewModels;

namespace MermaidDiagramApp
{
    public sealed partial class SyntaxIssuesDialog : ContentDialog
    {
        public SyntaxIssuesViewModel ViewModel { get; }

        public SyntaxIssuesDialog()
        {
            this.InitializeComponent();
            ViewModel = new SyntaxIssuesViewModel();
            this.DataContext = ViewModel;
        }

        public void LoadIssues(List<SyntaxIssue> issues, string originalCode)
        {
            ViewModel.LoadIssues(issues, originalCode);
            IssuesListView.ItemsSource = ViewModel.Issues;
            UpdateSummary();
            UpdateIssueTypeSummary();
        }

        private void UpdateSummary()
        {
            if (ViewModel.TotalIssuesCount == 0)
            {
                SummaryText.Text = "No syntax issues found. Your diagram is clean!";
                IsPrimaryButtonEnabled = false;
            }
            else
            {
                var errorText = ViewModel.ErrorCount > 0 ? $"{ViewModel.ErrorCount} error(s)" : "";
                var warningText = ViewModel.WarningCount > 0 ? $"{ViewModel.WarningCount} warning(s)" : "";
                var infoText = ViewModel.InfoCount > 0 ? $"{ViewModel.InfoCount} info" : "";

                var parts = new[] { errorText, warningText, infoText }.Where(s => !string.IsNullOrEmpty(s));
                var summary = string.Join(", ", parts);

                SummaryText.Text = $"Found {ViewModel.TotalIssuesCount} issue(s): {summary}. {ViewModel.SelectedIssuesCount} selected for fixing.";
                IsPrimaryButtonEnabled = ViewModel.SelectedIssuesCount > 0;
            }
        }

        private void UpdateIssueTypeSummary()
        {
            IssueTypeSummary.Children.Clear();

            var typeCounts = ViewModel.IssueTypeCounts;
            foreach (var (type, count) in typeCounts)
            {
                var button = new Button
                {
                    Content = $"{GetIssueTypeName(type)}: {count}"
                };
                button.Click += (s, e) => SelectIssuesByType(type);
                IssueTypeSummary.Children.Add(button);
            }
        }

        private string GetIssueTypeName(IssueType type)
        {
            return type switch
            {
                IssueType.UnicodeDash => "Unicode Dashes",
                IssueType.UnicodeArrow => "Unicode Arrows",
                IssueType.SmartQuote => "Smart Quotes",
                IssueType.LineBreak => "Line Breaks",
                _ => "Other"
            };
        }

        private void SelectIssuesByType(IssueType type)
        {
            ViewModel.SelectIssuesByType(type);
            UpdateSummary();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectAllIssues();
            UpdateSummary();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeselectAllIssues();
            UpdateSummary();
        }

        private void IssuesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IssuesListView.SelectedItem is SyntaxIssue issue)
            {
                UpdatePreview(issue);
            }
        }

        private void UpdatePreview(SyntaxIssue issue)
        {
            PreviewPanel.Children.Clear();

            // Issue details
            var detailsStack = new StackPanel { Spacing = 8 };
            
            detailsStack.Children.Add(new TextBlock
            {
                Text = "Issue Details",
                Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
            });

            detailsStack.Children.Add(new TextBlock
            {
                Text = issue.Description,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrEmpty(issue.UnicodeInfo))
            {
                detailsStack.Children.Add(new TextBlock
                {
                    Text = $"Character: {issue.UnicodeInfo}",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
                });
            }

            PreviewPanel.Children.Add(detailsStack);

            // Before/After comparison
            var comparisonStack = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };
            
            comparisonStack.Children.Add(new TextBlock
            {
                Text = "Before → After",
                Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style
            });

            var beforeAfterGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(40) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            // Before
            var beforeBorder = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 0, 0)),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };
            beforeBorder.Child = new TextBlock
            {
                Text = ViewModel.GetPreviewText(issue),
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(beforeBorder, 0);
            beforeAfterGrid.Children.Add(beforeBorder);

            // Arrow
            var arrowIcon = new FontIcon
            {
                Glyph = "\uE72A",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrowIcon, 1);
            beforeAfterGrid.Children.Add(arrowIcon);

            // After
            var afterBorder = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 0, 255, 0)),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Green),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };
            afterBorder.Child = new TextBlock
            {
                Text = ViewModel.GetFixedPreviewText(issue),
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(afterBorder, 2);
            beforeAfterGrid.Children.Add(afterBorder);

            comparisonStack.Children.Add(beforeAfterGrid);
            PreviewPanel.Children.Add(comparisonStack);

            // Replacement info
            var replacementStack = new StackPanel { Spacing = 4, Margin = new Thickness(0, 16, 0, 0) };
            replacementStack.Children.Add(new TextBlock
            {
                Text = "Replacement",
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            });
            replacementStack.Children.Add(new TextBlock
            {
                Text = $"\"{issue.OriginalText}\" → \"{issue.ReplacementText}\"",
                FontFamily = new FontFamily("Consolas")
            });
            PreviewPanel.Children.Add(replacementStack);
        }
    }
}
