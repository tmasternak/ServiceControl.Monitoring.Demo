using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace ShippingService
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("ShippingService");
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
                .SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", TimeSpan.FromSeconds(10), "Shipping Service");
#pragma warning restore 618

            var endpoint = await Endpoint.Start(configuration);

            Console.WriteLine("Endpoint started");
            Console.ReadKey();
        }
    }

    public class ShippingSaga : Saga<ShippingData>,
        IAmStartedByMessages<PaymentReceived>,
        IHandleMessages<StockReserved>
    {
        public async Task Handle(PaymentReceived message, IMessageHandlerContext context)
        {
            Console.WriteLine("Reserving stock");

            await context.Send(new ReserveStock { });

            await context.Send(new SendEmail());
        }

        public async Task Handle(StockReserved message, IMessageHandlerContext context)
        {
            if (Data.NumberOfOutstandingItems-- == 0)
            {
                await context.Publish(new OrderStockReserved { OrderId = Data.OrderId });
                await context.Send(new SendEmail());

                MarkAsComplete();
            }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ShippingData> mapper)
        {
            mapper.ConfigureMapping<PaymentReceived>(payment => payment.OrderId).ToSaga(saga => saga.OrderId);
        }
    }
}