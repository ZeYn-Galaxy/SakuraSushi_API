using System;
using System.Collections.Generic;

namespace SakuraSushi_API.DataContext;

public partial class Table
{
    public Guid Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
