using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models
{
    [Table("reviews")]
    public partial class Review
    {
        [Key]
        [Column("review_id")]
        public int ReviewId { get; set; }

        [Required]
        [Column("product_id")]
        [StringLength(50)]
        public string ProductId { get; set; } = null!;

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 1: Hiện ngoài Web | 0: Bị Admin ẩn (Spam, vi phạm...)
        [Column("is_visible")]
        public byte IsVisible { get; set; } = 1; 

        [Column("admin_reply")]
        public string? AdminReply { get; set; }

        // ==========================================
        // KHÓA NGOẠI (NAVIGATION PROPERTIES)
        // ==========================================
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();
    }
}