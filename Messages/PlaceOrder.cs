using NServiceBus;
using System;

namespace Messages
{
    public class PlaceOrder : ICommand
    {
        public Guid Id { get; set; }
        public string Product { get; set; }
    }
}