using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace OrderSite
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("OrderSite");
            var transportConfiguration = configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");

            transportConfiguration.Routing().RegisterPublisher(typeof(PaymentReceived), "PaymentProcessor");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderPlaced), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderTimedOut), "OrderSaga");
            transportConfiguration.Routing().RegisterPublisher(typeof(OrderCompleted), "OrderSaga");
            transportConfiguration.Routing().RouteToEndpoint(typeof(PlaceOrder), "OrderSaga");

#pragma warning disable 618
            configuration.EnableMetrics()
                .SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", TimeSpan.FromSeconds(10), "OrderSite");
#pragma warning restore 618

            var endpoint = await Endpoint.Start(configuration);

            var random = new Random();

            var continuousOrders = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(random.Next(15000));

                    await endpoint.Send(new PlaceOrder
                    {
                        Id = Guid.NewGuid()
                    });
                }
            });

            while (true)
            {
                var batchSize = 1000;
                Console.WriteLine($"Press any <b> to publish {batchSize} events or any <key> to publish 1 event.");

                if (Console.ReadKey().KeyChar == 'b')
                {
                    await Task.WhenAll(Enumerable.Range(1, batchSize).Select(_ => endpoint.Send<PlaceOrder>(se => { se.Id = Guid.NewGuid(); })));

                    Console.WriteLine("Events sent");
                }
                else
                {
                    await endpoint.Send<PlaceOrder>(se => { se.Id = Guid.NewGuid(); });
                }
            }
        }
    }

    public class OrderCompletedHandler : IHandleMessages<OrderCompleted>
    {
        public Task Handle(OrderCompleted message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }
}
