# 🔍 BIO600 Fingerprint Web API

A modern **ASP.NET Core 8.0 Web API** that provides a comprehensive web interface for the **BIO600 fingerprint scanner**. This project transforms the original Windows desktop application into a modern web service with real-time preview, REST API endpoints, and WebSocket communication.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple)
![SignalR](https://img.shields.io/badge/SignalR-WebSocket-green)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64%20ONLY-red)
![Architecture](https://img.shields.io/badge/Architecture-64--bit-important)

## ✨ Features Overview

Transform your BIO600 fingerprint scanner into a modern web service! This project provides:

### 🌐 **Web Interface**
- **Real-time Preview**: Live fingerprint scanning with WebSocket streaming
- **Modern UI**: Clean, responsive interface matching professional standards
- **Multiple Capture Modes**: Single finger, four fingers, two thumbs, roll capture
- **Quality Assessment**: Real-time quality scoring and finger detection

### 🚀 **REST API**
- **Device Management**: Initialize, close, status monitoring
- **Image Capture**: Multiple formats and resolutions
- **Template Generation**: ISO/ANSI standard formats
- **Template Comparison**: Fingerprint matching with scoring
- **Image Processing**: Splitting, enhancement, format conversion

### ⚡ **Real-time Features**
- **WebSocket Communication**: Instant preview updates
- **Live Quality Feedback**: Real-time quality assessment
- **Audio/Visual Feedback**: LED control and beep notifications
- **Timeout Handling**: Smart capture timeout management

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

## 🛠️ Prerequisites

⚠️ **CRITICAL: Windows x64 ONLY** ⚠️

- **.NET 8.0 x64** runtime or later
- **Windows 10/11 x64** operating system (32-bit NOT supported)
- **BIO600 fingerprint scanner** hardware with drivers installed
- **64-bit Native DLLs**: Included in repository
  - `ZAZ_FpStdLib.dll` (1MB) - Template generation
  - `GALSXXYY.dll` (3.7MB) - Image capture
  - `GAMC.dll` (86KB) - Image processing  
  - `FpSplit.dll` (83KB) - Finger splitting
  - `Fione.dll` (2.1MB) - Image enhancement
  - `ZhiAngCamera.dll` (1MB) - Camera interface
  - `Hos_Interface.dll` (28KB) - Hardware interface
  - `imagecut.dll` (23KB) - Image processing
  - `opencv_world*.dll` (105MB) - Computer vision

## 🚀 Quick Start

### 1. **Clone the Repository**
```bash
git clone <your-github-repo-url>
cd FingerprintWebAPI
```

### 2. **Build and Run**
```bash
dotnet restore
dotnet build
dotnet run
```

### 3. **Access the Application**
- **Web Interface**: `http://localhost:5000`
- **API Documentation**: `http://localhost:5000/swagger`
- **WebSocket Hub**: `ws://localhost:5000/ws/fingerprint`

### 4. **Hardware Setup**
1. Connect your BIO600 fingerprint scanner
2. Ensure device drivers are installed
3. The application will auto-detect and initialize the device

### ⚠️ **Architecture Requirements**
- **Windows x64 ONLY**: This application will NOT work on:
  - ❌ Linux systems
  - ❌ macOS systems  
  - ❌ Windows x86 (32-bit)
  - ❌ ARM processors
- **64-bit DLLs**: All native libraries are compiled for x64 architecture
- **Memory Management**: Uses 64-bit pointer arithmetic for proper operation

## 📱 **Web Interface Preview**

The web interface provides a complete control panel with:
- **Real-time fingerprint preview** with quality assessment
- **Multiple capture modes** (single, multi-finger, roll)
- **Template generation** and comparison
- **Image downloading** in multiple formats
- **Device control** (LED, LCD, audio feedback)

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

1. **Image Orientation**: Preview images are displayed in correct orientation (vertical flip removed from preview to fix upside-down display)
2. **Quality Assessment**: Real-time quality scoring and finger detection
3. **Scaling**: Proper aspect ratio preservation for web display
4. **Format Conversion**: BMP header generation for image downloads (vertical flip still applied for saved images)

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
