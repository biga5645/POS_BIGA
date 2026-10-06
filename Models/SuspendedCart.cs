using System;
using System.Collections.Generic;

namespace POS_BIGA.Models
{
    public class SuspendedCartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; }
    }

    public class SuspendedCart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string? Label { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<SuspendedCartItem> Items { get; set; } = new List<SuspendedCartItem>();
    }
}
