using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var configuration = new EndpointConfiguration("Publisher");
            configuration.UseTransport<MsmqTransport>();
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.SendFailedMessagesTo("error");

            configuration.Conventions().DefiningEventsAs(t => t.Namespace != null && t.Namespace == "Messages");

#pragma warning disable 618
            configuration.EnableMetrics()
                .SendMetricDataToServiceControl("Particular.Monitoring", TimeSpan.FromSeconds(10));
#pragma warning restore 618

            var endpoint = await Endpoint.Start(configuration);

            while (true)
            {
                var batchSize = 1000;
                Console.WriteLine($"Press any <b> to publish {batchSize} events or any <key> to publish 1 event.");

                if (Console.ReadKey().KeyChar == 'b')
                {
                    await Task.WhenAll(Enumerable.Range(1, batchSize).Select(_ =>
                    {
                        return Task.WhenAll(
                            endpoint.Publish<SampleEvent>(se => { }),
                            endpoint.Publish<YetAnotherEvent>(ae => { }));
                    }));

                    Console.WriteLine("Events sent");
                }
                else
                {
                    await endpoint.Publish<SampleEvent>(se => { });
                    await endpoint.Publish<YetAnotherEvent>(ae => { });
                }
            }
        }
    }
}
