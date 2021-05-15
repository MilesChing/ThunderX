using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TX.Resources.Strings;
using TX.Utils;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace TX.Controls
{
    public class ObservableSpanTemplateCollection : ObservableCollection<SubSpanStyle> { }

    public class SubSpanStyle : DependencyObject
    {
        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public Brush Foreground 
        {
            get { return GetValue(ForegroundProperty) as Brush; }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(SubSpanStyle),
            new PropertyMetadata(0.0, OnPropertyChanged));

        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(
            nameof(FontStretch), typeof(FontStretch), typeof(SubSpanStyle),
            new PropertyMetadata(FontStretch.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
            nameof(FontWeight), typeof(FontWeight), typeof(SubSpanStyle),
            new PropertyMetadata(FontWeights.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
            nameof(FontStyle), typeof(FontStyle), typeof(SubSpanStyle),
            new PropertyMetadata(FontStyle.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
            nameof(FontFamily), typeof(FontFamily), typeof(SubSpanStyle),
            new PropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            nameof(Foreground), typeof(Brush), typeof(SubSpanStyle),
            new PropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(SubSpanStyle),
            new PropertyMetadata(string.Empty, OnPropertyChanged));

        public event PropertyChangedCallback PropertyChanged;

        public void Apply(TextElement elem)
        {
            if (FontSize > 0.0) elem.FontSize = FontSize;
            elem.FontStretch = FontStretch;
            elem.FontWeight = FontWeight;
            elem.FontStyle = FontStyle;
            if (FontFamily != null) elem.FontFamily = FontFamily;
            if (Foreground != null) elem.Foreground = Foreground;
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SubSpanStyle tis)
            {
                tis.PropertyChanged?.Invoke(d, e);
            }
        }
    }

    public class ResourcedSpan : Span
    {
        public ResourcedSpan()
        {
            var templates = new ObservableCollection<SubSpanStyle>();
            SetValue(StylesProperty, templates);
        }

        public string InlineXamlUid
        {
            get { return (string)GetValue(InlineXamlUidProperty); }
            set { SetValue(InlineXamlUidProperty, value); }
        }

        public IList<SubSpanStyle> Styles
        {
            get { return (IList<SubSpanStyle>)GetValue(StylesProperty); }
            set { SetValue(StylesProperty, value); }
        }

        public static readonly DependencyProperty InlineXamlUidProperty = DependencyProperty.Register(
            nameof(InlineXamlUid), typeof(string), typeof(ResourcedSpan), 
            new PropertyMetadata(string.Empty, InlineXamlUidChanged));

        public static readonly DependencyProperty StylesProperty = DependencyProperty.Register(
            nameof(Styles), typeof(IList<SubSpanStyle>), typeof(ResourcedSpan),
            new PropertyMetadata(null, StylesChanged));

        private static void InlineXamlUidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResourcedSpan tis && e.NewValue is string newXamlUidString)
            {
                try
                {
                    tis.Inlines.Clear();
                    GenerateSpan(tis, newXamlUidString, tis.Styles);
                }
                catch (Exception) { }
            }
        }

        private static void StylesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResourcedSpan tis && e.NewValue is IEnumerable<SubSpanStyle> newCollection)
            {
                var oldCollection = e.OldValue;
                if (oldCollection is ObservableCollection<Inline> oldOc)
                    oldOc.CollectionChanged -= tis.TemplateCollectionChanged;
                if (newCollection is ObservableCollection<Inline> newOc)
                    newOc.CollectionChanged += tis.TemplateCollectionChanged;
                GenerateSpan(tis, tis.InlineXamlUid, newCollection);
            }
        }

        private void TemplateCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            GenerateSpan(this, InlineXamlUid, Styles);

        private static void GenerateSpan(ResourcedSpan tis, string xamlUidString, IEnumerable<SubSpanStyle> templates)
        {
            try
            {
                var xamlString = Loader.Get(xamlUidString);
                var spanXamlString = $"<Span xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                    $"xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{xamlString}</Span>";
                spanXamlString = ConvertNames(spanXamlString, templates);
                var span = XamlReader.Load(spanXamlString) as Span;
                ApplyTemplates(span, templates);
                tis.Inlines.Clear();
                tis.Inlines.Add(span);
            }
            catch(Exception) { }
        }

        private static string ConvertNames(string spanXamlString, IEnumerable<SubSpanStyle> styles)
        {
            spanXamlString = spanXamlString.Replace("\n", "<LineBreak/>");

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var style in styles)
                {
                    var res = Regex.Replace(spanXamlString,
                        @"\{(" + style.Key + @") ([^\{\}]*)\}",
                        $"<Run x:Name=\"$1\">$2</Run>");
                    changed |= (res != spanXamlString);
                    spanXamlString = res;
                }
            }

            return spanXamlString;
        }

        private static void ApplyTemplates(Inline inline, IEnumerable<SubSpanStyle> styles)
        {
            if (inline is Span span)
                foreach (var sub in span.Inlines)
                    ApplyTemplates(sub, styles);
            else
            {
                var targetStyle = styles.FirstOrDefault(
                    style => style.Key.Equals(inline.Name));
                if (targetStyle != null)
                {
                    targetStyle.PropertyChanged += (sender, e) =>
                        targetStyle.Apply(inline);
                    targetStyle.Apply(inline);
                }
            }
        }
    }
}
