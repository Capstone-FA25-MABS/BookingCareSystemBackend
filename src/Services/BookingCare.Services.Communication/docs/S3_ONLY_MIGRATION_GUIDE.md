# ?? S3-Only Migration Guide - Communication Service

## ?? **Overview**

Communication Service ?ã ???c c?u hình ?? **force t?t c? uploads lên AWS S3** thay vì Cloudinary. ?i?u này chu?n b? cho vi?c remove Cloudinary hoàn toàn.

## ? **Configuration Changes Made**

### **1. appsettings.json Updates**
```json
{
  "FileUpload": {
    "Provider": "S3",  // ? Changed from "Cloudinary" to "S3"
    // ... other config
  },
  "EnhancedFileUpload": {
    "Strategy": "S3Only",  // ? New strategy
    "ForceS3": true,       // ? Force flag
    "RoutingRules": {
      "File": "AWS",
      "Audio": "AWS", 
      "VoiceNote": "AWS",
      "Image": "AWS",      // ? Changed from "Cloudinary" to "AWS"
      "Video": "AWS"       // ? Changed from "Cloudinary" to "AWS"
    }
  }
}
```

### **2. HybridFileUploadService Updates**
- ? Added `IsForceS3Enabled()` method
- ? Modified `ShouldUseCloudinary()` to check force S3 flag
- ? Enhanced `UploadToS3Async()` with basic media info
- ? Added S3-compatible thumbnail handling

## ?? **Current Behavior**

### **All File Types ? AWS S3 + CloudFront**
```
MessageType.Image    ? AWS S3  ? (was Cloudinary)
MessageType.Video    ? AWS S3  ? (was Cloudinary)
MessageType.File     ? AWS S3  ? (unchanged)
MessageType.Audio    ? AWS S3  ? (unchanged)
MessageType.VoiceNote ? AWS S3  ? (unchanged)
```

### **Folder Structure**
```
S3 Bucket: mabs-capstone-fa25-booking-care-s3-bucket
??? communication/
?   ??? user-123/
?   ?   ??? image/         ? Images now go here (not Cloudinary)
?   ?   ??? video/         ? Videos now go here (not Cloudinary)
?   ?   ??? file/          ? Documents
?   ?   ??? audio/         ? Audio files
?   ?   ??? voicenote/     ? Voice messages
```

## ?? **API Usage Examples**

### **Smart Upload (Auto S3)**
```http
POST /api/enhanced-fileupload/smart-upload
Content-Type: multipart/form-data

file: photo.jpg
userId: user-123
messageType: Image
```

**Response:**
```json
{
  "success": true,
  "data": {
    "result": {
      "url": "https://d24em9p7s2uixh.cloudfront.net/communication/user-123/image/photo-unique.jpg",
      "fileName": "photo-unique.jpg",
      "size": 1024000,
      "mimeType": "image/jpeg",
      "thumbnailUrl": "https://d24em9p7s2uixh.cloudfront.net/communication/user-123/image/photo-unique.jpg",
      "width": null,
      "height": null,
      "duration": null
    },
    "provider": "AWS S3 + CloudFront",
    "routing": "Auto-routed based on MessageType and content"
  },
  "message": "Smart upload thành công qua AWS S3 + CloudFront!"
}
```

### **Direct S3 Upload**
```http
POST /api/enhanced-fileupload/s3/upload
Content-Type: multipart/form-data

file: document.pdf
userId: user-123  
messageType: File
customFolder: documents/important  # Optional
```

## ?? **Comparison: Before vs After**

| File Type | Before (Hybrid) | After (S3-Only) | Benefits |
|-----------|----------------|-----------------|----------|
| **Images** | Cloudinary | AWS S3 + CloudFront | Unified storage, lower cost |
| **Videos** | Cloudinary | AWS S3 + CloudFront | Unified storage, no processing |
| **Documents** | AWS S3 | AWS S3 + CloudFront | Unchanged |
| **Audio** | AWS S3 | AWS S3 + CloudFront | Unchanged |

