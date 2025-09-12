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
-- 2. INSERT PRICES (removed)
-- =============================================================================
-- Removed since Price entity is deprecated
-- GO

-- =============================================================================
-- 3. INSERT PRICE RULES (removed)
-- =============================================================================
-- Removed since price rules are deprecated
-- GO

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
-- 5. INSERT DOCTOR-PRICE RELATIONSHIPS (now amount stored directly on doctor_prices)
-- =============================================================================
DECLARE @DoctorId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Nguyễn' AND last_name = N'Văn A');
DECLARE @DoctorId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Trần' AND last_name = N'Thị B');
DECLARE @DoctorId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Lê' AND last_name = N'Văn C');
DECLARE @DoctorId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Phạm' AND last_name = N'Thị D');
DECLARE @DoctorId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Hoàng' AND last_name = N'Văn E');
DECLARE @DoctorId6 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Vũ' AND last_name = N'Thị F');
DECLARE @DoctorId7 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Đặng' AND last_name = N'Văn G');
DECLARE @DoctorId8 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Bùi' AND last_name = N'Thị H');
DECLARE @DoctorId9 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Ngô' AND last_name = N'Văn I');
DECLARE @DoctorId10 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM Doctors WHERE first_name = N'Lý' AND last_name = N'Thị J');

-- Insert direct amounts for each doctor
INSERT INTO doctor_prices (id, doctor_id, amount, created_at, updated_at)
VALUES 
    (NEWID(), @DoctorId1, 500000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId2, 300000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId3, 600000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId4, 700000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId5, 800000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId6, 550000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId7, 350000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId8, 650000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId9, 750000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId10, 850000, GETUTCDATE(), GETUTCDATE());
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

-- Removed additional prices section
-- GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================

-- Check inserted data
SELECT 'Positions' as table_name, COUNT(*) as record_count FROM Positions
UNION ALL
SELECT 'Doctors' as table_name, COUNT(*) as record_count FROM Doctors
UNION ALL
SELECT 'doctor_prices' as table_name, COUNT(*) as record_count FROM doctor_prices;

-- Show sample data
SELECT TOP 5 p.name as position_name
FROM Positions p;

-- price_rules removed

SELECT TOP 5 d.first_name + ' ' + d.last_name as doctor_name, d.email, d.years_of_experience, p.name as position_name
FROM Doctors d
LEFT JOIN Positions p ON d.position_id = p.id;

SELECT TOP 5 d.first_name + ' ' + d.last_name as doctor_name, dp.amount as price_amount
FROM doctor_prices dp
JOIN Doctors d ON dp.doctor_id = d.id;

PRINT 'Sample data insertion completed successfully!';
PRINT 'Total records inserted:';
PRINT '- Positions: 15 records';
-- Prices and Price Rules removed
PRINT '- Doctors: 10 records';
PRINT '- Doctor-Price Relationships: 10 records';


