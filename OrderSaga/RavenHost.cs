using Raven.Client.Embedded;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSaga
{
    class RavenHost : IDisposable
    {
        public RavenHost()
        {
            documentStore = new EmbeddableDocumentStore
            {
                DataDirectory = "Data",
                UseEmbeddedHttpServer = true,
                DefaultDatabase = "OrderSaga",
                Configuration =
                {
                    Port = 32075,
                    PluginsDirectory = Environment.CurrentDirectory,
                    HostName = "localhost",
                    DefaultStorageTypeName = "esent"
                }
            };
            documentStore.Initialize();
            // since hosting a fake raven server in process remove it from the logging pipeline
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Console.WriteLine("Raven server started on http://localhost:32075/");
        }

        public EmbeddableDocumentStore documentStore;

        public void Dispose()
        {
            documentStore?.Dispose();
        }
    }
}
