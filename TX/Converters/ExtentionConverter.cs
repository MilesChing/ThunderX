using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Converters
{

    public class ExtentionConverter
    {
        public static IDictionary<string, string> Dictionary { get; private set; } = null;

        public static async void InitializeDictionary()
        {
            Uri uri = new Uri("ms-appx:///Resources/XMLs/content_types_2_extentions.xml");
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var lines = await FileIO.ReadLinesAsync(file);
            Dictionary = new Dictionary<string, string>();
            foreach(string line in lines)
            {
                var ext_ctt = line.Split('#');  //Extention & ContentType
                if (Dictionary.ContainsKey(ext_ctt[1]))  Dictionary[ext_ctt[1]] = Dictionary[ext_ctt[1]] + "#" + ext_ctt[0];
                else Dictionary[ext_ctt[1]] = ext_ctt[0];
            }
            System.Diagnostics.Debug.WriteLine("ExtentionDictionary已加载完成，共" + Dictionary.Count + "条");
        }
    }

}
