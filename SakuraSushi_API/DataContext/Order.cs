using System;
using System.Collections.Generic;

namespace SakuraSushi_API.DataContext;

public partial class Order
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public DateTimeOffset OrderedAt { get; set; }

    public decimal Amount { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Transaction Transaction { get; set; } = null!;
}
