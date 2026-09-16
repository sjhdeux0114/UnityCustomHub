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
        private DateTime _lastOpened;
        private bool _isEditorInstalled;
        private string _matchedEditorPath;
        private bool _isFavorite;
        private bool _isRunning;

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
                OnPropertyChanged(nameof(EffectiveDate));
                OnPropertyChanged(nameof(DisplayDate));
            }
        }

        public DateTime LastOpened
        {
            get => _lastOpened;
            set
            {
                _lastOpened = value;
                OnPropertyChanged(nameof(LastOpened));
                OnPropertyChanged(nameof(EffectiveDate));
                OnPropertyChanged(nameof(DisplayDate));
            }
        }

        public DateTime EffectiveDate => (LastOpened > LastModified)
            ? LastOpened
            : LastModified;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(DisplayDate));
                }
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
            set
            {
                _isFavorite = value;
                OnPropertyChanged(nameof(IsFavorite));
                OnPropertyChanged(nameof(FavoriteIcon));
                OnPropertyChanged(nameof(FavoriteColor));
                OnPropertyChanged(nameof(FavoriteToolTip));
            }
        }

        public string FavoriteIcon => IsFavorite ? "★" : "☆";
        public string FavoriteColor => IsFavorite ? "#FBBF24" : "#71717A";
        public string FavoriteToolTip => IsFavorite ? "즐겨찾기 해제" : "즐겨찾기에 추가";

        public string DisplayDate
        {
            get
            {
                var dt = EffectiveDate;
                if (dt == DateTime.MinValue) return "-";
                if (IsRunning)
                {
                    return $"{dt:yyyy-MM-dd HH:mm} (실행 중)";
                }
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
        }

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
