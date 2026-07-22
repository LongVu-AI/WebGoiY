using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

public partial class Category
{
    [Column("category_id")]
    public string CategoryId { get; set; } = null!;

    [Column("category_name")]
    public string CategoryName { get; set; } = null!;

    [Column("image_path")]
    public string? ImagePath { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}