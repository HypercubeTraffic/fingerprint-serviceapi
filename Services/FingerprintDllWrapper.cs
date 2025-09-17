using System.Runtime.InteropServices;

namespace FingerprintWebAPI.Services
{
    /// <summary>
    /// P/Invoke wrapper for the native fingerprint DLLs
    /// This mirrors the FingerDll.cs from the original application
    /// Note: On Linux, these will be mock implementations for testing the web interface
    /// </summary>
    public static class FingerprintDllWrapper
    {
        // ZAZ_FpStdLib.dll functions - FIXED FOR 64-BIT
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern long ZAZ_FpStdLib_OpenDevice();
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern void ZAZ_FpStdLib_CloseDevice(long device);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CreateISOTemplate(long device, byte[] image, byte[] itemplate);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CreateANSITemplate(long device, byte[] image, byte[] itemplate);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CompareTemplates(long device, byte[] sTemplate, byte[] fTemplate);
        
        // MISSING FUNCTIONS FROM FOURFINGER_TEST
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_Calibration(long device);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_GetImage(long device, byte[] image);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_IsFinger(long device, byte[] image);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_GetImageQuality(long device, byte[] image);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_GetNFIQuality(long device, byte[] image);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_SearchingANSITemplates(long device, byte[] sTemplate, int arrayCnt, byte[] fTemplateArray, int matchedScoreTh);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_SearchingISOTemplates(long device, byte[] sTemplate, int arrayCnt, byte[] fTemplateArray, int matchedScoreTh);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CompressToWSQImage(long device, byte[] rawImage, byte[] wsqImage);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_UnCompressFromWSQImage(long device, byte[] wsqImage, int wsqSize, byte[] rawImage);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_GetANSIImageRecord(long device, byte[] image, byte[] itemplate);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_GetISOImageRecord(long device, byte[] image, byte[] itemplate);

        // GALSXXYY.dll functions
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_Init();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_Close();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetChannelCount();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_SetCaptWindow(int nChannel, int pnOriginX, int pnOriginY, int pnWidth, int pnHeight);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetFPRawData(int nChannel, byte[] pRawData);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_Beep(int beepType);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_SetLCDImage(int imageIndex);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_SetLedLight(int imageIndex);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_SetFingerDryWet(int nLevel);

        // MISSING LIVESCAN FUNCTIONS FROM FOURFINGER_TEST
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetSrcFPRawData(int nChannel, byte[] pRawData);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetRollFPRawData(byte[] pRawData, int width, int height);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetFlatFPRawData(byte[] pRawData, int width, int height);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_DistortionCorrection(byte[] pRawData, int width, int height, byte[] a);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetFingerArea(byte[] img, int width, int height);
        
