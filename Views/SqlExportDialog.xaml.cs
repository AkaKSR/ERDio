using System.Windows;
using System.Windows.Controls;
using ERDio.Models;

namespace ERDio
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL,
        Oracle,
        Tibero
    }

    public partial class SqlExportDialog : Window
    {
        private readonly List<Table> _tables;
        private readonly List<Relationship> _relationships;
        private readonly string _databaseName;

        public SqlExportDialog(List<Table> tables, List<Relationship> relationships, string databaseName)
        {
            InitializeComponent();
            _tables = tables;
            _relationships = relationships;
            _databaseName = databaseName;
            
            // Generate SQL with default selection (MySQL)
            GenerateAndDisplaySql();
        }

        private void OnDatabaseTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip if not fully initialized yet
            if (SqlTextBox == null) return;
            GenerateAndDisplaySql();
        }

        private void GenerateAndDisplaySql()
        {
            if (SqlTextBox == null || DatabaseTypeCombo == null) return;
            var dbType = GetSelectedDatabaseType();
            var sql = GenerateSql(dbType);
            SqlTextBox.Text = sql;
        }

        private DatabaseType GetSelectedDatabaseType()
        {
            if (DatabaseTypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                return Enum.Parse<DatabaseType>(tag);
            }
            return DatabaseType.MySQL;
        }

        private string GenerateSql(DatabaseType dbType)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"-- ERD: {_databaseName}");
            sb.AppendLine($"-- Database: {dbType}");
            sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Generate CREATE TABLE statements
            foreach (var table in _tables)
            {
                sb.AppendLine($"-- Table: {table.Name}");
                if (!string.IsNullOrWhiteSpace(table.Comment))
                {
                    sb.AppendLine($"-- Comment: {table.Comment}");
                }
                sb.AppendLine($"CREATE TABLE {FormatTableName(table.Name, dbType)} (");

                var columns = table.Columns.ToList();
                var primaryKeys = columns.Where(c => c.IsPrimaryKey).ToList();

                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var dataType = ConvertDataType(col.DataType, dbType);
                    var line = $"    {FormatColumnName(col.Name, dbType)} {dataType}";

                    if (!col.IsNullable)
                    {
                        line += " NOT NULL";
                    }

                    if (!string.IsNullOrWhiteSpace(col.DefaultValue))
                    {
                        line += $" DEFAULT {col.DefaultValue}";
                    }

                    // Add comma if not last item or if there are primary keys to add
                    if (i < columns.Count - 1 || primaryKeys.Count > 0)
                    {
                        line += ",";
                    }

                    if (!string.IsNullOrWhiteSpace(col.Comment))
                    {
                        line += $" -- {col.Comment}";
                    }

                    sb.AppendLine(line);
                }

                // Add primary key constraint
                if (primaryKeys.Count > 0)
                {
                    var pkColumns = string.Join(", ", primaryKeys.Select(c => FormatColumnName(c.Name, dbType)));
                    sb.AppendLine($"    CONSTRAINT PK_{table.Name} PRIMARY KEY ({pkColumns})");
                }

                sb.AppendLine(");");
                sb.AppendLine();

                // Add table comment for databases that support it
                if (!string.IsNullOrWhiteSpace(table.Comment))
                {
                    sb.AppendLine(GenerateTableComment(table.Name, table.Comment, dbType));
                }
            }

            // Generate foreign key constraints
            if (_relationships.Count > 0)
            {
                sb.AppendLine("-- Foreign Key Constraints");
                foreach (var rel in _relationships)
                {
                    var sourceTable = _tables.FirstOrDefault(t => t.Id == rel.SourceTableId);
                    var targetTable = _tables.FirstOrDefault(t => t.Id == rel.TargetTableId);

                    if (sourceTable != null && targetTable != null)
                    {
                        // FK is on the "many" side (target table) referencing the "one" side (source table)
                        // Source table has the PK, Target table has the FK column
                        var constraintName = $"FK_{targetTable.Name}_{sourceTable.Name}";
                        sb.AppendLine($"ALTER TABLE {FormatTableName(targetTable.Name, dbType)}");
                        sb.AppendLine($"    ADD CONSTRAINT {constraintName}");
                        sb.AppendLine($"    FOREIGN KEY ({FormatColumnName(rel.TargetColumnName, dbType)})");
                        sb.AppendLine($"    REFERENCES {FormatTableName(sourceTable.Name, dbType)}({FormatColumnName(rel.SourceColumnName, dbType)});");
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private string FormatTableName(string name, DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.MySQL => $"`{name}`",
                DatabaseType.PostgreSQL => $"\"{name}\"",
                DatabaseType.Oracle => name.ToUpper(),
                DatabaseType.Tibero => name.ToUpper(),
                _ => name
            };
        }

        private string FormatColumnName(string name, DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.MySQL => $"`{name}`",
                DatabaseType.PostgreSQL => $"\"{name}\"",
                DatabaseType.Oracle => name.ToUpper(),
                DatabaseType.Tibero => name.ToUpper(),
                _ => name
            };
        }

        private string ConvertDataType(string dataType, DatabaseType dbType)
        {
            var upperType = dataType.ToUpper();
            
            return dbType switch
            {
                DatabaseType.MySQL => ConvertToMySql(upperType),
                DatabaseType.PostgreSQL => ConvertToPostgreSql(upperType),
                DatabaseType.Oracle => ConvertToOracle(upperType),
                DatabaseType.Tibero => ConvertToTibero(upperType),
                _ => dataType
            };
        }

        private string ConvertToMySql(string dataType)
        {
            if (dataType.StartsWith("VARCHAR2")) return dataType.Replace("VARCHAR2", "VARCHAR");
            if (dataType == "NUMBER") return "INT";
            if (dataType.StartsWith("NUMBER(") && dataType.Contains(",")) return dataType.Replace("NUMBER", "DECIMAL");
            if (dataType.StartsWith("NUMBER(")) return dataType.Replace("NUMBER", "INT").Replace("INT(", "INT"); // Simple number to INT
            if (dataType == "CLOB") return "TEXT";
            if (dataType == "BLOB") return "BLOB";
            if (dataType == "DATE") return "DATETIME";
            return dataType;
        }

        private string ConvertToPostgreSql(string dataType)
        {
            if (dataType.StartsWith("VARCHAR2")) return dataType.Replace("VARCHAR2", "VARCHAR");
            if (dataType == "NUMBER") return "INTEGER";
            if (dataType.StartsWith("NUMBER(") && dataType.Contains(",")) return dataType.Replace("NUMBER", "NUMERIC");
            if (dataType.StartsWith("NUMBER(")) return "INTEGER";
            if (dataType == "CLOB") return "TEXT";
            if (dataType == "BLOB") return "BYTEA";
            if (dataType == "DATE") return "TIMESTAMP";
            return dataType;
        }

        private string ConvertToOracle(string dataType)
        {
            if (dataType.StartsWith("VARCHAR(")) return dataType.Replace("VARCHAR(", "VARCHAR2(");
            if (dataType == "INT" || dataType == "INTEGER") return "NUMBER";
            if (dataType == "TEXT") return "CLOB";
            if (dataType == "DATETIME" || dataType == "TIMESTAMP") return "DATE";
            if (dataType == "BYTEA") return "BLOB";
            return dataType;
        }

        private string ConvertToTibero(string dataType)
        {
            // Tibero is compatible with Oracle syntax
            return ConvertToOracle(dataType);
        }

        private string GenerateTableComment(string tableName, string comment, DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.MySQL => $"ALTER TABLE `{tableName}` COMMENT = '{EscapeString(comment)}';",
                DatabaseType.PostgreSQL => $"COMMENT ON TABLE \"{tableName}\" IS '{EscapeString(comment)}';",
                DatabaseType.Oracle => $"COMMENT ON TABLE {tableName.ToUpper()} IS '{EscapeString(comment)}';",
                DatabaseType.Tibero => $"COMMENT ON TABLE {tableName.ToUpper()} IS '{EscapeString(comment)}';",
                _ => ""
            };
        }

        private string EscapeString(string value)
        {
            return value.Replace("'", "''");
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SqlTextBox.Text))
            {
                Clipboard.SetText(SqlTextBox.Text);
                MessageBox.Show("SQL copied to clipboard!", "Copy", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}