using BookingCare.Services.Blog.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Blog.Data;

/// <summary>
/// Service for seeding initial data into Blog database
/// </summary>
public static class BlogDataSeeder
{
    /// <summary>
    /// Seed initial data if database is empty
    /// </summary>
    public static async Task SeedAsync(BlogDbContext context)
    {
        // Check if data already exists
        var hasCategories = await context.BlogCategories.AnyAsync();
        var hasBlogs = await context.Blogs.AnyAsync();

        if (hasCategories && hasBlogs)
        {
            return; // Data already exists, skip seeding
        }

        // Seed Categories
        if (!hasCategories)
        {
            var category1Id = Guid.NewGuid();
            var category2Id = Guid.NewGuid();
            var category3Id = Guid.NewGuid();

            var categories = new List<BlogCategoryEntity>
            {
                new BlogCategoryEntity
                {
                    Id = category1Id,
                    CategoryName = "Cẩm nang khám bệnh",
                    Description = "Tổng hợp kiến thức khám chữa bệnh, hướng dẫn chuẩn bị trước khi đi khám.",
                    ImageUrl = "/images/blog/categories/handbook.png",
                    Status = CategoryStatus.Active,
                    ParentId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new BlogCategoryEntity
                {
                    Id = category2Id,
                    CategoryName = "Tin tức y tế",
                    Description = "Cập nhật tin tức, thông báo và chính sách mới trong ngành y tế.",
                    ImageUrl = "/images/blog/categories/news.png",
                    Status = CategoryStatus.Active,
                    ParentId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new BlogCategoryEntity
                {
                    Id = category3Id,
                    CategoryName = "Sức khỏe tổng quát",
                    Description = "Kiến thức, lời khuyên về chăm sóc sức khỏe hằng ngày.",
                    ImageUrl = "/images/blog/categories/general-health.png",
                    Status = CategoryStatus.Active,
                    ParentId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.BlogCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Seed Blogs
            if (!hasBlogs)
            {
                var blog1Id = Guid.NewGuid();
                var blog2Id = Guid.NewGuid();
                var blog3Id = Guid.NewGuid();
                var blog4Id = Guid.NewGuid();

                var blogs = new List<BlogEntity>
                {
                    new BlogEntity
                    {
                        Id = blog1Id,
                        BlogCategoryId = category1Id,
                        TitleVi = "Cẩm nang khám tổng quát cho người đi lần đầu",
                        ContentVi = "<p>Khám sức khỏe tổng quát giúp bạn đánh giá toàn diện tình trạng sức khỏe hiện tại. Đây là bước đầu tiên quan trọng trong việc chăm sóc sức khỏe chủ động.</p><h2>Chuẩn bị trước khi khám</h2><p>Trước khi đi khám, bạn nên chuẩn bị:</p><ul><li>Danh sách các triệu chứng hoặc vấn đề sức khỏe</li><li>Hồ sơ bệnh án cũ (nếu có)</li><li>Danh sách thuốc đang sử dụng</li><li>Thông tin về tiền sử bệnh của gia đình</li></ul><h2>Quy trình khám tổng quát</h2><p>Quy trình khám tổng quát thường bao gồm:</p><ol><li>Khai thác tiền sử bệnh</li><li>Khám lâm sàng tổng quát</li><li>Xét nghiệm cơ bản (máu, nước tiểu)</li><li>Tư vấn và kết luận</li></ol>",
                        TitleEn = "General health checkup guide for first-timers",
                        ContentEn = "<p>A comprehensive health checkup helps you understand your current health status. This is an important first step in proactive health care.</p>",
                        ThumbnailUrl = "/images/blog/posts/checkup-first-time-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/checkup-first-time-hero.jpg",
                        Tag = "khám tổng quát, lần đầu đi khám",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 1, 10),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog2Id,
                        BlogCategoryId = category1Id,
                        TitleVi = "Chuẩn bị gì trước khi đi khám tim mạch?",
                        ContentVi = "<p>Trước khi đi khám tim mạch, bạn nên chuẩn bị đầy đủ hồ sơ bệnh án, đơn thuốc và các kết quả xét nghiệm trước đó.</p><h2>Những điều cần chuẩn bị</h2><p>Để buổi khám tim mạch diễn ra suôn sẻ, bạn cần:</p><ul><li>Nhịn ăn sáng (nếu có chỉ định xét nghiệm máu)</li><li>Mang theo hồ sơ bệnh án cũ</li><li>Ghi chép các triệu chứng gần đây</li><li>Danh sách thuốc đang dùng</li></ul>",
                        TitleEn = "How to prepare before a cardiology visit",
                        ContentEn = "<p>Before a cardiology visit, you should prepare your medical records, medications and previous test results.</p>",
                        ThumbnailUrl = "/images/blog/posts/cardiology-prepare-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/cardiology-prepare-hero.jpg",
                        Tag = "tim mạch, chuẩn bị đi khám",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 2, 5),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog3Id,
                        BlogCategoryId = category2Id,
                        TitleVi = "Cập nhật lịch nghỉ Tết của các bệnh viện lớn",
                        ContentVi = "<p>Dưới đây là tổng hợp lịch nghỉ Tết Nguyên đán của một số bệnh viện lớn tại TP.HCM và Hà Nội.</p><h2>Bệnh viện tại TP.HCM</h2><p>Nhiều bệnh viện sẽ nghỉ từ ngày 29 Tết đến mùng 3 Tết. Một số bệnh viện vẫn duy trì phòng cấp cứu 24/7.</p><h2>Bệnh viện tại Hà Nội</h2><p>Tương tự, các bệnh viện lớn tại Hà Nội cũng có lịch nghỉ tương ứng. Bệnh nhân nên liên hệ trước để xác nhận lịch làm việc.</p>",
                        TitleEn = "Tet holiday schedule of major hospitals",
                        ContentEn = "<p>Below is the Tet holiday schedule of major hospitals in Ho Chi Minh City and Hanoi.</p>",
                        ThumbnailUrl = "/images/blog/posts/tet-hospital-schedule-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/tet-hospital-schedule-hero.jpg",
                        Tag = "tin tức, lịch nghỉ tết",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = false,
                        PublishedAt = new DateTime(2024, 1, 25),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog4Id,
                        BlogCategoryId = category3Id,
                        TitleVi = "5 thói quen tốt giúp nâng cao sức khỏe mỗi ngày",
                        ContentVi = "<p>Duy trì chế độ ăn hợp lý, vận động thường xuyên và ngủ đủ giấc là những yếu tố quan trọng để có sức khỏe tốt.</p><h2>1. Ăn uống lành mạnh</h2><p>Chế độ ăn cân bằng với nhiều rau xanh, trái cây và protein chất lượng cao.</p><h2>2. Tập thể dục đều đặn</h2><p>Ít nhất 30 phút vận động mỗi ngày giúp cải thiện sức khỏe tim mạch và tinh thần.</p><h2>3. Ngủ đủ giấc</h2><p>Ngủ 7-8 giờ mỗi đêm giúp cơ thể phục hồi và tái tạo năng lượng.</p><h2>4. Uống đủ nước</h2><p>Uống ít nhất 2 lít nước mỗi ngày để duy trì chức năng cơ thể.</p><h2>5. Quản lý căng thẳng</h2><p>Thiền, yoga hoặc các hoạt động thư giãn giúp giảm stress hiệu quả.</p>",
                        TitleEn = "5 daily habits to improve your health",
                        ContentEn = "<p>Maintain a balanced diet, exercise regularly and get enough sleep are important factors for good health.</p>",
                        ThumbnailUrl = "/images/blog/posts/healthy-habits-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/healthy-habits-hero.jpg",
                        Tag = "sức khỏe tổng quát, thói quen tốt",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 3, 1),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Blogs.AddRangeAsync(blogs);
                await context.SaveChangesAsync();
            }
            return;
        }

        // Seed Blogs (if categories exist but blogs don't)
        if (!hasBlogs)
        {
            var categories = await context.BlogCategories.ToListAsync();
            if (categories.Count >= 3)
            {
                var category1Id = categories[0].Id;
                var category2Id = categories[1].Id;
                var category3Id = categories[2].Id;

                var blog1Id = Guid.NewGuid();
                var blog2Id = Guid.NewGuid();
                var blog3Id = Guid.NewGuid();
                var blog4Id = Guid.NewGuid();

                var blogs = new List<BlogEntity>
                {
                    new BlogEntity
                    {
                        Id = blog1Id,
                        BlogCategoryId = category1Id,
                        TitleVi = "Cẩm nang khám tổng quát cho người đi lần đầu",
                        ContentVi = "<p>Khám sức khỏe tổng quát giúp bạn đánh giá toàn diện tình trạng sức khỏe hiện tại. Đây là bước đầu tiên quan trọng trong việc chăm sóc sức khỏe chủ động.</p><h2>Chuẩn bị trước khi khám</h2><p>Trước khi đi khám, bạn nên chuẩn bị:</p><ul><li>Danh sách các triệu chứng hoặc vấn đề sức khỏe</li><li>Hồ sơ bệnh án cũ (nếu có)</li><li>Danh sách thuốc đang sử dụng</li><li>Thông tin về tiền sử bệnh của gia đình</li></ul><h2>Quy trình khám tổng quát</h2><p>Quy trình khám tổng quát thường bao gồm:</p><ol><li>Khai thác tiền sử bệnh</li><li>Khám lâm sàng tổng quát</li><li>Xét nghiệm cơ bản (máu, nước tiểu)</li><li>Tư vấn và kết luận</li></ol>",
                        TitleEn = "General health checkup guide for first-timers",
                        ContentEn = "<p>A comprehensive health checkup helps you understand your current health status. This is an important first step in proactive health care.</p>",
                        ThumbnailUrl = "/images/blog/posts/checkup-first-time-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/checkup-first-time-hero.jpg",
                        Tag = "khám tổng quát, lần đầu đi khám",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 1, 10),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog2Id,
                        BlogCategoryId = category1Id,
                        TitleVi = "Chuẩn bị gì trước khi đi khám tim mạch?",
                        ContentVi = "<p>Trước khi đi khám tim mạch, bạn nên chuẩn bị đầy đủ hồ sơ bệnh án, đơn thuốc và các kết quả xét nghiệm trước đó.</p><h2>Những điều cần chuẩn bị</h2><p>Để buổi khám tim mạch diễn ra suôn sẻ, bạn cần:</p><ul><li>Nhịn ăn sáng (nếu có chỉ định xét nghiệm máu)</li><li>Mang theo hồ sơ bệnh án cũ</li><li>Ghi chép các triệu chứng gần đây</li><li>Danh sách thuốc đang dùng</li></ul>",
                        TitleEn = "How to prepare before a cardiology visit",
                        ContentEn = "<p>Before a cardiology visit, you should prepare your medical records, medications and previous test results.</p>",
                        ThumbnailUrl = "/images/blog/posts/cardiology-prepare-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/cardiology-prepare-hero.jpg",
                        Tag = "tim mạch, chuẩn bị đi khám",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 2, 5),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog3Id,
                        BlogCategoryId = category2Id,
                        TitleVi = "Cập nhật lịch nghỉ Tết của các bệnh viện lớn",
                        ContentVi = "<p>Dưới đây là tổng hợp lịch nghỉ Tết Nguyên đán của một số bệnh viện lớn tại TP.HCM và Hà Nội.</p><h2>Bệnh viện tại TP.HCM</h2><p>Nhiều bệnh viện sẽ nghỉ từ ngày 29 Tết đến mùng 3 Tết. Một số bệnh viện vẫn duy trì phòng cấp cứu 24/7.</p><h2>Bệnh viện tại Hà Nội</h2><p>Tương tự, các bệnh viện lớn tại Hà Nội cũng có lịch nghỉ tương ứng. Bệnh nhân nên liên hệ trước để xác nhận lịch làm việc.</p>",
                        TitleEn = "Tet holiday schedule of major hospitals",
                        ContentEn = "<p>Below is the Tet holiday schedule of major hospitals in Ho Chi Minh City and Hanoi.</p>",
                        ThumbnailUrl = "/images/blog/posts/tet-hospital-schedule-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/tet-hospital-schedule-hero.jpg",
                        Tag = "tin tức, lịch nghỉ tết",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = false,
                        PublishedAt = new DateTime(2024, 1, 25),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new BlogEntity
                    {
                        Id = blog4Id,
                        BlogCategoryId = category3Id,
                        TitleVi = "5 thói quen tốt giúp nâng cao sức khỏe mỗi ngày",
                        ContentVi = "<p>Duy trì chế độ ăn hợp lý, vận động thường xuyên và ngủ đủ giấc là những yếu tố quan trọng để có sức khỏe tốt.</p><h2>1. Ăn uống lành mạnh</h2><p>Chế độ ăn cân bằng với nhiều rau xanh, trái cây và protein chất lượng cao.</p><h2>2. Tập thể dục đều đặn</h2><p>Ít nhất 30 phút vận động mỗi ngày giúp cải thiện sức khỏe tim mạch và tinh thần.</p><h2>3. Ngủ đủ giấc</h2><p>Ngủ 7-8 giờ mỗi đêm giúp cơ thể phục hồi và tái tạo năng lượng.</p><h2>4. Uống đủ nước</h2><p>Uống ít nhất 2 lít nước mỗi ngày để duy trì chức năng cơ thể.</p><h2>5. Quản lý căng thẳng</h2><p>Thiền, yoga hoặc các hoạt động thư giãn giúp giảm stress hiệu quả.</p>",
                        TitleEn = "5 daily habits to improve your health",
                        ContentEn = "<p>Maintain a balanced diet, exercise regularly and get enough sleep are important factors for good health.</p>",
                        ThumbnailUrl = "/images/blog/posts/healthy-habits-thumb.jpg",
                        HeroImageUrl = "/images/blog/posts/healthy-habits-hero.jpg",
                        Tag = "sức khỏe tổng quát, thói quen tốt",
                        Source = "BookingCare",
                        Status = BlogStatus.Active,
                        Featured = true,
                        PublishedAt = new DateTime(2024, 3, 1),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Blogs.AddRangeAsync(blogs);
                await context.SaveChangesAsync();
            }
        }
    }
}

