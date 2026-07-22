using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebGoiY.Helpers
{
    public static class ImageFileHelper
    {
        /// <summary>
        /// Hàm xử lý lưu danh sách nhiều ảnh tải lên từ form
        /// </summary>
        /// <param name="files">Danh sách file gửi lên từ form</param>
        /// <param name="subFolder">Thư mục con muốn lưu (Ví dụ: "reviews", "products", "avatars")</param>
        /// <param name="maxFiles">Số lượng ảnh tối đa cho phép (Mặc định 5)</param>
        /// <returns>Trả về Danh sách các đường dẫn tương đối (Relative paths) để lưu vào DB</returns>
        public static List<string> SaveMultipleImages(List<IFormFile>? files, string subFolder, int maxFiles = 3)
        {
            var savedFilePaths = new List<string>();

            // Nếu không có file hoặc danh sách rỗng thì trả về danh sách rỗng
            if (files == null || files.Count == 0)
            {
                return savedFilePaths;
            }

            // Kiểm tra giới hạn số lượng ảnh
            if (files.Count > maxFiles)
            {
                throw new ArgumentException($"You can only upload a maximum of {maxFiles} images!");
            }

            // 1. Tạo thư mục vật lý lưu ảnh: wwwroot/uploads/{subFolder}
            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subFolder);
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // 2. Các định dạng ảnh được phép lưu
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

            // 3. Duyệt và lưu từng file
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string extension = Path.GetExtension(file.FileName).ToLower();

                    // Bỏ qua nếu đuôi file không hợp lệ
                    if (!allowedExtensions.Contains(extension))
                    {
                        continue;
                    }

                    // Tạo tên file duy nhất tránh trùng lặp bằng GUID
                    string uniqueFileName = Guid.NewGuid().ToString() + extension;
                    string physicalPath = Path.Combine(uploadDir, uniqueFileName);

                    // Lưu file vật lý lên ổ cứng server
                    using (var stream = new FileStream(physicalPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    // Lưu đường dẫn tương đối để đưa ra View/DB hiển thị
                    savedFilePaths.Add($"/uploads/{subFolder}/{uniqueFileName}");
                }
            }

            return savedFilePaths;
        }
    }
}