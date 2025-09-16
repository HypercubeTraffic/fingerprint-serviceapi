# 🔍 BIO600 Fingerprint Web API

A modern **ASP.NET Core 8.0 Web API** that provides a comprehensive web interface for the **BIO600 fingerprint scanner**. This project transforms the original Windows desktop application into a modern web service with real-time preview, REST API endpoints, and WebSocket communication.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple)
![SignalR](https://img.shields.io/badge/SignalR-WebSocket-green)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

## ✨ Features Overview

## Features

- **Real-time Preview**: WebSocket-based real-time fingerprint preview with proper image processing
- **REST API**: Complete REST API for all fingerprint operations
- **Template Generation**: Support for ISO and ANSI template formats
- **Image Splitting**: Split multi-finger images into individual finger images
- **Web Interface**: Modern web UI matching the original desktop application functionality

## Architecture

### Backend Components

1. **FingerprintService**: Main service that wraps the native DLL functionality
2. **PreviewService**: Handles real-time preview streaming
3. **FingerprintHub**: SignalR hub for WebSocket communication
4. **Controllers**: REST API endpoints
5. **DLL Wrapper**: P/Invoke wrapper for native fingerprint DLLs

### Frontend Components

1. **SignalR Client**: Real-time communication with the backend
2. **Canvas Rendering**: Proper fingerprint image display with scaling and processing
3. **UI Controls**: Complete control interface matching the desktop application

## Prerequisites

- .NET 6.0 or later
- Windows OS (required for native DLLs)
- BIO600 fingerprint scanner hardware
- Native DLLs: `ZAZ_FpStdLib.dll`, `GALSXXYY.dll`, `Gamc.dll`, `FpSplit.dll`

## Installation

1. **Copy DLLs**: Ensure all native DLLs from the original application are in the output directory:
   - `ZAZ_FpStdLib.dll`
   - `GALSXXYY.dll` 
   - `Gamc.dll`
   - `FpSplit.dll`

2. **Build and Run**:
   ```bash
   cd FingerprintWebAPI
   dotnet restore
   dotnet build
   dotnet run
   ```

3. **Access the Interface**:
   - Web UI: `http://localhost:5000`
   - API Documentation: `http://localhost:5000/swagger`

## API Endpoints

### Device Management
- `POST /api/fingerprint/initialize` - Initialize the fingerprint device
- `POST /api/fingerprint/close` - Close the fingerprint device
- `GET /api/fingerprint/status` - Get device status

### Image Capture
- `POST /api/fingerprint/capture` - Capture a fingerprint image
- `POST /api/fingerprint/template` - Capture fingerprint template (ISO/ANSI)

### Image Processing
- `POST /api/fingerprint/split/four-right` - Split four right fingers
- `POST /api/fingerprint/split/two-thumbs` - Split two thumbs

### Preview
- `POST /api/preview/start` - Start real-time preview
- `POST /api/preview/stop` - Stop real-time preview
- `GET /api/preview/status` - Get preview status

## WebSocket Communication

The application uses SignalR for real-time communication:

- **Hub URL**: `/ws/fingerprint`
- **Commands**: JSON messages for device control
- **Events**: Real-time preview data, status updates, results

### Command Examples

```javascript
// Start preview
{
  "command": "start_preview",
  "channel": 0,
  "width": 1600,
  "height": 1500
}

// Capture template
{
  "command": "capture_template",
  "format": "ISO",
  "channel": 0,
  "width": 1600,
  "height": 1500
}
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "Fingerprint": {
    "DefaultChannel": 0,
    "DefaultWidth": 1600,
    "DefaultHeight": 1500,
    "DefaultSplitWidth": 300,
    "DefaultSplitHeight": 400,
    "PreviewFps": 30,
    "QualityThreshold": 0
  }
}
```

## Image Processing

The application implements the same image processing logic as the original desktop application:

1. **Vertical Flip**: Critical vertical flip applied to match the original display
2. **Quality Assessment**: Real-time quality scoring and finger detection
3. **Scaling**: Proper aspect ratio preservation for web display
4. **Format Conversion**: BMP header generation for image downloads

## Quality Thresholds

Following the original application's quality standards:

- **Quality < 0**: Invalid/no finger detected
- **Quality 0-9**: Poor quality, requires proper finger placement
- **Quality 10-19**: Low quality, may not be suitable for templates
- **Quality 20-49**: Acceptable quality
- **Quality 50+**: Good quality, suitable for template extraction

## Template Formats

- **ISO**: ISO/IEC 19794-2 standard format
- **ANSI**: ANSI/NIST-ITL 1-2000 standard format
- **BOTH**: Generate both ISO and ANSI templates simultaneously

## Troubleshooting

### Common Issues

1. **Device Not Found**: Ensure the fingerprint scanner is connected and drivers are installed
2. **DLL Not Found**: Verify all native DLLs are in the application directory
3. **Preview Not Working**: Check that the device is initialized and not in use by another application
4. **Poor Image Quality**: Ensure proper finger placement and clean scanner surface

### Logs

Application logs are written to:
- Console output
- `logs/fingerprint-api-{date}.txt`

## Development

### Adding New Features

1. **New API Endpoints**: Add controllers in the `Controllers` folder
2. **New Services**: Implement services in the `Services` folder
3. **UI Extensions**: Modify `wwwroot/app.js` and `wwwroot/index.html`

### Testing

The application includes comprehensive error handling and logging for troubleshooting device integration issues.

## License

This project wraps the existing BIO600 fingerprint scanner functionality. Ensure compliance with all hardware and software licensing requirements.
