using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class ImportOrder
{
    [Column("import_id")]
    public int ImportId { get; set; }

    [Column("supplier_id")]
    public string? SupplierId { get; set; }

    [Column("import_date")]
    public DateTime? ImportDate { get; set; }

    [Column("total_cost")]
    public decimal? TotalCost { get; set; }

    public virtual ICollection<ImportOrderDetail> ImportOrderDetails { get; set; } = new List<ImportOrderDetail>();

    public virtual Supplier? Supplier { get; set; }
}