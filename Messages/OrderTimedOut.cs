using NServiceBus;
using System;

namespace Messages
{
    public class OrderTimedOut : IEvent
    {
        public Guid OrderId { get; set; }
    }
}