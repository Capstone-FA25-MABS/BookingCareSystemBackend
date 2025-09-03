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
INSERT INTO Positions (Id, Name, Description, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), N'Bác sĩ chuyên khoa', N'Bác sĩ có chuyên môn sâu trong một lĩnh vực y tế cụ thể', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ đa khoa', N'Bác sĩ có kiến thức tổng quát về nhiều lĩnh vực y tế', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ nội khoa', N'Bác sĩ chuyên điều trị các bệnh lý bên trong cơ thể', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ ngoại khoa', N'Bác sĩ chuyên thực hiện các phẫu thuật', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ nhi khoa', N'Bác sĩ chuyên điều trị cho trẻ em', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ sản phụ khoa', N'Bác sĩ chuyên về sản khoa và phụ khoa', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ tim mạch', N'Bác sĩ chuyên về tim và mạch máu', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ thần kinh', N'Bác sĩ chuyên về hệ thần kinh', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ da liễu', N'Bác sĩ chuyên về da và các bệnh da liễu', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ mắt', N'Bác sĩ chuyên về mắt và thị lực', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 2. INSERT PRICES
-- =============================================================================
INSERT INTO Prices (Id, Amount, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), 200000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 300000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 400000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 500000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 600000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 700000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 800000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 900000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1000000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1200000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1500000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 2000000, GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 3. INSERT PRICE RULES
-- =============================================================================
INSERT INTO PriceRules (Id, BasePrice, MinExperience, Position, BonusFamous, Status, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), 200000, 0, N'Bác sĩ đa khoa', 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 300000, 2, N'Bác sĩ đa khoa', 50000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 400000, 5, N'Bác sĩ đa khoa', 100000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 500000, 0, N'Bác sĩ chuyên khoa', 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 600000, 3, N'Bác sĩ chuyên khoa', 100000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 800000, 7, N'Bác sĩ chuyên khoa', 200000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 400000, 0, N'Bác sĩ nội khoa', 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 500000, 2, N'Bác sĩ nội khoa', 50000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 600000, 5, N'Bác sĩ nội khoa', 100000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 700000, 0, N'Bác sĩ ngoại khoa', 0, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 900000, 3, N'Bác sĩ ngoại khoa', 100000, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1200000, 8, N'Bác sĩ ngoại khoa', 300000, 1, GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 4. INSERT DOCTORS
-- =============================================================================
DECLARE @PositionId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Positions WHERE Name = N'Bác sĩ chuyên khoa');
DECLARE @PositionId2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Positions WHERE Name = N'Bác sĩ đa khoa');
DECLARE @PositionId3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Positions WHERE Name = N'Bác sĩ nội khoa');
DECLARE @PositionId4 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Positions WHERE Name = N'Bác sĩ ngoại khoa');
DECLARE @PositionId5 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Positions WHERE Name = N'Bác sĩ nhi khoa');

