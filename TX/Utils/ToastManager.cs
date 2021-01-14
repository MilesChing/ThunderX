using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace TX.Utils
{
    class ToastManager
    {
        /// <summary>
        /// Show an simple text with title and a line of text.
        /// </summary>
        public static void ShowSimpleToast(string title, string text)
        {
            //https://blog.csdn.net/xiahn1a/article/details/44999165?utm_source=blogxgwz6
            ToastTemplateType simpleType = ToastTemplateType.ToastText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(simpleType);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(title));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(text));
            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Show a notification with a title, a line of text and two buttons.
        /// On is used to open a file and the other to open a folder.
        /// </summary>
        public static async void ShowDownloadCompleteToastAsync(string title, string text, string filePath, string folderPath)
        {
            Uri uri = new Uri("ms-appx:///Resources/XMLs/toast_complete.xml");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var Xml = await XmlDocument.LoadFromFileAsync(file);

            var textElements = Xml.GetElementsByTagName("text");
            textElements[0].AppendChild(Xml.CreateTextNode(title));
            textElements[1].AppendChild(Xml.CreateTextNode(text));

            var actionElements = Xml.GetElementsByTagName("action");
            actionElements[0].Attributes.GetNamedItem("arguments").InnerText = "file$"+filePath;
            actionElements[1].Attributes.GetNamedItem("arguments").InnerText = "folder$"+folderPath;

            var toast = new ToastNotification(Xml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
