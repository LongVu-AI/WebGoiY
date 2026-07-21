using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class Supplier
{
    [Column("supplier_id")]
    public string SupplierId { get; set; } = null!;

    [Column("supplier_name")]
    public string SupplierName { get; set; } = null!;

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    public virtual ICollection<ImportOrder> ImportOrders { get; set; } = new List<ImportOrder>();
}