using System.ComponentModel.DataAnnotations;

namespace WebGoiY.Models
{
    public class CheckoutViewModel
    {
        // Thông tin người nhận
        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? OrderNotes { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "COD";

        // Phân rã dòng tiền
        public decimal SubtotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalPrice { get; set; }
    }
}