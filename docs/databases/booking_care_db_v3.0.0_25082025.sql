-- Check and create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'booking_care_system')
BEGIN
    CREATE DATABASE booking_care_system;
END;


USE booking_care_system;


-- Roles
CREATE TABLE roles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(MAX),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Permissions
CREATE TABLE permissions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(MAX),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Role Permissions
CREATE TABLE role_permissions (
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    PRIMARY KEY (role_id, permission_id),
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE
);


-- Genders
-- CREATE TABLE genders (
--     id BIGINT IDENTITY(1,1) PRIMARY KEY,
--     name NVARCHAR(50) NOT NULL UNIQUE,
--     description NVARCHAR(MAX)
-- );


-- Positions
CREATE TABLE positions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Subscription Plans
CREATE TABLE subscription_plans (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(MAX),
    price DECIMAL(10, 2) NOT NULL,
    billing_cycle VARCHAR(20) CHECK (billing_cycle IN ('MONTHLY', 'QUARTERLY', 'YEARLY')) DEFAULT 'MONTHLY',
    max_doctors INT DEFAULT 0,
    max_specialties INT DEFAULT 0,
    features NVARCHAR(MAX),
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Accounts
CREATE TABLE accounts (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    email VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Account Roles
CREATE TABLE account_roles (
    account_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_at DATETIME NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (account_id, role_id),
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);


-- Clinics
CREATE TABLE clinics (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id BIGINT NOT NULL,
    name NVARCHAR(255) NOT NULL,
    address NVARCHAR(MAX) NOT NULL,
    phone NVARCHAR(20),
    email NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(MAX) NOT NULL,
    background_url VARCHAR(MAX) NOT NULL,
    avatar_url VARCHAR(MAX) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) NOT NULL DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE
);

-- create clinic_subscriptions table
CREATE TABLE clinic_subscriptions (
    clinic_subscriptions_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    clinic_id BIGINT NOT NULL,
    subscription_id BIGINT NOT NULL,
    start_date DATETIME NOT NULL DEFAULT GETDATE(),
    end_date DATETIME NOT NULL DEFAULT GETDATE(),
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'ACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE CASCADE,
    FOREIGN KEY (subscription_id) REFERENCES subscription_plans(id) ON DELETE CASCADE
);


-- Specialties
CREATE TABLE specialties (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    image_url VARCHAR(MAX) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) NOT NULL DEFAULT 'INACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- Clinic Specialties
CREATE TABLE clinic_specialties (
    clinic_id BIGINT NOT NULL,
    specialty_id BIGINT NOT NULL,
    PRIMARY KEY (clinic_id, specialty_id),
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE CASCADE,
    FOREIGN KEY (specialty_id) REFERENCES specialties(id) ON DELETE CASCADE
);


-- Users
CREATE TABLE users (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id BIGINT NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    gender VARCHAR(20) CHECK (gender IN ('MALE', 'FEMALE', 'OTHER')),
    address NVARCHAR(MAX),
    phone VARCHAR(20),
    avatar_url VARCHAR(MAX) DEFAULT 'https://bookingcaree.com/user-avatar-default.png',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE
);


-- Doctors
CREATE TABLE doctors (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id BIGINT NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    address NVARCHAR(MAX),
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    gender VARCHAR(20) CHECK (gender IN ('MALE', 'FEMALE', 'OTHER')),
    position_id BIGINT,
    specialty_id BIGINT,
    clinic_id BIGINT,
    bio NVARCHAR(MAX),
    years_of_experience INT DEFAULT 0,
    avatar_url VARCHAR(MAX) DEFAULT 'https://bookingcaree.com/user-avatar-default.png',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE,
    FOREIGN KEY (position_id) REFERENCES positions(id) ON DELETE SET NULL,
    FOREIGN KEY (specialty_id) REFERENCES specialties(id) ON DELETE SET NULL,
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE SET NULL
);


-- Appointment Times
CREATE TABLE appointment_times (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    start_time CHAR(5) NOT NULL,
    end_time CHAR(5) NOT NULL
);


-- Prices
CREATE TABLE prices (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    amount DECIMAL(10, 2) NOT NULL
);


-- Doctor Prices
CREATE TABLE doctor_prices (
    doctor_id BIGINT NOT NULL,
    price_id BIGINT NOT NULL,
    description NVARCHAR(MAX),
    PRIMARY KEY (doctor_id, price_id),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE CASCADE,
    FOREIGN KEY (price_id) REFERENCES prices(id) ON DELETE CASCADE
);

-- Doctor Schedule Times
CREATE TABLE doctor_schedule_times (
    doctor_schedule_time_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    doctor_id BIGINT NOT NULL,
    appointment_time_id BIGINT NOT NULL,
    appointment_date DATETIME NOT NULL,
    appointment_type_id BIGINT NOT NULL,
    is_available BIT NOT NULL DEFAULT 1,
	created_at DATETIME NOT NULL DEFAULT GETDATE(),
	updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id),
    FOREIGN KEY (appointment_time_id) REFERENCES appointment_times(id) ON DELETE SET NULL,
    FOREIGN KEY (appointment_type_id) REFERENCES appointment_types(id) ON DELETE SET NULL
);

-- Appointments
CREATE TABLE appointments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY, 
    patient_id BIGINT NOT NULL,
    doctor_id BIGINT,
    clinic_service_id BIGINT,
    clinic_id BIGINT,
    appointment_time_id BIGINT,
    appointment_date DATETIME NOT NULL,
    price_id BIGINT NOT NULL,
    status VARCHAR(20) CHECK (status IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'CANCELLED')) DEFAULT 'PENDING',
    appointment_type_id BIGINT NOT NULL,
    reason NVARCHAR(MAX),
    result NVARCHAR(MAX),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (patient_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE CASCADE,
    FOREIGN KEY (clinic_service_id) REFERENCES clinic_services(id) ON DELETE SET NULL,
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE SET NULL,
    FOREIGN KEY (appointment_time_id) REFERENCES appointment_times(id) ON DELETE SET NULL,
    FOREIGN KEY (appointment_type_id) REFERENCES appointment_types(id) ON DELETE SET NULL,
    FOREIGN KEY (price_id) REFERENCES prices(id)
);

