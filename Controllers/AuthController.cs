using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using WebGoiY.Helpers;
using WebGoiY.Models;

namespace WebGoiY.Controllers
{
    
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AuthController(ApplicationDbContext context)
        {
            _context = context;

        }
        [HttpGet]
        [Route("Auth/Login")] //  Ép hệ thống mở đúng cổng này khi gõ /Auth/Login
        [Route("login")]      //  Khuyến mãi thêm: gõ http://localhost:5051/login cũng vào được luôn
        public IActionResult Login(bool? success)
        {
            // Hứng biến success từ trang Register chuyển qua (nếu có)
            ViewBag.Success = success; 
            return View("Login"); 
        }
        [HttpPost]
      
        public IActionResult HandleLogin(string userName, string passWord )
        {
            //tìm tên người dùng
            var user = _context.Users.FirstOrDefault(p=>p.Username == userName);
            // 2. Kiểm tra tài khoản tồn tại và khớp mật khẩu thuần  
            if(user!= null && user.Password == passWord)
            {
                // Đăng nhập đúng -> Nhét nguyên con User vào Session dưới dạng JSON
                HttpContext.Session.SetObjectAsJson("loggedInUser", user);
                return RedirectToAction("Index", "Product");
            }
            // 3. Nếu sai tài khoản/mật khẩu, báo lỗi ra giao diện
            ViewBag.Error = "Invalid username or password!";

            return View("Login");
        }
         // [GET] Hiển thị giao diện trang Đăng Ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult HandleRegister(User userModel, string confirmPass )
        {
            //tìm tên người dùng
            
            if (userModel.Password != confirmPass)
            {
                ViewBag.Error = "Password do not match!";
                return View("Register");
            }
            var checkName = _context.Users.FirstOrDefault(p=>p.Username == userModel.Username);
             // 2. Kiểm tra xem tên tài khoản đã bị ai khác đăng ký chưa
             if(checkName != null)
            {
                ViewBag.Error ="Username is already taken!";
                return View("Register");
          
            }
            
        
            // Lưu vào Database thông qua EF Core
            _context.Users.Add(userModel);
            _context.SaveChanges();
            // 4. Đăng ký thành công thì đá sang trang Đăng nhập kèm tham số success
            // Dịch từ: return "redirect:/login?success=true";
            return RedirectToAction("Login","Auth", new {success = true });
        }

        [HttpGet]
        public IActionResult Logout() {
            HttpContext.Session.Remove("loggedInUser");
            return RedirectToAction("Login", "Auth");
        }
        
        [HttpGet]
        public IActionResult showProfilePage() {
            // Do có Interceptor bảo vệ nên chắc chắn loggedInUser ở đây không bao giờ bị null
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");

            if(loggedInUser == null)
                {
                    return RedirectToAction("Login", "Auth");
                }
            ViewBag.User = loggedInUser;
    
            return View("Profile");
        }
        [HttpPost]
        [Route("profile/update")]
        public IActionResult HandleUpdateProfile(string fullName, string email, string phone, string address) 
        {
            // 1. Lấy thông tin user hiện tại từ Session ra
            User loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");

            if (loggedInUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Tìm bản ghi thực tế trong Database để cập nhật 
            // (Vì Object lấy từ Session ra đã bị ngắt kết nối với DbContext)
            var userInDb = _context.Users.FirstOrDefault(u => u.UserId == loggedInUser.UserId);
            
            if (userInDb != null)
            {
                // Cập nhật trọn bộ thông tin mới bằng thuộc tính C# (dùng dấu = thay vì hàm set)
                userInDb.FullName = fullName; 
                userInDb.Email = email;       
                userInDb.Phone = phone;       // Đảm bảo trong Model User.cs có cột này
                userInDb.Address = address;   // Đảm bảo trong Model User.cs có cột này

                // Lưu thay đổi vào SQL Database
                _context.SaveChanges();

                // 3. Cập nhật lại thông tin mới vào Session để các trang khác đồng bộ theo
                HttpContext.Session.SetObjectAsJson("loggedInUser", userInDb);
                
                // Cập nhật lại biến cục bộ để gửi ra View
                loggedInUser = userInDb;
            }

            // 4. Đẩy thông báo thành công và dữ liệu mới ra giao diện
            ViewBag.SuccessMessage = "Profile updated successfully!";
            ViewBag.User = loggedInUser;

            // Trả về lại trang Profile để người dùng thấy thông tin mới cập nhật
            return View("Profile"); 
        }

        [HttpGet]
        [Route("order-history")]
        public IActionResult ShowOrderHistory()
        {
             // 1. Lấy thông tin user hiện tại từ Session ra
            var loggedInUser = HttpContext.Session.GetObjectFromJson<User>("loggedInUser");

            if (loggedInUser == null)
            {
                return RedirectToAction("Login", "Auth");                
            }
            int currentUserId = loggedInUser.UserId;
             
            var myOrders = _context.Orders
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            ViewBag.Order = myOrders;
            return View();
        }
    }
}