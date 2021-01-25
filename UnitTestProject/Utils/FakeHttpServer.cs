using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

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
                            WaitForRequest();
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }
            });
        }

        private void WaitForRequest()
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerResponse response = context.Response;
            var output = response.OutputStream;
            output.Write(ResponseContent, 0, ResponseContent.Length);
            output.Close();
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
