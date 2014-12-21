using System;
using System.IO;
using System.Reflection;
using System.Windows;

using Vlc.DotNet.Forms;

namespace TDV.Vlc.DotNet
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Viewer : Window
    {
        public Viewer()
        {
            InitializeComponent();

            myControl.MediaPlayer.VlcLibDirectoryNeeded += OnVlcControlNeedsLibDirectory;
        }

        private void OnVlcControlNeedsLibDirectory(object sender, VlcLibDirectoryNeededEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory == null)
                return;
            if (AssemblyName.GetAssemblyName(currentAssembly.Location).ProcessorArchitecture == ProcessorArchitecture.X86)
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"..\..\..\Vlc\x86\Rincewind\"));
            else
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"..\..\..\Vlc\x64\Rincewind\"));
        }

        private void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            myControl.MediaPlayer.Play(new FileInfo(@"..\..\..\Vlc.DotNet\Samples\Videos\BBB trailer.mov"));
        }
    }
}
