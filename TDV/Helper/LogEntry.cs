using System;

namespace TDV.Helper
{
    public class LogEntry : ViewModelBase
    {
        public DateTime DateTime { get; set; }

        public int Index { get; set; }

        public string Message { get; set; }
    }
}