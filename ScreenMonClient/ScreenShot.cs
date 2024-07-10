using System.Runtime.InteropServices;
using Size = System.Drawing.Size;

namespace ScreenMonClient
{
    internal class ScreenShot
    {
        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        enum DeviceCap
        {
            HORZRES = 8,
            VERTRES = 10,
        }
        public static Bitmap CaptureScreen()
        {
            var size = GetScreenSize();

            // 创建一个Bitmap对象
            Bitmap bitmap = new Bitmap(size.Width, size.Height);
            // 创建Graphics对象
            using Graphics graphics = Graphics.FromImage(bitmap);
            // 将屏幕复制到Bitmap对象
            graphics.CopyFromScreen(0, 0, 0, 0, size);
            // 保存截图到文件
            // bitmap.Save("screenshot.png", System.Drawing.Imaging.ImageFormat.Png);
            return bitmap;
        }

        private static Size GetScreenSize()
        {
            var g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            var physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            var physicalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.HORZRES);

            return new Size(physicalScreenWidth, physicalScreenHeight);
        }
    }
}
