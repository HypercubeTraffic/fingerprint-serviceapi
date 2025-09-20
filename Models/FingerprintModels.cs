namespace FingerprintWebAPI.Models
{
    public class FingerprintPreviewData
    {
        public string ImageData { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public int Quality { get; set; }
        public bool HasFinger { get; set; }
        public int Fps { get; set; }
    }

    public class CaptureRequest
    {
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class CaptureResponse
    {
        public bool Success { get; set; }
        public string? ImageData { get; set; }
        public int Quality { get; set; }
        public string? Message { get; set; }
    }

    public class TemplateRequest
    {
        public string Format { get; set; } = "ISO"; // ISO, ANSI, or BOTH
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class TemplateResponse
    {
        public bool Success { get; set; }
        public string? TemplateFormat { get; set; }
        public string? TemplateData { get; set; }
        public int TemplateSize { get; set; }
        public int QualityScore { get; set; }
        public string? Message { get; set; }
        public string? ErrorDetails { get; set; }
        
        // For BOTH format responses
        public TemplateData? IsoTemplate { get; set; }
        public TemplateData? AnsiTemplate { get; set; }
    }

    public class TemplateData
    {
        public string Data { get; set; } = string.Empty;
        public int Size { get; set; }
        public int Quality { get; set; }
    }

    public class SplitRequest
    {
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 300;
        public int SplitHeight { get; set; } = 400;
    }

    public class SplitResponse
    {
        public bool Success { get; set; }
        public int FingerCount { get; set; }
        public int ThumbCount { get; set; }
        public int SplitWidth { get; set; }
        public int SplitHeight { get; set; }
        public List<FingerData> Fingers { get; set; } = new();
        public List<ThumbData> Thumbs { get; set; } = new();
        public string? Message { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class FingerData
    {
        public string FingerName { get; set; } = string.Empty;
        public string ImageData { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Angle { get; set; }
        public int Quality { get; set; }
    }

    public class ThumbData
    {
        public string ThumbName { get; set; } = string.Empty;
        public string ImageData { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Angle { get; set; }
        public int Quality { get; set; }
    }

    public class DeviceStatus
    {
        public bool IsConnected { get; set; }
        public bool IsSupported { get; set; }
        public string DeviceInfo { get; set; } = string.Empty;
        public int ChannelCount { get; set; }
    }

    public class PreviewStatus
    {
        public bool IsRunning { get; set; }
        public int CurrentFps { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class StartPreviewCommand
    {
        public string Command { get; set; } = "start_preview";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class StopPreviewCommand
    {
        public string Command { get; set; } = "stop_preview";
    }

    public class CaptureCommand
    {
        public string Command { get; set; } = "capture";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class CaptureTemplateCommand
    {
        public string Command { get; set; } = "capture_template";
        public string Format { get; set; } = "ISO";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class SplitCommand
    {
        public string Command { get; set; } = "split_four_right";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 300;
        public int SplitHeight { get; set; } = 400;
    }

    public class SplitThumbsCommand
    {
        public string Command { get; set; } = "split_two_thumbs";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 300;
        public int SplitHeight { get; set; } = 400;
    }

    // NEW MISSING MODELS
    public class RollCaptureRequest
    {
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 750;
    }

    public class RollCaptureResponse
    {
        public bool Success { get; set; }
        public string? ImageData { get; set; }
        public int Quality { get; set; }
        public string? Message { get; set; }
    }

    public class CompareTemplatesRequest
    {
        public string Template1 { get; set; } = string.Empty;
        public string Template2 { get; set; } = string.Empty;
    }

    public class CompareTemplatesResponse
    {
        public bool Success { get; set; }
        public int Score { get; set; }
        public bool IsMatch { get; set; }
        public string? Message { get; set; }
    }

    public class FingerTypeRequest
    {
        public int FingerType { get; set; } = 1; // 1=Left four, 2=Right four, 3=Two thumbs
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    public class FingerTypeResponse
    {
        public bool Success { get; set; }
        public int FingerType { get; set; }
        public int DetectedFingerCount { get; set; }
        public string? ImageData { get; set; }
        public int Quality { get; set; }
        public string? Message { get; set; }
    }

    public class DeviceSettingsRequest
    {
        public int DryWetLevel { get; set; } = 4; // 3=Wet, 4=Normal, 5=Dry
        public int LedImageIndex { get; set; } = 0;
        public int LcdImageIndex { get; set; } = 0;
        public bool UseBeep { get; set; } = true;
    }

    public class RollCaptureCommand
    {
        public string Command { get; set; } = "capture_roll";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 750;
    }

    public class CompareTemplatesCommand
    {
        public string Command { get; set; } = "compare_templates";
        public string Template1 { get; set; } = string.Empty;
        public string Template2 { get; set; } = string.Empty;
    }

    public class CaptureFingerTypeCommand
    {
        public string Command { get; set; } = "capture_finger_type";
        public int FingerType { get; set; } = 1; // 1=Left four, 2=Right four, 3=Two thumbs
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
    }

    // NEW CUSTOM MODELS FOR RIGHT FOUR FINGERS TEMPLATE CAPTURE
    public class RightFourFingersTemplateRequest
    {
        public string Format { get; set; } = "BOTH"; // ISO, ANSI, or BOTH
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 256;
        public int SplitHeight { get; set; } = 360;
        public int MinQuality { get; set; } = 30; // Minimum quality threshold for each finger
    }

    public class RightFourFingersTemplateResponse
    {
        public bool Success { get; set; }
        public int DetectedFingerCount { get; set; }
        public string? Message { get; set; }
        public List<FingerTemplateData> FingerTemplates { get; set; } = new();
        public string? ErrorDetails { get; set; }
        public int OverallQuality { get; set; }
        public string? ImageData { get; set; } // Full capture image
    }

    public class FingerTemplateData
    {
        public string FingerName { get; set; } = string.Empty; // right_index, right_middle, right_ring, right_little
        public int FingerIndex { get; set; } // 0=index, 1=middle, 2=ring, 3=little
        public int Quality { get; set; }
        public string? ImageData { get; set; } // Individual finger image
        public TemplateData? IsoTemplate { get; set; }
        public TemplateData? AnsiTemplate { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Angle { get; set; }
    }

    public class RightFourFingersTemplateCommand
    {
        public string Command { get; set; } = "capture_right_four_templates";
        public string Format { get; set; } = "BOTH";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 256;
        public int SplitHeight { get; set; } = 360;
        public int MinQuality { get; set; } = 30;
    }

    // NEW: FULL RIGHT FOUR FINGERS AS ONE TEMPLATE
    public class FullRightFourFingersRequest
    {
        public string Format { get; set; } = "BOTH"; // ISO, ANSI, or BOTH
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int MinQuality { get; set; } = 30;
    }

    public class FullRightFourFingersResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int OverallQuality { get; set; }
        public string? ImageData { get; set; } // Full capture image
        public TemplateData? IsoTemplate { get; set; } // Combined template
        public TemplateData? AnsiTemplate { get; set; } // Combined template
        public string? ErrorDetails { get; set; }
    }

    public class FullRightFourFingersCommand
    {
        public string Command { get; set; } = "capture_full_right_four";
        public string Format { get; set; } = "BOTH";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int MinQuality { get; set; } = 30;
    }

    // NEW CUSTOM MODELS FOR LEFT FOUR FINGERS TEMPLATE CAPTURE
    public class LeftFourFingersTemplateRequest
    {
        public string Format { get; set; } = "BOTH"; // ISO, ANSI, or BOTH
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 256;
        public int SplitHeight { get; set; } = 360;
        public int MinQuality { get; set; } = 30; // Minimum quality threshold for each finger
    }

    public class LeftFourFingersTemplateResponse
    {
        public bool Success { get; set; }
        public int DetectedFingerCount { get; set; }
        public string? Message { get; set; }
        public List<LeftFingerTemplateData> FingerTemplates { get; set; } = new();
        public string? ErrorDetails { get; set; }
        public int OverallQuality { get; set; }
        public string? ImageData { get; set; } // Full capture image
    }

    public class LeftFingerTemplateData
    {
        public string FingerName { get; set; } = string.Empty; // left_index, left_middle, left_ring, left_little
        public int FingerIndex { get; set; } // 0=index, 1=middle, 2=ring, 3=little
        public int Quality { get; set; }
        public string? ImageData { get; set; } // Individual finger image
        public TemplateData? IsoTemplate { get; set; }
        public TemplateData? AnsiTemplate { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public int Angle { get; set; }
    }

    public class LeftFourFingersTemplateCommand
    {
        public string Command { get; set; } = "capture_left_four_templates";
        public string Format { get; set; } = "BOTH";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int SplitWidth { get; set; } = 256;
        public int SplitHeight { get; set; } = 360;
        public int MinQuality { get; set; } = 30;
    }

    // NEW: FULL LEFT FOUR FINGERS AS ONE TEMPLATE
    public class FullLeftFourFingersRequest
    {
        public string Format { get; set; } = "BOTH"; // ISO, ANSI, or BOTH
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int MinQuality { get; set; } = 30;
    }

    public class FullLeftFourFingersResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int OverallQuality { get; set; }
        public string? ImageData { get; set; } // Full capture image
        public TemplateData? IsoTemplate { get; set; } // Combined template
        public TemplateData? AnsiTemplate { get; set; } // Combined template
        public string? ErrorDetails { get; set; }
    }

    public class FullLeftFourFingersCommand
    {
        public string Command { get; set; } = "capture_full_left_four";
        public string Format { get; set; } = "BOTH";
        public int Channel { get; set; } = 0;
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 1500;
        public int MinQuality { get; set; } = 30;
    }
}