-- Appointment Types
CREATE TABLE appointment_types (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX)
);


-- Payment Methods Lookup Table
CREATE TABLE payment_methods (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description NVARCHAR(MAX)
);


-- Payments
CREATE TABLE payments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    appointment_id BIGINT NOT NULL,
    clinic_id BIGINT,
    patient_id BIGINT,
    amount DECIMAL(10, 2) NOT NULL,
    transaction_type VARCHAR(20) NOT NULL CHECK (transaction_type IN ('APPOINTMENT', 'SUBSCRIPTION')),
    payment_method_id BIGINT NOT NULL,
    status VARCHAR(10) CHECK (status IN ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED')) DEFAULT 'PENDING',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (appointment_id) REFERENCES appointments(id) ON DELETE CASCADE,
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE SET NULL,
    FOREIGN KEY (patient_id) REFERENCES users(id) ON DELETE SET NULL,
    FOREIGN KEY (payment_method_id) REFERENCES payment_methods(id) ON DELETE SET NULL
);


-- Reviews
CREATE TABLE reviews (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    patient_id BIGINT NOT NULL,
    doctor_id BIGINT,
    clinic_service_id BIGINT,
    appointment_id BIGINT,
    rating INT CHECK (rating BETWEEN 1 AND 5),
    comment NVARCHAR(MAX),
    parent_review_id BIGINT,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (patient_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE CASCADE,
    FOREIGN KEY (clinic_service_id) REFERENCES clinic_services(id) ON DELETE SET NULL,
    FOREIGN KEY (appointment_id) REFERENCES appointments(id) ON DELETE SET NULL,
    FOREIGN KEY (parent_review_id) REFERENCES reviews(id)
);


-- Blog Types
CREATE TABLE blog_types (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    type_name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url VARCHAR(MAX),
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
    parent_id BIGINT,
    FOREIGN KEY (parent_id) REFERENCES blog_types(id)
);


-- Blogs
CREATE TABLE blogs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    title NVARCHAR(255) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    thumbnail_url VARCHAR(MAX),
    blog_type_id BIGINT,
    status VARCHAR(20) CHECK (status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')) DEFAULT 'DRAFT',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (blog_type_id) REFERENCES blog_types(id) ON DELETE SET NULL
);


-- Blog Authors
CREATE TABLE blog_authors (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    blog_id BIGINT NOT NULL,
    user_id BIGINT,
    doctor_id BIGINT,
    role VARCHAR(20) CHECK (role IN ('AUTHOR', 'REVIEWER', 'ADVISOR')) DEFAULT 'AUTHOR',
    FOREIGN KEY (blog_id) REFERENCES blogs(id),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    FOREIGN KEY (doctor_id) REFERENCES doctors(id) ON DELETE SET NULL
);


-- Service Types
CREATE TABLE service_types (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url VARCHAR(MAX),
    parent_id BIGINT,
    service_id BIGINT,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
    FOREIGN KEY (parent_id) REFERENCES service_types(id)
    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE SET NULL
);


-- Services
CREATE TABLE services (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url VARCHAR(MAX),
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
);

-- Clinic Services
CREATE TABLE clinic_services (
    clinic_service_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    clinic_id BIGINT NOT NULL,
    service_id BIGINT NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
    FOREIGN KEY (clinic_id) REFERENCES clinics(id) ON DELETE CASCADE,
    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE SET NULL
);


-- FAQs Category
CREATE TABLE faqs_category (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);


-- FAQs
CREATE TABLE faqs (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    question NVARCHAR(MAX) NOT NULL,
    answer NVARCHAR(MAX) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('ACTIVE', 'INACTIVE')) DEFAULT 'INACTIVE',
    faqs_category_id BIGINT,
    created_by_account_id BIGINT,
    updated_by_account_id BIGINT,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (faqs_category_id) REFERENCES faqs_category(id),
    FOREIGN KEY (created_by_account_id) REFERENCES accounts(id),
    FOREIGN KEY (updated_by_account_id) REFERENCES accounts(id)
);


-- Favourites
CREATE TABLE favourites (
    patient_id BIGINT NOT NULL,
    doctor_id BIGINT NOT NULL,
    last_book DATETIME,
    PRIMARY KEY (patient_id, doctor_id),
    FOREIGN KEY (patient_id) REFERENCES users(id),
    FOREIGN KEY (doctor_id) REFERENCES doctors(id)
);


-- Messages
CREATE TABLE messages (
    message_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    sender_id BIGINT NOT NULL,
    receiver_id BIGINT NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    status VARCHAR(10) CHECK (status IN ('READ', 'UNREAD')) DEFAULT 'UNREAD',
    FOREIGN KEY (sender_id) REFERENCES accounts(id),
    FOREIGN KEY (receiver_id) REFERENCES accounts(id)
);


-- Notifications
CREATE TABLE notifications (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    message NVARCHAR(MAX) NOT NULL,
    status VARCHAR(10) CHECK (status IN ('READ', 'UNREAD')) DEFAULT 'UNREAD',
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);


-- Discounts
CREATE TABLE discounts (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX),
    clinic_id BIGINT NOT NULL,
    specialty_id BIGINT,
    doctor_id BIGINT,
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

