using System.ComponentModel;

namespace ERDio.Models
{
    public class Column : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _dataType = string.Empty;
        private bool _isPrimaryKey;
        private bool _isForeignKey;
        private bool _isNullable = true;
        private string _defaultValue = string.Empty;
        private string _comment = string.Empty;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string DataType
        {
            get => _dataType;
            set { _dataType = value; OnPropertyChanged(nameof(DataType)); }
        }

        public bool IsPrimaryKey
        {
            get => _isPrimaryKey;
            set { _isPrimaryKey = value; OnPropertyChanged(nameof(IsPrimaryKey)); }
        }

        public bool IsForeignKey
        {
            get => _isForeignKey;
            set { _isForeignKey = value; OnPropertyChanged(nameof(IsForeignKey)); }
        }

        public bool IsNullable
        {
            get => _isNullable;
            set { _isNullable = value; OnPropertyChanged(nameof(IsNullable)); OnPropertyChanged(nameof(NullableText)); }
        }

        public string NullableText => IsNullable ? "NULL" : "N-N";

        public string DefaultValue
        {
            get => _defaultValue;
            set { _defaultValue = value; OnPropertyChanged(nameof(DefaultValue)); }
        }

        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(nameof(Comment)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
