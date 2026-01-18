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
            // Table layout constants (based on actual TableControl dimensions)
            // Grid columns: 20+90+100+40+70+80 = 400px minimum + border/padding
            const double tableWidth = 470;
            const double baseHeight = 45;   // Header + outer borders
            const double rowHeight = 22;    // Row height with padding/border
            const double marginX = 50;      // Horizontal gap between tables
            const double marginY = 50;      // Vertical gap between tables

            // ========================================
            // 회원(MEMBER) 테이블 - 기준 테이블 (7 columns)
            // Height: 80 + 7*22 = 234
            // ========================================
            var memberTable = new Table
            {
                Name = "MEMBER",
                Comment = "회원 정보를 저장하는 테이블",
                X = 50,
                Y = 50,
                HeaderColor = Color.FromRgb(220, 60, 60)
            };
            memberTable.Columns.Add(new Column { Name = "MEMBER_ID", DataType = "VARCHAR2(30)", IsPrimaryKey = true, IsNullable = false, Comment = "회원 고유 ID (PK)" });
            memberTable.Columns.Add(new Column { Name = "PASSWORD", DataType = "VARCHAR2(200)", IsNullable = false, Comment = "비밀번호 (암호화)" });
            memberTable.Columns.Add(new Column { Name = "NAME", DataType = "VARCHAR2(50)", IsNullable = false, Comment = "회원 이름" });
            memberTable.Columns.Add(new Column { Name = "EMAIL", DataType = "VARCHAR2(100)", IsNullable = false, Comment = "이메일 주소 (UNIQUE)" });
            memberTable.Columns.Add(new Column { Name = "PHONE", DataType = "VARCHAR2(20)", IsNullable = true, Comment = "연락처" });
            memberTable.Columns.Add(new Column { Name = "CREATED_AT", DataType = "DATE", IsNullable = false, Comment = "가입일시" });
            memberTable.Columns.Add(new Column { Name = "STATUS", DataType = "CHAR(1)", IsNullable = false, Comment = "활성상태 (Y/N)" });
            Tables.Add(memberTable);
            double memberHeight = baseHeight + memberTable.Columns.Count * rowHeight;

            // ========================================
            // 카테고리(CATEGORY) 테이블 - 상품 분류 (5 columns)
            // Height: 80 + 5*22 = 190
            // ========================================
            var categoryTable = new Table
            {
                Name = "CATEGORY",
                Comment = "상품 카테고리 정보",
                X = 50 + tableWidth + marginX,  // 430
                Y = 50,
                HeaderColor = Color.FromRgb(60, 180, 60)
            };
            categoryTable.Columns.Add(new Column { Name = "CATEGORY_ID", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false, Comment = "카테고리 ID (PK)" });
            categoryTable.Columns.Add(new Column { Name = "CATEGORY_NAME", DataType = "VARCHAR2(100)", IsNullable = false, Comment = "카테고리명" });
            categoryTable.Columns.Add(new Column { Name = "PARENT_ID", DataType = "NUMBER", IsForeignKey = true, IsNullable = true, Comment = "상위 카테고리 ID (FK, 자기참조)" });
            categoryTable.Columns.Add(new Column { Name = "DEPTH", DataType = "NUMBER(2)", IsNullable = false, Comment = "카테고리 깊이" });
            categoryTable.Columns.Add(new Column { Name = "SORT_ORDER", DataType = "NUMBER", IsNullable = false, Comment = "정렬 순서" });
            Tables.Add(categoryTable);
            double categoryHeight = baseHeight + categoryTable.Columns.Count * rowHeight;

            // ========================================
            // 주문(ORDERS) 테이블 (6 columns)
            // Position: Below MEMBER
            // ========================================
            var orderTable = new Table
            {
                Name = "ORDERS",
                Comment = "주문 정보를 저장하는 테이블",
                X = 50,
                Y = 50 + memberHeight + marginY,  // Below MEMBER
                HeaderColor = Color.FromRgb(80, 80, 180)
            };
            orderTable.Columns.Add(new Column { Name = "ORDER_ID", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false, Comment = "주문 ID (PK)" });
            orderTable.Columns.Add(new Column { Name = "MEMBER_ID", DataType = "VARCHAR2(30)", IsForeignKey = true, IsNullable = false, Comment = "주문자 ID (FK)" });
            orderTable.Columns.Add(new Column { Name = "ORDER_DATE", DataType = "DATE", IsNullable = false, Comment = "주문일시" });
            orderTable.Columns.Add(new Column { Name = "TOTAL_AMOUNT", DataType = "NUMBER(12)", IsNullable = false, Comment = "총 주문금액" });
            orderTable.Columns.Add(new Column { Name = "STATUS", DataType = "VARCHAR2(20)", IsNullable = false, Comment = "주문상태" });
            orderTable.Columns.Add(new Column { Name = "SHIPPING_ADDR", DataType = "VARCHAR2(500)", IsNullable = false, Comment = "배송 주소" });
            Tables.Add(orderTable);
            double orderHeight = baseHeight + orderTable.Columns.Count * rowHeight;

            // ========================================
            // 상품(PRODUCT) 테이블 (8 columns)
            // Position: Below CATEGORY
            // ========================================
            var productTable = new Table
            {
                Name = "PRODUCT",
                Comment = "상품 정보를 저장하는 테이블",
                X = 50 + tableWidth + marginX,  // 430
                Y = 50 + categoryHeight + marginY,  // Below CATEGORY
                HeaderColor = Color.FromRgb(200, 100, 200)
            };
            productTable.Columns.Add(new Column { Name = "PRODUCT_ID", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false, Comment = "상품 ID (PK)" });
            productTable.Columns.Add(new Column { Name = "CATEGORY_ID", DataType = "NUMBER", IsForeignKey = true, IsNullable = false, Comment = "카테고리 ID (FK)" });
            productTable.Columns.Add(new Column { Name = "PRODUCT_NAME", DataType = "VARCHAR2(200)", IsNullable = false, Comment = "상품명" });
            productTable.Columns.Add(new Column { Name = "PRICE", DataType = "NUMBER(10)", IsNullable = false, Comment = "가격" });
            productTable.Columns.Add(new Column { Name = "STOCK_QTY", DataType = "NUMBER", IsNullable = false, Comment = "재고 수량" });
            productTable.Columns.Add(new Column { Name = "DESCRIPTION", DataType = "CLOB", IsNullable = true, Comment = "상품 설명" });
            productTable.Columns.Add(new Column { Name = "CREATED_AT", DataType = "DATE", IsNullable = false, Comment = "등록일시" });
            productTable.Columns.Add(new Column { Name = "STATUS", DataType = "CHAR(1)", IsNullable = false, Comment = "판매상태 (Y/N)" });
            Tables.Add(productTable);
            double productHeight = baseHeight + productTable.Columns.Count * rowHeight;

            // ========================================
            // 주문상세(ORDER_DETAIL) 테이블 - 다대다 해소 (6 columns)
            // Position: Below ORDERS
            // ========================================
            var orderDetailTable = new Table
            {
                Name = "ORDER_DETAIL",
                Comment = "주문 상세 정보 (주문-상품 다대다 관계 해소)",
                X = 50,
                Y = 50 + memberHeight + marginY + orderHeight + marginY,  // Below ORDERS
                HeaderColor = Color.FromRgb(255, 150, 50)
            };
            orderDetailTable.Columns.Add(new Column { Name = "DETAIL_ID", DataType = "NUMBER", IsPrimaryKey = true, IsNullable = false, Comment = "주문상세 ID (PK)" });
            orderDetailTable.Columns.Add(new Column { Name = "ORDER_ID", DataType = "NUMBER", IsForeignKey = true, IsNullable = false, Comment = "주문 ID (FK)" });
            orderDetailTable.Columns.Add(new Column { Name = "PRODUCT_ID", DataType = "NUMBER", IsForeignKey = true, IsNullable = false, Comment = "상품 ID (FK)" });
            orderDetailTable.Columns.Add(new Column { Name = "QUANTITY", DataType = "NUMBER", IsNullable = false, Comment = "주문 수량" });
            orderDetailTable.Columns.Add(new Column { Name = "UNIT_PRICE", DataType = "NUMBER(10)", IsNullable = false, Comment = "주문 당시 단가" });
            orderDetailTable.Columns.Add(new Column { Name = "SUBTOTAL", DataType = "NUMBER(12)", IsNullable = false, Comment = "소계 (수량 × 단가)" });
            Tables.Add(orderDetailTable);

            // ========================================
            // 관계(Relationship) 설정 - DB 기본 규칙 준수
            // ========================================

            // 1. MEMBER (1) → ORDERS (N) : 한 회원이 여러 주문 가능
            Relationships.Add(new Relationship
            {
                SourceTableId = memberTable.Id,
                TargetTableId = orderTable.Id,
                SourceColumnName = "MEMBER_ID",
                TargetColumnName = "MEMBER_ID",
                RelationType = RelationType.OneToMany
            });

            // 2. CATEGORY (1) → CATEGORY (N) : 자기참조 (상위-하위 카테고리)
            Relationships.Add(new Relationship
            {
                SourceTableId = categoryTable.Id,
                TargetTableId = categoryTable.Id,
                SourceColumnName = "CATEGORY_ID",
                TargetColumnName = "PARENT_ID",
                RelationType = RelationType.OneToMany
            });

            // 3. CATEGORY (1) → PRODUCT (N) : 한 카테고리에 여러 상품
            Relationships.Add(new Relationship
            {
                SourceTableId = categoryTable.Id,
                TargetTableId = productTable.Id,
                SourceColumnName = "CATEGORY_ID",
                TargetColumnName = "CATEGORY_ID",
                RelationType = RelationType.OneToMany
            });

            // 4. ORDERS (1) → ORDER_DETAIL (N) : 한 주문에 여러 상품
            Relationships.Add(new Relationship
            {
                SourceTableId = orderTable.Id,
                TargetTableId = orderDetailTable.Id,
                SourceColumnName = "ORDER_ID",
                TargetColumnName = "ORDER_ID",
                RelationType = RelationType.OneToMany
            });

            // 5. PRODUCT (1) → ORDER_DETAIL (N) : 한 상품이 여러 주문에 포함
            Relationships.Add(new Relationship
            {
                SourceTableId = productTable.Id,
                TargetTableId = orderDetailTable.Id,
                SourceColumnName = "PRODUCT_ID",
                TargetColumnName = "PRODUCT_ID",
                RelationType = RelationType.OneToMany
            });
        }

        public bool AddTable(Table table)
        {
            // Check for duplicate table name
            if (Tables.Any(t => t.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            Tables.Add(table);
            return true;
        }

        public void RemoveTable(Table table)
        {
            Tables.Remove(table);
        }

        public bool AddRelationship(Relationship relationship)
        {
            // Check for duplicate relationship (same source table, target table, source column, target column)
            if (Relationships.Any(r => 
                r.SourceTableId == relationship.SourceTableId &&
                r.TargetTableId == relationship.TargetTableId &&
                r.SourceColumnName.Equals(relationship.SourceColumnName, StringComparison.OrdinalIgnoreCase) &&
                r.TargetColumnName.Equals(relationship.TargetColumnName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            Relationships.Add(relationship);
            return true;
        }

        public bool IsTableNameDuplicate(string name, string? excludeId = null)
        {
            return Tables.Any(t => 
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                (excludeId == null || t.Id != excludeId));
        }

        public Table? GetTableById(string id)
        {
            return Tables.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Find a non-overlapping position for a new table
        /// </summary>
        public (double X, double Y) FindNonOverlappingPosition(double tableWidth, double tableHeight, double startX = 100, double startY = 100)
        {
            const double margin = 20;
            const double stepX = 50;
            const double stepY = 50;
            const double maxX = 3000;
            const double maxY = 5000;

            double x = startX;
            double y = startY;

            while (y < maxY)
            {
                x = startX;
                while (x < maxX)
                {
                    bool hasCollision = false;
                    foreach (var table in Tables)
                    {
                        double existingWidth = 470;  // Approximate table width
                        double existingHeight = 45 + table.Columns.Count * 22;  // baseHeight + rows

                        // Check if rectangles overlap
                        bool overlapsX = x < table.X + existingWidth + margin && x + tableWidth + margin > table.X;
                        bool overlapsY = y < table.Y + existingHeight + margin && y + tableHeight + margin > table.Y;

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

            // Fallback: return original position if no space found
            return (startX, startY);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
