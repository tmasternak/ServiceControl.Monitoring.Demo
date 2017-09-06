using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Metrics.ServiceControl;

namespace SubscriberV5
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Subscriber V5";

            var configuration = new BusConfiguration();
            configuration.EndpointName("Subscriber");
            configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.EnableInstallers();

            configuration.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Namespace == "Messages");

            configuration.RegisterComponents(c => c.RegisterSingleton(new ProgressReporter()));

            configuration.SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", "Subscriber.V5");

            using (Bus.Create(configuration).Start())
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }


        public class Handler : IHandleMessages<SampleEvent>, IHandleMessages<YetAnotherEvent>
        {
            readonly ProgressReporter progressReporter;

            static ILog log = LogManager.GetLogger<Handler>();
            static long numberOfMessages;

            public Handler(ProgressReporter progressReporter)
            {
                this.progressReporter = progressReporter;
            }

            public void Handle(SampleEvent message)
            {
                var messageNo = Interlocked.Increment(ref numberOfMessages);

                progressReporter.Record(messageNo);

                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            public void Handle(YetAnotherEvent message)
            {
                Thread.Sleep((int)TimeSpan.FromSeconds(3).TotalMilliseconds);
            }
        }
    }
}
