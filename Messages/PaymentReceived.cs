using NServiceBus;
using System;

namespace Messages
{
    public class PaymentReceived : IEvent
    {
        public Guid OrderId { get; set; }
    }
}