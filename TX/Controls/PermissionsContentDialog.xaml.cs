using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace TX.Controls
{
    public sealed partial class PermissionsContentDialog : ContentDialog
    {
        public PermissionsContentDialog()
        {
            this.InitializeComponent();
        }

        private async void SetButton_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchUriAsync(new Uri("ms-settings:network-wifi"));

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
    }
}
