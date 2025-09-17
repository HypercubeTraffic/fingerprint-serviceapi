# Fingerprint Split Debugging Guide

## Current Issue
The split operation returns `Result: 0, Count: 4` indicating the `FPSPLIT_DoSplit` function is failing.

## Memory Addresses Explanation
The numbers you see (1248, 1312, 1376, etc.) are memory addresses being logged during memory allocation. They are expected and normal - they show the memory allocation is working correctly.

## Step-by-Step Testing Procedure

### 1. Check Device Connection
First, make sure your BIO600 device is properly connected:

```bash
# Start the web API
cd FingerprintWebAPI
dotnet run
```

Open browser: `http://localhost:5000`

1. Click **"Start Preview"** button
2. Check if **Connection Status** shows "Connected"
3. Verify you can see live fingerprint preview

### 2. Test Basic Capture
Before testing split, test basic capture:

1. Place finger on scanner
2. Click **"Save Image"** button
3. Check if image is captured successfully

### 3. Test Split Operation with Debug Logs
1. Place 4 fingers on the scanner (right hand: index, middle, ring, little)
2. Click **"Split Four Right Fingers"** button
3. Check the console logs for detailed debug information

### 4. Analyze Debug Output
Look for these debug messages in the console:

```
[DEBUG] LIVESCAN_SetCaptWindow result: [should be 1]
[DEBUG] LIVESCAN_GetFPRawData result: [should be 1], Data size: [should be Width*Height*2]
[DEBUG] MOSAIC_FingerQuality result: [should be > 0]
[DEBUG] Allocating memory for 10 finger slots, struct size: [should be 28]
[DEBUG] Allocated memory slot 0-9: ptr=0x..., p=0x...
[DEBUG] Calling FPSPLIT_DoSplit with: Width=1600, Height=1500, SplitWidth=300, SplitHeight=400
[DEBUG] FPSPLIT_DoSplit result: [currently 0 - this is the problem], FingerNum: [currently 4]
```

## Possible Issues and Solutions

### Issue 1: Device Not Connected
**Symptoms**: LIVESCAN_GetFPRawData result != 1
**Solution**: 
- Check device USB connection
- Restart the application
- Try different USB port

### Issue 2: No Finger Detected
**Symptoms**: MOSAIC_FingerQuality result < 0
**Solution**:
- Make sure fingers are properly placed on scanner
- Clean the scanner surface
- Press fingers firmly but not too hard

### Issue 3: Split Algorithm Failure
**Symptoms**: FPSPLIT_DoSplit result = 0
**Possible Causes**:
- Image data format issue
- Incorrect parameters
- DLL compatibility issue

## Testing with curl (Alternative Method)

If the web interface doesn't work, test directly with curl:

```bash
# 1. Initialize device
curl -X POST http://localhost:5000/api/fingerprint/initialize

# 2. Test split operation
curl -X POST http://localhost:5000/api/fingerprint/split/four-right \
  -H "Content-Type: application/json" \
  -d '{
    "channel": 0,
    "width": 1600,
    "height": 1500,
    "splitWidth": 300,
    "splitHeight": 400
  }'
```

## Next Steps Based on Debug Output

1. **If LIVESCAN_GetFPRawData fails**: Device connection issue
2. **If MOSAIC_FingerQuality fails**: Finger detection issue  
3. **If FPSPLIT_DoSplit fails**: Algorithm/parameter issue

Please run the test and share the debug output from the console!
