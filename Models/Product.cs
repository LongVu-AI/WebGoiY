using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;  

namespace WebGoiY.Models;

public partial class Product
{
    [Column("product_id")]
    public string ProductId { get; set; } = null!;

    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("image_path")]
    public string? ImagePath { get; set; }

    [Column("category_id")]
    public string? CategoryId { get; set; }

    [Column("is_hot")]
    public int? IsHot { get; set; }

    [Column("physical_stock")]
    public int? PhysicalStock { get; set; }

    [Column("reserved_stock")]
    public int? ReservedStock { get; set; }

    [Column("sold")]
    public int? Sold { get; set; }

    [Column("is_active")]
    public sbyte? IsActive { get; set; }

    //  Thuộc tính tự động tính tồn kho khả dụng (không lưu dưới DB)
    [NotMapped]
    public int AvailableStock => (PhysicalStock ?? 0) - (ReservedStock ?? 0);

    public virtual Category? Category { get; set; }

    public virtual ICollection<ImportOrderDetail> ImportOrderDetails { get; set; } = new List<ImportOrderDetail>();
    // Thêm tập hợp các đánh giá của sản phẩm này
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}