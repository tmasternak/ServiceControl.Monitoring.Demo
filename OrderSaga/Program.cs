using Messages;
using NServiceBus;
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
            var configuration = new EndpointConfiguration("OrderSaga");
            var transportConfiguration = configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");

            configuration.Recoverability().Immediate(a => a.NumberOfRetries(1));
            configuration.Recoverability().Delayed(a => a.NumberOfRetries(0));

            transportConfiguration.Routing().RegisterPublisher(typeof(PaymentReceived), "PaymentProcessor");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderPlaced), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderTimedOut), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderCompleted), "OrderSaga");
            transportConfiguration.Routing().RouteToEndpoint(typeof(PlaceOrder), "OrderSaga");

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
