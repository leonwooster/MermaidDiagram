using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using MermaidDiagramApp.Models;
using MermaidDiagramApp.Models.Canvas;

namespace MermaidDiagramApp.ViewModels
{
    /// <summary>
    /// ViewModel for the shape toolbox panel
    /// </summary>
    public class ShapeToolboxViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private string _selectedCategory;

        public ObservableCollection<ShapeCategory> Categories { get; }
        public ObservableCollection<ShapeTemplate> FilteredShapes { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterShapes();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterShapes();
                }
            }
        }

        public ShapeToolboxViewModel()
        {
            Categories = new ObservableCollection<ShapeCategory>();
            FilteredShapes = new ObservableCollection<ShapeTemplate>();
            _searchText = string.Empty;
            _selectedCategory = "All";

            InitializeShapeLibrary();
            FilterShapes();
        }

        private void InitializeShapeLibrary()
        {
            // General Shapes
            var generalCategory = new ShapeCategory { Name = "General", IsExpanded = true };
            generalCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "general-rectangle",
                Name = "Rectangle",
                Description = "Basic rectangle shape",
                Category = "General",
                ShapeType = NodeShape.Rectangle,
                DefaultSize = new Size(120, 60),
                DefaultText = "Rectangle",
                IconGlyph = "\uE91B",
                MermaidSyntaxPattern = "[{text}]"
            });
            generalCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "general-rounded",
                Name = "Rounded Rectangle",
                Description = "Rectangle with rounded corners",
                Category = "General",
                ShapeType = NodeShape.RoundEdges,
                DefaultSize = new Size(120, 60),
                DefaultText = "Rounded",
                IconGlyph = "\uE91C",
                MermaidSyntaxPattern = "({text})"
            });
            generalCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "general-circle",
                Name = "Circle",
                Description = "Circular shape",
                Category = "General",
                ShapeType = NodeShape.Circle,
                DefaultSize = new Size(80, 80),
                DefaultText = "Circle",
                IconGlyph = "\uEA3A",
                MermaidSyntaxPattern = "(({text}))"
            });
            generalCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "general-diamond",
                Name = "Diamond",
                Description = "Diamond/rhombus shape",
                Category = "General",
                ShapeType = NodeShape.Rhombus,
                DefaultSize = new Size(100, 100),
                DefaultText = "Diamond",
                IconGlyph = "\uE9CE",
                MermaidSyntaxPattern = "{{{text}}}"
            });
            Categories.Add(generalCategory);

            // Flowchart Shapes
            var flowchartCategory = new ShapeCategory { Name = "Flowchart", IsExpanded = true };
            flowchartCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "flowchart-start",
                Name = "Start/End",
                Description = "Flowchart start or end point",
                Category = "Flowchart",
                ShapeType = NodeShape.Stadium,
                DefaultSize = new Size(120, 60),
                DefaultText = "Start",
                IconGlyph = "\uE768",
                MermaidSyntaxPattern = "([{text}])"
            });
            flowchartCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "flowchart-process",
                Name = "Process",
                Description = "Process or action step",
                Category = "Flowchart",
                ShapeType = NodeShape.Rectangle,
                DefaultSize = new Size(120, 60),
                DefaultText = "Process",
                IconGlyph = "\uE91B",
                MermaidSyntaxPattern = "[{text}]"
            });
            flowchartCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "flowchart-decision",
                Name = "Decision",
                Description = "Decision point",
                Category = "Flowchart",
                ShapeType = NodeShape.Rhombus,
                DefaultSize = new Size(100, 100),
                DefaultText = "Decision?",
                IconGlyph = "\uE9CE",
                MermaidSyntaxPattern = "{{{text}}}"
            });
            flowchartCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "flowchart-data",
                Name = "Data",
                Description = "Data input/output",
                Category = "Flowchart",
                ShapeType = NodeShape.Parallelogram,
                DefaultSize = new Size(120, 60),
                DefaultText = "Data",
                IconGlyph = "\uE8B7",
                MermaidSyntaxPattern = "[/{text}/]"
            });
            flowchartCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "flowchart-subroutine",
                Name = "Subroutine",
                Description = "Predefined process",
                Category = "Flowchart",
                ShapeType = NodeShape.Subroutine,
                DefaultSize = new Size(120, 60),
                DefaultText = "Subroutine",
                IconGlyph = "\uE8B5",
                MermaidSyntaxPattern = "[[{text}]]"
            });
            Categories.Add(flowchartCategory);

            // UML Shapes
            var umlCategory = new ShapeCategory { Name = "UML", IsExpanded = false };
            umlCategory.Shapes.Add(new ShapeTemplate
            {
                Id = "uml-class",
                Name = "Class",
                Description = "UML class",
                Category = "UML",
                ShapeType = NodeShape.Rectangle,
                DefaultSize = new Size(140, 80),
                DefaultText = "ClassName",
                IconGlyph = "\uE8B5",
                MermaidSyntaxPattern = "[{text}]"
            });
            Categories.Add(umlCategory);
        }

        private void FilterShapes()
        {
            FilteredShapes.Clear();

            var allShapes = Categories.SelectMany(c => c.Shapes);

            // Filter by category
            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
            {
                allShapes = allShapes.Where(s => s.Category == SelectedCategory);
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                allShapes = allShapes.Where(s =>
                    s.Name.ToLower().Contains(searchLower) ||
                    s.Description.ToLower().Contains(searchLower));
            }

            foreach (var shape in allShapes)
            {
                FilteredShapes.Add(shape);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class ShapeCategory : INotifyPropertyChanged
    {
        private string _name;
        private bool _isExpanded;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public ObservableCollection<ShapeTemplate> Shapes { get; }

        public ShapeCategory()
        {
            _name = string.Empty;
            _isExpanded = true;
            Shapes = new ObservableCollection<ShapeTemplate>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
