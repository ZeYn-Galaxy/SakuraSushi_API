using System;
using System.Collections.Generic;

namespace SakuraSushi_API.DataContext;

public partial class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
