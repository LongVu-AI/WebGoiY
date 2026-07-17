using System;
using System.Collections.Generic;

namespace WebGoiY.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public Double? TotalPrice { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? RecipientName { get; set; }

    public string? Email { get; set; }

    public string? OrderNotes { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User? User { get; set; }
}
