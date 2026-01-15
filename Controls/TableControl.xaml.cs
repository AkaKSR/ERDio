using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERDio.Models;

namespace ERDio.Controls
{
    public partial class TableControl : UserControl
    {
        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _tableStartPosition;
        private DateTime _lastClickTime;
        private TextBlock? _lastClickedTextBlock;
        private TextBox? _currentEditBox;
        private string _originalTableName = string.Empty;

        public event EventHandler? TableMoved;
        public event EventHandler? TableDeleted;
        public event EventHandler? TableChanged;
        
        /// <summary>
        /// Func to validate table name. Returns true if the name is valid (not duplicate).
        /// </summary>
        public Func<string, string, bool>? ValidateTableName { get; set; }

        public Table? TableData => DataContext as Table;

        public TableControl()
        {
            InitializeComponent();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TableData == null) return;
            
            // End any active edit when clicking elsewhere on the table
            EndCurrentEdit();
            
            _isDragging = true;
            _dragStartPoint = e.GetPosition(Parent as IInputElement);
            _tableStartPosition = new Point(TableData.X, TableData.Y);
            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || TableData == null) return;

            var currentPoint = e.GetPosition(Parent as IInputElement);
            var offset = currentPoint - _dragStartPoint;
            
            TableData.X = _tableStartPosition.X + offset.X;
            TableData.Y = _tableStartPosition.Y + offset.Y;
            
            Canvas.SetLeft(this, TableData.X);
            Canvas.SetTop(this, TableData.Y);
            
