-- =============================================================================
-- SAMPLE DATA INSERTION SCRIPT FOR BOOKING CARE DOCTOR SERVICE
-- =============================================================================
-- This script inserts sample data for all entities in the Doctor service
-- Run this script after creating the database schema

USE [MABS_Doctor]; -- Replace with your actual database name
GO

-- Define constants
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();
DECLARE @GenderMale NVARCHAR(10) = 'MALE';
DECLARE @GenderFemale NVARCHAR(10) = 'FEMALE';

-- =============================================================================
-- 1. INSERT POSITIONS (Expanded with additional positions)
-- =============================================================================
INSERT INTO positions (id, name, status, created_at, updated_at)
VALUES 
    (NEWID(), N'Bác sĩ chuyên khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ đa khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ nội khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ ngoại khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ nhi khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ sản phụ khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ tim mạch', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ thần kinh', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ da liễu', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ mắt', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ tai mũi họng', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ xương khớp', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ tiêu hóa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ hô hấp', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ nội tiết', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ ung bướu', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ tâm thần', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ vật lý trị liệu', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ dinh dưỡng', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ y học cổ truyền', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ cấp cứu', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ gây mê hồi sức', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ chẩn đoán hình ảnh', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ chỉnh hình', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ huyết học', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ thận học', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ dị ứng và miễn dịch', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ lão khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ phẫu thuật thẩm mỹ', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ răng hàm mặt', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ y khoa', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ cao cấp', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ chuyên khoa I', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ chuyên khoa II', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ nội trú', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Cử nhân', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Thạc sĩ', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiến sĩ', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Giáo sư', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Phó Giáo sư', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ truyền nhiễm', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ phục hồi chức năng', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ lao và bệnh phổi', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bác sĩ y học gia đình', 'ACTIVE', @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 2. INSERT LANGUAGES (Expanded)
-- =============================================================================
-- Re-declare variables for this section
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();

INSERT INTO languages (id, name, status, created_at, updated_at)
VALUES 
    (NEWID(), N'Tiếng Việt', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'English', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'中文', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'日本語', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'한국어', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Français', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Deutsch', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Español', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Русский', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'العربية', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Português', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'हिन्दी', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Bahasa Indonesia', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiếng Thái', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiếng Malaysia', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Italian', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'বাংলা', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Türkçe', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'فارسی', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Polski', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Українська', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Nederlands', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Svenska', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiếng Lào', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiếng Khmer', 'ACTIVE', @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 3. INSERT SPECIALTIES
-- =============================================================================
-- Re-declare variables for this section
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();

INSERT INTO specialties (id, name, image_url, status, created_at, updated_at)
VALUES 
    (NEWID(), N'Tim mạch', N'https://example.com/images/cardiology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Nội khoa', N'https://example.com/images/internal-medicine.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Ngoại khoa', N'https://example.com/images/surgery.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Nhi khoa', N'https://example.com/images/pediatrics.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Sản phụ khoa', N'https://example.com/images/obstetrics-gynecology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Da liễu', N'https://example.com/images/dermatology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Mắt', N'https://example.com/images/ophthalmology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tai mũi họng', N'https://example.com/images/ent.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Xương khớp', N'https://example.com/images/orthopedics.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tiêu hóa', N'https://example.com/images/gastroenterology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Hô hấp', N'https://example.com/images/pulmonology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Nội tiết', N'https://example.com/images/endocrinology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Ung bướu', N'https://example.com/images/oncology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Thần kinh', N'https://example.com/images/neurology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Tâm thần', N'https://example.com/images/psychiatry.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Vật lý trị liệu', N'https://example.com/images/physical-therapy.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Dinh dưỡng', N'https://example.com/images/nutrition.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Y học cổ truyền', N'https://example.com/images/traditional-medicine.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Cấp cứu', N'https://example.com/images/emergency.jpg', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'Gây mê hồi sức', N'https://example.com/images/anesthesiology.jpg', 'ACTIVE', @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 4. INSERT SERVICE TYPES (Doctor Service Types)
-- =============================================================================
-- Re-declare variables for this section
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();

INSERT INTO doctor_service_types (id, name, description, status, created_at, updated_at)
VALUES 
    (NEWID(), N'IN_PERSON', N'Khám trực tiếp tại phòng khám', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'TELEHEALTH', N'Tư vấn trực tuyến qua video call', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'HOME_VISIT', N'Khám tại nhà bệnh nhân', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'EMERGENCY', N'Cấp cứu khẩn cấp', 'ACTIVE', @CurrentTime, @CurrentTime),
    (NEWID(), N'FOLLOW_UP', N'Tái khám theo dõi', 'ACTIVE', @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 5. INSERT DOCTORS
-- =============================================================================
-- Re-declare variables for this section
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();
DECLARE @GenderMale NVARCHAR(10) = 'MALE';
DECLARE @GenderFemale NVARCHAR(10) = 'FEMALE';

DECLARE @PositionId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ chuyên khoa');
DECLARE @PositionId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ đa khoa');
DECLARE @PositionId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ nội khoa');
DECLARE @PositionId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ ngoại khoa');
DECLARE @PositionId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM positions WHERE name = N'Bác sĩ nhi khoa');

DECLARE @SpecialtyId1 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM specialties WHERE name = N'Tim mạch');
DECLARE @SpecialtyId2 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM specialties WHERE name = N'Nội khoa');
DECLARE @SpecialtyId3 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM specialties WHERE name = N'Ngoại khoa');
DECLARE @SpecialtyId4 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM specialties WHERE name = N'Nhi khoa');
DECLARE @SpecialtyId5 UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM specialties WHERE name = N'Da liễu');

