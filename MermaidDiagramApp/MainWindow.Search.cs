// MainWindow.Search.cs
// Partial class for MainWindow containing search panel UI wiring,
// CodeEditor search integration, and search-related event handlers.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MermaidDiagramApp
{
    public sealed partial class MainWindow
    {
        #region Search Functionality

        private string _currentSearchText = string.Empty;

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Visibility = Visibility.Visible;
            SearchTextBox.Focus(FocusState.Programmatic);
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            SearchResultsText.Text = string.Empty;
            _currentSearchText = string.Empty;
            
            // Safely end search if it's open
            try
            {
                if (CodeEditor.SearchIsOpen)
                {
                    CodeEditor.EndSearch();
                }
            }
            catch
            {
                // Ignore errors when closing search
            }
            
            CodeEditor.Focus(FocusState.Programmatic);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Just clear the results text - don't call EndSearch here to avoid crashes
            SearchResultsText.Text = string.Empty;
        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    FindPrevious_Click(sender, new RoutedEventArgs());
                }
                else
                {
                    FindNext_Click(sender, new RoutedEventArgs());
                }
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                CloseSearch_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch(forward: true);
        }

        private void FindPrevious_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch(forward: false);
        }

        private void PerformSearch(bool forward)
        {
            var searchText = SearchTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                SearchResultsText.Text = string.Empty;
                return;
            }

            try
            {
                // If search text changed, restart the search
                if (searchText != _currentSearchText)
                {
                    // End previous search if it exists
                    if (CodeEditor.SearchIsOpen)
                    {
                        CodeEditor.EndSearch();
                    }
                    
                    // Start new search with the new text
                    CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
                    _currentSearchText = searchText;
                }
                // If search not open (first search or after close), start it
                else if (!CodeEditor.SearchIsOpen)
                {
                    CodeEditor.BeginSearch(searchText, wholeWord: false, matchCase: false);
                    _currentSearchText = searchText;
                }

                // Navigate to next or previous match
                if (forward)
                {
                    CodeEditor.FindNext();
                }
                else
                {
                    CodeEditor.FindPrevious();
                }
                
                SearchResultsText.Text = string.Empty;
            }
            catch (Exception ex)
            {
                SearchResultsText.Text = "Search error";
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
            
            CodeEditor.Focus(FocusState.Programmatic);
        }

        #endregion
    }
}
