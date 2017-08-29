using Messages;
using NServiceBus;
using Raven.Client.Document;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSaga
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var raven = new RavenHost();


            var configuration = new EndpointConfiguration("OrderService");
            var routing = configuration.UseTransport<MsmqTransport>().Routing();

            var persistence = configuration.UsePersistence<RavenDBPersistence>();
            persistence.DoNotSetupDatabasePermissions();
            persistence.SetDefaultDocumentStore(raven.documentStore);

            configuration.SendFailedMessagesTo("error");

            configuration.Recoverability().Immediate(a => a.NumberOfRetries(1));
            configuration.Recoverability().Delayed(a => a.NumberOfRetries(0));

            routing.RegisterPublisher(typeof(PaymentReceived), "PaymentProcessor");
            routing.RegisterPublisher(typeof(OrderPlaced), "OrderService");
            routing.RegisterPublisher(typeof(OrderTimedOut), "OrderService");
            routing.RegisterPublisher(typeof(OrderCompleted), "OrderService");
            routing.RegisterPublisher(typeof(StockReserved), "StockService");
            routing.RouteToEndpoint(typeof(PlaceOrder), "OrderService");
            routing.RouteToEndpoint(typeof(ReserveStock), "StockService");
            routing.RouteToEndpoint(typeof(SendEmail), "Emailer");

            string instanceId = ConfigurationManager.AppSettings["instanceId"];

            Console.Title = $"{instanceId}";

#pragma warning disable 618
            configuration.EnableMetrics()
                         .SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", TimeSpan.FromSeconds(10), instanceId);
#pragma warning restore 618

            await Endpoint.Start(configuration);

            Console.WriteLine("Endpoint started");
            Console.ReadKey();
        }
    }
}