            TableMoved?.Invoke(this, EventArgs.Empty);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            EndCurrentEdit();
        }

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging from header
        }

        private void OnTableNameMouseDown(object sender, MouseButtonEventArgs e)
        {
            var now = DateTime.Now;
            if (_lastClickedTextBlock == TableNameText && (now - _lastClickTime).TotalMilliseconds < 500)
            {
                // Double click - edit table name
                _originalTableName = TableData?.Name ?? string.Empty;
                TableNameText.Visibility = Visibility.Collapsed;
                TableNameEdit.Visibility = Visibility.Visible;
                TableNameEdit.Focus();
                TableNameEdit.SelectAll();
                e.Handled = true;
            }
            else
            {
                _lastClickedTextBlock = TableNameText;
                _lastClickTime = now;
            }
        }

        private void OnTableNameEditLostFocus(object sender, RoutedEventArgs e)
        {
            CommitTableNameEdit();
        }

        private void OnTableNameEditKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitTableNameEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel edit, restore original name
                if (TableData != null)
                {
                    TableData.Name = _originalTableName;
                }
                TableNameEdit.Visibility = Visibility.Collapsed;
                TableNameText.Visibility = Visibility.Visible;
                e.Handled = true;
            }
        }

        private void CommitTableNameEdit()
        {
            if (TableData == null)
            {
                TableNameEdit.Visibility = Visibility.Collapsed;
                TableNameText.Visibility = Visibility.Visible;
                return;
            }

            string newName = TableNameEdit.Text.Trim();
            
            // Check if name is empty
            if (string.IsNullOrWhiteSpace(newName))
            {
                TableData.Name = _originalTableName;
                TableNameEdit.Visibility = Visibility.Collapsed;
                TableNameText.Visibility = Visibility.Visible;
                MessageBox.Show("Table name cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for duplicate name using the validation function
            if (ValidateTableName != null && !ValidateTableName(newName, TableData.Id))
            {
                TableData.Name = _originalTableName;
                TableNameEdit.Visibility = Visibility.Collapsed;
                TableNameText.Visibility = Visibility.Visible;
                MessageBox.Show("A table with this name already exists.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TableNameEdit.Visibility = Visibility.Collapsed;
            TableNameText.Visibility = Visibility.Visible;
            TableChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnColumnNameMouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleDoubleClickEdit(sender as TextBlock, "NameEdit");
            e.Handled = true;
        }

        private void OnDataTypeMouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleDoubleClickEdit(sender as TextBlock, "DataTypeEdit");
            e.Handled = true;
        }

        private void OnDefaultMouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleDoubleClickEdit(sender as TextBlock, "DefaultEdit");
            e.Handled = true;
        }

        private void OnCommentMouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleDoubleClickEdit(sender as TextBlock, "CommentEdit");
            e.Handled = true;
        }

        private void HandleDoubleClickEdit(TextBlock? textBlock, string editBoxName)
        {
            if (textBlock == null) return;

            var now = DateTime.Now;
            if (_lastClickedTextBlock == textBlock && (now - _lastClickTime).TotalMilliseconds < 500)
            {
                // End any previous edit first
                EndCurrentEdit();
                
                // Double click detected - switch to edit mode
                var parent = textBlock.Parent as Grid;
                if (parent != null)
                {
                    var editBox = parent.Children.OfType<TextBox>().FirstOrDefault();
                    if (editBox != null)
                    {
                        textBlock.Visibility = Visibility.Collapsed;
                        editBox.Visibility = Visibility.Visible;
                        editBox.Focus();
                        editBox.SelectAll();
                        _currentEditBox = editBox;
                    }
                }
                _lastClickedTextBlock = null;
            }
            else
            {
                _lastClickedTextBlock = textBlock;
                _lastClickTime = now;
            }
        }

        private void OnEditLostFocus(object sender, RoutedEventArgs e)
        {
            EndEdit(sender as TextBox);
            TableChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                EndEdit(sender as TextBox);
                TableChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void EndCurrentEdit()
        {
            if (_currentEditBox != null)
            {
                EndEdit(_currentEditBox);
                _currentEditBox = null;
            }
        }

        private void EndEdit(TextBox? editBox)
        {
            if (editBox == null) return;

            var parent = editBox.Parent as Grid;
            if (parent != null)
            {
                var textBlock = parent.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    editBox.Visibility = Visibility.Collapsed;
                    textBlock.Visibility = Visibility.Visible;
                }
            }
            
            if (_currentEditBox == editBox)
            {
                _currentEditBox = null;
            }
        }

        // Context menu handlers
        private void OnAddColumn(object sender, RoutedEventArgs e)
        {
            AddNewColumn();
        }

        private void OnAddColumnClick(object sender, RoutedEventArgs e)
        {
            AddNewColumn();
            e.Handled = true;
        }

        private void AddNewColumn()
        {
            if (TableData == null) return;
            var newColumn = new Column
            {
                Name = "NEW_COLUMN",
                DataType = "VARCHAR2(100)",
                IsNullable = true
            };
            TableData.Columns.Add(newColumn);
            TableChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditTable(object sender, RoutedEventArgs e)
        {
            // Start editing table name
            TableNameText.Visibility = Visibility.Collapsed;
            TableNameEdit.Visibility = Visibility.Visible;
            TableNameEdit.Focus();
            TableNameEdit.SelectAll();
        }

        private void OnDeleteTable(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Delete table '{TableData?.Name}'?", "Confirm Delete", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TableDeleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnChangeColorRed(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(220, 60, 60));
        private void OnChangeColorBlue(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(80, 80, 180));
        private void OnChangeColorGreen(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(60, 179, 113));
        private void OnChangeColorPurple(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(200, 100, 200));
        private void OnChangeColorOrange(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(255, 140, 0));
        private void OnChangeColorTeal(object sender, RoutedEventArgs e) => ChangeTableColor(Color.FromRgb(32, 178, 170));

        private void ChangeTableColor(Color color)
        {
            if (TableData != null)
            {
                TableData.HeaderColor = color;
                TableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Column context menu handlers
        private void OnColumnRightClick(object sender, MouseButtonEventArgs e)
        {
            // Context menu will be shown automatically
        }

        private void OnNullableClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Column column)
            {
                column.IsNullable = !column.IsNullable;
                TableChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void OnTogglePrimaryKey(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column)
            {
                column.IsPrimaryKey = !column.IsPrimaryKey;
                if (column.IsPrimaryKey) column.IsForeignKey = false;
                TableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnToggleForeignKey(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column)
            {
                column.IsForeignKey = !column.IsForeignKey;
                if (column.IsForeignKey) column.IsPrimaryKey = false;
                TableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnToggleNullable(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column)
            {
                column.IsNullable = !column.IsNullable;
                TableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnMoveColumnUp(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column && TableData != null)
            {
                int index = TableData.Columns.IndexOf(column);
                if (index > 0)
                {
                    TableData.Columns.Move(index, index - 1);
                    TableChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void OnMoveColumnDown(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column && TableData != null)
            {
                int index = TableData.Columns.IndexOf(column);
                if (index < TableData.Columns.Count - 1)
                {
                    TableData.Columns.Move(index, index + 1);
                    TableChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void OnDeleteColumn(object sender, RoutedEventArgs e)
        {
            if (GetColumnFromMenuItem(sender) is Column column && TableData != null)
            {
                TableData.Columns.Remove(column);
                TableChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private Column? GetColumnFromMenuItem(object sender)
        {
            if (sender is MenuItem menuItem)
            {
                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu?.PlacementTarget is Border border)
                {
                    return border.Tag as Column;
                }
            }
            return null;
        }
    }
}
