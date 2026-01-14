using System.ComponentModel;

namespace ERDio.Models
{
    public enum RelationType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }

    public class Relationship : INotifyPropertyChanged
    {
        private string _sourceTableId = string.Empty;
        private string _targetTableId = string.Empty;
        private string _sourceColumnName = string.Empty;
        private string _targetColumnName = string.Empty;
        private RelationType _relationType = RelationType.OneToMany;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string SourceTableId
        {
            get => _sourceTableId;
            set { _sourceTableId = value; OnPropertyChanged(nameof(SourceTableId)); }
        }

        public string TargetTableId
        {
            get => _targetTableId;
            set { _targetTableId = value; OnPropertyChanged(nameof(TargetTableId)); }
        }

        public string SourceColumnName
        {
            get => _sourceColumnName;
            set { _sourceColumnName = value; OnPropertyChanged(nameof(SourceColumnName)); }
        }

        public string TargetColumnName
        {
            get => _targetColumnName;
            set { _targetColumnName = value; OnPropertyChanged(nameof(TargetColumnName)); }
        }

        public RelationType RelationType
        {
            get => _relationType;
            set { _relationType = value; OnPropertyChanged(nameof(RelationType)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
