using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGoiY.Models;

namespace WebGoiY.ViewComponents
{
    public class CategoryMenuViewComponent: ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm này sẽ tự động chạy khi được gọi ở Layout
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy danh sách danh mục từ Database
            var categories = await _context.Categories.ToListAsync();
            
            // Truyền thẳng danh sách này vào giao diện riêng của nó
            return View(categories);
        }
    }
}