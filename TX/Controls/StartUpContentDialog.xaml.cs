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

        public void ClearPivotsToBeShownFirstLaunch()
        {
            MainPivot.Items.Remove(NetworkPermissionsPivotItem);
            MainPivot.Items.Remove(LogUploadingPermissionPivotItem);
        }

        private async void ThisLoaded(object sender, RoutedEventArgs e)
        {
            BuildTimestamp.Visibility = Visibility.Collapsed;
            var buildDateTime = await GetBuildTimestampAsync();
            if (buildDateTime.HasValue)
            {
                BuildTimeRun.Text = buildDateTime.Value.ToString("d");
                BuildTimestamp.Visibility = Visibility.Visible;
            }
        }

        private static async Task<DateTime?> GetBuildTimestampAsync()
        {
            try
            {
                var install_folder = Package.Current.InstalledLocation;
                var files = await install_folder.GetFilesAsync();
                var date_manifest = (await files.First().GetBasicPropertiesAsync()).DateModified;
                return date_manifest.DateTime;
            }
            catch (Exception) { return null; }
        }

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
    }
}
