// =========================================================================
// 1. CÁC HÀM XỬ LÝ TOÀN CỤC (GLOBAL FUNCTIONS)
// =========================================================================

// Hàm phân trang thông minh
 
    function executeGoToPage(maxPage) {
        // 1. Lấy giá trị số trang người dùng vừa gõ vào ô Input
        var inputPage = document.getElementById("goToPageInput").value;
        var page = parseInt(inputPage);
        var total = parseInt(maxPage);

        // 2. Kiểm tra tính hợp lệ của số trang gõ vào
        if (isNaN(page) || page < 1 || page > total) {
            alert("Vui lòng nhập số trang hợp lệ từ 1 đến " + total);
            return;
        }

        // 3. Lấy lại toàn bộ bộ lọc hiện tại được lưu ở ViewBag ra
        var search = "@ViewBag.currentSearch";
        var category = "@ViewBag.currentCategory";
        var sort = "@ViewBag.currentSort";

        // 4. Tiến hành xây dựng URL chuyển hướng chuẩn C# MVC
        // Thay vì: var url = "/shop?page=" + page;
        var url = "/Product/ShowShopPage?page=" + page;
        if (search) url += "&search=" + encodeURIComponent(search);
        if (category) url += "&category=" + category;
        if (sort) url += "&sort=" + sort;

        // 5. Chuyển trang
        window.location.href = url;
    }
 

// Hàm trung gian bốc thuộc tính data-id từ nút bấm ngoài HTML
function handleAddToCartWithDataAttr(buttonComponent) {
    var idMonHang = buttonComponent.getAttribute('data-id');
    if (idMonHang) {
        handleAddToCartAsync(idMonHang);
    } else {
        console.error("Không tìm thấy thuộc tính data-id trên thẻ button!");
    }
}
// Hàm Fetch gửi yêu cầu AJAX ngầm lên Backend C#
window.handleAddToCartAsync = function(productId) {
    
    //  SỬA DÒNG NÀY: Thêm dấu / ở đầu để luôn chạy từ gốc Domain, viết hoa chữ Cart
    fetch('/Cart/add-async?id=' + productId)
        .then(function(response) {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            return response.json();
        })
        .then(function(data) {
            if (data.success) {
                //  Tự động cập nhật số lượng trên Badge của Header
                var badgeEl = document.getElementById('cart-badge');
                if (badgeEl && data.totalItems !== undefined) {
                    badgeEl.innerText = data.totalItems;

                    // Hiệu ứng nảy nhẹ con số
                    badgeEl.style.transform = 'scale(1.3) translateX(40%)';
                    setTimeout(function() {
                        badgeEl.style.transform = 'none';
                    }, 200);
                }

                // 1. Tạo hộp chứa thông báo Toast dạng trôi nổi
                var toastContainer = document.getElementById('toast-container');
                if (!toastContainer) {
                    toastContainer = document.createElement('div');
                    toastContainer.id = 'toast-container';
                    toastContainer.setAttribute('style', `
                        position: fixed; top: 30px; left: 50%; transform: translateX(-50%); 
                        z-index: 9999; pointer-events: none; display: flex; flex-direction: column; gap: 10px;
                    `);
                    document.body.appendChild(toastContainer);
                }

                // 2. Tạo thẻ Toast Glassmorphism tiếng Anh xịn mịn
                var newToast = document.createElement('div');
                newToast.setAttribute('style', `
                    min-width: 320px; max-width: 450px; background: rgba(33, 37, 41, 0.85);
                    backdrop-filter: blur(8px); -webkit-backdrop-filter: blur(8px); color: #ffffff;
                    border: 1px solid rgba(255, 255, 255, 0.1); box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.3);
                    border-radius: 12px; padding: 16px 20px; pointer-events: auto; display: flex; align-items: center;
                    gap: 12px; animation: slideDownFadeIn 0.3s ease-out; transition: all 0.4s ease;
                `);

                newToast.innerHTML = `
                    <div style="color: #28a745; font-size: 24px; display: flex; align-items: center;">
                        <i class="fa-solid fa-circle-check"></i>
                    </div>
                    <div style="flex-grow: 1; text-align: left;">
                        <div style="font-size: 11px; font-weight: bold; letter-spacing: 1px; color: #aaa; margin-bottom: 2px;">CART UPDATE</div>
                        <div style="font-size: 13px; font-weight: 500; line-height: 1.4;">Item added to cart successfully.</div>
                    </div>
                `;
                toastContainer.appendChild(newToast);

                // Điều hướng Reload thông minh tùy theo trang hiện tại
                if (window.location.pathname.includes('/cart')) {
                    setTimeout(function() {
                        window.location.reload();
                    }, 1200);
                } else {
                    setTimeout(function() {
                        if (newToast) {
                            newToast.style.opacity = '0';
                            newToast.style.transform = 'translateY(-20px)';
                            setTimeout(function() { newToast.remove(); }, 400);
                        }
                    }, 2500);
                }

            } else {
                alert("Failed to add item to cart!");
            }
        })
        .catch(function(error) {
            console.error('Fetch Error:', error);
            alert("Could not connect to the cart system.");
        });
};

