using System.ComponentModel;
using System.IO;

namespace UnityHubCustom.Models
{
    public class UnityEditorInstall : INotifyPropertyChanged
    {
        private string _version;
        private string _executablePath;
        private bool _isCustom;

        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(nameof(Version)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(nameof(ExecutablePath)); OnPropertyChanged(nameof(Exists)); }
        }

        public bool IsCustom
        {
            get => _isCustom;
            set { _isCustom = value; OnPropertyChanged(nameof(IsCustom)); }
        }

        public string DisplayName => $"Unity {Version}";

        public bool Exists => !string.IsNullOrEmpty(ExecutablePath) && File.Exists(ExecutablePath);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
