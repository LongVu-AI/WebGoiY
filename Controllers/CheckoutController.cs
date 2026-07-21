using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGoiY.Helpers;
using WebGoiY.Models;

namespace WebGoiY.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("checkout")]
        public IActionResult ShowCheckOut()
        {
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");
            if (loggedInUser == null)
            {
                return RedirectToAction("Login", "Auth");                
            }

            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart");
            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("ViewCart", "Cart");
            }

            foreach (var item in cartItems)
            {
                item.Product = _context.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }
            cartItems = cartItems.Where(item => item.Product != null).ToList();

            decimal subtotal = (decimal)cartItems.Sum(item => item.Amount);
            decimal discount = 0.00m; 
            decimal shippingFee = 0.00m; 
            decimal tax = Math.Round(subtotal * 0.10m, 2); 
            decimal grandTotal = subtotal - discount + shippingFee + tax;

            ViewBag.CartItems = cartItems;
            ViewBag.SubtotalPrice = subtotal;
            ViewBag.DiscountAmount = discount;
            ViewBag.TaxAmount = tax;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.TotalPrice = grandTotal;
             
            return View();
        }

        [HttpPost]
        [Route("Checkout/PlaceOrder")]
        public IActionResult handlePlaceOrder(CheckoutViewModel model)
        {
            // 1. Kiểm tra session đăng nhập
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");
            if (loggedInUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            int currentUserId = loggedInUser.UserId;
            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart");

            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("ViewCart", "Cart");
            }

            // 2. TẠO ĐƠN HÀNG MẸ
            var order = new Order
            {
                UserId = currentUserId,
                RecipientName = model.RecipientName,
                PhoneNumber = model.PhoneNumber,
                ShippingAddress = model.ShippingAddress,
                Email = model.Email,
                OrderNotes = model.OrderNotes,
                PaymentMethod = model.PaymentMethod,
                OrderDate = DateTime.Now,  
                Status = "PENDING",       
                SubtotalPrice = model.SubtotalPrice,
                DiscountAmount = model.DiscountAmount,
                TaxAmount = model.TaxAmount,
                ShippingFee = model.ShippingFee,
                TotalPrice = model.TotalPrice
            };

            _context.Orders.Add(order);
            _context.SaveChanges(); // Lấy OrderId tự tăng

            //  Xóa sạch bộ nhớ tạm Tracking của EF Core để tránh xung đột
            _context.ChangeTracker.Clear();

            // 3. LƯU CHI TIẾT ĐƠN HÀNG CON & CẬP NHẬT RESERVED_STOCK BẰNG SQL TRỰC TIẾP
            foreach (var item in cartItems)
            {
                // Lấy đơn giá chuẩn từ DB bằng AsNoTracking
                var productInDb = _context.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == item.ProductId);
                
                if (productInDb != null)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = productInDb.Price  
                    };
                    _context.OrderDetails.Add(detail);

                    // Bắn thẳng SQL UPDATE xuống MySQL (Xử lý luôn trường hợp reserved_stock bị NULL)
                    _context.Database.ExecuteSqlInterpolated(
                        $"UPDATE products SET reserved_stock = COALESCE(reserved_stock, 0) + {item.Quantity} WHERE product_id = {item.ProductId}"
                    );
                }
            }

            // 4. GHI LOG LỊCH SỬ TRẠNG THÁI (ORDER STATUS HISTORY)
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = "PENDING",
                ChangedAt = DateTime.Now,
                ChangedBy = currentUserId,
                Notes = "The customer has successfully placed an order."
            };
            _context.OrderStatusHistories.Add(statusHistory);

            // Lưu OrderDetails và StatusHistory vào DB
            _context.SaveChanges(); 

            // 5. Dọn dẹp giỏ hàng Session
            HttpContext.Session.Remove("cart");
            TempData["OrderSuccess"] = "Placed success! Thank for buying.";

            return RedirectToAction("Index", "Product");
        }
    }
}