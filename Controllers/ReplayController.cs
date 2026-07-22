using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebGoiY.Enum;
using WebGoiY.Helpers;
using WebGoiY.Models;

namespace WebGoiY.Controllers
{
 
    public class ReplayController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReplayController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult PostReview(Review model, string ProductID, List<IFormFile>? reviewImages)  
        { // IFormFile vì file gửi lên từ cshtml
            // 1. Kiểm tra User đăng nhập chưa
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");
            if (loggedInUser == null)
            {
                return RedirectToAction("Login", "Auth");  
            }

            // 2. Kiểm tra đã mua hàng và được duyệt/giao chưa
            bool hasPurchased = _context.Orders.Any(o => o.UserId == loggedInUser.UserId
                    && (o.Status == OrderStatus.DELIVERED.ToString())
                    && o.OrderDetails.Any(od => od.ProductId == ProductID));

            if (!hasPurchased)
            {
                TempData["Error"] = "You can only review products that you have successfully purchased and received!";
                return RedirectToAction("Detail", "Product", new { id = ProductID });
            }

            // 3. Kiểm tra Rating chính xác (1 -> 5 sao)
            if (model.Rating > 5 || model.Rating < 1)
            {
                TempData["Error"] = "Invalid rating score! Please select between 1 and 5 stars.";
                return RedirectToAction("Detail", "Product", new { id = ProductID });
            }

            // 4. Khởi tạo đối tượng Review
            var userReview = new Review
            {
                ProductId = ProductID,
                UserId = loggedInUser.UserId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.Now,  
                IsVisible = 1,   
                AdminReply = null
            };

            // 5. XỬ LÝ UPLOAD ĐA ẢNH (NẾU CÓ CHỌN ẢNH)
            try
            {
                List<string> imagePaths = ImageFileHelper.SaveMultipleImages(reviewImages, "reviews", maxFiles: 3);

                foreach (var path in imagePaths)
                {
                    userReview.ReviewImages.Add(new ReviewImage
                    {
                        ImagePath = path
                    });
                }
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", "Product", new { id = ProductID });
            }

            //  6. LƯU VÀO CSDL (EF Core sẽ tự động lưu cả Review và các ReviewImage đi kèm)
            _context.Reviews.Add(userReview);
            _context.SaveChanges();

            TempData["Success"] = "Thank you for submitting your review!";
            return RedirectToAction("Detail", "Product", new { id = ProductID });
        }

        [HttpPost]
        public IActionResult ToggleReviewVisibility(int reviewID)
        {
            var review = _context.Reviews.Find(reviewID);
            
            if(review == null)
            {
                return NotFound();
            }
            // Nếu IsVisible đang là 1 -> đổi thành 0. Ngược lại -> đổi thành 1
            review.IsVisible = (byte)(review.IsVisible == 0 ?1:0);  
 
            _context.SaveChanges();
            return RedirectToAction("Index", "Admin");//chưa làm trang admin
        }


        [HttpPost]
        public IActionResult ReplyReview(string reviewID, string adminReplay)
        {
            var review = _context.Reviews.Find(reviewID);
            
            if(review == null)
            {
                return NotFound();
            }
             
            review.AdminReply = adminReplay;
 
            _context.SaveChanges();
            return RedirectToAction("Index", "Admin");//chưa làm trang admin
        }
        
    }
}