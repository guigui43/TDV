using System.IO;

namespace TDV.FileSystemWatcher
{
    public class Monitor
    {
        public string SourceFolderPath { get; set; }
        public bool IsExcelDetection { get; set; }
        public string CurrentFilename { get; set; }

        public Monitor()
        {
        }

        public void StartFileSystemWatcher()
        {
            if (string.IsNullOrWhiteSpace(SourceFolderPath))
                return;

            var fileSystemWatcher = new System.IO.FileSystemWatcher
            {
                Path = SourceFolderPath,
                NotifyFilter = NotifyFilters.FileName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size |
                               NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };

            fileSystemWatcher.Created += fileSystemWatcher_CreatedChangedDeleted;
            fileSystemWatcher.Changed += fileSystemWatcher_CreatedChangedDeleted;
            fileSystemWatcher.Deleted += fileSystemWatcher_CreatedChangedDeleted;
            fileSystemWatcher.Renamed += fileSystemWatcher_Renamed;

            //WriteLog(string.Format(Properties.Resources.MainWindowsVM_StartFileSystemWatcher_StartFileSystemWatcher, SourceFolderPath));
        }

        #region ----- FileSystem Events

        private void fileSystemWatcher_CreatedChangedDeleted(object sender, FileSystemEventArgs e)
        {
            DisplayFileSystemWatcherInfo(e.ChangeType, e.Name, e.FullPath);
        }

        private void fileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            DisplayFileSystemWatcherInfo(e.ChangeType, e.Name, e.FullPath, e.OldName);
        }

        #endregion

        private void DisplayFileSystemWatcherInfo(WatcherChangeTypes watcherChangeTypes, string name, string fullPath, string oldName = null)
        {
            if (watcherChangeTypes == WatcherChangeTypes.Renamed)
            {
                //WriteLog(string.Format(Properties.Resources.MainWindowsVM_DisplayFileSystemWatcherInfo_DateOldFileNameToNewName, watcherChangeTypes, oldName, name, DateTime.Now));
            }
            else
            {
                //WriteLog(string.Format("{0} -> {1} - {2}", watcherChangeTypes, name, DateTime.Now));
            }
            var extension = Path.GetExtension(fullPath);

            if (IsExcelDetection
                && watcherChangeTypes != WatcherChangeTypes.Deleted
                && !fullPath.Contains("~$")
                && extension != null && extension.Contains(".xls") && name != CurrentFilename)
            {
                //WriteLog(string.Format("Excel file : {0}", name));
                //LaunchExcel(fullPath, name);
                //LaunchOpenSDKExcel(fullPath, name);
            }

        }
    }
}