// Hàm xử lý ẩn/hiện nút See More trên lưới đề xuất trang Cart
window.handleLoadMoreRecommendations = function() {
    var allItems = document.querySelectorAll('.rec-card-item');
    var totalItems = allItems.length;

    var currentVisible = 0;
    allItems.forEach(function(item) {
        if (!item.classList.contains('d-none')) {
            currentVisible++;
        }
    });

    if (currentVisible === 0) {
        currentVisible = 4;
    }

    var nextTarget = currentVisible + 4;

    allItems.forEach(function(item) {
        var idx = parseInt(item.getAttribute('data-index'), 10);
        if (idx >= currentVisible && idx < nextTarget) {
            item.classList.remove('d-none');
        }
    });

    if (nextTarget >= totalItems) {
        var btnSeeMore = document.getElementById('btnSeeMoreRecs');
        if (btnSeeMore) {
            btnSeeMore.style.setProperty('display', 'none', 'important');
        }
    }
};
// Hàm cập nhật số lượng giỏ hàng bằng AJAX siêu tốc
window.handleUpdateQuantityAsync = function(productId, qty, inputElement) {
    var formData = new FormData();
    formData.append('productId', productId);
    formData.append('qty', qty);

    fetch('/cart/update-async', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) throw new Error("Network response was not ok");
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // 1. Cập nhật lại cột Thành tiền (Total) của riêng dòng sản phẩm đó
            var row = inputElement.closest('tr');
            var amountCell = row.querySelector('.text-brand.fw-bold');
            if (amountCell) {
                amountCell.innerText = data.amount;
            }

            // 2. Cập nhật khối tổng tiền đặt hàng (Order Summary) bên phải
            var summaryPrices = document.querySelectorAll('h3.text-brand.fw-bold, div.d-flex.justify-content-between.mb-2.small.text-secondary > span:last-child');
            summaryPrices.forEach(el => {
                if (el) el.innerText = data.grandTotal;
            });

            // 3. Cập nhật Badge trên Header
            var badgeEl = document.getElementById('cart-badge');
            if (badgeEl) {
                badgeEl.innerText = data.totalItems;
            }
        }
    })
    .catch(error => {
        console.error('Update Cart Error:', error);
    });
};
// =========================================================================
// 2. SỰ KIỆN KHỞI CHẠY KHI GIAO DIỆN LOAD XONG (CHỈ DÙNG 1 DOM CONTENT LOADED DUY NHẤT)
// =========================================================================
document.addEventListener("DOMContentLoaded", function() {
    console.log("Hệ thống giao diện Web đã khởi chạy thành công!");

    // Tránh lỗi gãy trang bằng khối kiểm tra điều kiện an toàn cho nút bấm phân trang
    var inputPage = document.getElementById('goToPageInput');
    if (inputPage) {
        inputPage.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                var btnGo = document.getElementById('btnGoToPage');
                if (btnGo) {
                    btnGo.click();
                }
            }
        });
    }

    // --- 🌟 CẤU HÌNH BIỂU ĐỒ AI DASHBOARD (ĐÃ ĐƯỢC GỘP PHẲNG, CHẠY CHUẨN CHỈ) ---
    const txChartCanvas = document.getElementById('transactionsChart');
    const catChartCanvas = document.getElementById('categoryRulesChart');

    if (txChartCanvas && catChartCanvas) {
        // Đọc dữ liệu thật từ Thymeleaf đẩy ra Canvas
        const successfulOrders = parseInt(txChartCanvas.getAttribute('data-successful-orders')) || 0;
        const totalProducts = parseInt(catChartCanvas.getAttribute('data-total-products')) || 0;

        // Tính toán số liệu phân bổ cột tự động
        const janData = Math.round(successfulOrders * 0.2);
        const febData = Math.round(successfulOrders * 0.3);
        const marData = Math.round(successfulOrders * 0.4);
        const aprData = Math.round(successfulOrders * 0.6);
        const mayData = Math.round(successfulOrders * 0.8);
        const junData = successfulOrders;

        // 1. VẼ BIỂU ĐỒ CỘT (APPROVED TRANSACTIONS)
        const ctxBar = txChartCanvas.getContext('2d');
        new Chart(ctxBar, {
            type: 'bar',
            data: {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                datasets: [{
                    label: 'Approved Transactions',
                    data: [janData, febData, marData, aprData, mayData, junData],
                    backgroundColor: 'rgba(25, 135, 84, 0.15)',
                    borderColor: '#198754',
                    borderWidth: 2,
                    borderRadius: 5,
                    barPercentage: 0.5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: '#f1f1f1' } },
                    x: { grid: { display: false } }
                }
            }
        });

        // 2. VẼ BIỂU ĐỒ TRÒN (PHÂN BỔ THEO DANH MỤC)
        const coffeeRules = Math.round(totalProducts * 0.4);
        const bakeryRules = Math.round(totalProducts * 0.3);
        const teaRules = Math.round(totalProducts * 0.2);
        const othersRules = Math.round(totalProducts * 0.1);

        const ctxPie = catChartCanvas.getContext('2d');
        new Chart(ctxPie, {
            type: 'doughnut',
            data: {
                labels: ['Coffee (CAT_02)', 'Bakery (CAT_01)', 'Tea (CAT_03)', 'Others'],
                datasets: [{
                    data: [coffeeRules, bakeryRules, teaRules, othersRules],
                    backgroundColor: ['#0d6efd', '#198754', '#ffc107', '#6c757d'],
                    borderWidth: 4,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 12, padding: 15, font: { size: 11, weight: 'bold' } }
                    }
                },
                cutout: '70%'
            }
        });
    }
}); // Kết thúc sự kiện DOMContentLoaded tổng duy nhất