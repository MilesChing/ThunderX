using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    public class PlainTextMessage
    {
        public PlainTextMessage(string message)
        {
            Message = message;
        }

        /// <summary>
        /// 消息包括的文本
        /// </summary>
        public string Message { get; set; } = "";

        public bool Equals(PlainTextMessage obj)
        {
            return Message.Equals(obj.Message);
        }
    }
}
