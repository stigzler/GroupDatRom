using CommunityToolkit.Mvvm.ComponentModel;
using iNKORE.UI.WPF.Modern;
using System;
using System.Collections.Generic;
using System.Text;

namespace GroupDatRom.ViewModels
{
    internal partial class MainVM: ObservableObject
    {
        [ObservableProperty]
        private bool isDarkMode = true;

        public MainVM()
        {
            IsDarkMode = true;
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            if (value == true)
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            else
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }
    }
}
