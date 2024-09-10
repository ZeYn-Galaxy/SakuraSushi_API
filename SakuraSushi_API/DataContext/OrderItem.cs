using System;
using System.Collections.Generic;

namespace SakuraSushi_API.DataContext;

public partial class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
