using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace TX.Toasts
{
    class ToastManager
    {
        /// <summary>
        /// 显示一个简单的通知，包含一个标题和一行文字
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
        /// 用于显示一个下载完成提示框，包含一个标题、一句提示语以及两个按钮。
        /// 第一个按钮用于打开文件，第二个用于打开文件夹
        /// </summary>
        /// <param name="filePath">要打开的文件目录</param>
        public static async void ShowDownloadCompleteToastAsync(string title, string text, string filePath)
        {
            Uri uri = new Uri("ms-appx:///Data/toast_complete.xml");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var Xml = await XmlDocument.LoadFromFileAsync(file);

            var textElements = Xml.GetElementsByTagName("text");
            textElements[0].AppendChild(Xml.CreateTextNode(title));
            textElements[1].AppendChild(Xml.CreateTextNode(text));

            var actionElements = Xml.GetElementsByTagName("action");
            actionElements[0].Attributes.GetNamedItem("arguments").InnerText = filePath;
            
            var toast = new ToastNotification(Xml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
