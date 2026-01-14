using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERDio.Models;

namespace ERDio.Controls
{
    public class RelationshipLine : FrameworkElement
    {
        private Relationship? _relationship;
        private Point _startPoint;
        private Point _endPoint;

        public event EventHandler? RelationshipDeleted;

        public Relationship? Relationship
        {
            get => _relationship;
            set { _relationship = value; InvalidateVisual(); }
        }

        public Point StartPoint
        {
            get => _startPoint;
            set { _startPoint = value; InvalidateVisual(); }
        }

        public Point EndPoint
        {
            get => _endPoint;
            set { _endPoint = value; InvalidateVisual(); }
        }

        public RelationshipLine()
        {
            // Create context menu
            var contextMenu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Relationship" };
            deleteItem.Click += (s, e) =>
            {
                var result = MessageBox.Show("Delete this relationship?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    RelationshipDeleted?.Invoke(this, EventArgs.Empty);
                }
            };
            contextMenu.Items.Add(deleteItem);
            ContextMenu = contextMenu;
        }

        protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        {
            // Check if point is near the bezier curve
            var point = hitTestParameters.HitPoint;
            var geometry = CreateRelationshipPath();
            
            // Widen the geometry for easier clicking
            var widenedGeometry = geometry.GetWidenedPathGeometry(new Pen(Brushes.Black, 10));
            
            if (widenedGeometry.FillContains(point))
            {
                return new PointHitTestResult(this, point);
            }
            
            return null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            
            if (Relationship == null) return;

            var pen = new Pen(new SolidColorBrush(Color.FromRgb(200, 150, 150)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };

            // Draw the relationship line with bezier curves
            var pathGeometry = CreateRelationshipPath();
            dc.DrawGeometry(null, pen, pathGeometry);

            // Draw crow's foot notation at endpoints
            DrawCrowsFootNotation(dc, pen);
        }

        private PathGeometry CreateRelationshipPath()
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = StartPoint };

            // Calculate control points for bezier curve
            double midX = (StartPoint.X + EndPoint.X) / 2;
            
            var segment = new BezierSegment(
                new Point(midX, StartPoint.Y),
                new Point(midX, EndPoint.Y),
                EndPoint,
                true);

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);

            return geometry;
        }

        private void DrawCrowsFootNotation(DrawingContext dc, Pen pen)
        {
            // Draw at end point (crow's foot for "many" side)
            if (Relationship?.RelationType == RelationType.OneToMany || 
                Relationship?.RelationType == RelationType.ManyToMany)
            {
                DrawCrowsFoot(dc, pen, EndPoint, true);
            }
            else
            {
                DrawOneLine(dc, pen, EndPoint, true);
            }

            // Draw at start point
            if (Relationship?.RelationType == RelationType.ManyToOne || 
                Relationship?.RelationType == RelationType.ManyToMany)
            {
                DrawCrowsFoot(dc, pen, StartPoint, false);
            }
            else
            {
                DrawOneLine(dc, pen, StartPoint, false);
            }
        }

        private void DrawCrowsFoot(DrawingContext dc, Pen pen, Point point, bool pointingLeft)
        {
            int direction = pointingLeft ? -1 : 1;
            double footLength = 10;
            double footSpread = 8;

            // Three lines for crow's foot
            dc.DrawLine(pen, point, new Point(point.X + direction * footLength, point.Y - footSpread));
            dc.DrawLine(pen, point, new Point(point.X + direction * footLength, point.Y));
            dc.DrawLine(pen, point, new Point(point.X + direction * footLength, point.Y + footSpread));
        }

        private void DrawOneLine(DrawingContext dc, Pen pen, Point point, bool pointingLeft)
        {
            int direction = pointingLeft ? -1 : 1;
            double lineOffset = 8;

            // Single vertical line for "one" side
            dc.DrawLine(pen, 
                new Point(point.X + direction * lineOffset, point.Y - 6),
                new Point(point.X + direction * lineOffset, point.Y + 6));
        }
    }
}