## ?? **Important Notes**

### **Thumbnail Generation**
- **Before:** Cloudinary auto-generated thumbnails for images/videos
- **After:** Simple thumbnail handling (returns original URL for now)
- **Future:** Can implement AWS Lambda for thumbnail generation

### **Image Processing**
- **Before:** Cloudinary provided automatic optimization, format conversion
- **After:** Files stored as-is on S3
- **Recommendation:** Implement client-side optimization if needed

### **Performance Considerations**
- **CDN:** Still using CloudFront for fast delivery
- **Caching:** CloudFront provides global caching
- **Bandwidth:** S3 + CloudFront is cost-effective for large files

## ?? **Migration Steps**

### **Phase 1: Current (S3-Only Configuration)**
```json
"FileUpload": { "Provider": "S3" }
```
- ? All new uploads go to S3
- ? Existing Cloudinary files still accessible
- ? Hybrid service handles both storage types

### **Phase 2: Remove Cloudinary Dependencies (Future)**
```bash
# Remove Cloudinary packages
dotnet remove package CloudinaryDotNet

# Remove Cloudinary configuration
# Remove ICloudStorageProvider from DI
```

### **Phase 3: Clean Architecture (Future)**
```csharp
// Simplify to single S3FileUploadService
services.AddScoped<IFileUploadService, S3FileUploadService>();
// Remove HybridFileUploadService
```

## ?? **Testing**

### **Test All File Types**
```bash
# Test image upload
curl -X POST "http://localhost:6009/api/enhanced-fileupload/smart-upload" \
  -F "file=@image.jpg" \
  -F "userId=user-123" \
  -F "messageType=Image"

# Test document upload  
curl -X POST "http://localhost:6009/api/enhanced-fileupload/smart-upload" \
  -F "file=@document.pdf" \
  -F "userId=user-123" \
  -F "messageType=File"

# Test video upload
curl -X POST "http://localhost:6009/api/enhanced-fileupload/smart-upload" \
  -F "file=@video.mp4" \
  -F "userId=user-123" \
  -F "messageType=Video"
```

### **Verify S3 Routing**
Check logs for:
```
Force S3 mode enabled - routing all uploads to S3
Routing to AWS S3 for Image with content type image/jpeg
```

## ?? **Benefits of S3-Only Approach**

1. **Cost Reduction:** Single storage provider, predictable pricing
2. **Simplified Architecture:** Remove hybrid complexity
3. **Unified Management:** All files in one place
4. **Scalability:** S3 handles massive scale effortlessly
5. **Integration:** Better integration with other AWS services

## ?? **Future Enhancements**

### **AWS Lambda Thumbnail Generation**
```yaml
# Deploy Lambda function for image processing
aws lambda create-function \
  --function-name bookingcare-thumbnail-generator \
  --runtime nodejs18.x \
  --handler index.handler
```

### **S3 Event-Driven Processing**
```json
{
  "Rules": [{
    "Id": "ProcessImages", 
    "Filter": {"Prefix": "communication/"},
    "Status": "Enabled",
    "Transitions": [{
      "Days": 30,
      "StorageClass": "STANDARD_IA"
    }]
  }]
}
```

## ?? **Support & Troubleshooting**

### **Common Issues**
1. **Images don't have thumbnails:** Expected behavior in S3-only mode
2. **Files upload slowly:** Check CloudFront configuration
3. **Access denied:** Verify AWS S3 credentials and permissions

### **Rollback Plan**
```json
// If needed to rollback to Cloudinary
"FileUpload": { "Provider": "Cloudinary" }
"EnhancedFileUpload": { "Strategy": "Hybrid" }
```

---

## ? **Status: S3-Only Mode ACTIVE**

T?t c? uploads hi?n t?i s? ???c route t?i **AWS S3 + CloudFront** thay vì Cloudinary! ??