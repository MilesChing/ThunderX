using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TX.Core.Downloaders;
using TX.Core.Models.Contexts;
using TX.Core.Models.Sources;
using TX.Core.Models.Targets;
using UnitTestProject.Utils;
using Windows.Storage;

namespace UnitTestProject.CoreTest.DownloaderTest
{
    [TestClass]
    public class HttpDownloaderTest
    {
        [TestMethod]
        public void Test100kB() => TestWithSize(100L * 1024L).Wait();

        [TestMethod]
        public void Test10MB() => TestWithSize(10L * 1024L * 1024L).Wait();

        [TestMethod]
        public void Test100MB() => TestWithSize(100L * 1024L * 1024L).Wait();

        [TestMethod]
        public void Test500MB() => TestWithSize(500L * 1024L * 1024L).Wait();

        private async Task TestWithSize(long size)
        {
            var rd = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
            var targetBytes = new byte[size];
            rd.NextBytes(targetBytes);
            int port = 8080;

            using (var server = new FakeHttpServer(port, targetBytes))
            {
                var folderProvider = new FakeFolderProvider();
                var cacheProvider = new FakeCacheProvider();
                var bufferProvider = new FakeBufferProvider();
                var downloader = new HttpDownloader(
                    new DownloadTask(
                        "test-task",
                        new HttpTarget(
                            new Uri($"http://localhost:{port}/"),
                            "test-file"
                        ),
                        "test-destination-file",
                        "test-destination-folder",
                        DateTime.Now
                    ),
                    folderProvider,
                    cacheProvider,
                    bufferProvider
                );

                downloader.Start();

                TimeSpan cancellationStep = TimeSpan.FromSeconds(1);
                CancellationTokenSource completeTokenSource = new CancellationTokenSource();

                downloader.StatusChanged += async (sender, newStatus) =>
                {
                    switch (downloader.Status)
                    {
                        case DownloaderStatus.Running:
                            var step = cancellationStep;
                            cancellationStep *= 2;
                            await Task.Delay(step);
                            if (downloader.CanCancel)
                                downloader.Cancel();
                            break;
                        case DownloaderStatus.Ready:
                            downloader.Start();
                            break;
                        case DownloaderStatus.Completed:
                            completeTokenSource.Cancel();
                            break;
                        case DownloaderStatus.Pending:
                            break;
                        case DownloaderStatus.Disposed:
                            break;
                        case DownloaderStatus.Error:
                            throw downloader.Errors.First();
                        default:
                            throw new Exception($"unknown status {downloader.Status}");
                    }
                };

                try 
                { 
                    await Task.Delay(TimeSpan.FromMinutes(5), completeTokenSource.Token); 
                } 
                catch (TaskCanceledException) { }

                Assert.AreEqual(downloader.Status, DownloaderStatus.Completed);

                using (var istream = await downloader.Result.OpenReadAsync())
                {
                    using (var ostream = new MemoryStream(targetBytes))
                    {
                        Assert.IsTrue(istream.AsStream().Compare(ostream));
                    }
                }

                await downloader.Result.DeleteAsync();
                downloader.Dispose();
            }
        }
    }
}
