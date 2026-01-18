using System.Data;
using System.Data.Common;
using System.Windows.Media;
using ERDio.Models;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace ERDio.Services;

public class DatabaseConnectionInfo
{
    public string DbType { get; set; } = "MySQL";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Schema { get; set; } = "public";
}

public class ErdGenerationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Table> Tables { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
}

public class DatabaseService
{
    private const double TableWidth = 470;
    private const double BaseTableHeight = 45;  // Header + outer borders
    private const double RowHeight = 22;        // Row height with padding/border
    private const double TableMarginX = 50;     // Horizontal gap between tables
    private const double TableMarginY = 50;     // Vertical gap between tables
    private const int TablesPerRow = 3;
    
    private static readonly Color[] TableColors =
    {
        Color.FromRgb(220, 60, 60),    // Red
        Color.FromRgb(60, 180, 60),    // Green
        Color.FromRgb(80, 80, 180),    // Blue
        Color.FromRgb(200, 100, 200),  // Purple
        Color.FromRgb(255, 150, 50),   // Orange
        Color.FromRgb(100, 149, 237),  // Cornflower Blue
        Color.FromRgb(180, 120, 80),   // Brown
        Color.FromRgb(100, 180, 180),  // Teal
    };

    public ErdGenerationResult GenerateErdFromDatabase(DatabaseConnectionInfo connectionInfo)
    {
        try
        {
            return connectionInfo.DbType switch
            {
                "MySQL" => GenerateFromMySql(connectionInfo),
                "PostgreSQL" => GenerateFromPostgreSql(connectionInfo),
                "Oracle" => GenerateFromOracle(connectionInfo),
                "Tibero" => GenerateFromTibero(connectionInfo),
                _ => new ErdGenerationResult { IsSuccess = false, ErrorMessage = $"Unsupported database type: {connectionInfo.DbType}" }
            };
        }
        catch (Exception ex)
        {
            return new ErdGenerationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private ErdGenerationResult GenerateFromMySql(DatabaseConnectionInfo info)
    {
        var connectionString = $"Server={info.Host};Port={info.Port};Database={info.Database};User Id={info.UserId};Password={info.Password};Charset=utf8mb4;";
        
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        var tables = new List<Table>();
        var relationships = new List<Relationship>();
        var tableDict = new Dictionary<string, Table>();

        // Get tables
        var tableQuery = @"
            SELECT TABLE_NAME, TABLE_COMMENT 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'";

        using (var cmd = new MySqlCommand(tableQuery, connection))
        {
            cmd.Parameters.AddWithValue("@schema", info.Database);
            using var reader = cmd.ExecuteReader();
            int colorIndex = 0;

            while (reader.Read())
            {
                var table = new Table
                {
                    Name = reader.GetString("TABLE_NAME"),
                    Comment = reader.IsDBNull("TABLE_COMMENT") ? "" : reader.GetString("TABLE_COMMENT"),
                    X = 0,
                    Y = 0,
                    HeaderColor = TableColors[colorIndex % TableColors.Length]
                };
                tables.Add(table);
                tableDict[table.Name] = table;
                colorIndex++;
            }
        }

        // Get columns for each table
        foreach (var table in tables)
        {
            var columnQuery = @"
                SELECT COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY, COLUMN_DEFAULT, COLUMN_COMMENT
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                ORDER BY ORDINAL_POSITION";

            using var cmd = new MySqlCommand(columnQuery, connection);
            cmd.Parameters.AddWithValue("@schema", info.Database);
            cmd.Parameters.AddWithValue("@table", table.Name);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var column = new Column
                {
                    Name = reader.GetString("COLUMN_NAME"),
                    DataType = reader.GetString("COLUMN_TYPE").ToUpper(),
                    IsPrimaryKey = reader.GetString("COLUMN_KEY") == "PRI",
                    IsForeignKey = reader.GetString("COLUMN_KEY") == "MUL",
                    IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                    DefaultValue = reader.IsDBNull("COLUMN_DEFAULT") ? "" : reader.GetString("COLUMN_DEFAULT"),
                    Comment = reader.IsDBNull("COLUMN_COMMENT") ? "" : reader.GetString("COLUMN_COMMENT")
                };
                table.Columns.Add(column);
            }
        }

        // Get foreign key relationships
        var fkQuery = @"
            SELECT 
                TABLE_NAME, COLUMN_NAME, 
                REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = @schema 
                AND REFERENCED_TABLE_NAME IS NOT NULL";

        using (var cmd = new MySqlCommand(fkQuery, connection))
        {
            cmd.Parameters.AddWithValue("@schema", info.Database);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var sourceTableName = reader.GetString("REFERENCED_TABLE_NAME");
                var targetTableName = reader.GetString("TABLE_NAME");

                if (tableDict.TryGetValue(sourceTableName, out var sourceTable) &&
                    tableDict.TryGetValue(targetTableName, out var targetTable))
                {
                    relationships.Add(new Relationship
                    {
                        SourceTableId = sourceTable.Id,
                        TargetTableId = targetTable.Id,
                        SourceColumnName = reader.GetString("REFERENCED_COLUMN_NAME"),
                        TargetColumnName = reader.GetString("COLUMN_NAME"),
                        RelationType = RelationType.OneToMany
                    });
                }
            }
        }

        // Arrange tables in grid layout
        ArrangeTablesInGrid(tables);

        return new ErdGenerationResult
        {
            IsSuccess = true,
            Tables = tables,
            Relationships = relationships
        };
    }

