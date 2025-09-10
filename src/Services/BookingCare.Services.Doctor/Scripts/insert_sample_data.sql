-- =============================================================================
-- SAMPLE DATA INSERTION SCRIPT FOR BOOKING CARE DOCTOR SERVICE
-- =============================================================================
-- This script inserts sample data for all entities in the Doctor service
-- Run this script after creating the database schema

USE [MABS_Doctor]; -- Replace with your actual database name
GO

-- =============================================================================
-- 1. INSERT POSITIONS
-- =============================================================================
INSERT INTO Positions (id, name, created_at, updated_at)
VALUES 
    (NEWID(), N'Bác sĩ chuyên khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ đa khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ nội khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ ngoại khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ nhi khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ sản phụ khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ tim mạch', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ thần kinh', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ da liễu', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ mắt', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 2. INSERT PRICES
-- =============================================================================
INSERT INTO Prices (id, amount)
VALUES 
    (NEWID(), 200000),
    (NEWID(), 300000),
    (NEWID(), 400000),
    (NEWID(), 500000),
    (NEWID(), 600000),
    (NEWID(), 700000),
    (NEWID(), 800000),
    (NEWID(), 900000),
    (NEWID(), 1000000),
    (NEWID(), 1200000),
    (NEWID(), 1500000),
    (NEWID(), 2000000);
GO

-- =============================================================================
-- 3. INSERT PRICE RULES
-- =============================================================================
INSERT INTO price_rules (id, name, base_price, min_experience, position, status, created_at, updated_at)
VALUES 
    (NEWID(), N'Khám đa khoa cơ bản', 200000, 0, N'Bác sĩ đa khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám đa khoa nâng cao', 300000, 2, N'Bác sĩ đa khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám đa khoa chuyên sâu', 400000, 5, N'Bác sĩ đa khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám chuyên khoa cơ bản', 500000, 0, N'Bác sĩ chuyên khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám chuyên khoa nâng cao', 600000, 3, N'Bác sĩ chuyên khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám chuyên khoa chuyên sâu', 800000, 7, N'Bác sĩ chuyên khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám nội khoa cơ bản', 400000, 0, N'Bác sĩ nội khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám nội khoa nâng cao', 500000, 2, N'Bác sĩ nội khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám nội khoa chuyên sâu', 600000, 5, N'Bác sĩ nội khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám ngoại khoa cơ bản', 700000, 0, N'Bác sĩ ngoại khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám ngoại khoa nâng cao', 900000, 3, N'Bác sĩ ngoại khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Khám ngoại khoa chuyên sâu', 1200000, 8, N'Bác sĩ ngoại khoa', N'ACTIVE', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 4. INSERT DOCTORS
-- =============================================================================
DECLARE @PositionId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Positions WHERE name = N'Bác sĩ chuyên khoa');
DECLARE @PositionId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Positions WHERE name = N'Bác sĩ đa khoa');
DECLARE @PositionId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Positions WHERE name = N'Bác sĩ nội khoa');
DECLARE @PositionId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Positions WHERE name = N'Bác sĩ ngoại khoa');
DECLARE @PositionId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Positions WHERE name = N'Bác sĩ nhi khoa');

