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
INSERT INTO positions (id, name, created_at, updated_at)
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
-- 2. INSERT LANGUAGES
-- =============================================================================
INSERT INTO languages (id, name, created_at, updated_at)
VALUES 
    (NEWID(), N'Tiếng Việt', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'English', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'中文', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'日本語', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'한국어', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Français', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Deutsch', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Español', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 3. INSERT SERVICE TYPES
-- =============================================================================
INSERT INTO service_types (id, name, description, created_at, updated_at)
VALUES 
    (NEWID(), N'IN_PERSON', N'Khám trực tiếp tại phòng khám', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'TELEHEALTH', N'Tư vấn trực tuyến qua video call', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'HOME_VISIT', N'Khám tại nhà bệnh nhân', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'EMERGENCY', N'Cấp cứu khẩn cấp', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'FOLLOW_UP', N'Tái khám theo dõi', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- 4. INSERT DOCTORS
-- =============================================================================
DECLARE @PositionId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ chuyên khoa');
DECLARE @PositionId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ đa khoa');
DECLARE @PositionId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ nội khoa');
DECLARE @PositionId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ ngoại khoa');
DECLARE @PositionId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ nhi khoa');

INSERT INTO doctors (id, account_id, email, address, first_name, last_name, gender, position_id, specialty_id, clinic_id, bio, years_of_experience, avatar_url, created_at, updated_at)
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
-- 5. INSERT DOCTOR-PRICE RELATIONSHIPS + DOCTOR LANGUAGES (gộp cùng batch)
-- =============================================================================
DECLARE @DoctorId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Nguyễn' AND last_name = N'Văn A');
DECLARE @DoctorId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Trần' AND last_name = N'Thị B');
DECLARE @DoctorId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Lê' AND last_name = N'Văn C');
DECLARE @DoctorId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Phạm' AND last_name = N'Thị D');
DECLARE @DoctorId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Hoàng' AND last_name = N'Văn E');
DECLARE @DoctorId6 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Vũ' AND last_name = N'Thị F');
DECLARE @DoctorId7 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Đặng' AND last_name = N'Văn G');
DECLARE @DoctorId8 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Bùi' AND last_name = N'Thị H');
DECLARE @DoctorId9 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Ngô' AND last_name = N'Văn I');
DECLARE @DoctorId10 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctors WHERE first_name = N'Lý' AND last_name = N'Thị J');

-- Get service type IDs
DECLARE @ServiceTypeInPerson UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM service_types WHERE name = N'IN_PERSON');
DECLARE @ServiceTypeTelehealth UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM service_types WHERE name = N'TELEHEALTH');
DECLARE @ServiceTypeHomeVisit UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM service_types WHERE name = N'HOME_VISIT');

-- Insert doctor prices
INSERT INTO doctor_prices (id, doctor_id, service_type_id, amount, created_at, updated_at)
VALUES 
    (NEWID(), @DoctorId1, @ServiceTypeInPerson, 500000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId1, @ServiceTypeTelehealth, 300000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId2, @ServiceTypeInPerson, 300000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId2, @ServiceTypeTelehealth, 200000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId3, @ServiceTypeInPerson, 600000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId3, @ServiceTypeHomeVisit, 800000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId4, @ServiceTypeInPerson, 700000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId4, @ServiceTypeTelehealth, 400000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId5, @ServiceTypeInPerson, 800000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId5, @ServiceTypeHomeVisit, 1000000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId6, @ServiceTypeInPerson, 550000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId6, @ServiceTypeTelehealth, 350000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId7, @ServiceTypeInPerson, 350000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId7, @ServiceTypeTelehealth, 250000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId8, @ServiceTypeInPerson, 650000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId8, @ServiceTypeHomeVisit, 900000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId9, @ServiceTypeInPerson, 750000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId9, @ServiceTypeTelehealth, 450000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId10, @ServiceTypeInPerson, 850000, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @DoctorId10, @ServiceTypeHomeVisit, 1100000, GETUTCDATE(), GETUTCDATE());

-- Get language IDs
DECLARE @LanguageVietnamese UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM languages WHERE name = N'Tiếng Việt');
DECLARE @LanguageEnglish UNIQUEIDENTIFIER   = (SELECT TOP 1 id FROM languages WHERE name = N'English');
DECLARE @LanguageChinese UNIQUEIDENTIFIER   = (SELECT TOP 1 id FROM languages WHERE name = N'中文');
DECLARE @LanguageJapanese UNIQUEIDENTIFIER  = (SELECT TOP 1 id FROM languages WHERE name = N'日本語');
DECLARE @LanguageKorean UNIQUEIDENTIFIER    = (SELECT TOP 1 id FROM languages WHERE name = N'한국어');

-- Insert doctor languages
INSERT INTO doctor_languages (id, doctor_id, language_id)
VALUES 
    (NEWID(), @DoctorId1, @LanguageVietnamese),
    (NEWID(), @DoctorId1, @LanguageEnglish),
    (NEWID(), @DoctorId1, @LanguageChinese),
    (NEWID(), @DoctorId2, @LanguageVietnamese),
    (NEWID(), @DoctorId2, @LanguageEnglish),
    (NEWID(), @DoctorId3, @LanguageVietnamese),
    (NEWID(), @DoctorId3, @LanguageEnglish),
    (NEWID(), @DoctorId3, @LanguageJapanese),
    (NEWID(), @DoctorId4, @LanguageVietnamese),
    (NEWID(), @DoctorId4, @LanguageEnglish),
    (NEWID(), @DoctorId4, @LanguageKorean),
    (NEWID(), @DoctorId5, @LanguageVietnamese),
    (NEWID(), @DoctorId5, @LanguageEnglish),
    (NEWID(), @DoctorId5, @LanguageChinese),
    (NEWID(), @DoctorId6, @LanguageVietnamese),
    (NEWID(), @DoctorId6, @LanguageEnglish),
    (NEWID(), @DoctorId7, @LanguageVietnamese),
    (NEWID(), @DoctorId7, @LanguageEnglish),
    (NEWID(), @DoctorId7, @LanguageJapanese),
    (NEWID(), @DoctorId8, @LanguageVietnamese),
    (NEWID(), @DoctorId8, @LanguageEnglish),
    (NEWID(), @DoctorId8, @LanguageKorean),
    (NEWID(), @DoctorId9, @LanguageVietnamese),
    (NEWID(), @DoctorId9, @LanguageEnglish),
    (NEWID(), @DoctorId9, @LanguageChinese),
    (NEWID(), @DoctorId10, @LanguageVietnamese),
    (NEWID(), @DoctorId10, @LanguageEnglish);
GO

-- =============================================================================
-- 6. INSERT ADDITIONAL SAMPLE DATA FOR TESTING
-- =============================================================================
INSERT INTO positions (id, name, created_at, updated_at)
VALUES 
    (NEWID(), N'Bác sĩ tâm thần', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ vật lý trị liệu', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ dinh dưỡng', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ y học cổ truyền', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), N'Bác sĩ cấp cứu', GETUTCDATE(), GETUTCDATE());
GO

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
SELECT 'positions' as table_name, COUNT(*) as record_count FROM positions
UNION ALL
SELECT 'languages' as table_name, COUNT(*) as record_count FROM languages
UNION ALL
SELECT 'service_types' as table_name, COUNT(*) as record_count FROM service_types
UNION ALL
SELECT 'doctors' as table_name, COUNT(*) as record_count FROM doctors
