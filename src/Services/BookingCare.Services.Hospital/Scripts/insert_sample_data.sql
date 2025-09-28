-- =============================================================================
-- SAMPLE DATA INSERTION SCRIPT FOR BOOKING CARE HOSPITAL SERVICE
-- =============================================================================
-- This script inserts sample data for all entities in the Hospital service
-- Run this script after creating the database schema

USE [MABS_Hospital];
GO

-- Define constants
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();
DECLARE @StatusActive NVARCHAR(20) = 'ACTIVE';
DECLARE @StatusInactive NVARCHAR(20) = 'INACTIVE';
DECLARE @SubscriptionStatusActive NVARCHAR(20) = 'ACTIVE';
DECLARE @SubscriptionStatusExpired NVARCHAR(20) = 'EXPIRED';
DECLARE @SubscriptionStatusPending NVARCHAR(20) = 'PENDING';
DECLARE @SubscriptionStatusTrial NVARCHAR(20) = 'TRIAL';
GO

-- =============================================================================
-- 1. INSERT SUBSCRIPTION PLANS
-- =============================================================================
INSERT INTO subscription_plans (id, name, description, price, billing_cycle, max_doctors, max_appointments, max_storage_gb, features, status, created_at, updated_at)
VALUES 
    (NEWID(), N'Gói Cơ Bản', N'Gói dành cho phòng khám nhỏ với các tính năng cơ bản', 500000, N'MONTHLY', 5, 100, 1, N'Quản lý bệnh nhân cơ bản, Lịch hẹn, Báo cáo đơn giản', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Tiêu Chuẩn', N'Gói dành cho phòng khám vừa với nhiều tính năng hơn', 1000000, N'MONTHLY', 15, 500, 5, N'Quản lý bệnh nhân nâng cao, Lịch hẹn, Báo cáo chi tiết, Tích hợp thanh toán', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Chuyên Nghiệp', N'Gói dành cho bệnh viện lớn với đầy đủ tính năng', 2000000, N'MONTHLY', 50, 2000, 20, N'Tất cả tính năng, API không giới hạn, Hỗ trợ 24/7, Tùy chỉnh giao diện', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Doanh Nghiệp', N'Gói dành cho hệ thống bệnh viện lớn', 5000000, N'MONTHLY', 200, 10000, 100, N'Tất cả tính năng Premium, Multi-tenant, Tích hợp hệ thống, Đào tạo chuyên sâu', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Năm Cơ Bản', N'Gói cơ bản thanh toán theo năm (giảm 20%)', 4800000, N'YEARLY', 5, 100, 1, N'Quản lý bệnh nhân cơ bản, Lịch hẹn, Báo cáo đơn giản', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Năm Tiêu Chuẩn', N'Gói tiêu chuẩn thanh toán theo năm (giảm 20%)', 9600000, N'YEARLY', 15, 500, 5, N'Quản lý bệnh nhân nâng cao, Lịch hẹn, Báo cáo chi tiết, Tích hợp thanh toán', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), N'Gói Dùng Thử', N'Gói dùng thử miễn phí 30 ngày', 0, N'TRIAL', 3, 50, 0.5, N'Tính năng cơ bản, Giới hạn 30 ngày', @StatusActive, @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 2. INSERT HOSPITALS
-- =============================================================================
INSERT INTO hospitals (id, account_id, name, address, phone, email, description, background_url, avatar_url, status, created_at, updated_at)
VALUES 
    (NEWID(), NEWID(), N'Bệnh viện Đa khoa Thành phố', N'123 Đường Lê Lợi, Quận 1, TP.HCM', N'028-3822-1234', N'info@bvdktphcm.vn', N'Bệnh viện đa khoa hàng đầu tại TP.HCM với đội ngũ bác sĩ giàu kinh nghiệm và trang thiết bị y tế hiện đại. Chuyên khám và điều trị các bệnh lý từ cơ bản đến phức tạp.', N'https://example.com/hospital1-bg.jpg', N'https://example.com/hospital1-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Phòng khám Tim Mạch Sài Gòn', N'456 Đường Nguyễn Huệ, Quận 1, TP.HCM', N'028-3822-5678', N'contact@timmaschsaigon.vn', N'Phòng khám chuyên khoa tim mạch với các bác sĩ chuyên khoa tim mạch hàng đầu. Trang bị máy siêu âm tim, điện tim và các thiết bị chẩn đoán hiện đại.', N'https://example.com/hospital2-bg.jpg', N'https://example.com/hospital2-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Bệnh viện Nhi đồng Thành phố', N'789 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', N'028-3899-1234', N'info@bvnidong.vn', N'Bệnh viện chuyên khoa nhi hàng đầu tại miền Nam, chuyên điều trị cho trẻ em từ sơ sinh đến 18 tuổi. Có khoa cấp cứu nhi 24/7 và các chuyên khoa nhi.', N'https://example.com/hospital3-bg.jpg', N'https://example.com/hospital3-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Phòng khám Da liễu Dr. Beauty', N'321 Đường Cách Mạng Tháng 8, Quận 10, TP.HCM', N'028-3865-9999', N'hello@drbeauty.vn', N'Phòng khám chuyên khoa da liễu và thẩm mỹ da với các liệu trình điều trị hiện đại. Chuyên điều trị mụn, nám, tàn nhang và các vấn đề về da.', N'https://example.com/hospital4-bg.jpg', N'https://example.com/hospital4-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Bệnh viện Mắt Sài Gòn', N'654 Đường Lý Tự Trọng, Quận 1, TP.HCM', N'028-3829-7777', N'info@matsaigon.vn', N'Bệnh viện chuyên khoa mắt với đội ngũ bác sĩ chuyên khoa mắt giàu kinh nghiệm. Chuyên phẫu thuật mắt, điều trị tật khúc xạ, bệnh võng mạc và các bệnh lý về mắt.', N'https://example.com/hospital5-bg.jpg', N'https://example.com/hospital5-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Phòng khám Răng Hàm Mặt Dental Plus', N'987 Đường Võ Văn Tần, Quận 3, TP.HCM', N'028-3930-8888', N'care@dentalplus.vn', N'Phòng khám nha khoa hiện đại với dịch vụ toàn diện từ khám tổng quát đến phẫu thuật răng hàm mặt. Trang bị máy X-quang kỹ thuật số và các thiết bị nha khoa tiên tiến.', N'https://example.com/hospital6-bg.jpg', N'https://example.com/hospital6-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Bệnh viện Phụ sản Hùng Vương', N'147 Đường Nguyễn Thị Minh Khai, Quận 3, TP.HCM', N'028-3930-5555', N'info@bvhungvuong.vn', N'Bệnh viện chuyên khoa phụ sản hàng đầu với dịch vụ khám thai, sinh con và điều trị các bệnh phụ khoa. Có phòng sinh hiện đại và khoa sơ sinh.', N'https://example.com/hospital7-bg.jpg', N'https://example.com/hospital7-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Phòng khám Tâm lý Dr. Mind', N'258 Đường Pasteur, Quận 3, TP.HCM', N'028-3822-6666', N'support@drmind.vn', N'Phòng khám chuyên về tâm lý học và tâm thần học với các bác sĩ tâm lý có kinh nghiệm. Chuyên tư vấn tâm lý, điều trị stress, trầm cảm và các rối loạn tâm lý.', N'https://example.com/hospital8-bg.jpg', N'https://example.com/hospital8-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Bệnh viện Chỉnh hình và Phục hồi chức năng', N'369 Đường Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM', N'028-3930-3333', N'info@bvchinhhinh.vn', N'Bệnh viện chuyên khoa chỉnh hình với các dịch vụ phẫu thuật xương khớp, phục hồi chức năng và vật lý trị liệu. Có khoa cấp cứu chấn thương 24/7.', N'https://example.com/hospital9-bg.jpg', N'https://example.com/hospital9-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'Phòng khám Dinh dưỡng Healthy Life', N'741 Đường Đinh Tiên Hoàng, Quận Bình Thạnh, TP.HCM', N'028-3899-4444', N'hello@healthylife.vn', N'Phòng khám chuyên về dinh dưỡng và tư vấn sức khỏe. Chuyên tư vấn chế độ ăn uống, giảm cân, tăng cân và điều trị các bệnh liên quan đến dinh dưỡng.', N'https://example.com/hospital10-bg.jpg', N'https://example.com/hospital10-avatar.jpg', @StatusActive, @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 3. INSERT HOSPITAL SUBSCRIPTIONS
-- =============================================================================
DECLARE @HospitalId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Bệnh viện Đa khoa Thành phố');
DECLARE @HospitalId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Phòng khám Tim Mạch Sài Gòn');
DECLARE @HospitalId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Bệnh viện Nhi đồng Thành phố');
DECLARE @HospitalId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Phòng khám Da liễu Dr. Beauty');
DECLARE @HospitalId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Bệnh viện Mắt Sài Gòn');
DECLARE @HospitalId6 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Phòng khám Răng Hàm Mặt Dental Plus');
DECLARE @HospitalId7 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Bệnh viện Phụ sản Hùng Vương');
DECLARE @HospitalId8 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Phòng khám Tâm lý Dr. Mind');
DECLARE @HospitalId9 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Bệnh viện Chỉnh hình và Phục hồi chức năng');
DECLARE @HospitalId10 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM hospitals WHERE name = N'Phòng khám Dinh dưỡng Healthy Life');

DECLARE @PlanBasic UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM subscription_plans WHERE name = N'Gói Cơ Bản');
DECLARE @PlanStandard UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM subscription_plans WHERE name = N'Gói Tiêu Chuẩn');
DECLARE @PlanProfessional UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM subscription_plans WHERE name = N'Gói Chuyên Nghiệp');
DECLARE @PlanEnterprise UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM subscription_plans WHERE name = N'Gói Doanh Nghiệp');
DECLARE @PlanTrial UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM subscription_plans WHERE name = N'Gói Dùng Thử');

