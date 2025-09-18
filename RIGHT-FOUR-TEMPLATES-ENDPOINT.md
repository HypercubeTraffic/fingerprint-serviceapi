# Right Four Fingers Templates Custom Endpoint

## Overview

This document describes the new custom endpoint for capturing right four fingers and creating ISO/ANSI templates in a single operation.

## Endpoint

**POST** `/api/fingerprint/capture/right-four-templates`

## Description

This custom endpoint captures the right four fingers (index, middle, ring, little) and automatically creates fingerprint templates in ISO and/or ANSI formats. It combines the functionality of:
- Right four fingers capture (similar to Fourfinger_Test project)
- Finger splitting/segmentation 
- Template generation (ISO/ANSI)
- Quality validation

## Request Body

```json
{
  "format": "BOTH",        // "ISO", "ANSI", or "BOTH"
  "channel": 0,            // Scanner channel (default: 0)
  "width": 1600,           // Capture width (default: 1600)
  "height": 1500,          // Capture height (default: 1500)
  "splitWidth": 256,       // Individual finger width (default: 256)
  "splitHeight": 360,      // Individual finger height (default: 360)
  "minQuality": 30         // Minimum quality threshold (default: 30)
}
```

## Response Body

```json
{
  "success": true,
  "detectedFingerCount": 4,
  "message": "Successfully captured and created BOTH templates for all 4 right fingers",
  "fingerTemplates": [
    {
      "fingerName": "right_index",
      "fingerIndex": 0,
      "quality": 85,
      "imageData": "base64_encoded_finger_image",
      "isoTemplate": {
        "data": "base64_encoded_iso_template",
        "size": 1024,
        "quality": 85
      },
      "ansiTemplate": {
        "data": "base64_encoded_ansi_template", 
        "size": 1024,
        "quality": 85
      },
      "x": 123,
      "y": 456,
      "top": 100,
      "left": 200,
      "angle": 15
    },
    // ... similar objects for right_middle, right_ring, right_little
  ],
  "overallQuality": 82,
  "imageData": "base64_encoded_full_capture_image"
}
```

## Features

### 1. **Automatic Right Four Fingers Detection**
- Uses the same splitting algorithm as the Fourfinger_Test project
- Expects exactly 4 fingers to be detected
- Provides detailed position and quality information for each finger

### 2. **Multi-Format Template Support**
- **ISO**: Creates ISO/IEC 19794-2 compliant templates
- **ANSI**: Creates ANSI/NIST-ITL 1-2000 compliant templates  
- **BOTH**: Creates both ISO and ANSI templates simultaneously

### 3. **Quality Validation**
- Individual finger quality checking
- Configurable minimum quality threshold
- Overall capture quality assessment

### 4. **Comprehensive Response**
- Individual finger images (256x360 by default)
- Full capture image (1600x1500 by default)
- Template data for each finger
- Position and angle information
- Quality scores

## Usage Examples

### Basic Usage (Both Templates)
```bash
curl -X POST "http://localhost:5000/api/fingerprint/capture/right-four-templates" \
  -H "Content-Type: application/json" \
  -d '{
    "format": "BOTH",
    "minQuality": 30
  }'
```

### ISO Templates Only
```bash
curl -X POST "http://localhost:5000/api/fingerprint/capture/right-four-templates" \
  -H "Content-Type: application/json" \
  -d '{
    "format": "ISO",
    "minQuality": 40
  }'
```

### High Quality ANSI Templates
```bash
curl -X POST "http://localhost:5000/api/fingerprint/capture/right-four-templates" \
  -H "Content-Type: application/json" \
  -d '{
    "format": "ANSI",
    "minQuality": 50,
    "splitWidth": 300,
    "splitHeight": 400
  }'
```

## Web Interface

The endpoint is also accessible through the web interface at `http://localhost:5000/`:

1. **Start Preview**: Begin live fingerprint preview
2. **Click "Capture Right Four Templates"**: Trigger the custom capture
3. **View Results**: See individual finger details, template information, and download options
4. **Download Options**: 
   - Download all templates as separate files
   - Download all finger images
   - Download full capture image

## Error Handling

### Common Error Scenarios

1. **Device Not Connected**
```json
{
  "success": false,
  "message": "Device not connected"
}
```

2. **Wrong Finger Count**
```json
{
  "success": false,
  "detectedFingerCount": 3,
  "message": "Expected 4 fingers but detected 3. Please ensure all four right fingers are properly placed."
}
```

3. **Poor Quality**
```json
{
  "success": false,
  "detectedFingerCount": 4,
  "message": "Some fingers have poor quality: right_index: 25 (min: 30); right_ring: 28 (min: 30);",
  "fingerTemplates": [...],
  "overallQuality": 45
}
```

4. **Template Creation Failed**
```json
{
  "success": false,
  "message": "Captured fingers but some template creation failed. Check individual finger results.",
  "fingerTemplates": [...]
}
```

## Integration Notes

### With Existing Endpoints
- Compatible with all existing fingerprint API endpoints
- Uses the same device initialization and connection
- Follows the same response patterns as other template endpoints

### With WebSocket Interface
- Supports WebSocket commands via SignalR hub
- Command: `capture_right_four_templates`
- Response type: `right_four_templates_result`

### Template Storage
- Templates can be stored using existing `/api/fingerprint/template/store/{templateId}` endpoint
- Compatible with template comparison endpoint `/api/fingerprint/compare`

## Technical Details

### Implementation
- Based on Fourfinger_Test project's right four finger capture logic
- Uses same DLL wrapper functions (FPSPLIT_DoSplit, ZAZ_FpStdLib_CreateISOTemplate, etc.)
- Implements proper memory management for 64-bit systems
- Includes image enhancement and vertical flip corrections

### Performance
- Typical capture time: 2-5 seconds
- Template generation: ~100ms per finger
- Memory usage: ~10MB during capture operation
- Supports concurrent requests (thread-safe)

### Dependencies
- Requires BIO600 fingerprint scanner hardware
- Uses ZAZ_FpStdLib.dll for template generation
- Uses FpSplit.dll for finger segmentation
- Requires device to be initialized before use

## Testing

Use the included Postman collection (`BIO600-Fingerprint-API.postman_collection.json`) to test the endpoint:

1. Import the collection into Postman
2. Set the `baseUrl` variable to your API server (e.g., `http://localhost:5000`)
3. Initialize the device first
4. Run the "Capture Right Four Fingers Templates (Custom)" request

## Changelog

- **v1.0.0**: Initial implementation of right four fingers template capture endpoint
- Added support for ISO, ANSI, and BOTH template formats
- Integrated with web interface and WebSocket hub
- Added comprehensive error handling and quality validation
