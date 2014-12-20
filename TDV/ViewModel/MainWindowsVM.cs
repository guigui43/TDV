using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Commands;
using TDV.Event;
using TDV.Helper;
using TDV.Office;
using TDV.VLC;

namespace TDV.ViewModel
{
    sealed class MainWindowVM : ViewModelBase, IContext
    {
        #region ----- Fields

        private string _sourceFolderPath;
        private string _destinationFolderPath;
        private string _vlcPath;
        private ObservableCollection<string> _fileSystemWatcherList = new AsyncObservableCollection<string>();

        #endregion

        #region ----- Properties

        private static KeyValuePair<string, string> OfficeVersion { get; set; }
        private string CurrentFilename { get; set; }
        private static List<KeyValuePair<string, string>> MacroRangeList { get; set; }
        private static List<string> FileExtensionExcluded { get; set; }
        private bool IsExcelDetection { get; set; }

        private VlcCommander _vlcPlayer;
        public DelegateCommand<string> FolderPathCommand { get; set; }
        public DelegateCommand StartVlcCommand { get; set; }

        public DelegateCommand<string> ReInitSettingsCommand { get; set; }

        public ObservableCollection<string> FileSystemWatcherList
        {
            get { return _fileSystemWatcherList; }
            set
            {
                if (_fileSystemWatcherList != value)
                {
                    _fileSystemWatcherList = value;
                    OnPropertyChanged("FileSystemWatcherList");
                }
            }
        }

        public string SourceFolderPath
        {
            get { return _sourceFolderPath; }
            set
            {
                if (_sourceFolderPath != value)
                {
                    _sourceFolderPath = value;
                    Properties.Settings.Default.SourceFolderPath = _sourceFolderPath;
                    Properties.Settings.Default.Save();
                    //StartFileSystemWatcher();
                    OnPropertyChanged("SourceFolderPath");
                }
            }
        }

        public string DestinationFolderPath
        {
            get { return _destinationFolderPath; }
            set
            {
                if (_destinationFolderPath != value)
                {
                    _destinationFolderPath = value;
                    Properties.Settings.Default.DestinationFolderPath = _destinationFolderPath;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("DestinationFolderPath");
                }
            }
        }

        public string VlcPath
        {
            get { return _vlcPath; }
            set
            {
                if (_vlcPath != value)
                {
                    _vlcPath = value;
                    Properties.Settings.Default.VlcPath = _vlcPath;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("VlcPath");
                }
            }
        }
        #endregion

        #region ----- Constructor

        public MainWindowVM()
            : this(Dispatcher.CurrentDispatcher)
        {
            FileSystemWatcherList = new ObservableCollection<string>();

            if (String.IsNullOrEmpty(Properties.Settings.Default.VlcPath) ||
                String.IsNullOrEmpty(Properties.Settings.Default.SourceFolderPath) ||
                String.IsNullOrEmpty(Properties.Settings.Default.DestinationFolderPath))
                Properties.Settings.Default.Upgrade();

            SetDefaultSourceFolderPath();

            OfficeVersion = Core.GetOfficeVersion();
            //WriteLog(string.Format(Properties.Resources.MainWindowsVM_SetOfficeVersion_ExcelVersion, OfficeVersion.Value));

            //SetSettings();

            Core.SetExcelSecuritySettings(OfficeVersion.Key);

            FolderPathCommand = new DelegateCommand<string>(ExecuteFolderBrowser);

            StartVlcCommand = new DelegateCommand(ExecuteStartVlc);

            ReInitSettingsCommand = new DelegateCommand<string>(ExecuteReInit);

            LogMessageEvent.Instance.Subscribe(ProcessLog);
        }

        public MainWindowVM(Dispatcher dispatcher)
        {
            Debug.Assert(dispatcher != null);

            this._dispatcher = dispatcher;
        }

        private void ExecuteReInit(string parameter)
        {
            var dropboxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dropbox");

            switch (parameter)
            {
                case "Source":
                    SourceFolderPath = Directory.Exists(dropboxPath) ? dropboxPath : AppDomain.CurrentDomain.BaseDirectory;
                    break;
                case "Destination":
                    DestinationFolderPath = Directory.Exists(dropboxPath) ? dropboxPath : AppDomain.CurrentDomain.BaseDirectory;

                    break;
                case "Vlc":
                    var vlcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"VideoLAN\VLC\vlc.exe");
                    if (File.Exists(vlcPath))
                        VlcPath = vlcPath;

                    break;
            }
        }

        private void ExecuteStartVlc()
        {
            _vlcPlayer = new VlcCommander(VlcPath);

            var commands = new List<VlcArgumentBuilder>
            {
                new VlcCommandBuilder()
                .SetVideoTitleShow(false)
                .SetFullscreen(true)
                .SetEmbedded(true)
            };

            //var files = Directory.GetFiles(DestinationFolderPath, "*", SearchOption.AllDirectories)
            var files = Directory.EnumerateFiles(DestinationFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(x => !FileExtensionExcluded.Contains(Path.GetExtension(x))).ToList();

            commands.AddRange(files.Select(x => new VlcFile(x)));
            _vlcPlayer.Start(commands.ToArray());
        }

        #endregion

        private void ExecuteFolderBrowser(string parameter)
        {
            // Browse folder to monitor
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                switch (parameter)
                {
                    case "Source":
                        SourceFolderPath = folderBrowserDialog.SelectedPath;
                        break;
                    case "Destination":
                        DestinationFolderPath = folderBrowserDialog.SelectedPath;
                        break;
                    case "Vlc":
                        VlcPath = folderBrowserDialog.SelectedPath;
                        break;
                }
            }
        }

