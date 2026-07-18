using System;
using System.Collections.Generic;

namespace WebGoiY.Models;

public partial class ImportOrder
{
    public int ImportId { get; set; }

    public string? SupplierId { get; set; }

    public DateTime? ImportDate { get; set; }

    public decimal? TotalCost { get; set; }

    public virtual ICollection<ImportOrderDetail> ImportOrderDetails { get; set; } = new List<ImportOrderDetail>();

    public virtual Supplier? Supplier { get; set; }
}
