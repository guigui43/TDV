using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;

namespace TDV.Event
{
    class LogMessageEvent : CompositePresentationEvent<string>
    {
        private static readonly EventAggregator _eventAggregator;
        private static readonly LogMessageEvent _event;

        static LogMessageEvent()
        {
            _eventAggregator = new EventAggregator();
            _event = _eventAggregator.GetEvent<LogMessageEvent>();
        }

        public static LogMessageEvent Instance
        {
            get { return _event; }
        }
    }
}
