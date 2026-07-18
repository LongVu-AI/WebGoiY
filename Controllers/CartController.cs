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

            // Chỉ giữ lại những sản phẩm hợp lệ và đang còn kinh doanh (IsActive == 1)
            cartItems = cartItems.Where(item => item.Product != null && item.Product.IsActive == 1).ToList();
            double total = cartItems.Sum(item => item.Amount);
            
            List<Product> recommendations = new List<Product>();
            HashSet<string> addedProductIds = new HashSet<string>();

            if (cartItems.Any())
            {
                foreach (var item in cartItems)
                {
                    string formatProdName = $"%{item.Product.ProductName}%";
                    string prodIdInCart = item.ProductId;

                    // 1. Lấy danh sách Consequents từ luật AI liên quan đến món hàng trong giỏ
                    var recommendedRules = _context.BestRules
                        .Where(r => EF.Functions.Like(r.Antecedents, formatProdName)) 
                        .Select(r => r.Consequents)
                        .Distinct()
                        .ToList(); // Đưa về List trên RAM để xử lý tách chuỗi combo

                    if (recommendedRules.Any())
                    {
                        //  Tách chuỗi luật dạng "Món A, Món B" thành các từ khóa đơn lẻ
                        var allRecommendedNames = recommendedRules
                            .SelectMany(rule => rule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            .Select(name => name.Trim())
                            .Distinct()
                            .ToList();

                        // Truy vấn danh sách sản phẩm gợi ý từ DB (Có lọc IsActive)
                        var rulesForItem = _context.Products
                            .Where(p => allRecommendedNames.Contains(p.ProductName) 
                                    && p.ProductId != prodIdInCart 
                                    && p.IsActive == 1) 
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
                }

                // 2. Nếu danh sách gợi ý từ AI quá ít (dưới 4 sản phẩm), tự động lấy bù hàng cùng danh mục
                if (recommendations.Count < 4)
                {
                    var categoryIdsInCart = cartItems.Select(c => c.Product.CategoryId).Distinct().ToList();
                    var productIdsInCart = cartItems.Select(c => c.ProductId).ToList();

                    var similarProducts = _context.Products
                        .Where(p => categoryIdsInCart.Contains(p.CategoryId) 
                                && !productIdsInCart.Contains(p.ProductId) 
                                && p.IsActive == 1) 
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

            // 3. Nếu giỏ hàng trống hoặc không tìm được gợi ý nào, bốc ngẫu nhiên top sản phẩm HOT (IsHot == 1)
            if (!recommendations.Any())
            {
                recommendations = _context.Products
                    .Where(p => p.IsHot == 1 && p.IsActive == 1) 
                    .Take(12)
                    .ToList();
            }

            // Giới hạn hiển thị tối đa 12 món trên thanh trượt của Giỏ hàng
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
            // 1. Tìm sản phẩm và kiểm tra xem sản phẩm có đang được kinh doanh không
            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId && p.IsActive == 1);
            
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!";
                return RedirectToAction("ShowShopPage", "Product");
            }

            // 2. Lấy giỏ hàng hiện tại từ Session
            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
            var existingItem = cartItems.FirstOrDefault(item => item.ProductId == productId);

            // Tính tổng số lượng mà khách muốn có trong giỏ sau khi thêm mới/cộng dồn
            int totalRequestedQty = qty;
            if (existingItem != null)
            {
                totalRequestedQty += existingItem.Quantity;
            }
            int physics = product.PhysicalStock ?? 0;
            int stock =  product.ReservedStock ?? 0;
            // 3. KIỂM TRA TỒN KHO KHẢ DỤNG: Số lượng bán trên web = Tồn kho thực tế - Giữ chỗ
            int availableStock = physics - stock;

            if (totalRequestedQty > availableStock)
            {
                // Gửi thông báo lỗi về giao diện
                if (availableStock <= 0)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{product.ProductName}' hiện đã hết hàng!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{product.ProductName}' chỉ còn lại {availableStock} sản phẩm khả dụng!";
                }
                
                return RedirectToAction("ViewCart", "Cart");
            }

            // 4. Nếu hợp lệ, tiến hành cập nhật vào Session giỏ hàng
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
                cartItems.Add(newItem);
            }

            HttpContext.Session.SetObjectAsJson("cart", cartItems);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";

            return RedirectToAction("ViewCart", "Cart");
        }
        // ==========================================
        // 3. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
        // ==========================================
        [HttpGet]
        [Route("/cart/remove/{productId}")]  
        public IActionResult RemoveFromCart(string productId) 
        {
            // 1. Lấy giỏ hàng từ Session
            var cartItems = HttpContext.Session.GetObjectFromJson<List<Cart>>("cart") ?? new List<Cart>();
            
            // 2. Tiến hành xóa sản phẩm khỏi danh sách giỏ hàng
            int rowsAffected = cartItems.RemoveAll(p => p.ProductId == productId);
            
            // 3. Nếu thực sự có xóa, cập nhật lại Session và bắn thông báo thành công
            if (rowsAffected > 0) 
            {
                HttpContext.Session.SetObjectAsJson("cart", cartItems);
                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
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
                    //  Check thêm điều kiện IsActive để tránh việc tăng số lượng sản phẩm vừa bị ẩn
                    var productInDB = _context.Products.FirstOrDefault(p => p.ProductId == productId && p.IsActive == 1);
                    if (productInDB != null)
                    {
                        // Tính toán tồn kho khả dụng hiện thực (Thực tế - Giữ chỗ)
                        int availableStock = productInDB.PhysicalStock - productInDB.ReservedStock ?? 0;

                        if (qty > availableStock)
                        {
                            // Trả về success = false để thông báo lỗi cho AJAX Frontend chặn lại
                            return Json(new
                            {
                                success = false,
                                message = $"Sorry, only {availableStock} items left available in stock!"
                            });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Product is no longer available!" });
                    }

                    existingItem.Quantity = qty;
                }
                HttpContext.Session.SetObjectAsJson("cart", cartItem);
            }

            // Map lại thông tin sản phẩm từ DB để tính tiền chuẩn
            foreach (var item in cartItem)
            {
                item.Product = _context.Products.FirstOrDefault(p => p.ProductId == item.ProductId)!;
            }
            cartItem = cartItem.Where(item => item.Product != null).ToList();

            // Tính toán số liệu mới trả về
            double newAmount = existingItem != null ? existingItem.Amount : 0;
            double newGrandTotal = cartItem.Sum(item => item.Amount);
            int newTotalItems = cartItem.Sum(item => item.Quantity);

            return Json(new
            {
                success = true, // Cập nhật thành công thực sự
                amount = newAmount.ToString("N2") + " £",
                grandTotal = newGrandTotal.ToString("N2") + " £",
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