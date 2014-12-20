using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using TDV.Helper;


namespace TDV.ViewModel
{
    class LogViewerVM : ViewModelBase, IContext
    {
        private string TestData = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";
        private List<string> words;
        private int maxword;
        private int index;

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public LogViewerVM()
        {
            random = new Random();
            words = TestData.Split(' ').ToList();
            maxword = words.Count - 1;

            LogEntries = new ObservableCollection<LogEntry>();
            Enumerable.Range(0, 200000)
                      .ToList()
                      .ForEach(x => LogEntries.Add(GetRandomEntry()));

            Timer = new Timer(x => AddRandomEntry(), null, 1000, 10);
        }

        private System.Threading.Timer Timer;
        private System.Random random;
        private void AddRandomEntry()
        {
            Invoke(() => LogEntries.Add(GetRandomEntry()));
        }

        private LogEntry GetRandomEntry()
        {
            if (random.Next(1, 10) > 1)
            {
                return new LogEntry()
                {
                    Index = index++,
                    DateTime = DateTime.Now,
                    Message = string.Join(" ", Enumerable.Range(5, random.Next(10, 50))
                                                         .Select(x => words[random.Next(0, maxword)])),
                };
            }

            return new CollapsibleLogEntry()
            {
                Index = index++,
                DateTime = DateTime.Now,
                Message = string.Join(" ", Enumerable.Range(5, random.Next(10, 50))
                                             .Select(x => words[random.Next(0, maxword)])),
                Contents = Enumerable.Range(5, random.Next(5, 10))
                                     .Select(i => GetRandomEntry())
                                     .ToList()
            };

        }

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
