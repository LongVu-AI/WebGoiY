using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class OrderStatusHistory
{
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("changed_at")]
    public DateTime? ChangedAt { get; set; }

    [Column("changed_by")]
    public int? ChangedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;
}