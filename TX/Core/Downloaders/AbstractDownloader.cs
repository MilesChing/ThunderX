using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Models;
using TX.Core.Models.Progresses;
using TX.Core.Models.Contexts;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using TX.Core.Interfaces;
using EnsureThat;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Downloaders
{
    public abstract class AbstractDownloader
    {
        /// <summary>
        /// Initialize the downloader with given task. 
        /// Each downloader could only be assigned with one task in its lifecycle.
        /// </summary>
        /// <param name="task">Task to be downloaded.</param>
        /// <param name="context">Context of the downloader.</param>
        public AbstractDownloader(DownloadTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task) + " must not be null.");
            DownloadTask = task;
        }

        /// <summary>
        /// Progress of the downloader.
        /// </summary>
        public IProgress Progress { get; protected set; }

        /// <summary>
        /// Speed of the downloader.
        /// </summary>
        public SpeedCalculator Speed { get; protected set; }

        /// <summary>
        /// Task to be downloaded.
        /// </summary>
        public DownloadTask DownloadTask { get; private set; }

        /// <summary>
        /// Status of the downloader.
        /// </summary>
        public DownloaderStatus Status
        {
            get => _status_;
            private set
            {
                if(_status_ != value)
                {
                    D($"Status changed {_status_} -> {value}, thread ID <{Thread.CurrentThread.ManagedThreadId}>");
                    _status_ = value;
                    var changedHandlerChain = StatusChanged;
                    if (changedHandlerChain != null)
                    {
                        foreach (Action<AbstractDownloader, DownloaderStatus> handler
                            in changedHandlerChain.GetInvocationList())
                            try { handler(this, value); } catch (Exception) { }
                    }
                }
            }
        }
        private DownloaderStatus _status_ = DownloaderStatus.Ready;

        /// <summary>
        /// StatusChanged occurs when status is changed.
        /// </summary>
        public event Action<AbstractDownloader, DownloaderStatus> StatusChanged;

        /// <summary>
        /// Start downloading.
        /// </summary>
        public void Start()
        {
            lock (statusLockObject)
                if (CanStart) Status = DownloaderStatus.Pending;
                else return;
            D($"Start() called");
            new Task(async () =>
            {
                try
                {
                    await HandleStartAsync();
                    lock (statusLockObject)
                        Status = DownloaderStatus.Running;
                } 
                catch (Exception e) 
                {
                    D($"Start() failed: {e.Message}");
                    exceptions.Insert(0, e);
                    lock (statusLockObject)
                        Status = DownloaderStatus.Error;
                }
            }).RunSynchronously();
        }

        /// <summary>
        /// StartAsync must be defined by downloader to handle starting.
        /// </summary>
        /// <returns>Starting task.</returns>
        protected abstract Task HandleStartAsync();

        /// <summary>
        /// Returns wether Start() is supported currently.
        /// </summary>
        public bool CanStart => (
                Status == DownloaderStatus.Ready ||
                Status == DownloaderStatus.Error
            );

        /// <summary>
        /// Cancel downloading.
        /// </summary>
        public void Cancel()
        {
            lock (statusLockObject)
                if (CanCancel) Status = DownloaderStatus.Pending;
                else return;
            D($"Cancel() called");
            new Task(async () =>
            {
                try
                {
                    await HandleCancelAsync();
                    lock (statusLockObject)
                        Status = DownloaderStatus.Ready;
                }
                catch (Exception e)
                {
                    D($"Cancel() failed: {e.Message}");
                    exceptions.Insert(0, e);
                    lock (statusLockObject)
                        Status = DownloaderStatus.Error;
                }
            }).RunSynchronously();
        }

        /// <summary>
        /// CancelAsync must be defined by downloader to handle cancelation.
        /// </summary>
        /// <returns>Cancelation task.</returns>
        protected abstract Task HandleCancelAsync();

        /// <summary>
        /// Returns wether Cancel() is supported currently.
        /// </summary>
        public bool CanCancel => (
                Status == DownloaderStatus.Running
            );

        /// <summary>
        /// DisposeAsync() always change status to Disposed.
        /// </summary>
        public async Task DisposeAsync()
        {
            // temptation to dispose the downloader should always work
            D($"Dispose() called");
            // dispose method loops to check and modify the downloader
            // until its status turns disposable.
            while (true)
            {
                bool needSleep = false;
                DownloaderStatus oldStatus;
                lock (statusLockObject)
                {
                    oldStatus = Status;
                    if (Status == DownloaderStatus.Pending) needSleep = true;
                    else Status = DownloaderStatus.Pending;
                }

                if (needSleep)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (oldStatus == DownloaderStatus.Completed ||
                    oldStatus == DownloaderStatus.Error ||
                    oldStatus == DownloaderStatus.Ready)
                {
                    // if the downloader is not running or pending, 
                    // call inner disposal method directly
                    D($"Disposing");
                    try { await HandleDisposeAsync(); }
                    catch (Exception e)
                    {
                        D($"Dispose() (disposing) failed: {e.Message}");
                    }

                    // a disposed downloader should not enter other statuses again,
                    // enter disposed status regardless of whatever happened to disposal process.
                    // throw the problem to garbage collector
                    Status = DownloaderStatus.Disposed;
                    return;
                }
                // the downloader is running, try to cancel it first
                else if (oldStatus == DownloaderStatus.Running)
                {
                    try 
                    {
                        await HandleCancelAsync();
                        Status = DownloaderStatus.Ready;
                    }
                    catch (Exception e)
                    {
                        // cancelation failed, enter error status directly to complete disposal
                        // during next loop
                        D($"Cancel() (disposing) failed: {e.Message}");
                        exceptions.Insert(0, e);
                        Status = DownloaderStatus.Error;
                    }
                }
            }
        }

        /// <summary>
        /// DisposeAsync must be defined by downloader to handle disposal.
        /// </summary>
        /// <returns>Disposal task.</returns>
        protected abstract Task HandleDisposeAsync();

        /// <summary>
        /// ReportError should be called when downloading failed,
        /// it will handle the error and retry automatically if required.
        /// </summary>
        /// <param name="exception">Exception of downloading.</param>
        /// <param name="dontRetry">Set false to suggest downloader retry.</param>
        protected async Task ReportErrorAsync(Exception exception, bool dontRetry = true)
        {
            lock (statusLockObject)
                if (Status != DownloaderStatus.Pending)
                    Status = DownloaderStatus.Pending;
                else return;
            // restore the exception
            exceptions.Insert(0, exception);
            D($"Error captured: <{exception.GetType().Name}> (total exceptions: {Errors.Count})");

            // for some specific cases (failed during cancelation, failed during completetion, etc.),
            // do not retry, enter error status directly.
            if (dontRetry)
            {
                D($"No need to retry, abort");
                lock (statusLockObject) Status = DownloaderStatus.Error;
            }
            // if number of retries exceeded the maximum retries, enter error status directly.
            // TODO: wrap maximum retries configuration in DownloaderContext
            else if(Retries >= MaximumRetries)
            {
                D($"Retries exceeded, abort (total retries: {Retries})");
                lock (statusLockObject) Status = DownloaderStatus.Error;
            }
            // else retry after 1000 thousands
            // TODO: wrap 1000 in DownloaderContext (retring period)
            else
            {
                try
                {
                    // simply cancel, wait for a period, and restart the downloader
                    Retries += 1;
                    D($"Retring in {1000} ms (total retries: {Retries})");
                    lock (statusLockObject)
                        Status = DownloaderStatus.Running;
                    Cancel();
                    lock (statusLockObject)
                        Status = DownloaderStatus.Pending;
                    await Task.Delay(1000);
                    lock (statusLockObject)
                        Status = DownloaderStatus.Ready;
                    Start();
                }
                catch (Exception e)
                {
                    // if exceptions occur during the retry, enter error status directly
                    exceptions.Insert(0, e);
                    lock (statusLockObject)
                        Status = DownloaderStatus.Error;
                }
            }
        }

        /// <summary>
        /// ReportCompleted should be called when downloading completed.
        /// </summary>
        /// <param name="file">Downloaded file.</param>
        protected void ReportCompleted(IStorageItem file)
        {
            lock (statusLockObject)
                if (Status != DownloaderStatus.Pending &&
                    Status != DownloaderStatus.Disposed &&
                    Status != DownloaderStatus.Completed)
                    Status = DownloaderStatus.Pending;
                else return;
            // set final file to Result
            Result = file;
            lock (statusLockObject)
                Status = DownloaderStatus.Completed;
        }

        /// <summary>
        /// Maximum retries automatically performed before
        /// changing status to error.
        /// </summary>
        public int MaximumRetries
        {
            get => _maximumRetries_;
            set
            {
                Ensure.That(value, nameof(MaximumRetries)).IsGte(0);
                _maximumRetries_ = value;
            }
        }
        private int _maximumRetries_ = 4;

        /// <summary>
        /// Result file downloaded, only valid when status is completed. 
        /// </summary>
        public IStorageItem Result { get; private set; } = null;

        /// <summary>
        /// Errors occured since the downloader was initialized.
        /// </summary>
        public IReadOnlyList<Exception> Errors => exceptions;
        private readonly List<Exception> exceptions = new List<Exception>();

        /// <summary>
        /// Amount of retries since the downloader was constructed.
        /// </summary>
        public int Retries { get; private set; } = 0;

        protected virtual void D(string message) => 
            Debug.WriteLine($"[{GetType().Name}] [task <{DownloadTask.Key}>] {message}");

        private readonly object statusLockObject = new object();
    }

    public enum DownloaderStatus
    {
        Pending,
        Ready,
        Running,
        Error,
        Completed,
        Disposed
    }
}