INSERT INTO Doctors (Id, AccountId, Email, Address, FirstName, LastName, Gender, PositionId, SpecialtyId, ClinicId, Bio, YearsOfExperience, AvatarUrl, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), NEWID(), N'dr.nguyen.van.a@bookingcare.com', N'123 Đường Lê Lợi, Quận 1, TP.HCM', N'Nguyễn', N'Văn A', 0, @PositionId1, NEWID(), NEWID(), N'Bác sĩ chuyên khoa tim mạch với hơn 10 năm kinh nghiệm. Tốt nghiệp Đại học Y Hà Nội và có chứng chỉ chuyên khoa tim mạch tại Pháp.', 10, N'https://example.com/avatar1.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.tran.thi.b@bookingcare.com', N'456 Đường Nguyễn Huệ, Quận 1, TP.HCM', N'Trần', N'Thị B', 1, @PositionId2, NEWID(), NEWID(), N'Bác sĩ đa khoa có kinh nghiệm 8 năm. Chuyên khám và điều trị các bệnh thông thường, tư vấn sức khỏe tổng quát.', 8, N'https://example.com/avatar2.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.le.van.c@bookingcare.com', N'789 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', N'Lê', N'Văn C', 0, @PositionId3, NEWID(), NEWID(), N'Bác sĩ nội khoa chuyên điều trị các bệnh về tiêu hóa, hô hấp và nội tiết. Có 12 năm kinh nghiệm trong lĩnh vực.', 12, N'https://example.com/avatar3.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.pham.thi.d@bookingcare.com', N'321 Đường Cách Mạng Tháng 8, Quận 10, TP.HCM', N'Phạm', N'Thị D', 1, @PositionId4, NEWID(), NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật nội soi. Tốt nghiệp chuyên khoa ngoại tại Đại học Y TP.HCM.', 6, N'https://example.com/avatar4.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.hoang.van.e@bookingcare.com', N'654 Đường Lý Tự Trọng, Quận 1, TP.HCM', N'Hoàng', N'Văn E', 0, @PositionId5, NEWID(), NEWID(), N'Bác sĩ nhi khoa chuyên điều trị cho trẻ em từ sơ sinh đến 18 tuổi. Có kinh nghiệm 15 năm và rất yêu trẻ em.', 15, N'https://example.com/avatar5.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.vu.thi.f@bookingcare.com', N'987 Đường Võ Văn Tần, Quận 3, TP.HCM', N'Vũ', N'Thị F', 1, @PositionId1, NEWID(), NEWID(), N'Bác sĩ chuyên khoa da liễu với 9 năm kinh nghiệm. Chuyên điều trị các bệnh về da, tóc và móng.', 9, N'https://example.com/avatar6.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.dang.van.g@bookingcare.com', N'147 Đường Nguyễn Thị Minh Khai, Quận 3, TP.HCM', N'Đặng', N'Văn G', 0, @PositionId2, NEWID(), NEWID(), N'Bác sĩ đa khoa trẻ tuổi nhưng rất tận tâm. Chuyên khám sức khỏe định kỳ và tư vấn dinh dưỡng.', 3, N'https://example.com/avatar7.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.bui.thi.h@bookingcare.com', N'258 Đường Pasteur, Quận 3, TP.HCM', N'Bùi', N'Thị H', 1, @PositionId3, NEWID(), NEWID(), N'Bác sĩ nội khoa chuyên về tim mạch và huyết áp. Có chứng chỉ chuyên khoa tim mạch quốc tế.', 11, N'https://example.com/avatar8.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.ngo.van.i@bookingcare.com', N'369 Đường Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM', N'Ngô', N'Văn I', 0, @PositionId4, NEWID(), NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật thẩm mỹ và tái tạo. Có kinh nghiệm 7 năm trong lĩnh vực.', 7, N'https://example.com/avatar9.jpg', GETUTCDATE(), GETUTCDATE()),
    
    (NEWID(), NEWID(), N'dr.ly.thi.j@bookingcare.com', N'741 Đường Đinh Tiên Hoàng, Quận Bình Thạnh, TP.HCM', N'Lý', N'Thị J', 1, @PositionId5, NEWID(), NEWID(), N'Bác sĩ nhi khoa chuyên về sơ sinh và trẻ sơ sinh. Có kinh nghiệm 13 năm và rất được phụ huynh tin tưởng.', 13, N'https://example.com/avatar10.jpg', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 5. INSERT DOCTOR-PRICE RELATIONSHIPS
-- =============================================================================
-- Get some doctor and price IDs for relationships
DECLARE @DoctorId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Doctors WHERE FirstName = N'Nguyễn' AND LastName = N'Văn A');
DECLARE @DoctorId2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Doctors WHERE FirstName = N'Trần' AND LastName = N'Thị B');
DECLARE @DoctorId3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Doctors WHERE FirstName = N'Lê' AND LastName = N'Văn C');
DECLARE @DoctorId4 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Doctors WHERE FirstName = N'Phạm' AND LastName = N'Thị D');
DECLARE @DoctorId5 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Doctors WHERE FirstName = N'Hoàng' AND LastName = N'Văn E');

