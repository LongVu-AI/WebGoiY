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
                                             .Where(p => p.IsHot ==1)
                                             .ToPageList(page, _pageSize, out int totalPages);
                                             

           

            return View(products);
        }

        // trang danh sách sp 
        public IActionResult ShowShopPage(int page = 1,string search = "",string sort ="", string? categories = null)
        {
            int ListPageSize = 12;
            var query = _context.Products.AsQueryable();
            
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
            // 1. Lấy thông tin chi tiết sản phẩm hiện tại
            var productDetail = _context.Products.Include(p=>p.Category).FirstOrDefault(p=>p.ProductId == ID);

            if(productDetail == null) return NotFound();

            // 2. KHỐI 1: Lấy 12 sản phẩm tương tự CÙNG DANH MỤC (Slider trên)
            List<Product> similarProducts = _context.Products.
                                            Where(p=>p.CategoryId == productDetail.CategoryId && p.ProductId != ID)
                                            .Take(12)
                                            .ToList();

            // 3. KHỐI 2: RECOMMENDED FOR YOU - ĐỀ XUẤT THEO LUẬT KẾT HỢP DATA MINING (Thanh cuộn dưới)
            // Bọc dấu % trực tiếp vào giá trị tham số của cái chi tiết sản phẩm đã tìm ở trên
            string formatProdName = $"%{productDetail.ProductName}%";
            // Lấy danh sách tên sản phẩm được đề xuất từ bảng BestRules trước
            var recommendedNames = _context.BestRules
                                    .Where(r=>EF.Functions.Like(r.Antecedents, formatProdName))
                                    .Select(r=>r.Consequents)
                                    .Distinct();
            // Sau đó lọc trong bảng Products                        
            var ruleBasedProducts = _context.Products
                                    .Where(p=>recommendedNames.Contains(p.ProductName) && p.ProductId != ID)
                                    .ToList();

            List<Product> finalRecommendations = new List<Product>();
            if(ruleBasedProducts != null && ruleBasedProducts.Any())
            {
                foreach (var p in ruleBasedProducts)
                {
                    // Lọc trùng tuyệt đối với khối Similar Products ở trên (Tương đương .stream().noneMatch trong Java)
                    if (!similarProducts.Any(sim => sim.ProductId == p.ProductId))
                    {
                        finalRecommendations.Add(p);
                    }
                    
                    // Giới hạn tối đa 40 sản phẩm
                    if (finalRecommendations.Count >= 40) 
                        break;
                }
            }

            // 3. ĐẨY DỮ LIỆU RA VIEW
            // Khối 1: Đã chia cụm 4 để làm Carousel (Như câu trước)
            ViewBag.SimilarProductChunks = similarProducts
                .Select((prod, index) => new { prod, index })
                .GroupBy(x => x.index / 4)
                .Select(g => g.Select(x => x.prod).ToList())
                .ToList();

            ViewBag.FinalRecommendations = finalRecommendations;
            return View(productDetail);
        }
        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}