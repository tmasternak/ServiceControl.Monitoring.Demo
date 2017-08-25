using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockService
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("StockService");
            var transportConfiguration = configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");

            transportConfiguration.Routing().RegisterPublisher(typeof(PaymentReceived), "PaymentProcessor");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderPlaced), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderTimedOut), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderCompleted), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(StockReserved), "StockService");
            transportConfiguration.Routing().RouteToEndpoint(typeof(PlaceOrder), "OrderSaga");
            transportConfiguration.Routing().RouteToEndpoint(typeof(ReserveStock), "StockService");
            transportConfiguration.Routing().RouteToEndpoint(typeof(SendEmail), "Emailer");

#pragma warning disable 618
            configuration.EnableMetrics()
                .SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", TimeSpan.FromSeconds(10), "Stock Service");
#pragma warning restore 618

            var endpoint = await Endpoint.Start(configuration);

            Console.WriteLine("Endpoint started");
            Console.ReadKey();
        }
    }

    public class StockService : IHandleMessages<ReserveStock>
    {
        static Random random = new Random();

        public async Task Handle(ReserveStock message, IMessageHandlerContext context)
        {
            await Task.Delay(random.Next(5000));

            await context.Publish(new StockReserved());
        }
    }
}