INSERT INTO doctors (id, account_id, email, address, first_name, last_name, gender, position_id, specialty_id, hospital_id, bio, years_of_experience, avatar_url, created_at, updated_at)
VALUES 
    (NEWID(), NEWID(), N'dr.nguyen.van.a@bookingcare.com', N'123 Đường Lê Lợi, Quận 1, TP.HCM', N'Nguyễn', N'Văn A', @GenderMale, @PositionId1, @SpecialtyId1, NEWID(), N'Bác sĩ chuyên khoa tim mạch với hơn 10 năm kinh nghiệm. Tốt nghiệp Đại học Y Hà Nội và có chứng chỉ chuyên khoa tim mạch tại Pháp.', 10, N'https://example.com/avatar1.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.tran.thi.b@bookingcare.com', N'456 Đường Nguyễn Huệ, Quận 1, TP.HCM', N'Trần', N'Thị B', @GenderFemale, @PositionId2, @SpecialtyId2, NEWID(), N'Bác sĩ đa khoa có kinh nghiệm 8 năm. Chuyên khám và điều trị các bệnh thông thường, tư vấn sức khỏe tổng quát.', 8, N'https://example.com/avatar2.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.le.van.c@bookingcare.com', N'789 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM', N'Lê', N'Văn C', @GenderMale, @PositionId3, @SpecialtyId2, NEWID(), N'Bác sĩ nội khoa chuyên điều trị các bệnh về tiêu hóa, hô hấp và nội tiết. Có 12 năm kinh nghiệm trong lĩnh vực.', 12, N'https://example.com/avatar3.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.pham.thi.d@bookingcare.com', N'321 Đường Cách Mạng Tháng 8, Quận 10, TP.HCM', N'Phạm', N'Thị D', @GenderFemale, @PositionId4, @SpecialtyId3, NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật nội soi. Tốt nghiệp chuyên khoa ngoại tại Đại học Y TP.HCM.', 6, N'https://example.com/avatar4.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.hoang.van.e@bookingcare.com', N'654 Đường Lý Tự Trọng, Quận 1, TP.HCM', N'Hoàng', N'Văn E', @GenderMale, @PositionId5, @SpecialtyId4, NEWID(), N'Bác sĩ nhi khoa chuyên điều trị cho trẻ em từ sơ sinh đến 18 tuổi. Có kinh nghiệm 15 năm và rất yêu trẻ em.', 15, N'https://example.com/avatar5.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.vu.thi.f@bookingcare.com', N'987 Đường Võ Văn Tần, Quận 3, TP.HCM', N'Vũ', N'Thị F', @GenderFemale, @PositionId1, @SpecialtyId5, NEWID(), N'Bác sĩ chuyên khoa da liễu với 9 năm kinh nghiệm. Chuyên điều trị các bệnh về da, tóc và móng.', 9, N'https://example.com/avatar6.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.dang.van.g@bookingcare.com', N'147 Đường Nguyễn Thị Minh Khai, Quận 3, TP.HCM', N'Đặng', N'Văn G', @GenderMale, @PositionId2, @SpecialtyId2, NEWID(), N'Bác sĩ đa khoa trẻ tuổi nhưng rất tận tâm. Chuyên khám sức khỏe định kỳ và tư vấn dinh dưỡng.', 3, N'https://example.com/avatar7.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.bui.thi.h@bookingcare.com', N'258 Đường Pasteur, Quận 3, TP.HCM', N'Bùi', N'Thị H', @GenderFemale, @PositionId3, @SpecialtyId1, NEWID(), N'Bác sĩ nội khoa chuyên về tim mạch và huyết áp. Có chứng chỉ chuyên khoa tim mạch quốc tế.', 11, N'https://example.com/avatar8.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.ngo.van.i@bookingcare.com', N'369 Đường Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM', N'Ngô', N'Văn I', @GenderMale, @PositionId4, @SpecialtyId3, NEWID(), N'Bác sĩ ngoại khoa chuyên về phẫu thuật thẩm mỹ và tái tạo. Có kinh nghiệm 7 năm trong lĩnh vực.', 7, N'https://example.com/avatar9.jpg', @CurrentTime, @CurrentTime),
    (NEWID(), NEWID(), N'dr.ly.thi.j@bookingcare.com', N'741 Đường Đinh Tiên Hoàng, Quận Bình Thạnh, TP.HCM', N'Lý', N'Thị J', @GenderFemale, @PositionId5, @SpecialtyId4, NEWID(), N'Bác sĩ nhi khoa chuyên về sơ sinh và trẻ sơ sinh. Có kinh nghiệm 13 năm và rất được phụ huynh tin tưởng.', 13, N'https://example.com/avatar10.jpg', @CurrentTime, @CurrentTime);
GO

-- =============================================================================
-- 6. INSERT DOCTOR-PRICE RELATIONSHIPS + DOCTOR LANGUAGES (gộp cùng batch)
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

-- Re-declare variables for this section
DECLARE @CurrentTime DATETIME2 = GETUTCDATE();

-- Get service type IDs
DECLARE @ServiceTypeInPerson UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctor_service_types WHERE name = N'IN_PERSON');
DECLARE @ServiceTypeTelehealth UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctor_service_types WHERE name = N'TELEHEALTH');
DECLARE @ServiceTypeHomeVisit UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM doctor_service_types WHERE name = N'HOME_VISIT');

