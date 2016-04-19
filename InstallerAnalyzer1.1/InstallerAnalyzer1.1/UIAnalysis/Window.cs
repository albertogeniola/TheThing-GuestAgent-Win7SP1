using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public class Window
    {
        #region Private object-fields
        private IntPtr _handle;
        private string _className;
        private string _title;
        private InstallerAnalyzer1_Guest.NativeMethods.RECT _pos;
        #endregion

        /// <summary>
        /// Constructor: creates the object and scan all its components. It is an heavy operation.
        /// </summary>
        /// <param name="hWnd"></param>
        public Window(IntPtr hWnd)
        {
            // Load prop. which won't change
            _handle = hWnd;
            _className = GetClassName(_handle);
            _title = GetWindowName(_handle);
            _pos = new InstallerAnalyzer1_Guest.NativeMethods.RECT();
            NativeMethods.GetWindowRect(hWnd, out _pos);
            
            //_pos = new Rectangle(new Point(pos.Left, pos.Top), new Size(pos.Right - pos.Left, pos.Bottom - pos.Top));
        }

        public Rectangle WindowLocation
        {
            get { return _pos; }
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
        }

        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        public static string GetClassName(IntPtr handle)
        {
            StringBuilder sb = new StringBuilder(500);
            NativeMethods.GetClassName(handle, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetWindowName(IntPtr hwnd)
        {
            int length = NativeMethods.GetWindowTextLength(hwnd);
            StringBuilder b = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hwnd, b, b.Capacity);
            return b.ToString();
        }

        public void Close()
        {
            NativeMethods.PostMessage(_handle, 0x0010, 0, 0);
        }

        public Bitmap GetWindowsScreenshot()
        {
            try
            {
                InstallerAnalyzer1_Guest.NativeMethods.RECT rc;
                if (NativeMethods.GetClientRect(_handle, out rc))
                {

                    Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
                    Graphics gfxBmp = Graphics.FromImage(bmp);
                    IntPtr hdcBitmap = gfxBmp.GetHdc();

                    NativeMethods.PrintWindow(_handle, hdcBitmap, 0);

                    gfxBmp.ReleaseHdc(hdcBitmap);
                    gfxBmp.Dispose();

                    return bmp;
                }
                else {
                    return new Bitmap(0, 0);
                }
            }
            catch (Exception e) {
                return new Bitmap(0,0);
            }
            /*
            // Note that if the destination window is hang, this method will stuck. Before running, check if the window is responding
            InstallerAnalyzer1_Guest.NativeMethods.RECT bounds = new InstallerAnalyzer1_Guest.NativeMethods.RECT();
            bool res = NativeMethods.GetWindowRect(_handle, ref bounds);
            int width = Math.Abs(bounds.Right - bounds.Left);
            int height = Math.Abs(bounds.Bottom - bounds.Top);

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics memoryGraphics = Graphics.FromImage(bmp))
            {
                IntPtr dc = memoryGraphics.GetHdc();
                bool success = NativeMethods.PrintWindow(Handle, dc, 0);
                memoryGraphics.ReleaseHdc(dc);
            }
            // bmp now contains the screenshot
            return bmp;
             * */
        }
    }

}
