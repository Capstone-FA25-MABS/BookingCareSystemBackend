# API Versioning Implementation Guide

## Overview

This document outlines the comprehensive API versioning strategy implemented for the BookingCare system, covering both backend microservices and frontend client applications.

## Backend API Versioning

### 1. Versioning Strategy

We use **URL Path Versioning** as the primary strategy with multiple fallback mechanisms:

- **Primary**: URL segments (`/api/v1.1/users`)
- **Secondary**: Header-based (`X-API-Version: 1.1`)
- **Tertiary**: Query parameters (`?version=1.1`)
- **Quaternary**: Content negotiation (`Accept: application/json;version=1.1`)

### 2. Version Format

- **Format**: `v{major}.{minor}` (e.g., `v1.0`, `v1.1`, `v2.0`)
- **Backwards Compatibility**: Maintained within major versions
- **Breaking Changes**: Only allowed in major version increments

### 3. Implementation Details

#### Base Controller Updates
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")] // Legacy support
[ApiVersion("1.0")]
[ApiVersion("1.1")]
public abstract class BaseApiController : ControllerBase
```

#### Service-Specific Versioning
```csharp
[HttpPost("login")]
[MapToApiVersion("1.0")]
public async Task<IActionResult> Login(LoginRequest request)

[HttpPost("login")]
[MapToApiVersion("1.1")]
public async Task<IActionResult> LoginV11(LoginRequest request)
```

#### API Gateway Configuration
```json
{
  "DownstreamPathTemplate": "/api/v{version}/auths/{everything}",
  "UpstreamPathTemplate": "/api/v{version}/auths/{everything}"
}
```

### 4. Supported Versions

| Version | Status | Description | Deprecation Date |
|---------|--------|-------------|------------------|
| v1.0 | Supported | Initial release | 2025-12-31 |
| v1.1 | Default | Enhanced features | - |
| v2.0 | Planned | Major update | - |

## Frontend API Versioning

### 1. Configuration

```typescript
export const API_VERSIONS = {
  'v1.0': {
    version: 'v1.0',
    isDefault: false,
    isSupported: true,
    deprecationDate: '2025-12-31'
  },
  'v1.1': {
    version: 'v1.1',
    isDefault: true,
    isSupported: true
  }
} as const;
```

### 2. HTTP Client Implementation

```typescript
class HttpClient {
  private buildVersionedUrl(url: string, version: ApiVersion): string {
    return `v${version}/${url}`;
  }

  async get<T>(url: string, config?: ApiRequestConfig): Promise<ApiResponse<T>> {
    const finalConfig = this.prepareConfig({ ...config, url });
    // ... implementation
  }
}
```

### 3. Service Layer Integration

```typescript
export const userService = {
  async login(data: UserLogin, options: UserServiceOptions = {}) {
    const { version = 'v1.1' } = options;
    
    try {
      const response = await httpClient.post('auths/login', data, { version });
      return response.data;
    } catch (error) {
      // Fallback to v1.0 if v1.1 fails
      if (version === 'v1.1') {
        console.warn('Login v1.1 failed, falling back to v1.0');
        const fallbackResponse = await httpClient.post('auths/login', data, { 
          version: 'v1.0' 
        });
        return fallbackResponse.data;
      }
      throw error;
    }
  }
};
```

## Best Practices

### 1. Version Management

- **Semantic Versioning**: Follow semver principles
- **Backwards Compatibility**: Maintain within major versions
- **Deprecation Policy**: 6-month notice period
- **Documentation**: Keep comprehensive version docs

### 2. Client Implementation

- **Graceful Fallback**: Implement automatic fallback to supported versions
- **Version Detection**: Auto-detect and use latest supported version
- **Error Handling**: Proper error handling for version mismatches
- **Caching**: Cache version compatibility checks

### 3. Testing Strategy

- **Multi-Version Testing**: Test all supported versions
- **Compatibility Tests**: Ensure backwards compatibility
- **Deprecation Alerts**: Alert users of deprecated versions
- **Performance Testing**: Test version negotiation overhead

## Migration Guide

### From v1.0 to v1.1

1. **Authentication Endpoints**:
   - Enhanced login response with additional metadata
   - Same request format, enriched response

2. **Error Handling**:
   - Improved error messages
   - Additional error context

3. **Headers**:
   - New optional headers for enhanced tracking
   - Backwards compatible

### Breaking Changes in v2.0 (Planned)

1. **Authentication**:
   - JWT token format changes
   - New refresh token mechanism

2. **Response Format**:
   - Standardized response envelope
   - Different pagination structure

## Monitoring and Analytics

### 1. Version Usage Tracking

- Track API version usage per endpoint
- Monitor deprecated version usage
- Alert on high deprecated usage

### 2. Performance Metrics

- Response times per version
- Error rates per version
- Migration success rates

### 3. Health Checks

```csharp
[HttpGet("health")]
[MapToApiVersions("1.0", "1.1")]
public IActionResult Health()
{
    return Ok(new { 
        Status = "Healthy", 
        Service = "Auth", 
        Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0",
        Timestamp = DateTime.UtcNow 
    });
}
```

## Error Handling

### 1. Version Not Supported

```json
{
  "error": {
    "code": "VERSION_NOT_SUPPORTED",
    "message": "API version 2.0 is not supported",
    "supportedVersions": ["1.0", "1.1"],
    "latestVersion": "1.1"
  }
}
```

### 2. Version Deprecated

```json
{
  "warning": {
    "code": "VERSION_DEPRECATED",
    "message": "API version 1.0 is deprecated and will be removed on 2025-12-31",
    "migrationGuide": "/docs/migration/v1.0-to-v1.1"
  }
}
```

## Security Considerations

### 1. Version-Based Access Control

- Different authentication requirements per version
- Version-specific rate limiting
- Security patches per version

### 2. Audit Logging

- Log API version used in all requests
- Track version-specific security events
- Monitor for version downgrade attacks

## Deployment Strategy

### 1. Blue-Green Deployment

- Deploy new versions alongside existing ones
- Gradual traffic shifting between versions
- Rollback capabilities per version

### 2. Feature Flags

- Control version availability via feature flags
- Gradual rollout of new versions
- A/B testing capabilities

### 3. Database Migrations

- Version-specific database schemas
- Backwards-compatible data structures
- Migration scripts per version

## Tools and Utilities

### 1. Version Validation

- Client-side version compatibility checker
- Server-side version negotiation
- Automatic version detection

### 2. Development Tools

- Swagger documentation per version
- Postman collections per version
- SDK generation per version

### 3. Monitoring Dashboard

- Real-time version usage metrics
- Deprecation warnings
- Migration progress tracking

---

## Quick Reference

### Backend Endpoint Examples

```
# Version-specific
GET /api/v1.1/users/profile
POST /api/v1.0/auths/login

# Legacy (auto-resolves to v1.0)
GET /api/users/profile
POST /api/auths/login

# Header-based
GET /api/users/profile
X-API-Version: 1.1

# Query parameter
GET /api/users/profile?version=1.1
```

### Frontend Usage Examples

```typescript
// Explicit version
await userService.login(credentials, { version: 'v1.1' });

// Default version (v1.1)
await userService.login(credentials);

// With fallback
const response = await httpClient.get('users/profile', {
  version: 'v1.1',
  fallbackVersion: 'v1.0'
});
```
