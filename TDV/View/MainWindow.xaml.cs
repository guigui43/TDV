using System;
using System.Diagnostics;
using System.Windows;
using TDV.ViewModel;

namespace TDV.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowVM _mainWindowsVm = new MainWindowVM();

        public MainWindow()
        {
            InitializeComponent();

            //Application.ThreadException += Application_ThreadException;

            AppDomain.CurrentDomain.UnhandledException += WorkerThreadHandler;

            DataContext = _mainWindowsVm;
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Trace.TraceError(e.Exception.ToString());
        }

        static void WorkerThreadHandler(object sender, UnhandledExceptionEventArgs args)
        {
            if (!(args.ExceptionObject is System.Threading.ThreadAbortException))
                Trace.TraceError(args.ExceptionObject.ToString());
        }
        
    }
}
