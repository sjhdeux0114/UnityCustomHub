using System;
using System.Collections.Generic;

namespace UnityHubCustom.Models
{
    public class AppSettings
    {
        public List<string> CustomEditorPaths { get; set; } = new List<string>();
        public List<string> CustomProjectPaths { get; set; } = new List<string>();
        public bool ResetProjectLayout { get; set; } = true;
        public bool ResetEditorLayout { get; set; } = true;
        public bool ResetRegistry { get; set; } = true;
        public bool UseOpenGL { get; set; } = false;
        public string GraphicsApi { get; set; } = "Default";
        public bool AutoSyncWithUnityHub { get; set; } = true;
        public bool FilterFavoritesOnly { get; set; } = false;
    }
}