INSERT INTO Doctors (id, account_id, email, address, first_name, last_name, gender, position_id, specialty_id, clinic_id, bio, years_of_experience, avatar_url, created_at, updated_at)
VALUES 
    (NEWID(), NEWID(), N'dr.nguyen.van.a@bookingcare.com', N'123 Đường Lê Lợi, Quận 1, TP.HCM', N'Nguyễn', N'Văn A', N'MALE', @PositionId1, NEWID(), NEWID(), N'Bác sĩ chuyên khoa tim mạch với hơn 10 năm kinh nghiệm. Tốt nghiệp Đại học Y Hà Nội và có chứng chỉ chuyên khoa tim mạch tại Pháp.', 10, N'https://example.com/avatar1.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.tran.thi.b@bookingcare.com', N'456 Đường Nguyễn Huệ, Quận 1, TP.HCM', N'Trần', N'Thị B', N'FEMALE', @PositionId2, NEWID(), NEWID(), N'Bác sĩ đa khoa có kinh nghiệm 8 năm. Chuyên khám và điều trị các bệnh thông thường, tư vấn sức khỏe tổng quát.', 8, N'https://example.com/avatar2.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.le.van.c@bookingcare.com', N'789 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', N'Lê', N'Văn C', N'MALE', @PositionId3, NEWID(), NEWID(), N'Bác sĩ nội khoa chuyên điều trị các bệnh về tiêu hóa, hô hấp và nội tiết. Có 12 năm kinh nghiệm trong lĩnh vực.', 12, N'https://example.com/avatar3.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.pham.thi.d@bookingcare.com', N'321 Đường Cách Mạng Tháng 8, Quận 10, TP.HCM', N'Phạm', N'Thị D', N'FEMALE', @PositionId4, NEWID(), NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật nội soi. Tốt nghiệp chuyên khoa ngoại tại Đại học Y TP.HCM.', 6, N'https://example.com/avatar4.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.hoang.van.e@bookingcare.com', N'654 Đường Lý Tự Trọng, Quận 1, TP.HCM', N'Hoàng', N'Văn E', N'MALE', @PositionId5, NEWID(), NEWID(), N'Bác sĩ nhi khoa chuyên điều trị cho trẻ em từ sơ sinh đến 18 tuổi. Có kinh nghiệm 15 năm và rất yêu trẻ em.', 15, N'https://example.com/avatar5.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.vu.thi.f@bookingcare.com', N'987 Đường Võ Văn Tần, Quận 3, TP.HCM', N'Vũ', N'Thị F', N'FEMALE', @PositionId1, NEWID(), NEWID(), N'Bác sĩ chuyên khoa da liễu với 9 năm kinh nghiệm. Chuyên điều trị các bệnh về da, tóc và móng.', 9, N'https://example.com/avatar6.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.dang.van.g@bookingcare.com', N'147 Đường Nguyễn Thị Minh Khai, Quận 3, TP.HCM', N'Đặng', N'Văn G', N'MALE', @PositionId2, NEWID(), NEWID(), N'Bác sĩ đa khoa trẻ tuổi nhưng rất tận tâm. Chuyên khám sức khỏe định kỳ và tư vấn dinh dưỡng.', 3, N'https://example.com/avatar7.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.bui.thi.h@bookingcare.com', N'258 Đường Pasteur, Quận 3, TP.HCM', N'Bùi', N'Thị H', N'FEMALE', @PositionId3, NEWID(), NEWID(), N'Bác sĩ nội khoa chuyên về tim mạch và huyết áp. Có chứng chỉ chuyên khoa tim mạch quốc tế.', 11, N'https://example.com/avatar8.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.ngo.van.i@bookingcare.com', N'369 Đường Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM', N'Ngô', N'Văn I', N'MALE', @PositionId4, NEWID(), NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật thẩm mỹ và tái tạo. Có kinh nghiệm 7 năm trong lĩnh vực.', 7, N'https://example.com/avatar9.jpg', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), NEWID(), N'dr.ly.thi.j@bookingcare.com', N'741 Đường Đinh Tiên Hoàng, Quận Bình Thạnh, TP.HCM', N'Lý', N'Thị J', N'FEMALE', @PositionId5, NEWID(), NEWID(), N'Bác sĩ nhi khoa chuyên về sơ sinh và trẻ sơ sinh. Có kinh nghiệm 13 năm và rất được phụ huynh tin tưởng.', 13, N'https://example.com/avatar10.jpg', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 5. INSERT DOCTOR-PRICE RELATIONSHIPS
-- =============================================================================
DECLARE @DoctorId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Nguyễn' AND last_name = N'Văn A');
DECLARE @DoctorId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Trần' AND last_name = N'Thị B');
DECLARE @DoctorId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Lê' AND last_name = N'Văn C');
DECLARE @DoctorId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Phạm' AND last_name = N'Thị D');
DECLARE @DoctorId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Hoàng' AND last_name = N'Văn E');

DECLARE @PriceId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Prices WHERE amount = 500000);
DECLARE @PriceId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Prices WHERE amount = 300000);
DECLARE @PriceId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Prices WHERE amount = 600000);
DECLARE @PriceId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Prices WHERE amount = 700000);
DECLARE @PriceId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Prices WHERE amount = 800000);

INSERT INTO doctor_prices (doctor_id, price_id, is_override)
VALUES 
    (@DoctorId1, @PriceId1, 1),
    (@DoctorId2, @PriceId2, 1),
    (@DoctorId3, @PriceId3, 1),
    (@DoctorId4, @PriceId4, 1),
    (@DoctorId5, @PriceId5, 1);
GO

-- =============================================================================
-- 6. INSERT ADDITIONAL SAMPLE DATA FOR TESTING
-- =============================================================================

-- Insert more positions
INSERT INTO Positions (id, name, created_at, updated_at)
VALUES 
    (NEWID(), N'Bác sĩ tâm thần', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ vật lý trị liệu', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ dinh dưỡng', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ y học cổ truyền', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ cấp cứu', GETUTCDATE(), GETUTCDATE());
GO

-- Insert more prices
INSERT INTO Prices (id, amount)
VALUES 
    (NEWID(), 250000),
    (NEWID(), 350000),
    (NEWID(), 450000),
    (NEWID(), 550000),
    (NEWID(), 650000),
    (NEWID(), 750000),
    (NEWID(), 850000),
    (NEWID(), 950000),
    (NEWID(), 1100000),
    (NEWID(), 1300000),
    (NEWID(), 1600000),
    (NEWID(), 1800000),
    (NEWID(), 2200000),
    (NEWID(), 2500000),
    (NEWID(), 3000000);
GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================

-- Check inserted data
SELECT 'Positions' as table_name, COUNT(*) as record_count FROM Positions
UNION ALL
SELECT 'Prices' as table_name, COUNT(*) as record_count FROM Prices
UNION ALL
SELECT 'PriceRules' as table_name, COUNT(*) as record_count FROM price_rules
UNION ALL
SELECT 'Doctors' as table_name, COUNT(*) as record_count FROM Doctors
UNION ALL
SELECT 'doctor_prices' as table_name, COUNT(*) as record_count FROM doctor_prices;

-- Show sample data
SELECT TOP 5 p.name as position_name
FROM Positions p;

SELECT TOP 5 pr.name, pr.base_price, pr.min_experience, pr.position
FROM price_rules pr;

SELECT TOP 5 d.first_name + ' ' + d.last_name as doctor_name, d.email, d.years_of_experience, p.name as position_name
FROM Doctors d
LEFT JOIN Positions p ON d.position_id = p.id;

SELECT TOP 5 d.first_name + ' ' + d.last_name as doctor_name, pr.amount as price_amount, dp.is_override
FROM doctor_prices dp
JOIN Doctors d ON dp.doctor_id = d.id
JOIN Prices pr ON dp.price_id = pr.id;

PRINT 'Sample data insertion completed successfully!';
PRINT 'Total records inserted:';
PRINT '- Positions: 15 records';
PRINT '- Prices: 27 records';
PRINT '- Price Rules: 12 records';
PRINT '- Doctors: 10 records';
PRINT '- Doctor-Price Relationships: 5 records';


