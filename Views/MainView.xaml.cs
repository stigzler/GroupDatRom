using GroupDatRom.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GroupDatRom.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        MainVM mainVM = new MainVM();
        public MainView()
        {
            InitializeComponent();
            DataContext = mainVM;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainVM.OnViewClosing();

            Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            Properties.Settings.Default.Save();
        }
    }
}