INSERT INTO hospital_subscriptions (hospital_subscription_id, hospital_id, subscription_id, start_date, end_date, status, created_at, updated_at)
VALUES 
    (NEWID(), @HospitalId1, @PlanEnterprise, DATEADD(day, -30, @CurrentTime), DATEADD(day, 335, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId2, @PlanStandard, DATEADD(day, -15, @CurrentTime), DATEADD(day, 345, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId3, @PlanProfessional, DATEADD(day, -45, @CurrentTime), DATEADD(day, 315, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId4, @PlanBasic, DATEADD(day, -10, @CurrentTime), DATEADD(day, 350, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId5, @PlanStandard, DATEADD(day, -20, @CurrentTime), DATEADD(day, 340, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId6, @PlanBasic, DATEADD(day, -5, @CurrentTime), DATEADD(day, 355, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId7, @PlanProfessional, DATEADD(day, -60, @CurrentTime), DATEADD(day, 300, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId8, @PlanTrial, DATEADD(day, -25, @CurrentTime), DATEADD(day, 5, @CurrentTime), @SubscriptionStatusTrial, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId9, @PlanStandard, DATEADD(day, -35, @CurrentTime), DATEADD(day, 325, @CurrentTime), @SubscriptionStatusActive, @CurrentTime, @CurrentTime),
    (NEWID(), @HospitalId10, @PlanBasic, DATEADD(day, -2, @CurrentTime), DATEADD(day, 358, @CurrentTime), @SubscriptionStatusPending, @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 4. INSERT HOSPITAL SPECIALTIES (Sample specialty IDs)
-- =============================================================================
-- Note: These are sample specialty IDs. In real implementation, these should reference actual specialty entities
INSERT INTO hospital_specialties (hospital_id, specialty_id)
VALUES 
    (@HospitalId1, NEWID()), -- Đa khoa - Nội khoa
    (@HospitalId1, NEWID()), -- Đa khoa - Ngoại khoa
    (@HospitalId1, NEWID()), -- Đa khoa - Nhi khoa
    (@HospitalId1, NEWID()), -- Đa khoa - Phụ sản
    (@HospitalId2, NEWID()), -- Tim mạch
    (@HospitalId3, NEWID()), -- Nhi khoa
    (@HospitalId3, NEWID()), -- Nhi khoa cấp cứu
    (@HospitalId4, NEWID()), -- Da liễu
    (@HospitalId4, NEWID()), -- Thẩm mỹ da
    (@HospitalId5, NEWID()), -- Khoa mắt
    (@HospitalId5, NEWID()), -- Phẫu thuật mắt
    (@HospitalId6, NEWID()), -- Nha khoa
    (@HospitalId6, NEWID()), -- Phẫu thuật răng hàm mặt
    (@HospitalId7, NEWID()), -- Phụ khoa
    (@HospitalId7, NEWID()), -- Sản khoa
    (@HospitalId8, NEWID()), -- Tâm lý học
    (@HospitalId8, NEWID()), -- Tâm thần học
    (@HospitalId9, NEWID()), -- Chỉnh hình
    (@HospitalId9, NEWID()), -- Phục hồi chức năng
    (@HospitalId10, NEWID()); -- Dinh dưỡng
GO

-- =============================================================================
-- 5. INSERT HOSPITAL IMAGES
-- =============================================================================
INSERT INTO hospital_images (id, hospital_id, s3_key, image_url, description, created_at)
VALUES 
    (NEWID(), @HospitalId1, 'hospitals/hospital1/main-entrance.jpg', 'https://example.com/hospital1-main.jpg', N'Cổng chính bệnh viện', @CurrentTime),
    (NEWID(), @HospitalId1, 'hospitals/hospital1/lobby.jpg', 'https://example.com/hospital1-lobby.jpg', N'Sảnh chính tiếp đón', @CurrentTime),
    (NEWID(), @HospitalId1, 'hospitals/hospital1/emergency-room.jpg', 'https://example.com/hospital1-emergency.jpg', N'Phòng cấp cứu', @CurrentTime),
    (NEWID(), @HospitalId2, 'hospitals/hospital2/consultation-room.jpg', 'https://example.com/hospital2-consultation.jpg', N'Phòng khám tim mạch', @CurrentTime),
    (NEWID(), @HospitalId2, 'hospitals/hospital2/ecg-room.jpg', 'https://example.com/hospital2-ecg.jpg', N'Phòng điện tim', @CurrentTime),
    (NEWID(), @HospitalId3, 'hospitals/hospital3/pediatric-ward.jpg', 'https://example.com/hospital3-pediatric.jpg', N'Khoa nhi', @CurrentTime),
    (NEWID(), @HospitalId3, 'hospitals/hospital3/nicu.jpg', 'https://example.com/hospital3-nicu.jpg', N'Khoa sơ sinh', @CurrentTime),
    (NEWID(), @HospitalId4, 'hospitals/hospital4/treatment-room.jpg', 'https://example.com/hospital4-treatment.jpg', N'Phòng điều trị da', @CurrentTime),
    (NEWID(), @HospitalId5, 'hospitals/hospital5/surgery-room.jpg', 'https://example.com/hospital5-surgery.jpg', N'Phòng phẫu thuật mắt', @CurrentTime),
    (NEWID(), @HospitalId6, 'hospitals/hospital6/dental-chair.jpg', 'https://example.com/hospital6-dental.jpg', N'Ghế nha khoa', @CurrentTime);
GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
SELECT 'subscription_plans' as table_name, COUNT(*) as record_count FROM subscription_plans
UNION ALL
SELECT 'hospitals' as table_name, COUNT(*) as record_count FROM hospitals
UNION ALL
SELECT 'hospital_subscriptions' as table_name, COUNT(*) as record_count FROM hospital_subscriptions
UNION ALL
SELECT 'hospital_specialties' as table_name, COUNT(*) as record_count FROM hospital_specialties
UNION ALL
SELECT 'hospital_images' as table_name, COUNT(*) as record_count FROM hospital_images;

-- Show sample hospitals with their active subscriptions
SELECT 
    h.name as hospital_name,
    h.email,
    h.phone,
    sp.name as subscription_plan,
    hs.status as subscription_status,
    hs.start_date,
    hs.end_date
FROM hospitals h
LEFT JOIN hospital_subscriptions hs ON h.id = hs.hospital_id
LEFT JOIN subscription_plans sp ON hs.subscription_id = sp.id
WHERE h.status = 'ACTIVE'
ORDER BY h.name;

PRINT 'Sample data insertion completed successfully!';
PRINT 'Total records inserted:';
PRINT '- Subscription Plans: 7';
PRINT '- Hospitals: 10';
PRINT '- Hospital Subscriptions: 10';
PRINT '- Hospital Specialties: 20';
PRINT '- Hospital Images: 10';
