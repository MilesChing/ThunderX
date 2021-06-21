using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TX.Utils;
using Windows.ApplicationModel;
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
    public sealed partial class StartUpContentDialog : ContentDialog
    {
        public StartUpContentDialog()
        {
            this.InitializeComponent();
        }

        public bool IsNewVersionPivotShown { get; set; } = true;

        public bool IsNetworkPermissionsPivotShown { get; set; } = true;

        public bool IsLogUploadingPermissionPivotShown { get; set; } = true;

        private async void SetButton_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchUriAsync(new Uri("ms-settings:network-wifi"));

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedIndex < MainPivot.Items.Count - 1)
            {
                NextStepButton.Visibility = Visibility.Visible;
                CloseDialogButton.Visibility = Visibility.Collapsed;
            } 
            else
            {
                NextStepButton.Visibility = Visibility.Collapsed;
                CloseDialogButton.Visibility = Visibility.Visible;
            }
        }

        private void NextStepButton_Click(object sender, RoutedEventArgs e) =>
            ++MainPivot.SelectedIndex;

        private void CloseDialogButton_Click(object sender, RoutedEventArgs e) => Hide();

        private readonly Settings SettingEntries = new Settings();

        private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (IsNewVersionPivotShown == false)
                MainPivot.Items.Remove(NewVersionPivotItem);
            if (IsNetworkPermissionsPivotShown == false)
                MainPivot.Items.Remove(NetworkPermissionsPivotItem);
            if (IsLogUploadingPermissionPivotShown == false)
                MainPivot.Items.Remove(LogUploadingPermissionPivotItem);
        }
    }
}