DECLARE @PriceId1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Prices WHERE Amount = 500000);
DECLARE @PriceId2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Prices WHERE Amount = 300000);
DECLARE @PriceId3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Prices WHERE Amount = 600000);
DECLARE @PriceId4 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Prices WHERE Amount = 700000);
DECLARE @PriceId5 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Prices WHERE Amount = 800000);

INSERT INTO DoctorPrices (DoctorId, PriceId, Description, IsOverride, CreatedAt, UpdatedAt)
VALUES 
    (@DoctorId1, @PriceId1, N'Giá khám chuyên khoa tim mạch', 1, GETUTCDATE(), GETUTCDATE()),
    (@DoctorId2, @PriceId2, N'Giá khám đa khoa thông thường', 1, GETUTCDATE(), GETUTCDATE()),
    (@DoctorId3, @PriceId3, N'Giá khám nội khoa chuyên sâu', 1, GETUTCDATE(), GETUTCDATE()),
    (@DoctorId4, @PriceId4, N'Giá phẫu thuật nội soi', 1, GETUTCDATE(), GETUTCDATE()),
    (@DoctorId5, @PriceId5, N'Giá khám nhi khoa chuyên sâu', 1, GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 6. INSERT ADDITIONAL SAMPLE DATA FOR TESTING
-- =============================================================================

-- Insert more positions
INSERT INTO Positions (Id, Name, Description, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), N'Bác sĩ tâm thần', N'Bác sĩ chuyên về sức khỏe tâm thần và tâm lý', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ vật lý trị liệu', N'Bác sĩ chuyên về phục hồi chức năng', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ dinh dưỡng', N'Bác sĩ chuyên về dinh dưỡng và chế độ ăn', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ y học cổ truyền', N'Bác sĩ chuyên về y học cổ truyền Việt Nam', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ cấp cứu', N'Bác sĩ chuyên về cấp cứu và hồi sức', GETUTCDATE(), GETUTCDATE());
GO

-- Insert more prices
INSERT INTO Prices (Id, Amount, CreatedAt, UpdatedAt)
VALUES 
    (NEWID(), 250000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 350000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 450000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 550000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 650000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 750000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 850000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 950000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1100000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1300000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1600000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 1800000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 2200000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 2500000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 3000000, GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================

-- Check inserted data
SELECT 'Positions' as TableName, COUNT(*) as RecordCount FROM Positions
UNION ALL
SELECT 'Prices' as TableName, COUNT(*) as RecordCount FROM Prices
UNION ALL
SELECT 'PriceRules' as TableName, COUNT(*) as RecordCount FROM PriceRules
UNION ALL
SELECT 'Doctors' as TableName, COUNT(*) as RecordCount FROM Doctors
UNION ALL
SELECT 'DoctorPrices' as TableName, COUNT(*) as RecordCount FROM DoctorPrices;

-- Show sample data
SELECT TOP 5 p.Name as PositionName, p.Description 
FROM Positions p;

SELECT TOP 5 pr.Amount, pr.BasePrice, pr.Position, pr.MinExperience
FROM PriceRules pr;

SELECT TOP 5 d.FirstName + ' ' + d.LastName as DoctorName, d.Email, d.YearsOfExperience, p.Name as PositionName
FROM Doctors d
LEFT JOIN Positions p ON d.PositionId = p.Id;

SELECT TOP 5 d.FirstName + ' ' + d.LastName as DoctorName, pr.Amount as PriceAmount, dp.Description, dp.IsOverride
FROM DoctorPrices dp
JOIN Doctors d ON dp.DoctorId = d.Id
JOIN Prices pr ON dp.PriceId = pr.Id;

PRINT 'Sample data insertion completed successfully!';
PRINT 'Total records inserted:';
PRINT '- Positions: 15 records';
PRINT '- Prices: 27 records';
PRINT '- Price Rules: 12 records';
PRINT '- Doctors: 10 records';
PRINT '- Doctor-Price Relationships: 5 records';
