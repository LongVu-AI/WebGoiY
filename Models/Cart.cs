using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models
{
    [Table("carts")]
    public class Cart
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("product_id")]
        public required string ProductId { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        // --- NAVIGATIONAL PROPERTIES ---
        [ForeignKey("ProductId")]
        public virtual required Product Product { get; set; }

        // Thành tiền = Đơn giá x Số lượng
        [NotMapped]
        public decimal Amount 
        {
            get 
            {
                return (Product?.Price ?? 0) * Quantity;
            }
        }
    }
}