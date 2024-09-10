using System;
using System.Collections.Generic;

namespace SakuraSushi_API.DataContext;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid TableId { get; set; }

    public Guid? CashierId { get; set; }

    public string UniqueCode { get; set; } = null!;

    public DateTimeOffset OpenedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User? Cashier { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Table Table { get; set; } = null!;
}
