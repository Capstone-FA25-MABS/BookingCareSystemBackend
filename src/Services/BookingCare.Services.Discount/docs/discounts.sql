-- Discounts
CREATE TABLE discounts (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX),
    clinic_id UNIQUEIDENTIFIER NOT NULL,
    specialty_id UNIQUEIDENTIFIER,
    doctor_id UNIQUEIDENTIFIER,
    applicable_to VARCHAR(20) CHECK (applicable_to IN ('ALL', 'SPECIALTY', 'DOCTOR')) DEFAULT 'ALL',
    amount DECIMAL(10, 2) NOT NULL,
    discount_type VARCHAR(20) NOT NULL CHECK (discount_type IN ('FIXED_AMOUNT', 'PERCENTAGE')),
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    max_uses INT,
    uses_count INT DEFAULT 0,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE', 'EXPIRED')) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE CASCADE,
    FOREIGN KEY (specialty_id) REFERENCES specialties(id) ON DELETE SET NULL,
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE SET NULL
);