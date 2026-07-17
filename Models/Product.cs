using System;
using System.Collections.Generic;

namespace WebGoiY.Models;

public partial class Product
{
    public string ProductId { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImagePath { get; set; }

    public string? CategoryId { get; set; }

    public int? IsHot { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
