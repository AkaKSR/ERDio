using System.Windows;
using System.Windows.Controls;
using ERDio.Models;

namespace ERDio
{
    public partial class RelationshipDialog : Window
    {
        private readonly List<Table> _tables;
        public Relationship? Result { get; private set; }

        public RelationshipDialog(List<Table> tables)
        {
            InitializeComponent();
            _tables = tables;
            
            SourceTableCombo.ItemsSource = tables;
            TargetTableCombo.ItemsSource = tables;
            
            if (tables.Count > 0)
            {
                SourceTableCombo.SelectedIndex = 0;
                if (tables.Count > 1)
                    TargetTableCombo.SelectedIndex = 1;
                else
                    TargetTableCombo.SelectedIndex = 0;
            }
        }

        private void OnSourceTableChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceTableCombo.SelectedItem is Table table)
            {
                SourceColumnCombo.ItemsSource = table.Columns;
                if (table.Columns.Count > 0)
                    SourceColumnCombo.SelectedIndex = 0;
            }
        }

        private void OnTargetTableChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetTableCombo.SelectedItem is Table table)
            {
                TargetColumnCombo.ItemsSource = table.Columns;
                if (table.Columns.Count > 0)
                    TargetColumnCombo.SelectedIndex = 0;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            if (SourceTableCombo.SelectedItem is not Table sourceTable ||
                TargetTableCombo.SelectedItem is not Table targetTable ||
                SourceColumnCombo.SelectedItem is not Column sourceColumn ||
                TargetColumnCombo.SelectedItem is not Column targetColumn)
            {
                MessageBox.Show("Please select all fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var relationType = RelationType.OneToMany;
            if (RelationTypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                relationType = Enum.Parse<RelationType>(tag);
            }

            Result = new Relationship
            {
                SourceTableId = sourceTable.Id,
                TargetTableId = targetTable.Id,
                SourceColumnName = sourceColumn.Name,
                TargetColumnName = targetColumn.Name,
                RelationType = relationType
            };

            DialogResult = true;
            Close();
        }
    }
}
