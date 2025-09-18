// BIO600 Fingerprint Preview System
// Improved to match Sample Code quality and functionality
// Key improvements:
// - Fixed canvas dimensions (640x600) to match fingerprint aspect ratio (16:15)
// - Removed backend vertical flip to fix upside-down display issue
// - Simplified image processing to match C# Sample Code approach
// - Better scaling and aspect ratio handling
// - Professional quality display matching the original application
class FingerprintPreview {
    constructor() {
        this.connection = null;
        this.canvas = document.getElementById('previewCanvas');
        this.ctx = this.canvas.getContext('2d');
        this.isPreviewRunning = false;
        this.frameCount = 0;
        this.lastFrameTime = Date.now();
        this.fps = 0;

        this.initializeElements();
        this.setupEventListeners();
        this.connectSignalR();
    }

    initializeElements() {
        this.startBtn = document.getElementById('startPreviewBtn');
        this.stopBtn = document.getElementById('stopPreviewBtn');
        this.captureBtn = document.getElementById('captureBtn');
        this.captureISOBtn = document.getElementById('captureISOBtn');
        this.captureANSIBtn = document.getElementById('captureANSIBtn');
        this.captureBothBtn = document.getElementById('captureBothBtn');
        this.splitTwoThumbsBtn = document.getElementById('splitTwoThumbsBtn');
        this.splitFourRightBtn = document.getElementById('splitFourRightBtn');
        this.downloadTemplateBtn = document.getElementById('downloadTemplateBtn');
        this.downloadSplitThumbsBtn = document.getElementById('downloadSplitThumbsBtn');
        this.downloadSplitBtn = document.getElementById('downloadSplitBtn');
        
        // NEW MISSING ELEMENTS FROM ORIGINAL SAMPLE CODE
        this.captureRollBtn = document.getElementById('captureRollBtn');
        this.captureSingleBtn = document.getElementById('captureSingleBtn');
        this.compareTemplatesBtn = document.getElementById('compareTemplatesBtn');
        this.captureLeftFourBtn = document.getElementById('captureLeftFourBtn');
        this.captureRightFourBtn = document.getElementById('captureRightFourBtn');
        this.captureTwoThumbsBtn = document.getElementById('captureTwoThumbsBtn');
        this.captureTwoThumbsDirectBtn = document.getElementById('captureTwoThumbsDirectBtn');
        this.playBeepBtn = document.getElementById('playBeepBtn');
        this.storeTemplate1Btn = document.getElementById('storeTemplate1Btn');
        this.storeTemplate2Btn = document.getElementById('storeTemplate2Btn');
        this.downloadRollBtn = document.getElementById('downloadRollBtn');
        
        // NEW CUSTOM ENDPOINT ELEMENTS
        this.captureRightFourTemplatesBtn = document.getElementById('captureRightFourTemplatesBtn');
        
        this.channelSelect = document.getElementById('channelSelect');
        this.widthInput = document.getElementById('widthInput');
        this.heightInput = document.getElementById('heightInput');
        this.splitWidthInput = document.getElementById('splitWidthInput');
        this.splitHeightInput = document.getElementById('splitHeightInput');
        this.templateFormatSelect = document.getElementById('templateFormatSelect');
        this.fingerTypeSelect = document.getElementById('fingerTypeSelect');
        this.rollWidthInput = document.getElementById('rollWidthInput');
        this.rollHeightInput = document.getElementById('rollHeightInput');
        
        this.connectionStatus = document.getElementById('connectionStatus');
        this.previewStatus = document.getElementById('previewStatus');
        this.deviceStatus = document.getElementById('deviceStatus');
        this.fpsCounter = document.getElementById('fpsCounter');
        this.qualityValue = document.getElementById('qualityValue');
        this.qualityIndicator = document.getElementById('qualityIndicator');
        this.qualityMessage = document.getElementById('qualityMessage');
        this.imageSize = document.getElementById('imageSize');
        this.templateFormat = document.getElementById('templateFormat');
        this.templateSize = document.getElementById('templateSize');
        this.templateQuality = document.getElementById('templateQuality');
        this.templateStatus = document.getElementById('templateStatus');
        this.splitThumbCount = document.getElementById('splitThumbCount');
        this.splitThumbSize = document.getElementById('splitThumbSize');
        this.splitThumbStatus = document.getElementById('splitThumbStatus');
        this.splitFingerCount = document.getElementById('splitFingerCount');
        this.splitSize = document.getElementById('splitSize');
        this.splitStatus = document.getElementById('splitStatus');
        this.logContainer = document.getElementById('logContainer');
        
        // NEW STATUS ELEMENTS
        this.template1Status = document.getElementById('template1Status');
        this.template2Status = document.getElementById('template2Status');
        this.matchScore = document.getElementById('matchScore');
        this.matchResult = document.getElementById('matchResult');
        this.rollSize = document.getElementById('rollSize');
        this.rollQuality = document.getElementById('rollQuality');
        this.rollStatus = document.getElementById('rollStatus');
        
        // NEW CUSTOM ENDPOINT STATUS ELEMENTS
        this.rightFourFingerCount = document.getElementById('rightFourFingerCount');
        this.rightFourTemplateFormat = document.getElementById('rightFourTemplateFormat');
        this.rightFourOverallQuality = document.getElementById('rightFourOverallQuality');
        this.rightFourTemplateStatus = document.getElementById('rightFourTemplateStatus');
        this.rightFourFingersDetails = document.getElementById('rightFourFingersDetails');

        // Store current template and split data
        this.currentTemplate = null;
        this.currentSplitThumbsResult = null;
        this.currentSplitResult = null;
        this.currentRollResult = null;
        this.storedTemplate1 = null;
        this.storedTemplate2 = null;
        
        // NEW CUSTOM ENDPOINT DATA
        this.currentRightFourTemplatesResult = null;
    }

