using System;
using System.ComponentModel;
using System.IO;

namespace UnityHubCustom.Models
{
    public class UnityProject : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private string _version;
        private DateTime _lastModified;
        private bool _isEditorInstalled;
        private string _matchedEditorPath;
        private bool _isFavorite;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(nameof(Version)); }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged(nameof(LastModified));
                OnPropertyChanged(nameof(DisplayDate));
            }
        }

        public bool IsEditorInstalled
        {
            get => _isEditorInstalled;
            set
            {
                _isEditorInstalled = value;
                OnPropertyChanged(nameof(IsEditorInstalled));
                OnPropertyChanged(nameof(EditorStatusText));
                OnPropertyChanged(nameof(EditorStatusColor));
            }
        }

        public string MatchedEditorPath
        {
            get => _matchedEditorPath;
            set { _matchedEditorPath = value; OnPropertyChanged(nameof(MatchedEditorPath)); }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(nameof(IsFavorite)); }
        }

        public string DisplayDate => LastModified != DateTime.MinValue 
            ? LastModified.ToString("yyyy-MM-dd HH:mm") 
            : "-";

        public string EditorStatusText => IsEditorInstalled ? "에디터 설치됨" : "에디터 미설치";
        public string EditorStatusColor => IsEditorInstalled ? "#10B981" : "#EF4444"; // Green or Red

        public bool ExistsOnDisk => !string.IsNullOrEmpty(Path) && Directory.Exists(Path);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