-- Get language IDs
DECLARE @LanguageVietnamese UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM languages WHERE name = N'Tiếng Việt');
DECLARE @LanguageEnglish UNIQUEIDENTIFIER   = (SELECT TOP 1 id FROM languages WHERE name = N'English');
DECLARE @LanguageChinese UNIQUEIDENTIFIER   = (SELECT TOP 1 id FROM languages WHERE name = N'中文');
DECLARE @LanguageJapanese UNIQUEIDENTIFIER  = (SELECT TOP 1 id FROM languages WHERE name = N'日本語');
DECLARE @LanguageKorean UNIQUEIDENTIFIER    = (SELECT TOP 1 id FROM languages WHERE name = N'한국어');

-- Insert doctor prices
INSERT INTO doctor_prices (id, doctor_id, service_type_id, amount, created_at, updated_at)
VALUES 
    (NEWID(), @DoctorId1, @ServiceTypeInPerson, 500000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId1, @ServiceTypeTelehealth, 300000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId2, @ServiceTypeInPerson, 300000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId2, @ServiceTypeTelehealth, 200000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId3, @ServiceTypeInPerson, 600000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId3, @ServiceTypeHomeVisit, 800000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId4, @ServiceTypeInPerson, 700000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId4, @ServiceTypeTelehealth, 400000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId5, @ServiceTypeInPerson, 800000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId5, @ServiceTypeHomeVisit, 1000000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId6, @ServiceTypeInPerson, 550000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId6, @ServiceTypeTelehealth, 350000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId7, @ServiceTypeInPerson, 350000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId7, @ServiceTypeTelehealth, 250000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId8, @ServiceTypeInPerson, 650000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId8, @ServiceTypeHomeVisit, 900000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId9, @ServiceTypeInPerson, 750000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId9, @ServiceTypeTelehealth, 450000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId10, @ServiceTypeInPerson, 850000, @CurrentTime, @CurrentTime),
    (NEWID(), @DoctorId10, @ServiceTypeHomeVisit, 1100000, @CurrentTime, @CurrentTime);

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
-- VERIFICATION QUERIES
-- =============================================================================
SELECT 'positions' as table_name, COUNT(*) as record_count FROM positions
UNION ALL
SELECT 'languages' as table_name, COUNT(*) as record_count FROM languages
UNION ALL
SELECT 'specialties' as table_name, COUNT(*) as record_count FROM specialties
UNION ALL
SELECT 'doctor_service_types' as table_name, COUNT(*) as record_count FROM doctor_service_types
UNION ALL
SELECT 'doctors' as table_name, COUNT(*) as record_count FROM doctors
UNION ALL
SELECT 'doctor_prices' as table_name, COUNT(*) as record_count FROM doctor_prices
UNION ALL
SELECT 'doctor_languages' as table_name, COUNT(*) as record_count FROM doctor_languages