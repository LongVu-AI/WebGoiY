using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            // 1. Lấy thông tin User đang đăng nhập từ Session
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");
            if(loggedInUser == null)
            {
                return RedirectToAction("Login","Auth");                
            }
            int currentUserId = loggedInUser.UserId;
            // 2. Lấy danh sách sản phẩm trong giỏ hàng từ DB của User này
            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart");


            if(cartItems == null|| !cartItems.Any())
            {
                // 3. Nếu giỏ hàng trống, không cho checkout, đá về trang hiển thị giỏ hàng
                return RedirectToAction("ViewCart","Checkout");
            }
            //Nạp thông tin Product từ DB vào để lấy Giá (Price)
            foreach(var item in cartItems)
            {
                item.Product = _context.Products.Include(p=>p.Category).FirstOrDefault(p=>p.ProductId == item.ProductId)!;
            }
            // Lọc bỏ sản phẩm lỗi nếu lỡ bị xóa khỏi DB để tránh lỗi Null giao diện
            cartItems = cartItems.Where(item => item.Product != null).ToList();
            // 4. Tính tổng tiền
            double total = cartItems.Sum(item => item.Amount);
            // 5. Truyền dữ liệu ra Giao diện bằng ViewBag (hoặc truyền qua Model)
            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = total;

             
            return View();
        }
        [HttpPost]
        [Route("Checkout/PlaceOrder")]
        public IActionResult handlePlaceOrder(string recipientName,
                                                string phoneNumber,
                                                string shippingAddress,
                                                string email,
                                                string orderNotes,
                                                string paymentMethod)
        {
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

            // Nạp dữ liệu sản phẩm ngầm để lấy giá chính xác từ Database
            foreach (var item in cartItems)
            {
                item.Product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }
            cartItems = cartItems.Where(item => item.Product != null).ToList();

            double total = cartItems.Sum(item => item.Amount);

            // 1. Tạo đơn hàng mẹ
            var order = new Order
            {
                UserId = currentUserId,
                RecipientName = recipientName,
                PhoneNumber = phoneNumber,
                ShippingAddress = shippingAddress,
                Email = email,
                OrderNotes = orderNotes,
                PaymentMethod = paymentMethod,
                OrderDate = DateTime.Now,  
                Status = "PENDING",       
                TotalPrice = total
            };

            _context.Orders.Add(order);
            _context.SaveChanges(); // EF Core tự sinh OrderId sau lệnh này

            // 2. Lưu chi tiết đơn hàng con
            foreach (var item in cartItems)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price  
                };
                _context.OrderDetails.Add(detail);
            }
            _context.SaveChanges(); 

            // 3. Xóa sạch giỏ hàng trong Session sau khi đã lưu DB thành công
            HttpContext.Session.Remove("cart");

            // Dùng TempData để giữ lại lời nhắn khi chuyển trang
            TempData["OrderSuccess"] = "Placed success! Thank for buying.";

            // Đá người dùng về hẳn trang chủ (Hàm Index của ProductController)
            return RedirectToAction("Index", "Product");
        }
     
    }
}