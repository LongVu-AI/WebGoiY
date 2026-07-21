using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class Order
{
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("order_date")]
    public DateTime? OrderDate { get; set; }

    [Column("subtotal_price")]
    public decimal SubtotalPrice { get; set; }

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("shipping_fee")]
    public decimal? ShippingFee { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("shipping_address")]
    public string ShippingAddress { get; set; } = null!;

    [Column("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [Column("recipient_name")]
    public string? RecipientName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("order_notes")]
    public string? OrderNotes { get; set; }

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual User? User { get; set; }
}