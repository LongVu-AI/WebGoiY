using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebGoiY.Models;

namespace WebGoiY.Helpers
{
    public static class CommonHelpers
    {
        public static List<T> ToPageList<T>(this IQueryable<T> source, int currentPage, int pageSize, out int totalPages )
        {
            //1. tính số dòng
            int totalItem = source.Count();

            //2. Tính tổng số trang
            totalPages = (int)Math.Ceiling((double)totalItem/pageSize);

            //3. Ràng buộc điều kiện trang an toàn
            if(currentPage< 1)
            {
                currentPage = 1;
            }
            if(currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            //4. Thực hiện skip, Take và ép kiểu tolist rồi trả về
            return source.Skip((currentPage -1)* pageSize).Take(pageSize).ToList();
        }

      
    }
     
}