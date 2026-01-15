using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ERDio.Controls;
using ERDio.Models;
using ERDio.ViewModels;
using Microsoft.Win32;

namespace ERDio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private readonly Dictionary<string, TableControl> _tableControls = new();
    private readonly List<RelationshipLine> _relationshipLines = new();
    
    // Panning state
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DrawGridPattern();
        LoadTables();
        DrawRelationships();
        UpdateMinimap();
    }

    private void DrawGridPattern()
    {
        int gridSize = 20;
        var brush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        
        for (int x = 0; x < 2000; x += gridSize)
        {
            var line = new Line
            {
                X1 = x, Y1 = 0,
                X2 = x, Y2 = 2000,
                Stroke = brush,
                StrokeThickness = 0.5
            };
            GridCanvas.Children.Add(line);
        }

        for (int y = 0; y < 2000; y += gridSize)
        {
            var line = new Line
            {
                X1 = 0, Y1 = y,
                X2 = 2000, Y2 = y,
                Stroke = brush,
                StrokeThickness = 0.5
            };
            GridCanvas.Children.Add(line);
        }
    }

    private void LoadTables()
    {
        foreach (var table in ViewModel.Tables)
        {
            AddTableControl(table);
        }
    }

    private void AddTableControl(Table table)
    {
        var tableControl = new TableControl
        {
            DataContext = table,
            ValidateTableName = (name, excludeId) => !ViewModel.IsTableNameDuplicate(name, excludeId)
        };
        
        tableControl.TableMoved += OnTableMoved;
        tableControl.TableDeleted += OnTableDeleted;
        tableControl.TableChanged += OnTableChanged;
        
        Canvas.SetLeft(tableControl, table.X);
        Canvas.SetTop(tableControl, table.Y);
        
        ErdCanvas.Children.Add(tableControl);
        _tableControls[table.Id] = tableControl;
    }

    private void OnTableMoved(object? sender, EventArgs e)
    {
        UpdateRelationshipLines();
        UpdateMinimap();
    }

    private void OnTableDeleted(object? sender, EventArgs e)
    {
        if (sender is TableControl tableControl && tableControl.TableData != null)
        {
            var table = tableControl.TableData;
            
            // Remove related relationship lines
            var relatedRelationships = ViewModel.Relationships
                .Where(r => r.SourceTableId == table.Id || r.TargetTableId == table.Id)
                .ToList();
            
            foreach (var rel in relatedRelationships)
            {
                var line = _relationshipLines.FirstOrDefault(l => l.Relationship == rel);
                if (line != null)
                {
                    ErdCanvas.Children.Remove(line);
                    _relationshipLines.Remove(line);
                }
                ViewModel.Relationships.Remove(rel);
            }
            
            // Remove table control
            ErdCanvas.Children.Remove(tableControl);
            _tableControls.Remove(table.Id);
            ViewModel.RemoveTable(table);
            
            UpdateMinimap();
        }
    }

    private void OnTableChanged(object? sender, EventArgs e)
    {
        UpdateMinimap();
    }

    private void DrawRelationships()
    {
        foreach (var relationship in ViewModel.Relationships)
        {
            AddRelationshipLine(relationship);
        }
    }

    private void AddRelationshipLine(Relationship relationship)
    {
        var line = new RelationshipLine
        {
            Relationship = relationship
        };
        
        line.RelationshipDeleted += OnRelationshipDeleted;
        UpdateRelationshipLinePoints(line, relationship);
        
        ErdCanvas.Children.Insert(0, line);
        _relationshipLines.Add(line);
    }

    private void OnRelationshipDeleted(object? sender, EventArgs e)
    {
        if (sender is RelationshipLine line && line.Relationship != null)
        {
            ErdCanvas.Children.Remove(line);
            _relationshipLines.Remove(line);
            ViewModel.Relationships.Remove(line.Relationship);
        }
    }

    private void UpdateRelationshipLines()
    {
        for (int i = 0; i < _relationshipLines.Count; i++)
        {
            var line = _relationshipLines[i];
            if (line.Relationship != null)
            {
                UpdateRelationshipLinePoints(line, line.Relationship);
            }
        }
    }

    private void UpdateRelationshipLinePoints(RelationshipLine line, Relationship relationship)
    {
        var sourceTable = ViewModel.GetTableById(relationship.SourceTableId);
        var targetTable = ViewModel.GetTableById(relationship.TargetTableId);

        if (sourceTable == null || targetTable == null) return;

        if (!_tableControls.TryGetValue(sourceTable.Id, out var sourceControl)) return;
        if (!_tableControls.TryGetValue(targetTable.Id, out var targetControl)) return;

        // Default table size when ActualWidth/Height not yet available
        const double defaultWidth = 320;
        const double defaultHeight = 100;

        // Use ActualWidth/Height if available, otherwise use default size
        double sourceWidth = sourceControl.ActualWidth > 0 ? sourceControl.ActualWidth : defaultWidth;
        double sourceHeight = sourceControl.ActualHeight > 0 ? sourceControl.ActualHeight : defaultHeight;
        double targetWidth = targetControl.ActualWidth > 0 ? targetControl.ActualWidth : defaultWidth;
        double targetHeight = targetControl.ActualHeight > 0 ? targetControl.ActualHeight : defaultHeight;

        // Calculate connection points
        double sourceRight = sourceTable.X + sourceWidth;
        double sourceY = sourceTable.Y + sourceHeight / 2;
        
        double targetLeft = targetTable.X;
        double targetY = targetTable.Y + targetHeight / 2;

        // Determine best connection points
        if (sourceTable.X > targetTable.X + targetWidth)
        {
            // Source is to the right of target
            line.StartPoint = new Point(sourceTable.X, sourceY);
            line.EndPoint = new Point(targetTable.X + targetWidth, targetY);
        }
        else
        {
            // Source is to the left of target
            line.StartPoint = new Point(sourceRight, sourceY);
            line.EndPoint = new Point(targetLeft, targetY);
        }
    }

    private void UpdateMinimap()
    {
        MinimapCanvas.Children.Clear();
        
        double scale = 0.05;
        
        // Default table size for minimap when ActualWidth/Height not yet available
        const double defaultWidth = 320;
        const double defaultHeight = 100;
        
        foreach (var table in ViewModel.Tables)
        {
            if (!_tableControls.TryGetValue(table.Id, out var control)) continue;
            
            // Use ActualWidth/Height if available, otherwise use default size
            double width = control.ActualWidth > 0 ? control.ActualWidth : defaultWidth;
            double height = control.ActualHeight > 0 ? control.ActualHeight : defaultHeight;
            
            var rect = new Rectangle
            {
                Width = Math.Max(5, width * scale),
                Height = Math.Max(3, height * scale),
                Fill = table.HeaderBrush,
                Opacity = 0.7
            };
            
            Canvas.SetLeft(rect, table.X * scale);
            Canvas.SetTop(rect, table.Y * scale);
            
            MinimapCanvas.Children.Add(rect);
        }
    }

    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double zoomDelta = e.Delta > 0 ? 10 : -10;
        ViewModel.Zoom = Math.Clamp(ViewModel.Zoom + zoomDelta, 25, 200);
    }

    private void OnAddTable(object sender, RoutedEventArgs e)
    {
        // Generate unique table name
        string baseName = "NEW_TABLE";
        string tableName = baseName;
        int counter = 1;
        while (ViewModel.IsTableNameDuplicate(tableName))
        {
            tableName = $"{baseName}_{counter}";
            counter++;
        }

        var newTable = new Table
        {
            Name = tableName,
            Comment = "comment",
            X = 100,
            Y = 100,
            HeaderColor = Color.FromRgb(100, 149, 237)
        };
        
        newTable.Columns.Add(new Column { Name = "ID", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false });
        newTable.Columns.Add(new Column { Name = "NAME", DataType = "VARCHAR2(100)", IsNullable = true });
        
        ViewModel.AddTable(newTable);
        AddTableControl(newTable);
    }

    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // End any active editing in table controls
        EndAllTableEditing();
        
        // Only start panning if clicking directly on the canvas background
        if (e.OriginalSource == ErdCanvas || e.OriginalSource is Line)
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(CanvasScrollViewer);
            _panStartOffsetX = CanvasScrollViewer.HorizontalOffset;
            _panStartOffsetY = CanvasScrollViewer.VerticalOffset;
            ErdCanvas.CaptureMouse();
            ErdCanvas.Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }
    }

    private void EndAllTableEditing()
    {
        // Remove focus from any focused element to trigger LostFocus
        var focusedElement = Keyboard.FocusedElement as TextBox;
        if (focusedElement != null)
        {
            // Move focus to the canvas to end editing
            ErdCanvas.Focus();
        }
    }

    private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ErdCanvas.ReleaseMouseCapture();
            ErdCanvas.Cursor = Cursors.Arrow;
            e.Handled = true;
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            Point currentPoint = e.GetPosition(CanvasScrollViewer);
            double deltaX = currentPoint.X - _panStartPoint.X;
            double deltaY = currentPoint.Y - _panStartPoint.Y;
            
            CanvasScrollViewer.ScrollToHorizontalOffset(_panStartOffsetX - deltaX);
            CanvasScrollViewer.ScrollToVerticalOffset(_panStartOffsetY - deltaY);
            e.Handled = true;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "ERD Files (*.erd)|*.erd|JSON Files (*.json)|*.json",
            DefaultExt = ".erd",
            FileName = ViewModel.DatabaseName
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var data = new ErdFileData
                {
                    DatabaseName = ViewModel.DatabaseName,
                    Tables = ViewModel.Tables.Select(t => new TableData
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Comment = t.Comment,
                        X = t.X,
                        Y = t.Y,
                        HeaderColor = $"#{t.HeaderColor.R:X2}{t.HeaderColor.G:X2}{t.HeaderColor.B:X2}",
                        Columns = t.Columns.Select(c => new ColumnData
                        {
                            Name = c.Name,
                            DataType = c.DataType,
                            IsPrimaryKey = c.IsPrimaryKey,
                            IsForeignKey = c.IsForeignKey,
                            IsNullable = c.IsNullable,
                            DefaultValue = c.DefaultValue,
                            Comment = c.Comment
                        }).ToList()
                    }).ToList(),
                    Relationships = ViewModel.Relationships.Select(r => new RelationshipData
                    {
                        SourceTableId = r.SourceTableId,
                        TargetTableId = r.TargetTableId,
                        SourceColumnName = r.SourceColumnName,
                        TargetColumnName = r.TargetColumnName,
                        RelationType = r.RelationType.ToString()
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveDialog.FileName, json);
                MessageBox.Show("Saved successfully!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnLoad(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "ERD Files (*.erd)|*.erd|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".erd"
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(openDialog.FileName);
                var data = JsonSerializer.Deserialize<ErdFileData>(json);
                
                if (data == null) return;

                // Clear existing data
                foreach (var control in _tableControls.Values)
                {
                    ErdCanvas.Children.Remove(control);
                }
                _tableControls.Clear();
                
                foreach (var line in _relationshipLines)
                {
                    ErdCanvas.Children.Remove(line);
                }
                _relationshipLines.Clear();
                
                ViewModel.Tables.Clear();
                ViewModel.Relationships.Clear();

                // Load new data
                ViewModel.DatabaseName = data.DatabaseName;

                foreach (var tableData in data.Tables)
                {
                    var table = new Table
                    {
                        Id = tableData.Id,
                        Name = tableData.Name,
                        Comment = tableData.Comment,
                        X = tableData.X,
                        Y = tableData.Y,
                        HeaderColor = (Color)ColorConverter.ConvertFromString(tableData.HeaderColor)
                    };

                    foreach (var colData in tableData.Columns)
                    {
                        table.Columns.Add(new Column
                        {
                            Name = colData.Name,
                            DataType = colData.DataType,
                            IsPrimaryKey = colData.IsPrimaryKey,
                            IsForeignKey = colData.IsForeignKey,
                            IsNullable = colData.IsNullable,
                            DefaultValue = colData.DefaultValue,
                            Comment = colData.Comment
                        });
                    }

                    ViewModel.Tables.Add(table);
                    AddTableControl(table);
                }

                foreach (var relData in data.Relationships)
                {
                    var relationship = new Relationship
                    {
                        SourceTableId = relData.SourceTableId,
                        TargetTableId = relData.TargetTableId,
                        SourceColumnName = relData.SourceColumnName,
                        TargetColumnName = relData.TargetColumnName,
                        RelationType = Enum.Parse<RelationType>(relData.RelationType)
                    };
                    ViewModel.Relationships.Add(relationship);
                }

                DrawRelationships();
                UpdateMinimap();
                
                MessageBox.Show("Loaded successfully!", "Load", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnExportImage(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
            DefaultExt = ".png",
            FileName = $"{ViewModel.DatabaseName}_ERD"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                // Find the bounds of all tables
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = 0, maxY = 0;

                foreach (var table in ViewModel.Tables)
                {
                    if (_tableControls.TryGetValue(table.Id, out var control))
                    {
                        minX = Math.Min(minX, table.X);
                        minY = Math.Min(minY, table.Y);
                        maxX = Math.Max(maxX, table.X + control.ActualWidth);
                        maxY = Math.Max(maxY, table.Y + control.ActualHeight);
                    }
                }

                double width = maxX - minX + 100;
                double height = maxY - minY + 100;

                var renderBitmap = new RenderTargetBitmap(
                    (int)width, (int)height, 96, 96, PixelFormats.Pbgra32);

                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Draw background
                    context.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 30, 30)), null, 
                        new Rect(0, 0, width, height));

                    // Render the canvas
                    var brush = new VisualBrush(ErdCanvas)
                    {
                        Viewbox = new Rect(minX - 50, minY - 50, width, height),
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Stretch = Stretch.None
                    };
                    context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
                }
                
                renderBitmap.Render(visual);

                BitmapEncoder encoder;
                if (saveDialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    encoder = new JpegBitmapEncoder();
                }
                else
                {
                    encoder = new PngBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                
                using (var stream = File.Create(saveDialog.FileName))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show("Image exported successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnAddRelationship(object sender, RoutedEventArgs e)
    {
        // Show relationship dialog
        var dialog = new RelationshipDialog(ViewModel.Tables.ToList());
        dialog.Owner = this;
        
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            if (ViewModel.AddRelationship(dialog.Result))
            {
                AddRelationshipLine(dialog.Result);
            }
            else
            {
                MessageBox.Show("This relationship already exists.", "Duplicate Relationship", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void OnExportSql(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Tables.Count == 0)
        {
            MessageBox.Show("No tables to export.", "Export SQL", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SqlExportDialog(
            ViewModel.Tables.ToList(),
            ViewModel.Relationships.ToList(),
            ViewModel.DatabaseName);
        dialog.Owner = this;
        dialog.ShowDialog();
    }
}

// Data classes for serialization
public class ErdFileData
{
    public string DatabaseName { get; set; } = "";
    public List<TableData> Tables { get; set; } = new();
    public List<RelationshipData> Relationships { get; set; } = new();
}

public class TableData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Comment { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public string HeaderColor { get; set; } = "";
    public List<ColumnData> Columns { get; set; } = new();
}

public class ColumnData
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public bool IsNullable { get; set; }
    public string DefaultValue { get; set; } = "";
    public string Comment { get; set; } = "";
}

public class RelationshipData
{
    public string SourceTableId { get; set; } = "";
    public string TargetTableId { get; set; } = "";
    public string SourceColumnName { get; set; } = "";
    public string TargetColumnName { get; set; } = "";
    public string RelationType { get; set; } = "";
}