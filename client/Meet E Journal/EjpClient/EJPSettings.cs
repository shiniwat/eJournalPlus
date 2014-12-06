using System;

namespace ejpClient
{
    [Serializable]
    public class EJPSettings
    {
        public string VersionString { get; set; }
        public string UserName { get; set; }
        public string EjsAddress { get; set; }
        public bool IsEjsConfigured { get; set; }
        public string LiveSpaceUri { get; set; }
        public bool SaveUserSettings { get; set; }
        public bool ShowMapLock { get; set; }
        public int UndoCount { get; set; }
        public bool IsAutoSaveToDesktop { get; set; }
        public int AutoSaveInterval { get; set; }

        public EJPSettings()
        {

        }
    }
}
