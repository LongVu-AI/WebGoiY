using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class ImportOrderDetail
{
    [Column("import_detail_id")]
    public int ImportDetailId { get; set; }

    [Column("import_id")]
    public int? ImportId { get; set; }

    [Column("product_id")]
    public string? ProductId { get; set; }

    [Column("import_quantity")]
    public int ImportQuantity { get; set; }

    [Column("import_price")]
    public decimal ImportPrice { get; set; }

    public virtual ImportOrder? Import { get; set; }

    public virtual Product? Product { get; set; }
}