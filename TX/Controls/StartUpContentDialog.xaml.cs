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
            this.Loaded += ThisLoaded;
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

        private readonly Settings SettingEntries = new Settings();

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
    }
}
