using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Drawing;

namespace InstallerAnalyzer1_Guest
{
    public class Window
    {
        #region Private object-fields
        private IntPtr _handle;
        private string _className;
        private string _title;
        private AutomationElementCollection _elements;
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
            _pos = new Rectangle();
            GetWindowRect(_handle.ToInt32(), ref _pos);

            _pos.Width = _pos.Width - _pos.X;
            _pos.Height = _pos.Height - _pos.Y;
            
            // Get all the elements in the window
            _elements = AutomationElement.FromHandle(_handle).FindAll(TreeScope.Descendants, Condition.TrueCondition);
        }

        public Rectangle WindowLocation
        {
            get { return _pos; }
        }

        public int ControlCount { get { return _elements.Count; } }

        public AutomationElementCollection Elements {
            get { return _elements; }
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            // In dept equal
            if (obj == null)
                return false;
            Window other = (Window)obj;
            return DeppEquals(other._elements, this._elements) && other.Title == this.Title && other.Handle == this.Handle && other.WindowLocation == this.WindowLocation;

        }

        private bool DeppEquals(AutomationElementCollection automationElementCollection1, AutomationElementCollection automationElementCollection2)
        {
            foreach (AutomationElement a1 in automationElementCollection1) {
                bool found = false;
                foreach (AutomationElement a2 in automationElementCollection2)
                {
                    if (a2 == a1)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
                
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

        /*
        public byte[] GetWindowsScreenshot()
        {
            // Get bounds of the current window
            RECT bounds;
            bool res = GetWindowRect(_handle, out bounds);
            if (res)
            {
                int width = Math.Abs(bounds.Right-bounds.Left);
                int height = Math.Abs(bounds.Bottom-bounds.Top);

                using (Bitmap bitmap = new Bitmap(width, height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(new Point(bounds.Left,bounds.Top), new Point(0,0), new Size(width,height));
                        g.Flush();
                    }
                    ImageConverter converter = new ImageConverter();
                    return (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
                }
            }
            else
                return null;
        }
         * */

        [DllImport("user32.dll")]
        public static extern long GetWindowRect(int hWnd, ref Rectangle lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public System.Windows.Rect GetBounds()
        {
            RECT r;
            if (GetWindowRect(_handle, out r))
            {
                return new System.Windows.Rect(r.Left, r.Top, Math.Abs(r.Right - r.Left), Math.Abs(r.Top - r.Bottom));
            }
            else
                throw new ArgumentException("Error during bound calculation.");
            
        }
    }

}
