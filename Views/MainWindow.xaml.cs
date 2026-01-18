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
    
    // Minimap drag state
    private bool _isMinimapDragging;
    private double _minimapScale;
    private double _minimapOffsetX;
    private double _minimapOffsetY;
    private double _minimapMinX;
    private double _minimapMinY;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set initial canvas size
        ErdCanvas.Width = 2000;
        ErdCanvas.Height = 2000;
        
        DrawGridPattern();
        LoadTables();
        DrawRelationships();
        ExpandCanvasIfNeeded();
        // Defer overlap resolution until after initial layout so ActualWidth/ActualHeight are available
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ResolveOverlapsUsingActualSizes();
            UpdateMinimap();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// After initial controls are measured, resolve any overlaps by nudging colliding tables downward/rightward.
    /// This iteratively moves tables until no collisions remain or a max iteration count is reached.
    /// </summary>
    private void ResolveOverlapsUsingActualSizes()
    {
        const double margin = 12;
        var controls = _tableControls.Values.ToList();

        bool changed;
        int iter = 0;
        const int maxIter = 1000;

        do
        {
            changed = false;

            for (int i = 0; i < controls.Count; i++)
            {
                var a = controls[i];
                if (a.TableData == null) continue;

                double ax = a.TableData.X;
                double ay = a.TableData.Y;
                double aw = a.ActualWidth > 0 ? a.ActualWidth : 320;
                double ah = a.ActualHeight > 0 ? a.ActualHeight : 100;

                for (int j = 0; j < controls.Count; j++)
                {
                    if (i == j) continue;
                    var b = controls[j];
                    if (b.TableData == null) continue;

                    double bx = b.TableData.X;
                    double by = b.TableData.Y;
                    double bw = b.ActualWidth > 0 ? b.ActualWidth : 320;
                    double bh = b.ActualHeight > 0 ? b.ActualHeight : 100;

                    bool overlapsX = ax < bx + bw + margin && ax + aw + margin > bx;
                    bool overlapsY = ay < by + bh + margin && ay + ah + margin > by;

                    if (overlapsX && overlapsY)
                    {
                        // Move the lower-indexed (earlier) control upward and the later one downward to resolve
                        // Prefer nudging the one further down already, otherwise push 'b' down
                        double newBy = ay + ah + margin;
                        if (newBy <= by)
                        {
                            // push b down
                            b.TableData.Y = newBy;
                            Canvas.SetTop(b, b.TableData.Y);
                            changed = true;
                        }
                        else
                        {
                            // fallback: push b further down
                            b.TableData.Y = newBy;
                            Canvas.SetTop(b, b.TableData.Y);
                            changed = true;
                        }
                    }
                }
            }

            iter++;
        } while (changed && iter < maxIter);

        if (iter >= maxIter)
        {
            // safety: expand canvas to reduce further crowding if stuck
            ExpandCanvasIfNeeded();
        }

        // Refresh derived visuals
        UpdateRelationshipLines();
        ExpandCanvasIfNeeded();
    }

    private void DrawGridPattern()
    {
        double width = ErdCanvas.Width > 0 ? ErdCanvas.Width : 2000;
        double height = ErdCanvas.Height > 0 ? ErdCanvas.Height : 2000;
        DrawGridPattern((int)width, (int)height);
    }

    private void DrawGridPattern(int width, int height)
    {
        GridCanvas.Children.Clear();
        
        int gridSize = 20;
        var brush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        
        for (int x = 0; x < width; x += gridSize)
        {
            var line = new Line
            {
                X1 = x, Y1 = 0,
                X2 = x, Y2 = height,
                Stroke = brush,
                StrokeThickness = 0.5
            };
            GridCanvas.Children.Add(line);
        }

        for (int y = 0; y < height; y += gridSize)
        {
            var line = new Line
            {
                X1 = 0, Y1 = y,
                X2 = width, Y2 = y,
                Stroke = brush,
                StrokeThickness = 0.5
            };
            GridCanvas.Children.Add(line);
        }
    }

    private void ExpandCanvasIfNeeded()
    {
        const double padding = 200;
        const double expandStep = 500;
        
        double maxX = 0, maxY = 0;
        
        foreach (var table in ViewModel.Tables)
        {
            if (_tableControls.TryGetValue(table.Id, out var control))
            {
                double tableRight = table.X + (control.ActualWidth > 0 ? control.ActualWidth : 320);
                double tableBottom = table.Y + (control.ActualHeight > 0 ? control.ActualHeight : 100);
                
                maxX = Math.Max(maxX, tableRight);
                maxY = Math.Max(maxY, tableBottom);
            }
        }
        
        bool needsExpand = false;
        double newWidth = ErdCanvas.Width;
        double newHeight = ErdCanvas.Height;
        
        if (maxX + padding > ErdCanvas.Width)
        {
            newWidth = Math.Ceiling((maxX + padding) / expandStep) * expandStep;
            needsExpand = true;
        }
        
        if (maxY + padding > ErdCanvas.Height)
        {
            newHeight = Math.Ceiling((maxY + padding) / expandStep) * expandStep;
            needsExpand = true;
        }
        
        if (needsExpand)
        {
            ErdCanvas.Width = newWidth;
            ErdCanvas.Height = newHeight;
            DrawGridPattern((int)newWidth, (int)newHeight);
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
            ValidateTableName = (name, excludeId) => !ViewModel.IsTableNameDuplicate(name, excludeId),
            CheckCollision = CheckTableCollision
        };
        
        tableControl.TableMoved += OnTableMoved;
        tableControl.TableDeleted += OnTableDeleted;
        tableControl.TableChanged += OnTableChanged;
        
        Canvas.SetLeft(tableControl, table.X);
        Canvas.SetTop(tableControl, table.Y);
        
        ErdCanvas.Children.Add(tableControl);
        _tableControls[table.Id] = tableControl;
    }

    private bool CheckTableCollision(string tableId, double x, double y, double width, double height)
    {
        const double margin = 10; // Minimum gap between tables
        
        foreach (var kvp in _tableControls)
        {
            if (kvp.Key == tableId) continue; // Skip self
            
            var otherTable = kvp.Value.TableData;
            if (otherTable == null) continue;
            
            double otherWidth = kvp.Value.ActualWidth > 0 ? kvp.Value.ActualWidth : 320;
            double otherHeight = kvp.Value.ActualHeight > 0 ? kvp.Value.ActualHeight : 100;
            
            // Check if rectangles overlap (with margin)
            bool overlapsX = x < otherTable.X + otherWidth + margin && x + width + margin > otherTable.X;
            bool overlapsY = y < otherTable.Y + otherHeight + margin && y + height + margin > otherTable.Y;
            
            if (overlapsX && overlapsY)
            {
                return true; // Collision detected
            }
        }
        
        return false; // No collision
    }

    private void OnTableMoved(object? sender, EventArgs e)
    {
        UpdateRelationshipLines();
        UpdateMinimap();
        ExpandCanvasIfNeeded();
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
        for (int i = 0; i < _relationshipLines.Count; i++)
        {
            var line = _relationshipLines[i];
            if (line.Relationship != null)
            {
                UpdateRelationshipLinePoints(line, line.Relationship);
            }
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
