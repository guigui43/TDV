using System;
using System.Collections.Generic;
using System.Windows.Controls;
using TDV.ViewModel;

namespace TDV.View
{
    public partial class LogViewer : UserControl
    {
        LogViewerVM _logViewerVm = new LogViewerVM();

        public LogViewer()
        {
            InitializeComponent();

            DataContext = _logViewerVm;
        }
    }
}
