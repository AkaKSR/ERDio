using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace ERDio.Models
{
    public class Table : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _comment = string.Empty;
        private double _x;
        private double _y;
        private Color _headerColor = Colors.Crimson;
        private bool _isSelected;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(nameof(Comment)); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(X)); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(Y)); }
        }

        public Color HeaderColor
        {
            get => _headerColor;
            set { _headerColor = value; OnPropertyChanged(nameof(HeaderColor)); OnPropertyChanged(nameof(HeaderBrush)); }
        }

        public SolidColorBrush HeaderBrush => new SolidColorBrush(HeaderColor);

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public ObservableCollection<Column> Columns { get; set; } = new ObservableCollection<Column>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
