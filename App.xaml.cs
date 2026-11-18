using System.Configuration;
using System.Data;
using System.Windows;

namespace GroupDatRom
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Run user settings upgrade on first launch of a new version
            if (GroupDatRom.Properties.Settings.Default.UpgradeRequired)
            {
                GroupDatRom.Properties.Settings.Default.Upgrade();
                GroupDatRom.Properties.Settings.Default.UpgradeRequired = false;
                GroupDatRom.Properties.Settings.Default.Save();
            }
        }
    }
}
