using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using ERDio.Models;

namespace ERDio.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private double _zoom = 100;
        private string _databaseName = "Database";

        public ObservableCollection<Table> Tables { get; set; } = new ObservableCollection<Table>();
        public ObservableCollection<Relationship> Relationships { get; set; } = new ObservableCollection<Relationship>();

        public double Zoom
        {
            get => _zoom;
            set { _zoom = value; OnPropertyChanged(nameof(Zoom)); OnPropertyChanged(nameof(ZoomScale)); }
        }

        public double ZoomScale => Zoom / 100.0;

        public string DatabaseName
        {
            get => _databaseName;
            set { _databaseName = value; OnPropertyChanged(nameof(DatabaseName)); }
        }

        public MainViewModel()
        {
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // MEMBER2 테이블
            var memberTable = new Table
            {
                Name = "MEMBER2",
                Comment = "comment",
                X = 400,
                Y = 100,
                HeaderColor = Color.FromRgb(220, 60, 60)
            };
            memberTable.Columns.Add(new Column { Name = "USERID", DataType = "VARCHAR2(30)", IsPrimaryKey = true, IsNullable = false });
            memberTable.Columns.Add(new Column { Name = "USERPW", DataType = "VARCHAR2(200)", IsNullable = false });
            memberTable.Columns.Add(new Column { Name = "USERNAME", DataType = "VARCHAR2(15)", IsNullable = false });
            memberTable.Columns.Add(new Column { Name = "USERAGE", DataType = "NUMBER(3)", IsNullable = false });
            memberTable.Columns.Add(new Column { Name = "USERDATE", DataType = "DATE", IsNullable = false });
            memberTable.Columns.Add(new Column { Name = "ORDNO", DataType = "NUMBER", IsNullable = false });
            Tables.Add(memberTable);

            // ITEM2 테이블
            var itemTable = new Table
            {
                Name = "ITEM2",
                Comment = "comment",
                X = 50,
                Y = 350,
                HeaderColor = Color.FromRgb(200, 100, 200)
            };
            itemTable.Columns.Add(new Column { Name = "ITEMNO", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false });
            itemTable.Columns.Add(new Column { Name = "ITEMNAME", DataType = "VARCHAR2(30)", IsNullable = true });
            itemTable.Columns.Add(new Column { Name = "ITEMPRICE", DataType = "NUMBER", IsNullable = true });
            itemTable.Columns.Add(new Column { Name = "ITEMQTY", DataType = "NUMBER", IsNullable = true });
            itemTable.Columns.Add(new Column { Name = "ITEMDATE", DataType = "DATE", IsNullable = true });
            itemTable.Columns.Add(new Column { Name = "ORDNO", DataType = "NUMBER", IsForeignKey = true, IsNullable = false });
            itemTable.Columns.Add(new Column { Name = "ORDNO", DataType = "NUMBER", IsForeignKey = true, IsNullable = false });
            itemTable.Columns.Add(new Column { Name = "USERID", DataType = "VARCHAR2(30)", IsForeignKey = true, IsNullable = false });
            Tables.Add(itemTable);

            // ORDER1 테이블
            var orderTable = new Table
            {
                Name = "ORDER1",
                Comment = "comment",
                X = 650,
                Y = 380,
                HeaderColor = Color.FromRgb(80, 80, 180)
            };
            orderTable.Columns.Add(new Column { Name = "ORDNO", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false });
            orderTable.Columns.Add(new Column { Name = "ORDCNT", DataType = "NUMBER", IsNullable = true });
            orderTable.Columns.Add(new Column { Name = "ORDDATE", DataType = "DATE", IsNullable = true });
            orderTable.Columns.Add(new Column { Name = "ITEMNO", DataType = "NUMBER", IsForeignKey = true, IsNullable = false });
            orderTable.Columns.Add(new Column { Name = "USERID", DataType = "VARCHAR2(30)", IsForeignKey = true, IsNullable = false });
            Tables.Add(orderTable);

            // Relationships
            Relationships.Add(new Relationship
            {
                SourceTableId = memberTable.Id,
                TargetTableId = orderTable.Id,
                SourceColumnName = "USERID",
                TargetColumnName = "USERID",
                RelationType = RelationType.OneToMany
            });

            Relationships.Add(new Relationship
            {
                SourceTableId = itemTable.Id,
                TargetTableId = orderTable.Id,
                SourceColumnName = "ORDNO",
                TargetColumnName = "ORDNO",
                RelationType = RelationType.ManyToOne
            });

            Relationships.Add(new Relationship
            {
                SourceTableId = itemTable.Id,
                TargetTableId = orderTable.Id,
                SourceColumnName = "USERID",
                TargetColumnName = "USERID",
                RelationType = RelationType.ManyToOne
            });
        }

        public void AddTable(Table table)
        {
            Tables.Add(table);
        }

        public void RemoveTable(Table table)
        {
            Tables.Remove(table);
        }

        public void AddRelationship(Relationship relationship)
        {
            Relationships.Add(relationship);
        }

        public Table? GetTableById(string id)
        {
            return Tables.FirstOrDefault(t => t.Id == id);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
