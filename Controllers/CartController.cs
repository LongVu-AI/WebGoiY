using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGoiY.Models;
using WebGoiY.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace WebGoiY.Controllers
{ 
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. HIỂN THỊ TRANG GIỎ HÀNG
        // ==========================================
        [HttpGet]
        [Route("cart")]
        [Route("Cart/ViewCart")]
        public IActionResult ViewCart()
        {
            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();

            foreach (var item in cartItems)
            {
                item.Product = _context.Products
                    .Include(p => p.Category) 
                    .FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }

            cartItems = cartItems.Where(item => item.Product != null).ToList();
            double total = cartItems.Sum(item => item.Amount);
            
            // Khối logic AI gợi ý (Giữ nguyên gốc của bạn)
            List<Product> recommendations = new List<Product>();
            HashSet<string> addedProductIds = new HashSet<string>();

            if (cartItems.Any())
            {
                foreach (var item in cartItems)
                {
                    string formatProdName = $"%{item.Product.ProductName}%";
                    string prodIdInCart = item.ProductId;

                    var recommendedNames = _context.BestRules
                        .Where(r => EF.Functions.Like(r.Antecedents, formatProdName))
                        .Select(r => r.Consequents)
                        .Distinct()
                        .ToList();

                    var rulesForItem = _context.Products
                        .Where(p => recommendedNames.Contains(p.ProductName) && p.ProductId != prodIdInCart)
                        .ToList();

                    foreach (var p in rulesForItem)
                    {
                        bool isDuplicateName = cartItems.Any(cItem => 
                            p.ProductName.ToLower().Contains(cItem.Product.ProductName.ToLower()) || 
                            cItem.Product.ProductName.ToLower().Contains(p.ProductName.ToLower())
                        );

                        bool alreadyInCart = cartItems.Any(cItem => cItem.ProductId == p.ProductId);

                        if (!addedProductIds.Contains(p.ProductId) && !alreadyInCart && !isDuplicateName)
                        {
                            recommendations.Add(p);
                            addedProductIds.Add(p.ProductId);
                        }
                    }
                }

                if (recommendations.Count < 4)
                {
                    var categoryIdsInCart = cartItems.Select(c => c.Product.CategoryId).Distinct().ToList();
                    var productIdsInCart = cartItems.Select(c => c.ProductId).ToList();

                    var similarProducts = _context.Products
                        .Where(p => categoryIdsInCart.Contains(p.CategoryId) && !productIdsInCart.Contains(p.ProductId))
                        .Take(12)
                        .ToList();

                    foreach (var sp in similarProducts)
                    {
                        if (!addedProductIds.Contains(sp.ProductId))
                        {
                            recommendations.Add(sp);
                            addedProductIds.Add(sp.ProductId);
                        }
                        if (recommendations.Count >= 12) break;
                    }
                }
            }

            if (!recommendations.Any())
            {
                recommendations = _context.Products.Where(p => p.IsHot == 1).Take(12).ToList();
            }

            if (recommendations.Count > 12)
            {
                recommendations = recommendations.Take(12).ToList();
            }

            ViewBag.Recommendations = recommendations;
            ViewBag.CartItems = cartItems;
            ViewBag.TotalPrice = total;

            return View("ViewCart"); 
        }
        
        // ==========================================
        // 2. THÊM VÀO GIỎ HÀNG (ĐỒNG BỘ - CHUYỂN TRANG)
        // ==========================================
        [HttpGet]
        [Route("/cart/add")]  
        public IActionResult AddToCart(string productId, int qty = 1)
        {
            var product = _context.Products.Find(productId);
            
            if(product != null)
            {
                var cartItem = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
                var existingItem = cartItem.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += qty;
                }
                else
                {
                    var newItem = new Cart
                    {
                        ProductId = product.ProductId,
                        Quantity = qty 
                    };
                    cartItem.Add(newItem);
                }
                HttpContext.Session.SetObjectAsJson("cart", cartItem);
            }
            return RedirectToAction("ViewCart", "Cart");
        }

        // ==========================================
        // 3. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        // ==========================================
        [HttpGet]
        [Route("/cart/remove/{id}")]  
        public IActionResult removeFromCart(string id) 
        {
            var cartItem = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
            int rowsAffected = cartItem.RemoveAll(p => p.ProductId == id);
            
            if (rowsAffected > 0) 
            {
                HttpContext.Session.SetObjectAsJson("cart", cartItem);
            }
            
            return RedirectToAction("ViewCart", "Cart");
        }

        // ==========================================
        // 4. CẬP NHẬT SỐ LƯỢNG Ô INPUT TẠI TRANG GIỎ HÀNG
        // ==========================================
        [HttpPost]
        [Route("/cart/update-async")]
        public IActionResult UpdateCartAsync(string productId, int qty)
        {
            var cartItem = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
            var existingItem = cartItem.FirstOrDefault(item => item.ProductId == productId);
            
            if (existingItem != null)
            {
                if (qty <= 0)
                {
                    cartItem.Remove(existingItem);
                }
                else
                {
                    existingItem.Quantity = qty;
                }
                HttpContext.Session.SetObjectAsJson("cart", cartItem);
            }

            // Sau khi cập nhật, cần map lại thông tin sản phẩm để tính toán tiền chính xác
            foreach (var item in cartItem)
            {
                item.Product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }
            cartItem = cartItem.Where(item => item.Product != null).ToList();

            // Tính toán số liệu mới trả về cho giao diện cập nhật ngay lập tức
            double newAmount = existingItem != null ? existingItem.Amount : 0;
            double newGrandTotal = cartItem.Sum(item => item.Amount);
            int newTotalItems = cartItem.Sum(item => item.Quantity);

            return Json(new
            {
                success = true,
                amount = newAmount.ToString("N0") + " £",
                grandTotal = newGrandTotal.ToString("N0") + " £",
                totalItems = newTotalItems
            });
        }
        // ==========================================
        // 5. THÊM VÀO GIỎ HÀNG ASYNC (AJAX - KHÔNG LOAD TRANG)
        // ==========================================
        [HttpGet]
        [Route("/Cart/add-async")] 
        public IActionResult AddToCartAsync(string id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);

            if (product != null)
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
                var existingItem = cart.FirstOrDefault(item => item.ProductId == id);

                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    var newItem = new Cart
                    {
                        ProductId = id,
                        Quantity = 1
                    };
                    cart.Add(newItem);
                }

                HttpContext.Session.SetObjectAsJson("cart", cart);
                int totalQuantity = cart.Sum(item => item.Quantity);

                return Json(new
                {
                    success = true,
                    message = $"Product {product.ProductName} has been successfully added to your cart!",
                    totalItems = totalQuantity  
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Product not found!"
                });
            }
        }
    }
}