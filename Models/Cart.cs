using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models
{
    [Table("carts")] // Đặt tên bảng trong SQL là Carts
    public class Cart
    {
        [Key]
        public int Id { get; set; } // Khóa chính tự tăng

        [Required]
        public int UserId { get; set; } // Khóa ngoại liên kết với bảng User

        [Required]
        public string ProductId { get; set; } // Khóa ngoại liên kết với bảng Product

        [Required]
        public int Quantity { get; set; } // Số lượng sản phẩm mua

        // --- NAVIGATIONAL PROPERTIES (Liên kết bảng) ---
        // EF Core sẽ tự động dựa vào ProductId để bốc trọn bộ thông tin: 
        // ProductName, Price, ImagePath từ bảng Product qua đây mà không cần khai báo lại.
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // --- ĐỔI TỪ getAmount() CỦA JAVA SANG PROPERTY C# ---
        // Tính tổng tiền của riêng món này (Đơn giá x Số lượng)
        [NotMapped] // Khai báo cho EF Core biết thuộc tính này chỉ tính toán, không tạo cột trong DB
        public double Amount 
        {
            get 
            {
                // Ép kiểu Price về double nếu trong DB bạn đang để kiểu decimal
                return (double)(Product?.Price ?? 0) * Quantity;
            }
        }
    }
}