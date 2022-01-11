using Digitalizador.ViewModels;
using NAPS2.Wia;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Digitalizador
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            using WiaDeviceManager deviceManager = new(WiaVersion.Wia20);
            foreach (WiaDeviceInfo device in deviceManager.GetDeviceInfos())
            {
                using (device)
                {
                    if(device.Properties[7].Value.ToString().Contains("Eko"))
                    {
                        DataContext = new ScanTWAINViewModel();
                    }
                    else
                    {
                        DataContext = new ScanWIAViewModel();
                    }
                }
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (ScanWIAViewModel)DataContext;
            //if (viewModel.MyCommand.CanExecute(null))
            //    viewModel.MyCommand.Execute(null);
        }
    }
}
