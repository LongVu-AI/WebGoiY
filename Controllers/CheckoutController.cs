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

            //  PHÂN RÃ DÒNG TIỀN THEO DATABASE MỚI
            decimal subtotal = (decimal)cartItems.Sum(item => item.Amount);
            decimal discount = 0.00m; // Có thể xử lý thêm logic Voucher ở đây nếu có
            decimal shippingFee = 0.00m; // Mặc định FREE Ship như giao diện cũ
            decimal tax = Math.Round(subtotal * 0.10m, 2); // Giả định thuế VAT 10% theo chuẩn hệ thống
            decimal grandTotal = subtotal - discount + shippingFee + tax;

            // Truyền toàn bộ dữ liệu phân rã ra Giao diện
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
        public IActionResult handlePlaceOrder(string recipientName,
                                                string phoneNumber,
                                                string shippingAddress,
                                                string email,
                                                string orderNotes,
                                                string paymentMethod,
                                                decimal subtotalPrice,
                                                decimal discountAmount,
                                                decimal taxAmount,
                                                decimal shippingFee,
                                                decimal totalPrice)
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

            foreach (var item in cartItems)
            {
                item.Product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }
            cartItems = cartItems.Where(item => item.Product != null).ToList();

            //  1. TẠO ĐƠN HÀNG MẸ ĐỒNG BỘ 100% CÁC CỘT DÒNG TIỀN MỚI
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
                SubtotalPrice = subtotalPrice,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                ShippingFee = shippingFee,
                TotalPrice = totalPrice
            };

            _context.Orders.Add(order);
            _context.SaveChanges(); 

            // 2. LƯU CHI TIẾT ĐƠN HÀNG CON
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

            HttpContext.Session.Remove("cart");
            TempData["OrderSuccess"] = "Placed success! Thank for buying.";

            return RedirectToAction("Index", "Product");
        }
    }
}