    setupEventListeners() {
        this.startBtn.addEventListener('click', () => this.startPreview());
        this.stopBtn.addEventListener('click', () => this.stopPreview());
        this.captureBtn.addEventListener('click', () => this.captureImage());
        this.captureISOBtn.addEventListener('click', () => this.captureTemplate('ISO'));
        this.captureANSIBtn.addEventListener('click', () => this.captureTemplate('ANSI'));
        this.captureBothBtn.addEventListener('click', () => this.captureTemplate('BOTH'));
        this.splitTwoThumbsBtn.addEventListener('click', () => this.splitTwoThumbs());
        this.splitFourRightBtn.addEventListener('click', () => this.splitFourRightFingers());
        this.downloadTemplateBtn.addEventListener('click', () => this.downloadTemplate());
        this.downloadSplitThumbsBtn.addEventListener('click', () => this.downloadSplitThumbs());
        this.downloadSplitBtn.addEventListener('click', () => this.downloadSplitImages());
        
        // NEW MISSING EVENT LISTENERS FROM ORIGINAL SAMPLE CODE
        this.captureRollBtn.addEventListener('click', () => this.captureRollFingerprint());
        this.captureSingleBtn.addEventListener('click', () => this.captureSingleFinger());
        this.compareTemplatesBtn.addEventListener('click', () => this.compareTemplates());
        this.captureLeftFourBtn.addEventListener('click', () => this.captureFingerType(1));
        this.captureRightFourBtn.addEventListener('click', () => this.captureFingerType(2));
        this.captureTwoThumbsBtn.addEventListener('click', () => this.captureFingerType(3));
        this.captureTwoThumbsDirectBtn.addEventListener('click', () => this.captureTwoThumbsDirect());
        this.playBeepBtn.addEventListener('click', () => this.playBeep());
        this.storeTemplate1Btn.addEventListener('click', () => this.storeTemplate(1));
        this.storeTemplate2Btn.addEventListener('click', () => this.storeTemplate(2));
        this.downloadRollBtn.addEventListener('click', () => this.downloadRollImage());
        
        // NEW CUSTOM ENDPOINT EVENT LISTENERS
        this.captureRightFourTemplatesBtn.addEventListener('click', () => this.captureRightFourTemplates());
        
        this.fingerTypeSelect.addEventListener('change', () => this.setFingerDryWet());
    }

