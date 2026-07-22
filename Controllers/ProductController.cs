using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebGoiY.Models;
using WebGoiY.Helpers;
using Microsoft.EntityFrameworkCore;

namespace WebGoiY.Controllers
{
 
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;

        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _configuration;
        // tạo biến hứng thông tin trang trong file appSetting
        private readonly int _pageSize;
        public ProductController(ILogger<ProductController> logger, ApplicationDbContext context,
                                                                    IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
             // lấy thông tin từ file appsetting lấy số lượng trang nếu dc còn ko thì lấy giá trị đằng sau
            _pageSize = _configuration.GetValue<int>("PaginationSettings:ProductPageSize",8); 
        }

        [HttpGet]
        public IActionResult Index(int page = 1)
        {
            //in ra danh sách các sản phẩm nổi bật mà thuât toán tìm dc (ishot =1) và phan tramg
            List<Product> products = _context.Products
                                             .Where(p => p.IsHot ==1 && p.IsActive == 1)
                                             .ToPageList(page, _pageSize, out int totalPages);
            ViewBag.Categories = _context.Categories.ToList();
            return View(products);
        }

        // trang danh sách sp 
        public IActionResult ShowShopPage(int page = 1,string search = "",string sort ="", string? categories = null)
        {
            int ListPageSize = 12;
            var query = _context.Products.Where(p=>p.IsActive == 1).AsQueryable();
            
            //1. tìm kiếm sản phẩm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                // Loại bỏ khoảng trắng thừa và tìm kiếm tương đối (chuyển về chữ thường để không phân biệt HOA/thường nếu cần)
                string keyword = search.Trim().ToLower();
                query = query.Where(p=>p.ProductName.ToLower().Contains(keyword));
            }
            
            // Tìm kiếm theo danh mục sản phẩm => tiện hệ lấy tên danh mục để ghép vào html 
            if (!string.IsNullOrEmpty(categories))
            {
                query = query.Where(p=>p.CategoryId == categories);
                
                ViewBag.CategoryName = _context.Categories
                                                .Where(c => c.CategoryId == categories)
                                                .Select(c => c.CategoryName)
                                                .FirstOrDefault();
            }
            
            //3. sắp xếp sản phẩm theo giá bán  
            if(sort.Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(p=>p.Price);// sắp xếp tăng dần
            }else if(sort.Equals("desc", StringComparison.OrdinalIgnoreCase)){
                query = query.OrderByDescending(p=>p.Price);
            }
            

            //4. thực thi phân trang sau khi lọc sản phẩm 
            List<Product> allproduct = query.ToPageList(page, ListPageSize, out int totalPages);
            
            // truyền thông tin số trang và trang hiện tại 
            ViewBag.currentSearch = search;    
            ViewBag.currentPage = page;
            ViewBag.totalPages = totalPages;
            ViewBag.currentSort = sort;
            ViewBag.currentCategory = categories;
            
            return View("shop",allproduct);
        } 

        public IActionResult Detail(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return NotFound(); // trả về trang 404 nếu ko có id
            }   
           
            // 1. Lấy thông tin chi tiết sản phẩm hiện tại (Chỉ lấy sản phẩm đang hoạt động)
            var productDetail = _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Reviews.Where(r => r.IsVisible == 1))// Chỉ lấy review đang HIỆN
                                            .ThenInclude(r => r.User)   //  Lấy thông tin User để hiển thị Tên người review
                                        .FirstOrDefault(p => p.ProductId == ID && p.IsActive == 1);

            if (productDetail == null) return NotFound();
             
            // 2. KHỐI 1: Lấy 12 sản phẩm tương tự CÙNG DANH MỤC (Lọc IsActive và ưu tiên còn hàng)
            List<Product> similarProducts = _context.Products
                .Where(p => p.CategoryId == productDetail.CategoryId 
                        && p.ProductId != ID 
                        && p.IsActive == 1) //  Chuẩn nghiệp vụ: Chỉ lấy sản phẩm đang bán
                .OrderByDescending(p => p.PhysicalStock - p.ReservedStock > 0) // Ưu tiên hàng còn trên kệ hiện lên trước
                .Take(12)
                .ToList();

            // 3. KHỐI 2: RECOMMENDED FOR YOU - ĐỀ XUẤT THEO LUẬT KẾT HỢP DATA MINING
            string formatProdName = $"%{productDetail.ProductName}%";

            // Lấy danh sách chuỗi kết quả (Consequents) từ bảng BestRules
            var recommendedRules = _context.BestRules
                .Where(r => EF.Functions.Like(r.Antecedents, formatProdName))
                .Select(r => r.Consequents)
                .Distinct()
                .ToList(); // Ép về List trên RAM để xử lý tách chuỗi combo dễ dàng

            List<Product> finalRecommendations = new List<Product>();

            if (recommendedRules.Any())
            {
                //  GIẢI QUYẾT COMBO: Tách các sản phẩm trong chuỗi "SP A, SP B" thành các từ khóa riêng lẻ
                var allRecommendedNames = recommendedRules
                    .SelectMany(rule => rule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(name => name.Trim())
                    .Distinct()
                    .ToList();

                // Quét DB một lần duy nhất để lấy các sản phẩm được AI gợi ý
                var ruleBasedProducts = _context.Products
                    .Where(p => allRecommendedNames.Contains(p.ProductName) 
                            && p.ProductId != ID 
                            && p.IsActive == 1) //  Chỉ lấy sản phẩm đang bán
                    .ToList();

                // Lọc trùng với khối 1 và giới hạn 40 sản phẩm
                foreach (var p in ruleBasedProducts)
                {
                    if (!similarProducts.Any(sim => sim.ProductId == p.ProductId))
                    {
                        finalRecommendations.Add(p);
                    }
                    
                    // Giới hạn tối đa 40 sản phẩm
                    if (finalRecommendations.Count >= 40) 
                        break;
                }
            }

            // 4. ĐẨY DỮ LIỆU RA VIEW
            // Khối 1: Chia cụm 4 để làm Carousel Slider
            ViewBag.SimilarProductChunks = similarProducts
                .Select((prod, index) => new { prod, index })
                .GroupBy(x => x.index / 4)
                .Select(g => g.Select(x => x.prod).ToList())
                .ToList();
            
            ViewBag.FinalRecommendations = finalRecommendations;
            
            ViewBag.Reviews = productDetail.Reviews
                .OrderByDescending(r=>r.CreatedAt)
                .ToList();
            return View(productDetail);
        }

      

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public IActionResult About()
        {
            return View("about"); 
        }
        
    }
}