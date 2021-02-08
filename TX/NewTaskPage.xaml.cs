using EnsureThat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Models.Sources;
using TX.Core.Models.Targets;
using TX.Core.Providers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace TX
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class NewTaskPage : Page
    {
        App CurrentApp => ((App)App.Current);
        private readonly string UnknownText = Windows.ApplicationModel.Resources
            .ResourceLoader.GetForCurrentView().GetString("Unknown");

        public NewTaskPage()
        {
            this.InitializeComponent();
        }

        private IStorageFolder TargetFolder
        {
            get => targetFolder;
            set
            {
                targetFolder = value;
                DestinationPathTextBlock.Text = targetFolder.Path;
            }
        }
        private IStorageFolder targetFolder;

        private string DestinationFileName 
        {
            get
            {
                if (CustomFilenameCheckBox.IsChecked == false ||
                    CustomFilenameTextBox.Text.Equals(string.Empty))
                    return SuggestedFilenameTextBox.Text;
                else return CustomFilenameTextBox.Text;
            }
        }

        private AbstractTarget FinalTarget;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            TargetFolder = await LocalFolderManager.GetOrCreateDownloadFolderAsync();
            mainURITextBoxAnalyzingTaskCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => UrlAnalyzer(mainURITextBoxAnalyzingTaskCancellationTokenSource.Token));
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            mainURITextBoxAnalyzingTaskCancellationTokenSource?.Cancel();
            mainURITextBoxAnalyzingTaskCancellationTokenSource = null;
            base.OnNavigatedFrom(e);
        }

        private async void FolderSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add(".");
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null) return;
            TargetFolder = folder;
        }

        private void CustomFilenameCheckBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.IsChecked == true)
                {
                    CustomFilenameTextBox.Visibility = Visibility.Visible;
                    SuggestedFilenameTextBox.Opacity = 0.2;
                }
                else if (cb.IsChecked == false)
                {
                    CustomFilenameTextBox.Visibility = Visibility.Collapsed;
                    SuggestedFilenameTextBox.Opacity = 1;
                }
            }
        }

        private CancellationTokenSource mainURITextBoxAnalyzingTaskCancellationTokenSource;

        private void MainURITextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            lock (userOperationStatusLockObject) UriChanged = true;
        }

        private void StreamSelectionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (userOperationStatusLockObject)
                ComboBoxSelectionChanged = true;
        }

        private async void ClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView con = Clipboard.GetContent();
            if (con.Contains(StandardDataFormats.Text))
                MainURITextBox.Text = await con.GetTextAsync();
        }

        private async void FileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add("*");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                StorageApplicationPermissions.FutureAccessList.Add(file);
                MainURITextBox.Text = file.Path;
            }
        }

        private readonly object userOperationStatusLockObject = new object();
        private bool ComboBoxSelectionChanged = false;
        private bool UriChanged = false;
        private bool UserOperated => ComboBoxSelectionChanged | UriChanged;

        private async void UrlAnalyzer(CancellationToken token)
        {
            KeyValuePair<string, string>[] optionalKeyValues = 
                Array.Empty<KeyValuePair<string, string>>();
            IMultiTargetsExtracted multiTargetsExtractedSource = null;
            while (!token.IsCancellationRequested)
            {
                bool operated = false;
                bool uriOperated = false;
                bool selectionOperated = false;
                lock (userOperationStatusLockObject)
                {
                    uriOperated = UriChanged;
                    selectionOperated = ComboBoxSelectionChanged;
                    operated = uriOperated | selectionOperated;
                    if (operated) UriChanged = ComboBoxSelectionChanged = false;
                }

                if (!operated)
                {
                    await Task.Delay(200);
                    continue;
                }
                
                if (!uriOperated)
                {
                    if (selectionOperated)
                    {
                        ItemIndexRange[] selectedRanges = null;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            selectedRanges = StreamSelectionListView.SelectedRanges.ToArray();
                            UriAnalyzingProgressBar.IsIndeterminate = true;
                            SuggestedFilenameTextBox.Text = UnknownText;
                            AcceptButton.IsEnabled = false;
                        });

                        try
                        {
                            // check if selection legal
                            Ensure.That(selectedRanges).IsNotNull();
                            Ensure.That(selectedRanges.Length).IsGt(0);
                            Ensure.That(multiTargetsExtractedSource).IsNotNull();
                            // handle new selection
                            var keysList = new List<string>();
                            foreach (var range in selectedRanges)
                                for (int i = range.FirstIndex; i <= range.LastIndex; ++i)
                                    keysList.Add(optionalKeyValues[i].Key);
                            Ensure.That(UserOperated).IsFalse();
                            var target = await multiTargetsExtractedSource.GetTargetAsync(keysList);
                            Ensure.That(UserOperated).IsFalse();
                            FinalTarget = target;
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                                SuggestedFilenameTextBox.Text = FinalTarget.SuggestedName;
                                AcceptButton.IsEnabled = true;
                            });
                            Ensure.That(UserOperated).IsFalse();
                        }
                        catch (Exception)
                        {
                            FinalTarget = null;
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                                SuggestedFilenameTextBox.Text = UnknownText;
                                AcceptButton.IsEnabled = false;
                            });
                        }
                        finally
                        {
                            // end processing
                            if (!UserOperated)
                            {
                                await UriAnalyzingProgressBar.Dispatcher.RunAsync(
                                    CoreDispatcherPriority.Normal,
                                    () => UriAnalyzingProgressBar.IsIndeterminate = false);
                            }
                        }
                    }
                }
                else
                {
                    string nowUri = string.Empty;
                    optionalKeyValues = Array.Empty<KeyValuePair<string, string>>();
                    multiTargetsExtractedSource = null;

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        nowUri = MainURITextBox.Text;
                        UriAnalyzingProgressBar.IsIndeterminate = true;
                        SuggestedFilenameTextBox.Text = UnknownText;
                        StreamSelectionStackPanel.Visibility = Visibility.Collapsed;
                        AcceptButton.IsEnabled = false;
                    });

                    try
                    {
                        var source = AbstractSource.ConstructSource(new Uri(nowUri));
                        while (true)
                        {
                            Ensure.That(UserOperated).IsFalse();
                            if (source is ISingleSubsourceExtracted singleSubsourceExtracted)
                            {
                                source = await singleSubsourceExtracted.GetSubsourceAsync();
                                Ensure.That(UserOperated).IsFalse();
                                continue;
                            }
                            else if (source is IMultiTargetsExtracted multiSubsourcesExtracted)
                            {
                                multiTargetsExtractedSource = multiSubsourcesExtracted;
                                var infos = await multiSubsourcesExtracted.GetTargetInfosAsync();
                                Ensure.That(UserOperated).IsFalse();
                                optionalKeyValues = infos.ToArray();
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    StreamSelectionListView.SelectionMode =
                                        multiSubsourcesExtracted.IsMultiSelectionSupported ?
                                        ListViewSelectionMode.Multiple : ListViewSelectionMode.Single;
                                    StreamSelectionListView.ItemsSource = infos.Select(
                                        kvp => kvp.Value).ToArray();
                                    StreamSelectionStackPanel.Visibility = Visibility.Visible;
                                });
                                break;
                            }
                            else if (source is ISingleTargetExtracted singleTargetExtracted)
                            {
                                var target = await singleTargetExtracted.GetTargetAsync();
                                Ensure.That(UserOperated).IsFalse();
                                FinalTarget = target;
                                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                {
                                    SuggestedFilenameTextBox.Text = FinalTarget.SuggestedName;
                                    AcceptButton.IsEnabled = true;
                                });
                                break;
                            }
                            else break;
                        }
                    }
                    catch (Exception)
                    {
                        FinalTarget = null;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            SuggestedFilenameTextBox.Text = UnknownText;
                            AcceptButton.IsEnabled = false;
                        });
                    }
                    finally
                    {
                        // end processing
                        if (!UserOperated)
                        {
                            await UriAnalyzingProgressBar.Dispatcher.RunAsync(
                                CoreDispatcherPriority.Normal,
                                () => UriAnalyzingProgressBar.IsIndeterminate = false);
                        }
                    }
                }
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (FinalTarget != null && TargetFolder != null)
            {
                CurrentApp.Core.CreateTask(
                    FinalTarget, 
                    TargetFolder, 
                    BackgroundAllowedToggleSwitch.IsOn,
                    DestinationFileName);
                MainPage.Current.NavigateEmptyPage();
            }
        }
    }
}
