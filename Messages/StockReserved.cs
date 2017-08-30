using NServiceBus;
using System;

namespace Messages
{
    public class StockReserved : IEvent
    {
        public Guid OrderId { get; set; }
    }
}