    private ErdGenerationResult GenerateFromPostgreSql(DatabaseConnectionInfo info)
    {
        var connectionString = $"Host={info.Host};Port={info.Port};Database={info.Database};Username={info.UserId};Password={info.Password};";
        
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var tables = new List<Table>();
        var relationships = new List<Relationship>();
        var tableDict = new Dictionary<string, Table>();
        
        // Use provided schema or default to 'public'
        var schema = string.IsNullOrWhiteSpace(info.Schema) ? "public" : info.Schema;

        // Get tables
        var tableQuery = @"
            SELECT t.table_name, 
                   COALESCE(pg_catalog.obj_description(c.oid, 'pg_class'), '') as table_comment
            FROM information_schema.tables t
            LEFT JOIN pg_catalog.pg_class c ON c.relname = t.table_name
            WHERE t.table_schema = @schema AND t.table_type = 'BASE TABLE'";

        using (var cmd = new NpgsqlCommand(tableQuery, connection))
        {
            cmd.Parameters.AddWithValue("@schema", schema);
            using var reader = cmd.ExecuteReader();
            int colorIndex = 0;

            while (reader.Read())
            {
                var table = new Table
                {
                    Name = reader.GetString(0).ToUpper(),
                    Comment = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    X = 0,
                    Y = 0,
                    HeaderColor = TableColors[colorIndex % TableColors.Length]
                };
                tables.Add(table);
                tableDict[reader.GetString(0)] = table;
                colorIndex++;
            }
        }

        // Get columns for each table
        foreach (var table in tables)
        {
            var originalName = tableDict.FirstOrDefault(x => x.Value == table).Key;
            var columnQuery = @"
                SELECT 
                    c.column_name, 
                    c.data_type,
                    c.character_maximum_length,
                    c.numeric_precision,
                    c.numeric_scale,
                    c.is_nullable,
                    c.column_default,
                    COALESCE(pgd.description, '') as column_comment,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary
                FROM information_schema.columns c
                LEFT JOIN pg_catalog.pg_statio_all_tables st ON st.relname = c.table_name
                LEFT JOIN pg_catalog.pg_description pgd ON pgd.objoid = st.relid AND pgd.objsubid = c.ordinal_position
                LEFT JOIN (
                    SELECT ku.column_name, ku.table_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku ON tc.constraint_name = ku.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY' AND tc.table_schema = @schema
                ) pk ON pk.table_name = c.table_name AND pk.column_name = c.column_name
                WHERE c.table_schema = @schema AND c.table_name = @table
                ORDER BY c.ordinal_position";

            using var cmd = new NpgsqlCommand(columnQuery, connection);
            cmd.Parameters.AddWithValue("@schema", schema);
            cmd.Parameters.AddWithValue("@table", originalName);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dataType = reader.GetString(1).ToUpper();
                var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                var precision = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var scale = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);

                if (maxLength.HasValue)
                    dataType = $"{dataType}({maxLength})";
                else if (precision.HasValue && scale.HasValue && scale > 0)
                    dataType = $"{dataType}({precision},{scale})";
                else if (precision.HasValue)
                    dataType = $"{dataType}({precision})";

                var column = new Column
                {
                    Name = reader.GetString(0).ToUpper(),
                    DataType = dataType,
                    IsPrimaryKey = reader.GetBoolean(8),
                    IsForeignKey = false,
                    IsNullable = reader.GetString(5) == "YES",
                    DefaultValue = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Comment = reader.GetString(7)
                };
                table.Columns.Add(column);
            }
        }

        // Get foreign key relationships
        var fkQuery = @"
            SELECT
                kcu1.table_name as source_table,
                kcu1.column_name as source_column,
                kcu2.table_name as target_table,
                kcu2.column_name as target_column
            FROM information_schema.referential_constraints rc
            JOIN information_schema.key_column_usage kcu1 
                ON kcu1.constraint_name = rc.constraint_name AND kcu1.constraint_schema = rc.constraint_schema
            JOIN information_schema.key_column_usage kcu2 
                ON kcu2.constraint_name = rc.unique_constraint_name AND kcu2.constraint_schema = rc.unique_constraint_schema
            WHERE rc.constraint_schema = @schema";

        using (var cmd = new NpgsqlCommand(fkQuery, connection))
        {
            cmd.Parameters.AddWithValue("@schema", schema);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var sourceTableName = reader.GetString(2);
                var targetTableName = reader.GetString(0);

                if (tableDict.TryGetValue(sourceTableName, out var sourceTable) &&
                    tableDict.TryGetValue(targetTableName, out var targetTable))
                {
                    // Mark FK column
                    var fkColumn = targetTable.Columns.FirstOrDefault(c => 
                        c.Name.Equals(reader.GetString(1).ToUpper(), StringComparison.OrdinalIgnoreCase));
                    if (fkColumn != null)
                        fkColumn.IsForeignKey = true;

                    relationships.Add(new Relationship
                    {
                        SourceTableId = sourceTable.Id,
                        TargetTableId = targetTable.Id,
                        SourceColumnName = reader.GetString(3).ToUpper(),
                        TargetColumnName = reader.GetString(1).ToUpper(),
                        RelationType = RelationType.OneToMany
                    });
                }
            }
        }

        // Arrange tables in grid layout
        ArrangeTablesInGrid(tables);

        return new ErdGenerationResult
        {
            IsSuccess = true,
            Tables = tables,
            Relationships = relationships
        };
    }

    private ErdGenerationResult GenerateFromOracle(DatabaseConnectionInfo info)
    {
        var connectionString = $"Data Source={info.Host}:{info.Port}/ORCL;User Id={info.UserId};Password={info.Password};";
        
        using var connection = new OracleConnection(connectionString);
        connection.Open();

        var tables = new List<Table>();
        var relationships = new List<Relationship>();
        var tableDict = new Dictionary<string, Table>();

        // Get tables
        var tableQuery = @"
            SELECT t.TABLE_NAME, c.COMMENTS
            FROM ALL_TABLES t
            LEFT JOIN ALL_TAB_COMMENTS c ON t.TABLE_NAME = c.TABLE_NAME AND t.OWNER = c.OWNER
            WHERE t.OWNER = :schema";

        using (var cmd = new OracleCommand(tableQuery, connection))
        {
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            using var reader = cmd.ExecuteReader();
            int colorIndex = 0;
            int xPos = 50, yPos = 50;

            while (reader.Read())
            {
                var table = new Table
                {
                    Name = reader.GetString(0),
                    Comment = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    X = xPos,
                    Y = yPos,
                    HeaderColor = TableColors[colorIndex % TableColors.Length]
                };
                tables.Add(table);
                tableDict[table.Name] = table;

                colorIndex++;
                xPos += 380;
                if (xPos > 1200)
                {
                    xPos = 50;
                    yPos += 300;
                }
            }
        }

        // Get columns for each table
        foreach (var table in tables)
        {
            var columnQuery = @"
                SELECT 
                    c.COLUMN_NAME, 
                    c.DATA_TYPE,
                    c.DATA_LENGTH,
                    c.DATA_PRECISION,
                    c.DATA_SCALE,
                    c.NULLABLE,
                    c.DATA_DEFAULT,
                    cc.COMMENTS,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'Y' ELSE 'N' END as IS_PK
                FROM ALL_TAB_COLUMNS c
                LEFT JOIN ALL_COL_COMMENTS cc ON c.TABLE_NAME = cc.TABLE_NAME AND c.COLUMN_NAME = cc.COLUMN_NAME AND c.OWNER = cc.OWNER
                LEFT JOIN (
                    SELECT cols.COLUMN_NAME, cols.TABLE_NAME
                    FROM ALL_CONSTRAINTS cons
                    JOIN ALL_CONS_COLUMNS cols ON cons.CONSTRAINT_NAME = cols.CONSTRAINT_NAME AND cons.OWNER = cols.OWNER
                    WHERE cons.CONSTRAINT_TYPE = 'P' AND cons.OWNER = :schema
                ) pk ON pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
                WHERE c.OWNER = :schema AND c.TABLE_NAME = :table
                ORDER BY c.COLUMN_ID";

            using var cmd = new OracleCommand(columnQuery, connection);
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            cmd.Parameters.Add(":table", table.Name);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dataType = reader.GetString(1);
                var length = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                var precision = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var scale = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);

                if (dataType == "VARCHAR2" || dataType == "CHAR" || dataType == "NVARCHAR2")
                    dataType = $"{dataType}({length})";
                else if (dataType == "NUMBER" && precision.HasValue)
                    dataType = scale.HasValue && scale > 0 ? $"{dataType}({precision},{scale})" : $"{dataType}({precision})";

                var column = new Column
                {
                    Name = reader.GetString(0),
                    DataType = dataType,
                    IsPrimaryKey = reader.GetString(8) == "Y",
                    IsForeignKey = false,
                    IsNullable = reader.GetString(5) == "Y",
                    DefaultValue = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
                table.Columns.Add(column);
            }
        }

        // Get foreign key relationships
        var fkQuery = @"
            SELECT 
                cols.TABLE_NAME as TARGET_TABLE,
                cols.COLUMN_NAME as TARGET_COLUMN,
                rcols.TABLE_NAME as SOURCE_TABLE,
                rcols.COLUMN_NAME as SOURCE_COLUMN
            FROM ALL_CONSTRAINTS cons
            JOIN ALL_CONS_COLUMNS cols ON cons.CONSTRAINT_NAME = cols.CONSTRAINT_NAME AND cons.OWNER = cols.OWNER
            JOIN ALL_CONSTRAINTS rcons ON cons.R_CONSTRAINT_NAME = rcons.CONSTRAINT_NAME AND cons.R_OWNER = rcons.OWNER
            JOIN ALL_CONS_COLUMNS rcols ON rcons.CONSTRAINT_NAME = rcols.CONSTRAINT_NAME AND rcons.OWNER = rcols.OWNER
            WHERE cons.CONSTRAINT_TYPE = 'R' AND cons.OWNER = :schema";

        using (var cmd = new OracleCommand(fkQuery, connection))
        {
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var sourceTableName = reader.GetString(2);
                var targetTableName = reader.GetString(0);

                if (tableDict.TryGetValue(sourceTableName, out var sourceTable) &&
                    tableDict.TryGetValue(targetTableName, out var targetTable))
                {
                    // Mark FK column
                    var fkColumn = targetTable.Columns.FirstOrDefault(c => c.Name == reader.GetString(1));
                    if (fkColumn != null)
                        fkColumn.IsForeignKey = true;

                    relationships.Add(new Relationship
                    {
                        SourceTableId = sourceTable.Id,
                        TargetTableId = targetTable.Id,
                        SourceColumnName = reader.GetString(3),
                        TargetColumnName = reader.GetString(1),
                        RelationType = RelationType.OneToMany
                    });
                }
            }
        }

        // Arrange tables in grid layout
        ArrangeTablesInGrid(tables);

        return new ErdGenerationResult
        {
            IsSuccess = true,
            Tables = tables,
            Relationships = relationships
        };
    }

    private ErdGenerationResult GenerateFromTibero(DatabaseConnectionInfo info)
    {
        // Tibero uses similar connection string format to Oracle
        // Using Oracle.ManagedDataAccess as it's compatible with Tibero
        var connectionString = $"Data Source={info.Host}:{info.Port}/tibero;User Id={info.UserId};Password={info.Password};";
        
        using var connection = new OracleConnection(connectionString);
        connection.Open();

        var tables = new List<Table>();
        var relationships = new List<Relationship>();
        var tableDict = new Dictionary<string, Table>();

        // Get tables (Tibero uses similar system tables to Oracle)
        var tableQuery = @"
            SELECT t.TABLE_NAME, c.COMMENTS
            FROM ALL_TABLES t
            LEFT JOIN ALL_TAB_COMMENTS c ON t.TABLE_NAME = c.TABLE_NAME AND t.OWNER = c.OWNER
            WHERE t.OWNER = :schema";

        using (var cmd = new OracleCommand(tableQuery, connection))
        {
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            using var reader = cmd.ExecuteReader();
            int colorIndex = 0;

            while (reader.Read())
            {
                var table = new Table
                {
                    Name = reader.GetString(0),
                    Comment = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    X = 0,
                    Y = 0,
                    HeaderColor = TableColors[colorIndex % TableColors.Length]
                };
                tables.Add(table);
                tableDict[table.Name] = table;
                colorIndex++;
            }
        }

        // Get columns for each table
        foreach (var table in tables)
        {
            var columnQuery = @"
                SELECT 
                    c.COLUMN_NAME, 
                    c.DATA_TYPE,
                    c.DATA_LENGTH,
                    c.DATA_PRECISION,
                    c.DATA_SCALE,
                    c.NULLABLE,
                    c.DATA_DEFAULT,
                    cc.COMMENTS,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'Y' ELSE 'N' END as IS_PK
                FROM ALL_TAB_COLUMNS c
                LEFT JOIN ALL_COL_COMMENTS cc ON c.TABLE_NAME = cc.TABLE_NAME AND c.COLUMN_NAME = cc.COLUMN_NAME AND c.OWNER = cc.OWNER
                LEFT JOIN (
                    SELECT cols.COLUMN_NAME, cols.TABLE_NAME
                    FROM ALL_CONSTRAINTS cons
                    JOIN ALL_CONS_COLUMNS cols ON cons.CONSTRAINT_NAME = cols.CONSTRAINT_NAME AND cons.OWNER = cols.OWNER
                    WHERE cons.CONSTRAINT_TYPE = 'P' AND cons.OWNER = :schema
                ) pk ON pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
                WHERE c.OWNER = :schema AND c.TABLE_NAME = :table
                ORDER BY c.COLUMN_ID";

            using var cmd = new OracleCommand(columnQuery, connection);
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            cmd.Parameters.Add(":table", table.Name);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dataType = reader.GetString(1);
                var length = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                var precision = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var scale = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);

                if (dataType == "VARCHAR2" || dataType == "CHAR" || dataType == "NVARCHAR2")
                    dataType = $"{dataType}({length})";
                else if (dataType == "NUMBER" && precision.HasValue)
                    dataType = scale.HasValue && scale > 0 ? $"{dataType}({precision},{scale})" : $"{dataType}({precision})";

                var column = new Column
                {
                    Name = reader.GetString(0),
                    DataType = dataType,
                    IsPrimaryKey = reader.GetString(8) == "Y",
                    IsForeignKey = false,
                    IsNullable = reader.GetString(5) == "Y",
                    DefaultValue = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Comment = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
                table.Columns.Add(column);
            }
        }

        // Get foreign key relationships
        var fkQuery = @"
            SELECT 
                cols.TABLE_NAME as TARGET_TABLE,
                cols.COLUMN_NAME as TARGET_COLUMN,
                rcols.TABLE_NAME as SOURCE_TABLE,
                rcols.COLUMN_NAME as SOURCE_COLUMN
            FROM ALL_CONSTRAINTS cons
            JOIN ALL_CONS_COLUMNS cols ON cons.CONSTRAINT_NAME = cols.CONSTRAINT_NAME AND cons.OWNER = cols.OWNER
            JOIN ALL_CONSTRAINTS rcons ON cons.R_CONSTRAINT_NAME = rcons.CONSTRAINT_NAME AND cons.R_OWNER = rcons.OWNER
            JOIN ALL_CONS_COLUMNS rcols ON rcons.CONSTRAINT_NAME = rcols.CONSTRAINT_NAME AND rcons.OWNER = rcols.OWNER
            WHERE cons.CONSTRAINT_TYPE = 'R' AND cons.OWNER = :schema";

        using (var cmd = new OracleCommand(fkQuery, connection))
        {
            cmd.Parameters.Add(":schema", info.Database.ToUpper());
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var sourceTableName = reader.GetString(2);
                var targetTableName = reader.GetString(0);

                if (tableDict.TryGetValue(sourceTableName, out var sourceTable) &&
                    tableDict.TryGetValue(targetTableName, out var targetTable))
                {
                    // Mark FK column
                    var fkColumn = targetTable.Columns.FirstOrDefault(c => c.Name == reader.GetString(1));
                    if (fkColumn != null)
                        fkColumn.IsForeignKey = true;

                    relationships.Add(new Relationship
                    {
                        SourceTableId = sourceTable.Id,
                        TargetTableId = targetTable.Id,
                        SourceColumnName = reader.GetString(3),
                        TargetColumnName = reader.GetString(1),
                        RelationType = RelationType.OneToMany
                    });
                }
            }
        }

        // Arrange tables in grid layout
        ArrangeTablesInGrid(tables);

        return new ErdGenerationResult
        {
            IsSuccess = true,
            Tables = tables,
            Relationships = relationships
        };
    }

    /// <summary>
    /// Arranges tables in a grid layout without overlapping
    /// </summary>
    private void ArrangeTablesInGrid(List<Table> tables)
    {
        if (tables.Count == 0) return;

        // Place each table at a non-overlapping position
        var placedTables = new List<(double X, double Y, double Width, double Height)>();
        const double margin = 20;
        const double startX = 50;
        const double startY = 50;

        // Estimate table size from content to avoid placing tables too close.
        const double minWidth = 320;
        const double maxWidth = 900;
        const double charWidth = 7.0; // rough avg pixel width per character
        const double widthPadding = 140; // padding for columns/comments and controls

        foreach (var table in tables)
        {
            // Estimate width based on longest text (table name, comment, columns)
            int maxChars = 0;
            if (!string.IsNullOrEmpty(table.Name)) maxChars = Math.Max(maxChars, table.Name.Length);
            if (!string.IsNullOrEmpty(table.Comment)) maxChars = Math.Max(maxChars, table.Comment.Length);

            foreach (var col in table.Columns)
            {
                if (!string.IsNullOrEmpty(col.Name)) maxChars = Math.Max(maxChars, col.Name.Length);
                if (!string.IsNullOrEmpty(col.DataType)) maxChars = Math.Max(maxChars, col.DataType.Length + 4);
                if (!string.IsNullOrEmpty(col.Comment)) maxChars = Math.Max(maxChars, col.Comment.Length);
            }

            double tableWidth = Math.Clamp(maxChars * charWidth + widthPadding, minWidth, maxWidth);
            double tableHeight = BaseTableHeight + (table.Columns.Count * RowHeight);

            var position = FindNonOverlappingPosition(placedTables, tableWidth, tableHeight, startX, startY, margin);

            table.X = position.X;
            table.Y = position.Y;

            placedTables.Add((position.X, position.Y, tableWidth, tableHeight));
        }
    }

    /// <summary>
    /// Find a position that doesn't overlap with existing tables
    /// </summary>
    private (double X, double Y) FindNonOverlappingPosition(
        List<(double X, double Y, double Width, double Height)> placedTables,
        double tableWidth, double tableHeight,
        double startX, double startY, double margin)
    {
        const double stepX = 50;
        const double stepY = 50;
        const double maxX = 3000;
        const double maxY = 5000;

        double y = startY;
        while (y < maxY)
        {
            double x = startX;
            while (x < maxX)
            {
                bool hasCollision = false;
                foreach (var placed in placedTables)
                {
                    // Check if rectangles overlap
                    bool overlapsX = x < placed.X + placed.Width + margin && x + tableWidth + margin > placed.X;
                    bool overlapsY = y < placed.Y + placed.Height + margin && y + tableHeight + margin > placed.Y;

                    if (overlapsX && overlapsY)
                    {
                        hasCollision = true;
                        break;
                    }
                }

                if (!hasCollision)
                {
                    return (x, y);
                }

                x += stepX;
            }
            y += stepY;
        }

        // Fallback
        return (startX, startY);
    }
}
