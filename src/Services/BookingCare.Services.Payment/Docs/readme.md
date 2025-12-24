# 💳 BookingCare Payment Service - Frontend Developer Guide

<div align="center">

![BookingCare Logo](https://via.placeholder.com/400x100/0056b3/ffffff?text=BookingCare+Payment+Service)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022+-red?style=flat-square)](https://www.microsoft.com/sql-server)
[![REST API](https://img.shields.io/badge/REST-API-green?style=flat-square)](https://restfulapi.net/)
[![Swagger](https://img.shields.io/badge/Swagger-Documentation-orange?style=flat-square)](https://swagger.io/)

**Quản lý thanh toán cho appointments và subscriptions trong hệ thống BookingCare**

</div>

---

## 📑 Mục Lục

- [🎯 Tổng Quan](#-tổng-quan)
- [🚀 Quick Start](#-quick-start)
- [💰 Payments API](#-payments-api)
- [💳 Payment Methods API](#-payment-methods-api)
- [📊 Statistics API](#-statistics-api)
- [🔧 Error Handling](#-error-handling)
- [📝 TypeScript Interfaces](#-typescript-interfaces)
- [💡 Best Practices](#-best-practices)
- [🎨 UI Integration Examples](#-ui-integration-examples)

---

## 🎯 Tổng Quan

**BookingCare Payment Service** quản lý tất cả các giao dịch thanh toán trong hệ thống. Service hỗ trợ 2 loại thanh toán chính:

### ✨ **Core Features**
- **🏥 Appointment Payments**: Patient thanh toán cho lịch hẹn với bác sĩ
- **📋 Subscription Payments**: Clinic thanh toán cho gói dịch vụ
- **📊 Payment Statistics**: Thống kê và báo cáo đầy đủ
- **💳 Payment Methods**: Quản lý các phương thức thanh toán
- **🔍 Advanced Search**: Tìm kiếm và phân trang
- **📈 Analytics Dashboard**: Dữ liệu cho charts và dashboard

---

## 🚀 Quick Start

### **Base URL**
```
https://api.bookingcare.com/api/payments
https://api.bookingcare.com/api/paymentmethods
```

### **Authentication**
```javascript
// All requests require JWT token
const headers = {
  'Authorization': `Bearer ${jwtToken}`,
  'Content-Type': 'application/json'
}
```

### **Standard API Response Format**
```typescript
interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errors?: string[]
}

interface PagedResponse<T> extends ApiResponse<PagedResult<T>> {}

interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}
```

---

## 💰 Payments API

### **🔍 1. Get Payment by ID**
```javascript
// GET /api/payments/{id}
async function getPayment(paymentId) {
  const response = await fetch(`${baseUrl}/payments/${paymentId}`, {
    headers: authHeaders
  })
  return await response.json()
}

// Response
{
  "success": true,
  "message": "Lấy payment thành công",
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "appointmentId": "123e4567-e89b-12d3-a456-426614174001",
    "patientId": "123e4567-e89b-12d3-a456-426614174002",
    "clinicId": null,
    "subscriptionId": null,
    "amount": 500000,
    "transactionType": "APPOINTMENT",
    "paymentMethodId": "11111111-1111-1111-1111-111111111111",
    "paymentMethodName": "VNPAY",
    "status": "COMPLETED",
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### **🏥 2. Create Appointment Payment**
```javascript
// POST /api/payments/appointment
async function createAppointmentPayment(paymentData) {
  const response = await fetch(`${baseUrl}/payments/appointment`, {
    method: 'POST',
    headers: authHeaders,
    body: JSON.stringify({
      appointmentId: paymentData.appointmentId,
      patientId: paymentData.patientId,
      amount: paymentData.amount,
      paymentMethodId: paymentData.paymentMethodId
    })
  })
  return await response.json()
}

// Example Usage
const newPayment = await createAppointmentPayment({
  appointmentId: "123e4567-e89b-12d3-a456-426614174001",
  patientId: "123e4567-e89b-12d3-a456-426614174002", 
  amount: 500000,
  paymentMethodId: "11111111-1111-1111-1111-111111111111"
})
```

### **📋 3. Create Subscription Payment**
```javascript
// POST /api/payments/subscription
async function createSubscriptionPayment(paymentData) {
  const response = await fetch(`${baseUrl}/payments/subscription`, {
    method: 'POST',
    headers: authHeaders,
    body: JSON.stringify({
      subscriptionId: paymentData.subscriptionId,
      clinicId: paymentData.clinicId,
      amount: paymentData.amount,
      paymentMethodId: paymentData.paymentMethodId
    })
  })
  return await response.json()
}
```

### **🔄 4. Update Payment Status**
```javascript
// PUT /api/payments/{id}/status
async function updatePaymentStatus(paymentId, status) {
  const response = await fetch(`${baseUrl}/payments/${paymentId}/status`, {
    method: 'PUT',
    headers: authHeaders,
    body: JSON.stringify({
      status: status // "PENDING" | "COMPLETED" | "FAILED" | "REFUNDED"
    })
  })
  return await response.json()
}

// Usage
await updatePaymentStatus("123e4567-e89b-12d3-a456-426614174000", "COMPLETED")
```

### **📄 5. Get Paginated Payments**
```javascript
// GET /api/payments/patient/{patientId}?pageNumber=1&pageSize=10
async function getPatientPayments(patientId, options = {}) {
  const params = new URLSearchParams({
    pageNumber: options.pageNumber || 1,
    pageSize: options.pageSize || 10,
    sortBy: options.sortBy || 'CreatedAt',
    sortOrder: options.sortOrder || 'desc',
    ...(options.searchTerm && { searchTerm: options.searchTerm })
  })
  
  const response = await fetch(`${baseUrl}/payments/patient/${patientId}?${params}`, {
    headers: authHeaders
  })
  return await response.json()
}

// Example Usage
const patientPayments = await getPatientPayments("patient-123", {
  pageNumber: 1,
  pageSize: 20,
  searchTerm: "VNPAY",
  sortBy: "Amount",
  sortOrder: "desc"
})
```

### **🏥 6. Get Clinic Payments**
```javascript
// GET /api/payments/clinic/{clinicId}
async function getClinicPayments(clinicId, options = {}) {
  const params = new URLSearchParams({
    pageNumber: options.pageNumber || 1,
    pageSize: options.pageSize || 10,
    sortBy: options.sortBy || 'CreatedAt',
    sortOrder: options.sortOrder || 'desc',
    ...(options.searchTerm && { searchTerm: options.searchTerm })
  })
  
  const response = await fetch(`${baseUrl}/payments/clinic/${clinicId}?${params}`, {
    headers: authHeaders
  })
  return await response.json()
}
```

---

## 💳 Payment Methods API

### **📋 1. Get All Payment Methods**
```javascript
// GET /api/paymentmethods
async function getAllPaymentMethods() {
  const response = await fetch(`${baseUrl}/paymentmethods`, {
    headers: authHeaders
  })
  return await response.json()
}

// Response
{
  "success": true,
  "message": "Lấy danh sách payment methods thành công",
  "data": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "CASH",
      "description": "Thanh toán bằng tiền mặt",
      "status": "ACTIVE"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222", 
      "name": "VNPAY",
      "description": "Thanh toán qua VNPay",
      "status": "ACTIVE"
    }
  ]
}
```

### **✅ 2. Get Active Payment Methods Only**
```javascript
// GET /api/paymentmethods/active
async function getActivePaymentMethods() {
  const response = await fetch(`${baseUrl}/paymentmethods/active`, {
    headers: authHeaders
  })
  return await response.json()
}

// Perfect for payment forms - only shows available options
```

### **🔄 3. Toggle Payment Method Status**
```javascript
// PUT /api/paymentmethods/{id}/toggle
async function togglePaymentMethodStatus(paymentMethodId) {
  const response = await fetch(`${baseUrl}/paymentmethods/${paymentMethodId}/toggle`, {
    method: 'PUT',
    headers: authHeaders
  })
  return await response.json()
}

// Usage (for admin panel)
await togglePaymentMethodStatus("11111111-1111-1111-1111-111111111111")
```

---

## 📊 Statistics API

### **📈 1. Get Payment Statistics (Simple)**
```javascript
// GET /api/payments/statistics
// Uses smart defaults: Last 6 months, Monthly period
async function getDefaultStatistics() {
  const response = await fetch(`${baseUrl}/payments/statistics`, {
    headers: authHeaders
  })
  return await response.json()
}

// Perfect for dashboard - no parameters needed!
const dashboardStats = await getDefaultStatistics()
```

### **🎛 2. Custom Statistics Query**
```javascript
// GET /api/payments/statistics with custom parameters
async function getCustomStatistics(options) {
  const params = new URLSearchParams()
  
  if (options.fromDate) params.append('fromDate', options.fromDate)
  if (options.toDate) params.append('toDate', options.toDate)
  if (options.period) params.append('period', options.period)
  if (options.clinicId) params.append('clinicId', options.clinicId)
  if (options.transactionType) params.append('transactionType', options.transactionType)
  if (options.status) params.append('status', options.status)
  
  const response = await fetch(`${baseUrl}/payments/statistics?${params}`, {
    headers: authHeaders
  })
  return await response.json()
}

// Example Usage - Last 3 months by week
const weeklyStats = await getCustomStatistics({
  fromDate: '2024-04-01',
  toDate: '2024-06-30', 
  period: 'Weekly',
  status: 'COMPLETED'
})

// Example Usage - Clinic specific yearly stats
const clinicStats = await getCustomStatistics({
  fromDate: '2020-01-01',
  toDate: '2024-12-31',
  period: 'Yearly',
  clinicId: 'clinic-123',
  transactionType: 'SUBSCRIPTION'
})
```

### **📊 3. Statistics Response Structure**
```javascript
// Statistics API returns comprehensive data for charts
{
  "success": true,
  "message": "Lấy thống kê payments thành công (mặc định: 2024-01-01 - 2024-07-31)",
  "data": {
    // Time series data for line/bar charts
    "timeSeries": [
      {
        "timeLabel": "2024-01",
        "periodStart": "2024-01-01T00:00:00Z",
        "periodEnd": "2024-01-31T23:59:59Z",
        "totalCount": 150,
        "totalAmount": 75000000,
        "completedCount": 130,
        "completedAmount": 65000000,
        "pendingCount": 15,
        "failedCount": 5,
        "refundedCount": 0,
        "averageAmount": 500000
      }
    ],
    
    // Summary stats for dashboard cards
    "summary": {
      "totalPayments": 900,
      "totalAmount": 450000000,
      "totalCompletedAmount": 400000000,
      "successRate": 88.9,
      "averagePaymentAmount": 500000,
      "maxPaymentAmount": 2000000,
      "minPaymentAmount": 100000,
      "averagePaymentsPerDay": 4.3,
      "growthRate": 15.2
    },
    
    // Payment method breakdown for pie charts
    "paymentMethodBreakdown": [
      {
        "paymentMethodId": "11111111-1111-1111-1111-111111111111",
        "paymentMethodName": "VNPAY", 
        "count": 270,
        "totalAmount": 135000000,
        "percentage": 30.0,
        "averageAmount": 500000
      }
    ],
    
    // Status breakdown for doughnut charts
    "statusBreakdown": [
      {
        "status": "COMPLETED",
        "count": 800,
        "totalAmount": 400000000,
        "percentage": 88.9
      }
    ],
    
    // Transaction type breakdown
    "transactionTypeBreakdown": [
      {
        "transactionType": "APPOINTMENT",
        "count": 720,
        "totalAmount": 360000000,
        "percentage": 80.0,
        "averageAmount": 500000
      }
    ],
    
    "period": "Monthly",
    "dateRange": "2024-01-01 - 2024-07-31"
  }
}
```

---

## 🔧 Error Handling

### **Standard Error Responses**
```javascript
// Validation Error (400)
{
  "success": false,
  "message": "Dữ liệu không hợp lệ",
  "errors": [
    "AppointmentId không được để trống",
    "Amount phải lớn hơn 0"
  ]
}

// Not Found (404)
{
  "success": false,
  "message": "Payment với ID 123 không tìm thấy"
}

// Conflict (409) - Duplicate payment
{
  "success": false,
  "message": "Payment already exists for appointment 456"
}

// Server Error (500)
{
  "success": false,
  "message": "Có lỗi xảy ra khi tạo payment"
}
```

### **Frontend Error Handling Example**
```javascript
async function handleApiCall(apiFunction) {
  try {
    const response = await apiFunction()
    
    if (!response.success) {
      // Handle business logic errors
      if (response.errors) {
        // Show validation errors
        showValidationErrors(response.errors)
      } else {
        // Show general error
        showErrorMessage(response.message)
      }
      return null
    }
    
    return response.data
  } catch (error) {
    // Handle network/HTTP errors
    if (error.status === 401) {
      // Redirect to login
      redirectToLogin()
    } else if (error.status === 403) {
      showErrorMessage("Bạn không có quyền thực hiện thao tác này")
    } else {
      showErrorMessage("Có lỗi kết nối. Vui lòng thử lại sau.")
    }
    return null
  }
}
```

---

## 📝 TypeScript Interfaces

```typescript
// Payment Types
interface Payment {
  id: string
  appointmentId?: string
  clinicId?: string
  patientId?: string
  subscriptionId?: string
  amount: number
  transactionType: 'APPOINTMENT' | 'SUBSCRIPTION'
  paymentMethodId: string
  paymentMethodName: string
  status: 'PENDING' | 'COMPLETED' | 'FAILED' | 'REFUNDED'
  createdAt: string
}

interface CreateAppointmentPaymentRequest {
  appointmentId: string
  patientId: string
  amount: number
  paymentMethodId: string
}

interface CreateSubscriptionPaymentRequest {
  subscriptionId: string
  clinicId: string
  amount: number
  paymentMethodId: string
}

// Payment Method Types
interface PaymentMethod {
  id: string
  name: string
  description?: string
  status: 'ACTIVE' | 'INACTIVE'
}

// Statistics Types
interface PaymentStatistics {
  timeSeries: PaymentTimeSeriesData[]
  summary: PaymentSummaryStatistics
  paymentMethodBreakdown: PaymentMethodStatistics[]
  statusBreakdown: PaymentStatusStatistics[]
  transactionTypeBreakdown: TransactionTypeStatistics[]
  period: string
  dateRange: string
}

interface PaymentTimeSeriesData {
  timeLabel: string
  periodStart: string
  periodEnd: string
  totalCount: number
  totalAmount: number
  completedCount: number
  completedAmount: number
  pendingCount: number
  failedCount: number
  refundedCount: number
  averageAmount: number
}

interface PaymentSummaryStatistics {
  totalPayments: number
  totalAmount: number
  totalCompletedAmount: number
  successRate: number
  averagePaymentAmount: number
  maxPaymentAmount: number
  minPaymentAmount: number
  averagePaymentsPerDay: number
  growthRate: number
}

// Pagination Types
interface PaginationOptions {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  sortBy?: 'CreatedAt' | 'Amount' | 'Status'
  sortOrder?: 'asc' | 'desc'
}

interface StatisticsOptions {
  fromDate?: string
  toDate?: string
  period?: 'Daily' | 'Weekly' | 'Monthly' | 'Quarterly' | 'Yearly'
  clinicId?: string
  patientId?: string
  transactionType?: 'APPOINTMENT' | 'SUBSCRIPTION'
  status?: 'PENDING' | 'COMPLETED' | 'FAILED' | 'REFUNDED'
}
```

---

## 💡 Best Practices

### **1. 🚀 Performance Optimization**
```javascript
// ✅ DO: Use active payment methods for forms
const paymentMethods = await getActivePaymentMethods()

// ❌ DON'T: Get all then filter client-side
const allMethods = await getAllPaymentMethods()
const activeMethods = allMethods.data.filter(m => m.status === 'ACTIVE')

// ✅ DO: Use pagination for large lists
const payments = await getPatientPayments(patientId, { 
  pageSize: 20,
  pageNumber: 1 
})

// ✅ DO: Use default statistics for quick dashboard
const quickStats = await getDefaultStatistics()
```

### **2. 🔒 Security & Validation**
```javascript
// ✅ DO: Validate amounts on frontend
function validatePaymentAmount(amount) {
  if (amount <= 0) return "Số tiền phải lớn hơn 0"
  if (amount > 99999999.99) return "Số tiền quá lớn"
  return null
}

// ✅ DO: Check payment method availability
async function validatePaymentMethod(paymentMethodId) {
  const activeMethods = await getActivePaymentMethods()
  return activeMethods.data.some(m => m.id === paymentMethodId)
}
```

### **3. 📱 User Experience**
```javascript
// ✅ DO: Show loading states
const [isCreatingPayment, setIsCreatingPayment] = useState(false)

async function handleCreatePayment(paymentData) {
  setIsCreatingPayment(true)
  try {
    const result = await createAppointmentPayment(paymentData)
    showSuccessMessage("Thanh toán được tạo thành công!")
    return result
  } finally {
    setIsCreatingPayment(false)
  }
}

// ✅ DO: Handle different payment statuses
function getStatusColor(status) {
  switch(status) {
    case 'COMPLETED': return 'green'
    case 'PENDING': return 'orange' 
    case 'FAILED': return 'red'
    case 'REFUNDED': return 'blue'
    default: return 'gray'
  }
}
```

---

## 🎨 UI Integration Examples

### **💳 Payment Form Component**
```typescript
// React Payment Form Example
import React, { useState, useEffect } from 'react'

interface PaymentFormProps {
  appointmentId: string
  patientId: string
  amount: number
  onSuccess: (payment: Payment) => void
}

export function PaymentForm({ appointmentId, patientId, amount, onSuccess }: PaymentFormProps) {
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([])
  const [selectedMethodId, setSelectedMethodId] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    loadPaymentMethods()
  }, [])

  async function loadPaymentMethods() {
    const response = await getActivePaymentMethods()
    if (response.success) {
      setPaymentMethods(response.data)
      if (response.data.length > 0) {
        setSelectedMethodId(response.data[0].id)
      }
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setIsLoading(true)

    try {
      const response = await createAppointmentPayment({
        appointmentId,
        patientId,
        amount,
        paymentMethodId: selectedMethodId
      })

      if (response.success) {
        onSuccess(response.data)
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="payment-form">
      <div className="form-group">
        <label>Số tiền thanh toán</label>
        <div className="amount-display">
          {amount.toLocaleString('vi-VN')} VNĐ
        </div>
      </div>

      <div className="form-group">
        <label>Phương thức thanh toán</label>
        <select 
          value={selectedMethodId} 
          onChange={(e) => setSelectedMethodId(e.target.value)}
          required
        >
          {paymentMethods.map(method => (
            <option key={method.id} value={method.id}>
              {method.name} - {method.description}
            </option>
          ))}
        </select>
      </div>

      <button 
        type="submit" 
        disabled={isLoading || !selectedMethodId}
        className="btn-primary"
      >
        {isLoading ? 'Đang xử lý...' : 'Thanh toán'}
      </button>
    </form>
  )
}
```

### **📊 Payment Statistics Dashboard**
```typescript
// React Dashboard Component
import React, { useState, useEffect } from 'react'
import { LineChart, PieChart, BarChart } from 'recharts'

export function PaymentDashboard() {
  const [stats, setStats] = useState<PaymentStatistics | null>(null)
  const [period, setPeriod] = useState<'Monthly' | 'Weekly' | 'Daily'>('Monthly')

  useEffect(() => {
    loadStatistics()
  }, [period])

  async function loadStatistics() {
    const response = await getCustomStatistics({
      period,
      // Uses smart defaults for date range
    })
    
    if (response.success) {
      setStats(response.data)
    }
  }

  if (!stats) return <div>Loading...</div>

  return (
    <div className="payment-dashboard">
      {/* Summary Cards */}
      <div className="summary-cards">
        <div className="card">
          <h3>Tổng thanh toán</h3>
          <p className="amount">{stats.summary.totalAmount.toLocaleString('vi-VN')} VNĐ</p>
        </div>
        <div className="card">
          <h3>Tỷ lệ thành công</h3>
          <p className="percentage">{stats.summary.successRate.toFixed(1)}%</p>
        </div>
        <div className="card">
          <h3>Trung bình/ngày</h3>
          <p className="count">{stats.summary.averagePaymentsPerDay.toFixed(1)} payments</p>
        </div>
      </div>

      {/* Period Selector */}
      <div className="period-selector">
        {['Daily', 'Weekly', 'Monthly'].map(p => (
          <button 
            key={p}
            className={period === p ? 'active' : ''}
            onClick={() => setPeriod(p as any)}
          >
            {p}
          </button>
        ))}
      </div>

      {/* Time Series Chart */}
      <div className="chart-container">
        <h3>Xu hướng thanh toán theo thời gian</h3>
        <LineChart data={stats.timeSeries} width={800} height={300}>
          {/* Chart configuration */}
        </LineChart>
      </div>

      {/* Payment Methods Pie Chart */}
      <div className="chart-container">
        <h3>Phân bố theo phương thức thanh toán</h3>
        <PieChart data={stats.paymentMethodBreakdown} width={400} height={300}>
          {/* Chart configuration */}
        </PieChart>
      </div>
    </div>
  )
}
```

### **📋 Payment History Table**
```typescript
// React Payment History Component
export function PaymentHistory({ patientId }: { patientId: string }) {
  const [payments, setPayments] = useState<PagedResult<Payment> | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    loadPayments()
  }, [currentPage, searchTerm])

  async function loadPayments() {
    const response = await getPatientPayments(patientId, {
      pageNumber: currentPage,
      pageSize: 10,
      searchTerm: searchTerm || undefined,
      sortBy: 'CreatedAt',
      sortOrder: 'desc'
    })

    if (response.success) {
      setPayments(response.data)
    }
  }

  return (
    <div className="payment-history">
      {/* Search */}
      <div className="search-box">
        <input
          type="text"
          placeholder="Tìm kiếm theo phương thức thanh toán..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </div>

      {/* Table */}
      <table className="payments-table">
        <thead>
          <tr>
            <th>Ngày</th>
            <th>Số tiền</th>
            <th>Phương thức</th>
            <th>Trạng thái</th>
            <th>Loại</th>
          </tr>
        </thead>
        <tbody>
          {payments?.items.map(payment => (
            <tr key={payment.id}>
              <td>{new Date(payment.createdAt).toLocaleDateString('vi-VN')}</td>
              <td>{payment.amount.toLocaleString('vi-VN')} VNĐ</td>
              <td>{payment.paymentMethodName}</td>
              <td>
                <span className={`status ${payment.status.toLowerCase()}`}>
                  {payment.status}
                </span>
              </td>
              <td>{payment.transactionType}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Pagination */}
      <div className="pagination">
        <button 
          disabled={!payments?.hasPreviousPage}
          onClick={() => setCurrentPage(prev => prev - 1)}
        >
          Previous
        </button>
        
        <span>
          Trang {payments?.pageNumber} / {payments?.totalPages}
        </span>
        
        <button 
          disabled={!payments?.hasNextPage}
          onClick={() => setCurrentPage(prev => prev + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}
```

---

## 🔍 Available Query Parameters

### **Pagination Parameters**
```typescript
interface PaginationParams {
  pageNumber?: number     // Default: 1, Min: 1
  pageSize?: number       // Default: 10, Max: 100
  searchTerm?: string     // Search in payment method name, amount, status
  sortBy?: string         // "CreatedAt" | "Amount" | "Status"
  sortOrder?: string      // "asc" | "desc" (default: "desc")
}
```

### **Statistics Parameters** 
```typescript
interface StatisticsParams {
  fromDate?: string       // YYYY-MM-DD format (default: 6 months ago)
  toDate?: string         // YYYY-MM-DD format (default: end of current month)
  period?: string         // "Daily" | "Weekly" | "Monthly" | "Quarterly" | "Yearly"
  clinicId?: string       // Filter by specific clinic
  patientId?: string      // Filter by specific patient
  transactionType?: string // "APPOINTMENT" | "SUBSCRIPTION"
  status?: string         // "PENDING" | "COMPLETED" | "FAILED" | "REFUNDED"
}
```

---

## 🎯 Common Use Cases

### **1. 💰 Payment Flow for Appointments**
```javascript
// Step 1: Patient selects appointment
// Step 2: Show payment form
const paymentMethods = await getActivePaymentMethods()

// Step 3: Create payment
const payment = await createAppointmentPayment({
  appointmentId: selectedAppointment.id,
  patientId: currentUser.id,
  amount: selectedAppointment.price,
  paymentMethodId: selectedPaymentMethod.id
})

// Step 4: Handle payment gateway (if needed)
if (payment.data.paymentMethodName === 'VNPAY') {
  // Redirect to VNPay
  window.location.href = payment.data.paymentUrl
}

// Step 5: Update payment status after callback
await updatePaymentStatus(payment.data.id, 'COMPLETED')
```

### **2. 📊 Admin Dashboard Statistics**
```javascript
// Quick dashboard with defaults
const overviewStats = await getDefaultStatistics()

// Detailed clinic analysis
const clinicAnalysis = await getCustomStatistics({
  clinicId: selectedClinic.id,
  period: 'Monthly',
  fromDate: '2024-01-01',
  toDate: '2024-12-31'
})

// Transaction type comparison
const appointmentStats = await getCustomStatistics({
  transactionType: 'APPOINTMENT',
  period: 'Weekly'
})

const subscriptionStats = await getCustomStatistics({
  transactionType: 'SUBSCRIPTION', 
  period: 'Weekly'
})
```

### **3. 🔧 Admin Panel - Payment Method Management**
```javascript
// Toggle payment method availability
async function handleTogglePaymentMethod(methodId) {
  const response = await togglePaymentMethodStatus(methodId)
  if (response.success) {
    // Refresh the list
    await loadPaymentMethods()
    showSuccessMessage(`${response.data.name} đã được ${response.data.status === 'ACTIVE' ? 'kích hoạt' : 'vô hiệu hóa'}`)
  }
}
```

---

Đây là guide hoàn chỉnh để sử dụng BookingCare Payment Service API. Để biết thêm chi tiết hoặc có thắc mắc, vui lòng liên hệ team Backend hoặc tham khảo Swagger documentation tại `/swagger`.

**Happy Coding! 🚀**