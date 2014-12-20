using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDV.Helper
{
    public class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }
    }
}
