using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace UnitTestProject.Utils
{
    class FakeHttpServer : IDisposable
    {
        public FakeHttpServer(int port, byte[] response)
        {
            Port = port;
            ResponseContent = response;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            listener.Start();
            serverTask = Task.Run(() =>
            {
                while (listener != null)
                {
                    if (listener.IsListening)
                    {
                        try
                        {
                            var context = listener.GetContext();
                            Task.Run(() => ProcessRequest(context));
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            long begin = 0, end = ResponseContent.Length;
            var rangeString = context.Request.Headers["range"];
            if (rangeString != null && rangeString != string.Empty)
            {
                var fromString = Regex.Replace(rangeString, "(bytes=)|(-[0-9]+)", "");
                var toString = Regex.Replace(rangeString, "(bytes=)|([0-9]+-)", "");
                if (long.TryParse(fromString, out long from) &&
                    long.TryParse(toString, out long to))
                {
                    begin = from;
                    end = to + 1;
                }
            }

            HttpListenerResponse response = context.Response;
            var output = response.OutputStream;
            try
            {
                output.Write(ResponseContent, (int)begin, (int)(end - begin));
            }
            catch (Exception) { }
            finally { output.Close(); }
        }

        public int Port { get; private set; }

        public byte[] ResponseContent { get; private set; }

        public void Dispose()
        {
            listener?.Close();
            listener = null;
            serverTask?.Wait();
            serverTask = null;
            ResponseContent = null;
        }

        private HttpListener listener = null;
        private Task serverTask = null;
    }
}
