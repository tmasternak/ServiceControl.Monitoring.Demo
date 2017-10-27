using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace SubscriberV7
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("Subscriber");
            var transportConfiguration = configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");

            transportConfiguration.Routing().RegisterPublisher(typeof(SampleEvent), "Publisher");

            configuration.RegisterComponents(c => c.RegisterSingleton(new ProgressReporter()));

            configuration.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Namespace == "Messages");

            var instanceId = ConfigurationManager.AppSettings["instanceId"];

            Console.Title = $"{instanceId}";

            configuration.EnableMetrics()
                .SendMetricDataToServiceControl("Particular.Monitoring", TimeSpan.FromMilliseconds(100), instanceId);

            await Endpoint.Start(configuration);

            Console.WriteLine("Endpoint started");
            Console.ReadKey();
        }
    }

    public class SampleHandler : IHandleMessages<SampleEvent>, IHandleMessages<YetAnotherEvent>
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

        public async Task Handle(SampleEvent message, IMessageHandlerContext context)
        {
            var messageNo = Interlocked.Increment(ref numberOfMessages);

            progressReporter.Record(messageNo);

            if (failRandomly && new Random().Next(5) == 1)
            {
                throw new Exception("Boom!");
            }

            await Task.Delay(processingDelay);
        }

        public Task Handle(YetAnotherEvent message, IMessageHandlerContext context)
        {
            return Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