    async connectSignalR() {
        // Prevent multiple connection attempts
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.log('Already connected to SignalR', 'info');
            return;
        }
        
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connecting) {
            this.log('Connection attempt already in progress', 'info');
            return;
        }

        this.log('Connecting to SignalR...', 'info');

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/ws/fingerprint", {
                skipNegotiation: false,
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling,
                // Optimize for network connections
                accessTokenFactory: () => null,
                logMessageContent: false,
                withCredentials: false
            })
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000]) // More aggressive reconnection
            .configureLogging(signalR.LogLevel.Warning) // Reduce logging overhead
            .build();

        this.connection.on("ReceiveMessage", (message) => {
            this.handleMessage(message);
        });

        try {
            await this.connection.start();
            this.log('SignalR connected', 'success');
            this.updateConnectionStatus(true);
            
            // Initialize device automatically
            await this.initializeDevice();
        } catch (err) {
            this.log('SignalR connection error: ' + err, 'error');
            this.updateConnectionStatus(false);
            
            // Don't attempt manual reconnection since we have automatic reconnect
            this.log('Automatic reconnection will be attempted...', 'info');
        }

        this.connection.onreconnecting(() => {
            this.log('SignalR reconnecting...', 'warning');
            this.updateConnectionStatus(false);
        });

        this.connection.onreconnected(() => {
            this.log('SignalR reconnected', 'success');
            this.updateConnectionStatus(true);
        });

        this.connection.onclose(async () => {
            this.log('SignalR disconnected', 'warning');
            this.updateConnectionStatus(false);
            this.updatePreviewStatus(false);
            
            // Don't attempt manual reconnection since we have automatic reconnect
            this.log('Connection closed. Automatic reconnection will be attempted...', 'info');
        });
    }

    async initializeDevice() {
        try {
            const response = await fetch('/api/fingerprint/initialize', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const result = await response.json();
                if (result) {
                    this.log('Device initialized successfully', 'success');
                    this.updateDeviceStatus({ isSupported: true, isConnected: true });
                } else {
                    this.log('Device initialization failed', 'error');
                    this.updateDeviceStatus({ isSupported: false, isConnected: false });
                }
            } else {
                this.log('Failed to initialize device: HTTP ' + response.status, 'error');
            }
        } catch (error) {
            this.log('Error initializing device: ' + error.message, 'error');
        }
    }

    handleMessage(message) {
        const { type, data } = message;

        switch (type) {
            case 'connection':
                this.log(`Connected: ${data.message}`, 'success');
                break;

            case 'preview_started':
                this.log(`Preview started: ${data.width}x${data.height} @ ${data.fps} FPS`, 'success');
                this.updatePreviewStatus(true);
                this.startBtn.disabled = true;
                this.stopBtn.disabled = false;
                this.captureBtn.disabled = false;
                this.captureISOBtn.disabled = false;
                this.captureANSIBtn.disabled = false;
                this.captureBothBtn.disabled = false;
                this.splitTwoThumbsBtn.disabled = false;
                this.splitFourRightBtn.disabled = false;
                
                // Enable new buttons from original Sample Code
                this.captureRollBtn.disabled = false;
                this.captureSingleBtn.disabled = false;
                this.captureLeftFourBtn.disabled = false;
                this.captureRightFourBtn.disabled = false;
                this.captureTwoThumbsBtn.disabled = false;
                this.captureTwoThumbsDirectBtn.disabled = false;
                this.playBeepBtn.disabled = false;
                
                // Enable new custom endpoint button
                this.captureRightFourTemplatesBtn.disabled = false;
                break;

            case 'preview_stopped':
                this.log('Preview stopped', 'info');
                this.updatePreviewStatus(false);
                this.startBtn.disabled = false;
                this.stopBtn.disabled = true;
                this.captureBtn.disabled = true;
                this.captureISOBtn.disabled = true;
                this.captureANSIBtn.disabled = true;
                this.captureBothBtn.disabled = true;
                this.splitTwoThumbsBtn.disabled = true;
                this.splitFourRightBtn.disabled = true;
                
                // Disable new buttons from original Sample Code
                this.captureRollBtn.disabled = true;
                this.captureSingleBtn.disabled = true;
                this.captureLeftFourBtn.disabled = true;
                this.captureRightFourBtn.disabled = true;
                this.captureTwoThumbsBtn.disabled = true;
                this.captureTwoThumbsDirectBtn.disabled = true;
                this.playBeepBtn.disabled = true;
                
                // Disable new custom endpoint button
                this.captureRightFourTemplatesBtn.disabled = true;
                break;

            case 'preview':
                this.displayPreviewFrame(data);
                break;

            case 'capture_result':
                this.log(`Image captured: Quality ${data.quality}`, 'success');
                if (data.success && data.imageData) {
                    this.downloadImage(data.imageData, `fingerprint_${Date.now()}.bmp`);
                } else {
                    this.log(`Capture failed: ${data.message}`, 'error');
                }
                break;

            case 'template_result':
                this.handleTemplateResult(data);
                break;

                case 'split_thumbs_result':
                    this.handleSplitThumbsResult(data);
                    break;

                case 'split_result':
                    this.handleSplitResult(data);
                    break;

                case 'roll_capture_result':
                    this.handleRollCaptureResult(data);
                    break;

                case 'compare_result':
                    this.handleCompareResult(data);
                    break;

                case 'finger_type_result':
                    this.handleFingerTypeResult(data);
                    break;

                case 'two_thumbs_capture_result':
                    this.handleTwoThumbsCaptureResult(data);
                    break;

                case 'right_four_templates_result':
                    this.handleRightFourTemplatesResult(data);
                    break;

            case 'status':
                this.updateDeviceStatus(data);
                break;

            case 'error':
                this.log(`Error: ${data.message}`, 'error');
                break;
        }
    }

    displayPreviewFrame(data) {
        // Update FPS counter
        this.frameCount++;
        const now = Date.now();
        if (now - this.lastFrameTime >= 1000) {
            this.fps = Math.round((this.frameCount * 1000) / (now - this.lastFrameTime));
            this.fpsCounter.textContent = this.fps;
            this.frameCount = 0;
            this.lastFrameTime = now;
        }

        // Update quality indicator
        this.qualityValue.textContent = data.quality;
        this.updateQualityIndicator(data.quality);
        
        // Add quality message following Sample Code pattern
        this.updateQualityMessage(data.quality);

        // Update image size
        this.imageSize.textContent = `${data.width}x${data.height}`;

        // Update finger detection status
        const fingerStatus = document.getElementById('fingerStatus');
        if (data.hasFinger) {
            fingerStatus.textContent = 'Detected';
        } else {
            fingerStatus.textContent = 'Not Detected';
        }

        // Display image on canvas
        if (data.imageData) {
            this.drawImageFromBase64(data.imageData, data.width, data.height);
        }
    }

    drawImageFromBase64(base64Data, width, height) {
        try {
            // Convert base64 to bytes
            const binaryString = atob(base64Data);
            let bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            // Try to detect if data is compressed (GZip magic number: 1f 8b)
            if (bytes.length > 2 && bytes[0] === 0x1f && bytes[1] === 0x8b) {
                // Data is compressed, decompress it
                bytes = this.decompressGZip(bytes);
                this.log('Decompressed image data for network optimization', 'debug');
            }

            // Handle reduced resolution data (if bytes.length suggests it's half the expected size)
            const expectedSize = width * height;
            if (bytes.length < expectedSize * 0.7) {
                // Data was reduced for network transmission, interpolate back
                bytes = this.interpolateImageData(bytes, width, height);
            }

            // Create ImageData with proper dimensions
            const imageData = this.ctx.createImageData(width, height);
            const data = imageData.data;

        // Simple image processing for black fingerprints on white background
        for (let i = 0; i < bytes.length; i++) {
            let gray = bytes[i];

            // Apply contrast enhancement
            gray = Math.max(0, Math.min(255, (gray - 128) * 2.0 + 128));

            // Keep original colors - black fingerprint on white background
            // No color inversion needed

            const pixelIndex = i * 4;
            data[pixelIndex] = gray;     // R
            data[pixelIndex + 1] = gray; // G
            data[pixelIndex + 2] = gray; // B
            data[pixelIndex + 3] = 255;  // A
        }

        // Clear canvas with pure white background
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        // Calculate proper scaling to maintain aspect ratio
        const scaleX = this.canvas.width / width;
        const scaleY = this.canvas.height / height;
        const scale = Math.min(scaleX, scaleY) * 0.95; // 95% to add some padding

        const scaledWidth = Math.floor(width * scale);
        const scaledHeight = Math.floor(height * scale);

        // Center the image
        const offsetX = (this.canvas.width - scaledWidth) / 2;
        const offsetY = (this.canvas.height - scaledHeight) / 2;

        // Create a temporary canvas for scaling
        const tempCanvas = document.createElement('canvas');
        tempCanvas.width = width;
        tempCanvas.height = height;
        const tempCtx = tempCanvas.getContext('2d');
        tempCtx.putImageData(imageData, 0, 0);

        // Draw scaled image
        this.ctx.drawImage(tempCanvas, 0, 0, width, height,
            offsetX, offsetY, scaledWidth, scaledHeight);

        // Add simple border
        this.ctx.strokeStyle = '#d0d0d0';
        this.ctx.lineWidth = 1;
        this.ctx.strokeRect(offsetX, offsetY, scaledWidth, scaledHeight);
        } catch (error) {
            this.log('Error drawing image: ' + error.message, 'error');
            // Clear canvas on error
            this.ctx.fillStyle = '#f0f0f0';
            this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
            this.ctx.fillStyle = '#666';
            this.ctx.font = '16px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.fillText('Image processing error', this.canvas.width / 2, this.canvas.height / 2);
        }
    }

    decompressGZip(compressedData) {
        try {
            // Simple GZip decompression using pako library if available
            // For now, we'll implement a fallback that just returns the data
            // In production, you'd want to include a proper GZip library
            this.log('GZip decompression not fully implemented - using fallback', 'warning');
            
            // Remove GZip header and footer for basic decompression attempt
            if (compressedData.length > 18) {
                return compressedData.slice(10, compressedData.length - 8);
            }
            return compressedData;
        } catch (error) {
            this.log('Decompression failed: ' + error.message, 'warning');
            return compressedData;
        }
    }

    interpolateImageData(reducedData, width, height) {
        try {
            const expectedSize = width * height;
            const interpolatedData = new Uint8Array(expectedSize);
            
            // Simple nearest-neighbor interpolation
            const reductionFactor = Math.sqrt(reducedData.length / expectedSize);
            
            for (let y = 0; y < height; y++) {
                for (let x = 0; x < width; x++) {
                    const sourceX = Math.floor(x * reductionFactor);
                    const sourceY = Math.floor(y * reductionFactor);
                    const sourceWidth = Math.floor(width * reductionFactor);
                    
                    const sourceIndex = sourceY * sourceWidth + sourceX;
                    const targetIndex = y * width + x;
                    
                    if (sourceIndex < reducedData.length) {
                        interpolatedData[targetIndex] = reducedData[sourceIndex];
                    }
                }
            }
            
            return interpolatedData;
        } catch (error) {
            this.log('Image interpolation failed: ' + error.message, 'warning');
            return reducedData;
        }
    }

    updateQualityIndicator(quality) {
        this.qualityIndicator.className = 'quality-indicator ';
        
        // Following Sample Code and Java Demo quality thresholds exactly:
        // Sample Code: Quality >= 0 for acceptance
        // Java Demo: < 10 = bad, < 50 = acceptable, >= 50 = good
        if (quality >= 50) {
            this.qualityIndicator.classList.add('quality-excellent');
        } else if (quality >= 20) {
            this.qualityIndicator.classList.add('quality-good');
        } else if (quality >= 10) {
            this.qualityIndicator.classList.add('quality-poor');
        } else if (quality >= 0) {
            this.qualityIndicator.classList.add('quality-poor'); // Still detected but low quality
        } else {
            this.qualityIndicator.classList.add('quality-none'); // Invalid/no finger detected
        }
    }

    updateQualityMessage(quality) {
        // Following Sample Code message patterns exactly:
        // Sample Code shows: "Get Image Ok! Quality: X" when Quality >= 0
        // Java Demo shows: < 10 = "请按捺指纹", < 50 = "获取图像成功... 质量", >= 50 = "获取图像成功,可以提取特征"
        
        if (quality < 0) {
            this.qualityMessage.textContent = "Invalid quality measurement";
            this.qualityMessage.style.color = "#dc3545";
        } else if (quality < 10) {
            this.qualityMessage.textContent = "Please place finger properly on scanner";
            this.qualityMessage.style.color = "#dc3545";
        } else if (quality < 20) {
            this.qualityMessage.textContent = "Quality may be too low for templates";
            this.qualityMessage.style.color = "#ffc107";
        } else if (quality < 50) {
            this.qualityMessage.textContent = `Get Image Ok! Quality: ${quality}`;
            this.qualityMessage.style.color = "#ffc107";
        } else {
            this.qualityMessage.textContent = `Image quality is good! Quality: ${quality}`;
            this.qualityMessage.style.color = "#28a745";
        }
    }

    async startPreview() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const message = {
            command: 'start_preview',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error starting preview: ' + err, 'error');
        }
    }

    async stopPreview() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const message = { command: 'stop_preview' };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error stopping preview: ' + err, 'error');
        }
    }

    async captureImage() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const message = {
            command: 'capture',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing image: ' + err, 'error');
        }
    }

    async captureTemplate(format) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.templateStatus.textContent = 'Capturing...';
        this.log(`Capturing ${format} template...`, 'info');

        const message = {
            command: 'capture_template',
            format: format,
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing template: ' + err, 'error');
        }
    }

    async splitFourRightFingers() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.splitStatus.textContent = 'Splitting...';
        this.log('Splitting four right fingers...', 'info');

        const message = {
            command: 'split_four_right',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value),
            splitWidth: parseInt(this.splitWidthInput.value),
            splitHeight: parseInt(this.splitHeightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error splitting fingers: ' + err, 'error');
        }
    }

    async splitTwoThumbs() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.splitThumbStatus.textContent = 'Splitting...';
        this.log('Splitting two thumbs...', 'info');

        const message = {
            command: 'split_two_thumbs',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value),
            splitWidth: parseInt(this.splitWidthInput.value),
            splitHeight: parseInt(this.splitHeightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error splitting thumbs: ' + err, 'error');
        }
    }

    handleTemplateResult(data) {
        if (data.success) {
            this.currentTemplate = data;
            this.templateFormat.textContent = data.templateFormat || 'Multiple';
            this.templateSize.textContent = data.templateSize ? `${data.templateSize} bytes` : 'Unknown';
            this.templateQuality.textContent = data.qualityScore || '0';
            this.templateStatus.textContent = 'Captured';
            this.downloadTemplateBtn.disabled = false;
            
            // Enable template storage buttons
            this.storeTemplate1Btn.disabled = false;
            this.storeTemplate2Btn.disabled = false;

            this.log(`Template captured successfully: ${data.templateFormat || 'Multiple'} format, Quality: ${data.qualityScore}`, 'success');
        } else {
            this.templateStatus.textContent = 'Failed';
            this.downloadTemplateBtn.disabled = true;
            this.log(`Template capture failed: ${data.errorDetails || data.message}`, 'error');
        }
    }

    handleSplitResult(data) {
        if (data.success) {
            this.currentSplitResult = data;
            this.splitFingerCount.textContent = data.fingerCount || 0;
            this.splitSize.textContent = `${data.splitWidth || 300}x${data.splitHeight || 400}`;
            this.splitStatus.textContent = 'Split Complete';
            this.downloadSplitBtn.disabled = false;

            this.log(`Split completed successfully: ${data.fingerCount} fingers found`, 'success');
        } else {
            this.splitStatus.textContent = 'Split Failed';
            this.downloadSplitBtn.disabled = true;
            this.log(`Split failed: ${data.errorDetails || data.message}`, 'error');
        }
    }

    handleSplitThumbsResult(data) {
        if (data.success) {
            this.currentSplitThumbsResult = data;
            this.splitThumbCount.textContent = data.thumbCount || 0;
            this.splitThumbSize.textContent = `${data.splitWidth || 300}x${data.splitHeight || 400}`;
            this.splitThumbStatus.textContent = 'Split Complete';
            this.downloadSplitThumbsBtn.disabled = false;

            this.log(`Thumbs split completed successfully: ${data.thumbCount} thumbs found`, 'success');
        } else {
            this.splitThumbStatus.textContent = 'Split Failed';
            this.downloadSplitThumbsBtn.disabled = true;
            this.log(`Thumbs split failed: ${data.errorDetails || data.message}`, 'error');
        }
    }

    downloadTemplate() {
        if (!this.currentTemplate) {
            this.log('No template to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const format = this.currentTemplate.templateFormat || 'template';

        if (this.currentTemplate.templateData) {
            // Single template
            const filename = `fingerprint_${format}_${timestamp}.bin`;
            this.downloadBinaryData(this.currentTemplate.templateData, filename);
            this.log(`Downloaded ${format} template: ${filename}`, 'success');
        } else if (this.currentTemplate.isoTemplate && this.currentTemplate.ansiTemplate) {
            // Both templates
            const isoTemplate = this.currentTemplate.isoTemplate.data;
            const ansiTemplate = this.currentTemplate.ansiTemplate.data;

            const isoFilename = `fingerprint_ISO_${timestamp}.bin`;
            const ansiFilename = `fingerprint_ANSI_${timestamp}.bin`;

            this.downloadBinaryData(isoTemplate, isoFilename);
            this.downloadBinaryData(ansiTemplate, ansiFilename);

            this.log(`Downloaded both templates: ${isoFilename}, ${ansiFilename}`, 'success');
        } else {
            this.log('Invalid template data for download', 'error');
        }
    }

    downloadBinaryData(base64Data, filename) {
        const binaryString = atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        const blob = new Blob([bytes], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        URL.revokeObjectURL(url);
    }

    downloadSplitImages() {
        if (!this.currentSplitResult || !this.currentSplitResult.fingers) {
            this.log('No split images to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const fingers = this.currentSplitResult.fingers;

        fingers.forEach((finger, index) => {
            if (finger.imageData) {
                const filename = `finger_${finger.fingerName || `right_${index + 1}`}_${timestamp}.bmp`;
                this.downloadImage(finger.imageData, filename);
            }
        });

        this.log(`Downloaded ${fingers.length} split finger images`, 'success');
    }

    downloadSplitThumbs() {
        if (!this.currentSplitThumbsResult || !this.currentSplitThumbsResult.thumbs) {
            this.log('No split thumb images to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const thumbs = this.currentSplitThumbsResult.thumbs;

        thumbs.forEach((thumb, index) => {
            if (thumb.imageData) {
                const filename = `thumb_${thumb.thumbName || `thumb_${index + 1}`}_${timestamp}.bmp`;
                this.downloadImage(thumb.imageData, filename);
            }
        });

        this.log(`Downloaded ${thumbs.length} split thumb images`, 'success');
    }

    updateConnectionStatus(connected) {
        this.connectionStatus.textContent = connected ? 'Connected' : 'Disconnected';
        this.connectionStatus.className = connected ? 'status-value status-connected' : 'status-value status-disconnected';
    }

    updatePreviewStatus(running) {
        this.previewStatus.textContent = running ? 'Running' : 'Stopped';
        this.previewStatus.className = running ? 'status-value status-running' : 'status-value status-stopped';
    }

    updateDeviceStatus(data) {
        this.deviceStatus.textContent = data.isSupported ? 'Supported' : 'Not Supported';
        this.deviceStatus.className = data.isSupported ? 'status-value status-connected' : 'status-value status-disconnected';
    }

    downloadImage(base64Data, filename) {
        const link = document.createElement('a');
        link.href = 'data:application/octet-stream;base64,' + base64Data;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // NEW MISSING METHODS FROM ORIGINAL SAMPLE CODE

    async captureRollFingerprint() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.rollStatus.textContent = 'Capturing...';
        this.log('Capturing roll fingerprint...', 'info');

        const message = {
            command: 'capture_roll',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.rollWidthInput.value),
            height: parseInt(this.rollHeightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing roll fingerprint: ' + err, 'error');
        }
    }

    async captureSingleFinger() {
        // Single finger is just regular capture with 300x400 size
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const message = {
            command: 'capture',
            channel: parseInt(this.channelSelect.value),
            width: 300,
            height: 400
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing single finger: ' + err, 'error');
        }
    }

    async captureFingerType(fingerType) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const typeNames = { 1: 'Left Four Fingers', 2: 'Right Four Fingers', 3: 'Two Thumbs' };
        this.log(`Capturing ${typeNames[fingerType]}...`, 'info');

        const message = {
            command: 'capture_finger_type',
            fingerType: fingerType,
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing finger type: ' + err, 'error');
        }
    }

    async compareTemplates() {
        if (!this.storedTemplate1 || !this.storedTemplate2) {
            this.log('Please store both templates before comparing', 'warning');
            return;
        }

        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.matchResult.textContent = 'Comparing...';
        this.log('Comparing stored templates...', 'info');

        const message = {
            command: 'compare_templates',
            template1: this.storedTemplate1,
            template2: this.storedTemplate2
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error comparing templates: ' + err, 'error');
        }
    }

    async playBeep() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        const message = {
            command: 'play_beep',
            beepType: 1
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error playing beep: ' + err, 'error');
        }
    }

    async storeTemplate(templateNumber) {
        if (!this.currentTemplate) {
            this.log('No template to store. Please capture a template first.', 'warning');
            return;
        }

        const templateData = this.currentTemplate.templateData;
        if (!templateData) {
            this.log('Invalid template data', 'error');
            return;
        }

        if (templateNumber === 1) {
            this.storedTemplate1 = templateData;
            this.template1Status.textContent = 'Stored';
            this.log('Template 1 stored successfully', 'success');
        } else {
            this.storedTemplate2 = templateData;
            this.template2Status.textContent = 'Stored';
            this.log('Template 2 stored successfully', 'success');
        }

        // Enable compare button if both templates are stored
        if (this.storedTemplate1 && this.storedTemplate2) {
            this.compareTemplatesBtn.disabled = false;
        }
    }

    async setFingerDryWet() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        const selectedType = this.fingerTypeSelect.value;
        let level = 4; // Normal
        if (selectedType === 'Dry') level = 5;
        else if (selectedType === 'Wet') level = 3;

        const message = {
            command: 'set_dry_wet',
            level: level
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
            this.log(`Set finger type to: ${selectedType}`, 'info');
        } catch (err) {
            this.log('Error setting finger type: ' + err, 'error');
        }
    }

    handleRollCaptureResult(data) {
        if (data.success) {
            this.currentRollResult = data;
            this.rollQuality.textContent = data.quality || '0';
            this.rollStatus.textContent = 'Captured';
            this.downloadRollBtn.disabled = false;

            this.log(`Roll fingerprint captured successfully: Quality ${data.quality}`, 'success');
        } else {
            this.rollStatus.textContent = 'Failed';
            this.downloadRollBtn.disabled = true;
            this.log(`Roll capture failed: ${data.message}`, 'error');
        }
    }

    handleCompareResult(data) {
        if (data.success) {
            this.matchScore.textContent = data.score || '0';
            this.matchResult.textContent = data.isMatch ? 'Match!' : 'No Match';
            this.matchResult.style.color = data.isMatch ? '#28a745' : '#dc3545';

            this.log(`Template comparison: Score ${data.score}, ${data.isMatch ? 'Match' : 'No Match'}`, 
                data.isMatch ? 'success' : 'warning');
        } else {
            this.matchResult.textContent = 'Failed';
            this.log(`Comparison failed: ${data.message}`, 'error');
        }
    }

    handleFingerTypeResult(data) {
        if (data.success) {
            const typeNames = { 1: 'Left Four', 2: 'Right Four', 3: 'Two Thumbs' };
            this.log(`${typeNames[data.fingerType]} captured: ${data.detectedFingerCount} fingers, Quality ${data.quality}`, 'success');
            
            if (data.imageData) {
                this.downloadImage(data.imageData, `finger_type_${data.fingerType}_${Date.now()}.bmp`);
            }
        } else {
            this.log(`Finger type capture failed: ${data.message}`, 'error');
        }
    }

    handleTwoThumbsCaptureResult(data) {
        if (data.success) {
            this.log(`Two thumbs captured successfully: ${data.detectedFingerCount} thumbs, Quality ${data.quality}`, 'success');
            
            if (data.imageData) {
                this.downloadImage(data.imageData, `two_thumbs_${Date.now()}.bmp`);
            }
            
            // Play success sound (like original Sample Code)
            this.playBeep();
        } else {
            this.log(`Two thumbs capture failed: ${data.message}`, 'error');
        }
    }

    handleRightFourTemplatesResult(data) {
        console.log('Right four templates result:', data); // Debug logging
        
        if (data.success) {
            this.currentRightFourTemplatesResult = data;
            this.updateRightFourTemplatesUI(data);
            
            // Check what templates were actually created
            let templateInfo = 'Unknown';
            if (data.fingerTemplates && data.fingerTemplates.length > 0) {
                const hasIso = data.fingerTemplates.some(f => f.isoTemplate);
                const hasAnsi = data.fingerTemplates.some(f => f.ansiTemplate);
                templateInfo = hasIso && hasAnsi ? 'BOTH' : hasIso ? 'ISO' : hasAnsi ? 'ANSI' : 'None';
            }
            
            this.log(`Right four fingers templates captured successfully! Format: ${templateInfo}, Fingers: ${data.detectedFingerCount}`, 'success');
            
            // Display full image if available
            if (data.imageData) {
                this.displayImage(data.imageData);
            }
            
            // Play success sound
            this.playBeep();
        } else {
            this.rightFourTemplateStatus.textContent = 'Failed';
            this.log(`Failed to capture right four fingers templates: ${data.message}`, 'error');
        }
    }

    async captureTwoThumbsDirect() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('SignalR not connected', 'error');
            return;
        }

        this.log('Capturing two thumbs directly...', 'info');

        const message = {
            command: 'capture_two_thumbs',
            channel: parseInt(this.channelSelect.value),
            width: parseInt(this.widthInput.value),
            height: parseInt(this.heightInput.value)
        };

        try {
            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.log('Error capturing two thumbs: ' + err, 'error');
        }
    }

    downloadRollImage() {
        if (!this.currentRollResult || !this.currentRollResult.imageData) {
            this.log('No roll image to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const filename = `roll_fingerprint_${timestamp}.bmp`;
        this.downloadImage(this.currentRollResult.imageData, filename);
        this.log(`Downloaded roll image: ${filename}`, 'success');
    }

    // NEW CUSTOM ENDPOINT METHODS
    async captureRightFourTemplates() {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            this.log('Not connected to server', 'error');
            return;
        }

        try {
            this.log('Starting right four fingers template capture...', 'info');
            
            // Disable all buttons during capture
            this.captureRightFourTemplatesBtn.disabled = true;
            this.captureBtn.disabled = true;
            this.captureISOBtn.disabled = true;
            this.captureANSIBtn.disabled = true;
            this.captureBothBtn.disabled = true;
            
            this.rightFourTemplateStatus.textContent = 'Capturing...';

            const format = this.templateFormatSelect.value;
            const channel = parseInt(this.channelSelect.value);
            const width = parseInt(this.widthInput.value);
            const height = parseInt(this.heightInput.value);
            const splitWidth = parseInt(this.splitWidthInput.value);
            const splitHeight = parseInt(this.splitHeightInput.value);

            const message = {
                command: 'capture_right_four_templates',
                format: format,
                channel: channel,
                width: width,
                height: height,
                splitWidth: splitWidth,
                splitHeight: splitHeight,
                minQuality: 20  // Lower threshold to be more permissive
            };
            
            this.log(`Sending capture command: Format=${format}, MinQuality=20`, 'info');

            await this.connection.invoke("SendMessage", JSON.stringify(message));
        } catch (err) {
            this.rightFourTemplateStatus.textContent = 'Error';
            this.log('Error capturing right four fingers templates: ' + err, 'error');
        } finally {
            // Re-enable buttons after capture
            this.captureRightFourTemplatesBtn.disabled = false;
            this.captureBtn.disabled = false;
            this.captureISOBtn.disabled = false;
            this.captureANSIBtn.disabled = false;
            this.captureBothBtn.disabled = false;
        }
    }

    updateRightFourTemplatesUI(result) {
        this.rightFourFingerCount.textContent = result.detectedFingerCount || 0;
        this.rightFourTemplateFormat.textContent = result.fingerTemplates?.[0]?.isoTemplate && result.fingerTemplates?.[0]?.ansiTemplate ? 'BOTH' : 
                                                   result.fingerTemplates?.[0]?.isoTemplate ? 'ISO' : 
                                                   result.fingerTemplates?.[0]?.ansiTemplate ? 'ANSI' : 'None';
        this.rightFourOverallQuality.textContent = result.overallQuality || 0;
        this.rightFourTemplateStatus.textContent = result.success ? 'Success' : 'Failed';

        // AUTOMATIC DOWNLOAD: Download templates and images immediately if successful
        if (result.success && result.fingerTemplates?.length > 0) {
            const hasTemplates = result.fingerTemplates.some(f => f.isoTemplate || f.ansiTemplate);
            if (hasTemplates) {
                this.log('🚀 Auto-downloading templates and images...', 'info');
                this.downloadRightFourTemplates();
                this.downloadRightFourImages();
            } else {
                this.log('⚠️ Fingers detected but templates not created. Check finger quality or service logs.', 'warning');
            }
        }

        // Display individual finger details
        if (result.fingerTemplates && result.fingerTemplates.length > 0) {
            let detailsHtml = '<div style="margin-top: 5px; font-weight: bold;">Individual Fingers:</div>';
            
            result.fingerTemplates.forEach((finger, index) => {
                const hasIso = finger.isoTemplate ? '✓' : '✗';
                const hasAnsi = finger.ansiTemplate ? '✓' : '✗';
                detailsHtml += `
                    <div style="margin: 3px 0; padding: 3px; background: rgba(0,0,0,0.05);">
                        <strong>${finger.fingerName}</strong> (Q: ${finger.quality})
                        <br>ISO: ${hasIso} | ANSI: ${hasAnsi} | Pos: ${finger.x},${finger.y}
                    </div>
                `;
            });
            
            this.rightFourFingersDetails.innerHTML = detailsHtml;
        } else {
            this.rightFourFingersDetails.innerHTML = '<div style="color: #999;">No finger details available</div>';
        }
    }

    downloadRightFourTemplates() {
        if (!this.currentRightFourTemplatesResult || !this.currentRightFourTemplatesResult.fingerTemplates) {
            this.log('No right four fingers templates to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        let downloadCount = 0;

        this.currentRightFourTemplatesResult.fingerTemplates.forEach((finger, index) => {
            if (finger.isoTemplate) {
                const filename = `${finger.fingerName}_ISO_template_${timestamp}.dat`;
                this.downloadTemplate(finger.isoTemplate.data, filename);
                downloadCount++;
            }
            
            if (finger.ansiTemplate) {
                const filename = `${finger.fingerName}_ANSI_template_${timestamp}.dat`;
                this.downloadTemplate(finger.ansiTemplate.data, filename);
                downloadCount++;
            }
        });

        this.log(`Downloaded ${downloadCount} template files`, 'success');
    }

    downloadRightFourImages() {
        if (!this.currentRightFourTemplatesResult || !this.currentRightFourTemplatesResult.fingerTemplates) {
            this.log('No right four fingers images to download', 'warning');
            return;
        }

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        let downloadCount = 0;

        // Download full capture image
        if (this.currentRightFourTemplatesResult.imageData) {
            const fullImageFilename = `right_four_fingers_full_${timestamp}.bmp`;
            this.downloadImage(this.currentRightFourTemplatesResult.imageData, fullImageFilename);
            downloadCount++;
        }

        // Download individual finger images
        this.currentRightFourTemplatesResult.fingerTemplates.forEach((finger, index) => {
            if (finger.imageData) {
                const filename = `${finger.fingerName}_${timestamp}.bmp`;
                this.downloadImage(finger.imageData, filename);
                downloadCount++;
            }
        });

        this.log(`Downloaded ${downloadCount} image files`, 'success');
    }

    log(message, type = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('div');
        logEntry.className = `log-entry log-${type}`;
        logEntry.textContent = `[${timestamp}] ${message}`;

        this.logContainer.appendChild(logEntry);
        this.logContainer.scrollTop = this.logContainer.scrollHeight;

        // Keep only last 50 log entries
        while (this.logContainer.children.length > 50) {
            this.logContainer.removeChild(this.logContainer.firstChild);
        }
    }
}

// Initialize the application when the page loads
document.addEventListener('DOMContentLoaded', () => {
    new FingerprintPreview();
});
