# Secure Kestrel Configuration Guide

This guide explains how to implement secure Kestrel configuration across all microservices in the BookingCare system.

## Overview

The `KestrelConfigurationExtensions` provides a standardized, secure way to configure Kestrel web server for all services with:

- ✅ Environment-specific configurations (Development vs Production)
- ✅ Automatic HTTPS enforcement in production
- ✅ Configurable ports per service
- ✅ Security limits and timeouts
- ✅ gRPC support with HTTP/2
- ✅ Connection limits for DoS protection

## Implementation Steps

### 1. Add Project Reference

Add this to your service's `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Shared\BookingCare.Shared.Common\BookingCare.Shared.Common.csproj" />
</ItemGroup>
```

### 2. Update Program.cs

Replace the hardcoded Kestrel configuration:

```csharp
// OLD - Insecure hardcoded configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6100, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// NEW - Secure configurable setup
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "serviceName");
```

### 3. Service Names and Default Ports

| Service | Service Name | HTTP Port | gRPC Port |
|---------|-------------|-----------|-----------|
| AI Service | "ai" | 6000 | 6100 |
| User Service | "user" | 6010 | 6110 |
| Appointment Service | "appointment" | 6020 | 6120 |
| Notification Service | "notification" | 6030 | 6130 |
| Payment Service | "payment" | 6040 | 6140 |
| API Gateway | "gateway" | 5000 | N/A |

### 4. Configuration Files

#### appsettings.Development.json
```json
{
  "Kestrel": {
    "HttpPort": 6000,
    "GrpcPort": 6100,
    "EnableGrpc": true,
    "MaxConcurrentConnections": 100,
    "MaxConcurrentUpgradedConnections": 100,
    "MaxRequestBodySize": 31457280,
    "RequestHeadersTimeoutSeconds": 30,
    "KeepAliveTimeoutSeconds": 120
  }
}
```

#### appsettings.Production.json
```json
{
  "Kestrel": {
    "HttpPort": 6000,
    "GrpcPort": 6100,
    "EnableGrpc": true,
    "CertificatePath": "/path/to/certificate.pfx",
    "CertificatePassword": "certificate_password",
    "MaxConcurrentConnections": 200,
    "MaxConcurrentUpgradedConnections": 100,
    "MaxRequestBodySize": 31457280,
    "RequestHeadersTimeoutSeconds": 30,
    "KeepAliveTimeoutSeconds": 120
  }
}
```

## Security Features

### 1. Automatic HTTPS
- **Development**: HTTP allowed for easier debugging
- **Production**: HTTPS enforced automatically

### 2. Connection Limits
- `MaxConcurrentConnections`: Prevents resource exhaustion
- `MaxConcurrentUpgradedConnections`: Limits WebSocket connections
- `MaxRequestBodySize`: Prevents large payload attacks

### 3. Timeouts
- `RequestHeadersTimeout`: Prevents slow-loris attacks
- `KeepAliveTimeout`: Manages connection lifecycle

### 4. HTTP/2 Security
- Development: HTTP/2 without TLS for gRPC (internal communication)
- Production: HTTP/2 with TLS enforced

## Usage Examples

### Basic Service (REST API only)
```csharp
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "user");
```

### Service with gRPC disabled
```json
{
  "Kestrel": {
    "EnableGrpc": false,
    "HttpPort": 6010
  }
}
```

### Custom Port Configuration
```json
{
  "Kestrel": {
    "HttpPort": 8080,
    "GrpcPort": 8081,
    "EnableGrpc": true
  }
}
```

## Configuration Options

| Property | Description | Default | Environment |
|----------|-------------|---------|-------------|
| `HttpPort` | REST API port | Service-based | Both |
| `GrpcPort` | gRPC service port | Service-based | Both |
| `EnableGrpc` | Enable gRPC endpoint | `true` | Both |
| `CertificatePath` | SSL certificate path | `null` | Production |
| `CertificatePassword` | SSL certificate password | `null` | Production |
| `MaxConcurrentConnections` | Max concurrent connections | 100 (dev), 200 (prod) | Both |
| `MaxRequestBodySize` | Max request size (bytes) | 30MB | Both |
| `RequestHeadersTimeoutSeconds` | Header timeout | 30s | Both |
| `KeepAliveTimeoutSeconds` | Keep-alive timeout | 120s | Both |

## Migration Checklist

For each service:
- [ ] Add Shared.Common project reference
- [ ] Add using statement: `using BookingCare.Shared.Common.Extensions;`
- [ ] Replace Kestrel configuration with `ConfigureSecureKestrel()`
- [ ] Add service name parameter
- [ ] Update appsettings.Development.json with Kestrel section
- [ ] Update appsettings.Production.json with Kestrel section and certificates
- [ ] Test both development and production configurations
- [ ] Verify HTTPS enforcement in production
- [ ] Test connection limits and timeouts

## Benefits

1. **Consistency**: All services use the same secure configuration pattern
2. **Security**: Built-in security best practices and HTTPS enforcement
3. **Maintainability**: Centralized configuration logic
4. **Flexibility**: Easy to customize per service or environment
5. **Monitoring**: Built-in connection limits and timeout protection
6. **Documentation**: Self-documenting configuration structure