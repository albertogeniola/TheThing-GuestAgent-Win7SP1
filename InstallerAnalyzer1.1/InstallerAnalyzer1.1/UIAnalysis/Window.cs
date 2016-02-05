using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public class Window
    {
        #region Private object-fields
        private IntPtr _handle;
        private string _className;
        private string _title;
        private Rectangle _pos;
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
            RECT pos = new RECT();
            GetWindowRect(hWnd, ref pos);
            _pos = new Rectangle(new Point(pos.Left, pos.Top), new Size(pos.Right - pos.Left, pos.Bottom - pos.Top));
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
            GetClassName(handle, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetWindowName(IntPtr hwnd)
        {
            int length = GetWindowTextLength(hwnd);
            StringBuilder b = new StringBuilder(length + 1);
            GetWindowText(hwnd, b, b.Capacity);
            return b.ToString();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        public void Close()
        {
            PostMessage(_handle, 0x0010,0,0);
        }

        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        public System.Windows.Rect GetBounds()
        {
            RECT r = new RECT();
            if (GetWindowRect(_handle, ref r))
            {
                return new System.Windows.Rect(r.Left, r.Top, Math.Abs(r.Right - r.Left), Math.Abs(r.Top - r.Bottom));
            }
            else
                throw new ArgumentException("Error during bound calculation.");
            
        }

        public Bitmap GetWindowsScreenshot()
        {
            // Get bounds of the current window
            RECT bounds = new RECT();
            bool res = GetWindowRect(_handle, ref bounds);
            if (res)
            {
                int width = Math.Abs(bounds.Right - bounds.Left);
                int height = Math.Abs(bounds.Bottom - bounds.Top);

                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point(bounds.Left, bounds.Top), new Point(0, 0), new Size(width, height));
                    g.Flush();
                }
                return bitmap;
            }
            throw new Exception("Cannot get Window Bitmap");
        }
    }

}
