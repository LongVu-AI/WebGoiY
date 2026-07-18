using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebGoiY.Models;

namespace WebGoiY.Helpers
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;
        public ProductService(ApplicationDbContext context) { _context = context; }

        // Xử lý tăng kho khi nhập hàng
        public async Task HandleProductImportAsync(string productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.PhysicalStock += quantity;
                await _context.SaveChangesAsync();
            }
        }
    }
}