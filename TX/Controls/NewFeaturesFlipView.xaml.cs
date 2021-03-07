using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace TX.Controls
{
    public sealed partial class NewFeaturesFlipView : UserControl
    {
        private bool viewed = false;
        private readonly NewFeature[] Features = Array.Empty<NewFeature>();

        public NewFeaturesFlipView(NewFeature[] features)
        {
            this.InitializeComponent();
            MainFlipView.ItemsSource = Features = features;
            NewFeaturesPanelThemeShadow.Receivers.Add(NewFeaturesFlipViewBackgroundMask);
            MainFlipView.Translation = new System.Numerics.Vector3(0, 0, 20);
        }

        /// <summary>
        /// Fired after the flipview is closed by user.
        /// </summary>
        public Action Disposed = delegate { };

        private void MainFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewed == false &&
                sender is FlipView flipView && 
                flipView.SelectedItem != null &&
                Features != null &&
                object.Equals(flipView.SelectedItem, Features.LastOrDefault()))
            {
                // flipped to the end
                viewed = true;
                GuideTextAppear.Begin();
            }
        }

        private void NewFeaturesFlipViewBackgroundMask_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (viewed)
                HideUserControl.Begin();
        }

        private void HideUserControl_Completed(object sender, object e)
        {
            Visibility = Visibility.Collapsed;
            if (Parent is Panel panel) panel.Children.Remove(this);
            else if (Parent is Border border) border.Child = null;
            else if (Parent is ContentControl contentControl) contentControl.Content = null;
            else if (Parent is ItemsControl itemsControl) itemsControl.Items.Remove(this);
            Disposed();
        }
    }

    public class NewFeature
    {
        public ImageSource Hero { get; set; }

        public string Title { get; set; }

        public string GuideText { get; set; }
    }
}
