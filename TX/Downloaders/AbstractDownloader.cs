﻿using System;
using TX.Enums;
using TX.Models;

namespace TX.Downloaders
{
    public abstract class AbstractDownloader : IDisposable
    {
        /// <summary>
        /// 当前下载器状态
        /// </summary>
        public DownloadState State {
            get { return _state_; }
            protected set
            {
                _state_ = value;
                StateChanged?.Invoke(_state_);
            }
        }
        private DownloadState _state_;

        /// <summary>
        /// 下载任务有关的信息
        /// </summary>
        public DownloaderMessage Message { get; protected set; }

        /// <summary>
        /// 事件，下载进度变化，参数为已下载字节数
        /// </summary>
        public abstract event Action<Progress> DownloadProgressChanged;

        /// <summary>
        /// 事件，指示下载已结束
        /// </summary>
        public abstract event Action<DownloaderMessage> DownloadComplete;

        /// <summary>
        /// 事件，指示下载发生错误
        /// </summary>
        public abstract event Action<Exception> DownloadError;

        /// <summary>
        /// 事件，指示外部控件下载状态变化（由第三方对下载器进行了更改）
        /// </summary>
        public event Action<DownloadState> StateChanged;

        /// <summary>
        /// 暂停
        /// </summary>
        public abstract void Pause();

        /// <summary>
        /// 开始
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// 重置
        /// </summary>
        public abstract void Refresh();

        /// <summary>
        /// 使用链接重置下载器
        /// </summary>
        public abstract void SetDownloader(InitializeMessage imessage);

        /// <summary>
        /// 使用Message从断点恢复下载器
        /// </summary>
        public abstract void SetDownloaderFromBreakpoint(DownloaderMessage message);

        /// <summary>
        /// 释放一切资源
        /// </summary>
        public abstract void Dispose();
    }
}