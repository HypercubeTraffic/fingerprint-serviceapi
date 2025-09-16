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
        // ZAZ_FpStdLib.dll functions
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_OpenDevice();
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern void ZAZ_FpStdLib_CloseDevice(int device);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CreateISOTemplate(int device, byte[] image, byte[] itemplate);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CreateANSITemplate(int device, byte[] image, byte[] itemplate);
        
        [DllImport("ZAZ_FpStdLib.dll")]
        public static extern int ZAZ_FpStdLib_CompareTemplates(int device, byte[] sTemplate, byte[] fTemplate);

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
            public IntPtr pOutBuf;  // Changed from byte[] to IntPtr for proper P/Invoke
        }

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
    }
}
