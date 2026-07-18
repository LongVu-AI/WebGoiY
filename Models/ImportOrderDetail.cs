using System;
using System.Collections.Generic;

namespace WebGoiY.Models;

public partial class ImportOrderDetail
{
    public int ImportDetailId { get; set; }

    public int? ImportId { get; set; }

    public string? ProductId { get; set; }

    public int ImportQuantity { get; set; }

    public decimal ImportPrice { get; set; }

    public virtual ImportOrder? Import { get; set; }

    public virtual Product? Product { get; set; }
}