        [DllImport("GALSXXYY.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int LIVESCAN_GetPreviewImageSize();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetPreviewData(int nChannel, byte[] pRawData);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_IsSupportPreview();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetVersion();
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetDesc(byte[] pszDesc);
        
        [DllImport("GALSXXYY.dll")]
        public static extern int LIVESCAN_GetErrorInfo(int nErrorNo, byte[] pszErrorInfo);

        // GAMC.dll functions
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_Init();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_Close();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_FingerQuality(byte[] pFingerBuf, int nWidth, int nHeight);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsFinger(byte[] pFingerBuf, int nWidth, int nHeight);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_Start(byte[] pFingerBuf, int nWidth, int nHeight);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_DoMosaic(byte[] pFingerBuf, int nWidth, int nHeight);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_Stop();

        // MISSING MOSAIC FUNCTIONS FROM FOURFINGER_TEST
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsSupportIdentifyFinger();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsSupportImageQuality();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsSupportFingerQuality();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsSupportImageEnhance();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_IsSupportRollCap();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_SetRollMode(int nRollMode);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_ImageQuality(byte[] pFingerBuf, int nWidth, int nHeight);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_ImageEnhance(byte[] pFingerBuf, int nWidth, int nHeight, byte[] pTargetImg);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_GetErrorInfo(int nErrorNo, byte[] pszErrorInfo);
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_GetVersion();
        
        [DllImport("Gamc.dll")]
        public static extern int MOSAIC_GetDesc(byte[] pszDesc);

        // Fione.dll functions - IMAGE ENHANCEMENT FROM FOURFINGER_TEST
        [DllImport("Fione.dll")]
        public static extern int NewImageDelFog(byte[] image, int width, int height);
        
        [DllImport("Fione.dll")]
        public static extern void ImageNormalOfImage(byte[] image, int width, int height);
        
        [DllImport("Fione.dll")]
        public static extern void ImageWeightFilter(byte[] image, int width, int height);

        // FpSplit.dll functions
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct FPSPLIT_INFO
        {
            public int x;
            public int y;
            public int top;
            public int left;
            public int angle;
            public int quality;
            public byte[] pOutBuf;
        }

        [DllImport("FpSplit.dll")]
        public static extern int FPSPLIT_Init(int nImgW, int nImgH, int nPreview);
        
        [DllImport("FpSplit.dll")]
        public static extern void FPSPLIT_Uninit();

        [DllImport("FpSplit.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int FPSPLIT_DoSplit(byte[] pImgBuf, int nImgW, int nImgH, int nPreview,
            int nSplitW, int nSplitH, ref int pnFpNum, IntPtr pInfo);

        /// <summary>
        /// Creates BMP header for raw fingerprint data - mirrors WriteHead from original FingerDll.cs
        /// </summary>
        public static int WriteHead(byte[] output, byte[] input, int nWidth, int nHeight)
        {
            int imageX = nWidth, imageY = nHeight;
            byte[] head = new byte[1078];
            byte[] head1 = new byte[] {
                0x42, 0x4d, // file type 
                0x0, 0x0, 0x0, 0x00, // file size***
                0x00, 0x00, // reserved
                0x00, 0x00, // reserved
                0x36, 0x4, 0x00, 0x00, // head byte***
                // infoheader
                0x28, 0x00, 0x00, 0x00, // struct size
                0x00, 0x00, 0x0, 0x00, // map width*** 
                0x00, 0x00, 0x00, 0x00, // map height***
                0x01, 0x00, // must be 1
                0x08, 0x00, // color count***
                0x00, 0x00, 0x00, 0x00, // compression
                0x00, 0x00, 0x00, 0x00, // data size***
                0x00, 0x00, 0x00, 0x00, // dpix
                0x00, 0x00, 0x00, 0x00, // dpiy
                0x00, 0x00, 0x00, 0x00, // color used
                0x00, 0x00, 0x00, 0x00, // color important
            };
            
            for (int z = 0; z < head1.Length; z++)
            {
                head[z] = head1[z];
            }

            long num;
            num = imageX; head[18] = (byte)(num & 0xFF);
            num = num >> 8; head[19] = (byte)(num & 0xFF);
            num = num >> 8; head[20] = (byte)(num & 0xFF);
            num = num >> 8; head[21] = (byte)(num & 0xFF);

            num = imageY; head[22] = (byte)(num & 0xFF);
            num = num >> 8; head[23] = (byte)(num & 0xFF);
            num = num >> 8; head[24] = (byte)(num & 0xFF);
            num = num >> 8; head[25] = (byte)(num & 0xFF);

            int i, j;
            j = 0;
            for (i = 54; i < 1078; i = i + 4)
            {
                head[i] = head[i + 1] = head[i + 2] = (byte)j;
                head[i + 3] = 0;
                j++;
            }
            
            head.CopyTo(output, 0);
            Array.Copy(input, 0, output, 1078, imageX * imageY);
            return 1;
        }

        /// <summary>
        /// Applies vertical flip to fingerprint image data - mirrors the flip logic from Form1.cs
        /// </summary>
        public static void FlipImageVertically(byte[] imageData, int width, int height)
        {
            for (int y = 0; y < height / 2; y++)
            {
                int swapY = height - y - 1;
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int swapIndex = swapY * width + x;
                    byte temp = imageData[index];
                    imageData[index] = imageData[swapIndex];
                    imageData[swapIndex] = temp;
                }
            }
        }

        /// <summary>
        /// Creates BMP header for roll fingerprint - mirrors WriteHeadRoll from original FingerDll.cs
        /// </summary>
        public static int WriteHeadRoll(byte[] output, byte[] input, int nWidth, int nHeight)
        {
            return WriteHead(output, input, nWidth, nHeight); // Same logic as WriteHead
        }

        /// <summary>
        /// Apply image enhancement like Fourfinger_Test - improves image quality before processing
        /// </summary>
        public static void ApplyImageEnhancement(byte[] imageData, int width, int height)
        {
            try
            {
                ImageNormalOfImage(imageData, width, height);
                ImageWeightFilter(imageData, width, height);
                NewImageDelFog(imageData, width, height);
            }
            catch (Exception)
            {
                // If enhancement fails, continue without it - not critical for basic functionality
            }
        }
    }
}
