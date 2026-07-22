using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models
{
    [Table("review_images")]
    public class ReviewImage
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Column("review_id")]
        public int ReviewId { get; set; }

        [Column("image_path")]
        public string ImagePath { get; set; } = null!;

        [ForeignKey("ReviewId")]
        public virtual Review? Review { get; set; }
    }
}