        private void SetDefaultSourceFolderPath()
        {

            var dropboxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Dropbox");
            if (string.IsNullOrEmpty(Properties.Settings.Default.SourceFolderPath) ||
                !Directory.Exists(Properties.Settings.Default.SourceFolderPath))
            {
                SourceFolderPath = Directory.Exists(dropboxPath) ? dropboxPath : AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                SourceFolderPath = Properties.Settings.Default.SourceFolderPath;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.DestinationFolderPath) ||
                !Directory.Exists(Properties.Settings.Default.DestinationFolderPath))
            {
                DestinationFolderPath = Directory.Exists(dropboxPath) ? dropboxPath : AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                DestinationFolderPath = Properties.Settings.Default.DestinationFolderPath;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.VlcPath) ||
                !File.Exists(Properties.Settings.Default.VlcPath))
            {
                var vlcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"VideoLAN\VLC\vlc.exe");
                if (File.Exists(vlcPath))
                    VlcPath = vlcPath;
            }
            else
            {
                VlcPath = Properties.Settings.Default.VlcPath;
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.FileExtensionExcluded) ||
                !File.Exists(Properties.Settings.Default.FileExtensionExcluded))
            {
                FileExtensionExcluded = new List<string> { ".xls", ".xlsx", ".db" };
            }
            else
            {
                FileExtensionExcluded = Properties.Settings.Default.FileExtensionExcluded.Split(',').Select(s => s.Trim()).ToList(); ;
            }


            //FileExtensionExcluded = ConfigurationManager.AppSettings["exclusion"].Split(',').Select(s => s.Trim()).ToList();
        }

        private void SetSettings()
        {
            try
            {
                var sheets = ConfigurationManager.AppSettings["sheets"].Split(',').Select(s => s.Trim()).ToList();
                var screeenshot = ConfigurationManager.AppSettings["screeenshot"].Split(',').Select(s => s.Trim()).ToList();

                if (sheets.Count() != screeenshot.Count())
                {
                    WriteLog(string.Format("sheets '{0}' != screenshot '{1}'", sheets.Count(), screeenshot.Count()));
                    IsExcelDetection = false;
                    WriteLog(Properties.Resources.MainWindowsVM_SetSettings_ExcelDectectionSettingsKO);
                }
                else
                {
                    MacroRangeList = sheets.Zip(screeenshot, (x, y) => new KeyValuePair<string, string>(x, y)).ToList();
                    IsExcelDetection = true;
                    WriteLog(Properties.Resources.MainWindowsVM_SetSettings_ExcelDectectionSettingsOK);
                }
            }
            catch (Exception)
            {
                IsExcelDetection = false;
                WriteLog(Properties.Resources.MainWindowsVM_SetSettings_ExcelDectectionSettingsKO);
            }
        }

        private void StartFileSystemWatcher()
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

            WriteLog(string.Format(Properties.Resources.MainWindowsVM_StartFileSystemWatcher_StartFileSystemWatcher, SourceFolderPath));
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
                // When using FileSystemWatcher event's be aware that these events will be called on a separate thread automatically!!!
                // If you call method AddListLine() in a normal way, it will throw following exception:
                // "The calling thread cannot access this object because a different thread owns it"
                // To fix this, you must call this method using Dispatcher.BeginInvoke(...)!
                WriteLog(string.Format(Properties.Resources.MainWindowsVM_DisplayFileSystemWatcherInfo_DateOldFileNameToNewName, watcherChangeTypes, oldName, name, DateTime.Now));
            }
            else
            {
                WriteLog(string.Format("{0} -> {1} - {2}", watcherChangeTypes, name, DateTime.Now));
            }
            var extension = Path.GetExtension(fullPath);

            if (IsExcelDetection
                && watcherChangeTypes != WatcherChangeTypes.Deleted
                && !fullPath.Contains("~$")
                && extension != null && extension.Contains(".xls") && name != CurrentFilename)
            {
                WriteLog(string.Format("Excel file : {0}", name));
                //LaunchExcel(fullPath, name);
                //LaunchOpenSDKExcel(fullPath, name);
            }

        }

        #region ---- Logger method

        private void WriteLog(string text)
        {
            Invoke(() => FileSystemWatcherList.Add(string.Concat(DateTime.Now + " - ", text)));
        }

        private void ProcessLog(string text)
        {
            Invoke(() => FileSystemWatcherList.Add(string.Concat(DateTime.Now + " - ", text)));
        }

        #endregion

        #region IContext

        private readonly Dispatcher _dispatcher;

        public bool IsSynchronized
        {
            get
            {
                return this._dispatcher.Thread == Thread.CurrentThread;
            }
        }
        public void Invoke(Action action)
        {
            Debug.Assert(action != null);

            this._dispatcher.Invoke(action);
        }

        public void BeginInvoke(Action action)
        {
            Debug.Assert(action != null);

            this._dispatcher.BeginInvoke(action);
        }

        #endregion

    }
}
