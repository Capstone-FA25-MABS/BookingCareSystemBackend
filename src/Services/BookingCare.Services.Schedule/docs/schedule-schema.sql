-- -- Appointment Times
-- CREATE TABLE appointment_times (
--     id BIGINT IDENTITY(1,1) PRIMARY KEY,
--     start_time CHAR(5) NOT NULL,
--     end_time CHAR(5) NOT NULL
-- );

-- -- 2. Pattern (mẫu lịch): full day, morning only, afternoon only, evening only
-- CREATE TABLE schedule_patterns (
--     id BIGINT IDENTITY(1,1) PRIMARY KEY,
--     name NVARCHAR(100) CHECK (name IN ('FULL_DAY', 'MORNING_ONLY', 'AFTERNOON_ONLY', 'EVENING_ONLY')) DEFAULT 'FULL_DAY',
--     description NVARCHAR(MAX),
--     created_at DATETIME NOT NULL DEFAULT GETDATE(),
--     updated_at DATETIME NOT NULL DEFAULT GETDATE()
-- );

-- 3. Slot thuộc 1 pattern (pattern = tập hợp slot)
CREATE TABLE schedule_pattern_slots (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    pattern_id BIGINT NOT NULL,
    appointment_time_id BIGINT NOT NULL,
    FOREIGN KEY (pattern_id) REFERENCES schedule_patterns(id) ON DELETE CASCADE,
    FOREIGN KEY (appointment_time_id) REFERENCES appointment_times(id) ON DELETE CASCADE
);

-- 4. Lịch hàng ngày của bác sĩ (theo pattern)
CREATE TABLE doctor_daily_schedules (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    doctor_id BIGINT NOT NULL,
    schedule_date DATE NOT NULL,
    pattern_id BIGINT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE CASCADE,
    FOREIGN KEY (pattern_id) REFERENCES schedule_patterns(id),
    UNIQUE(doctor_id, schedule_date) -- One schedule per doctor per day
);

-- 5. Ngoại lệ (override lịch: nghỉ cả ngày, nghỉ slot hoặc mở lại slot)
CREATE TABLE doctor_schedule_exceptions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    doctor_id BIGINT NOT NULL,
    exception_date DATE NOT NULL,
    appointment_time_id BIGINT NULL, -- NULL = nghỉ cả ngày
    exception_type NVARCHAR(20) CHECK (exception_type IN ('BLOCK_SLOT', 'UNBLOCK_SLOT', 'DAY_OFF', 'CAPACITY_CHANGE')) NOT NULL,
    is_available BIT NOT NULL DEFAULT 0, -- 0 = khóa slot/ngày, 1 = mở lại slot
    reason NVARCHAR(255),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id),
    FOREIGN KEY (appointment_time_id) REFERENCES appointment_times(id)
);

-- 8. Ngoại lệ lịch của clinic/hospital
CREATE TABLE clinic_exceptions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    clinic_id BIGINT NOT NULL,
    exception_date DATE NOT NULL,
    reason NVARCHAR(255),
    FOREIGN KEY (clinic_id) REFERENCES clinics(id),
);

-- 9. Lịch dịch vụ (service schedules) - định nghĩa dịch vụ có thể thực hiện vào khung giờ nào
CREATE TABLE service_schedules (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    service_id BIGINT NOT NULL,
    pattern_id BIGINT NOT NULL,
    clinic_id BIGINT NULL, -- nếu dịch vụ chỉ áp dụng tại 1 clinic
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE CASCADE,
    FOREIGN KEY (pattern_id) REFERENCES schedule_patterns(id),
    FOREIGN KEY (clinic_id) REFERENCES clinics(id)
);
