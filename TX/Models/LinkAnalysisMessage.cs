﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Models
{
    /// <summary>
    /// 对链接进行分析后，返回的单条消息内容
    /// </summary>
    public class LinkAnalysisMessage
    {
        public LinkAnalysisMessage(string message)
        {
            Message = message;
        }

        /// <summary>
        /// 消息包括的文本
        /// </summary>
        public string Message { get; set; } = "";
    }
}