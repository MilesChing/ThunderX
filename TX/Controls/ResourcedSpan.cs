using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Resources.Strings;
using TX.Utils;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace TX.Controls
{
    class ResourcedSpan : Span
    {
        public string InlineXamlUid
        {
            get { return (string)GetValue(InlineXamlUidProperty); }
            set { SetValue(InlineXamlUidProperty, value); }
        }

        public static readonly DependencyProperty InlineXamlUidProperty = DependencyProperty.Register(
            nameof(InlineXamlUid), typeof(string), typeof(ResourcedSpan), 
            new PropertyMetadata(string.Empty, InlineXamlUidChanged));

        private static void InlineXamlUidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResourcedSpan tis && e.NewValue is string newXamlUidString)
            {
                try
                {
                    tis.Inlines.Clear();
                    var xamlString = Loader.Get(newXamlUidString);
                    if (!string.IsNullOrEmpty(xamlString))
                    {
                        var spanXamlString = $"<Span xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{xamlString}</Span>";
                        var span = XamlReader.Load(spanXamlString) as Span;
                        if (span != null) tis.Inlines.Add(span);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
