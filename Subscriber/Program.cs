using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace PaymentProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("PaymentProcessor");
            var transportConfiguration = configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");
            configuration.Recoverability().Immediate(a => a.NumberOfRetries(1));
            configuration.Recoverability().Delayed(a => a.NumberOfRetries(0));

            transportConfiguration.Routing().RegisterPublisher(typeof(OrderPlaced), "OrderService");
            transportConfiguration.Routing().RouteToEndpoint(typeof(SendEmail), "Emailer");

            configuration.RegisterComponents(c => c.RegisterSingleton(new ProgressReporter()));

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

    public class SampleHandler : IHandleMessages<OrderPlaced>
    {
        static long numberOfMessages;
        TimeSpan processingDelay;
        bool failRandomly;
        ProgressReporter progressReporter;

        public SampleHandler(ProgressReporter progressReporter)
        {
            this.progressReporter = progressReporter;

            processingDelay = TimeSpan.Parse(ConfigurationManager.AppSettings["processingDelay"]);
            failRandomly = bool.Parse(ConfigurationManager.AppSettings["failRandomly"]);
        }

        public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
        {
            var messageNo = Interlocked.Increment(ref numberOfMessages);

            progressReporter.Record(messageNo);

            if (failRandomly && new Random().Next(5) == 1)
            {
                throw new Exception("Boom!");
            }

            await Task.Delay(processingDelay);

            await context.Publish(new PaymentReceived
            {
                OrderId = message.OrderId
            });

            await context.Send(new SendEmail());
        }
    }

    public class ProgressReporter
    {
        Task task;
        long messageNo;

        public ProgressReporter()
        {
            task = Task.Run(async () =>
            {
                while (true)
                {
                    Console.Write($"Event handled. No: {messageNo}.");

                    Console.SetCursorPosition(0, Console.CursorTop);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
        }

        public void Record(long messageNo)
        {
            this.messageNo = messageNo;
        }
    }
}
