using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using InstallerAnalyzer1_Guest.Controls;
using System.Drawing;

namespace InstallerAnalyzer1_Guest
{
    public class Window
    {
        /*
        #region Private Readonly Static Conditions
        private static readonly Condition enabledButtonCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsEnabledProperty, true),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)
                );
        private static readonly Condition enabledCheckboxeCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsEnabledProperty, true),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox)
                );
        private static readonly Condition enabledRadiobuttonCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsEnabledProperty, true),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.RadioButton)
                );
        #endregion
        */
        #region Private object-fields
        private IntPtr _handle;
        private string _className;
        private string _title;
        private List<InteractiveControl> _interactiveControls;
        private List<Control> _othersControls;
        private string _statusHash;
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
            _interactiveControls = new List<InteractiveControl>();
            _othersControls = new List<Control>();

            string ctrlHash = "";

            AutomationElementCollection elementCollection = AutomationElement.FromHandle(_handle).FindAll(TreeScope.Descendants, Condition.TrueCondition);
            foreach (AutomationElement ae in elementCollection)
            {
                AutomationElement.AutomationElementInformation info = ae.Current;
                // Skip handleless buttons: the close button, title bar button, and so on won't be added
                if (ae.Current.NativeWindowHandle == 0)
                    continue;

                string status = "";

                if (info.ControlType==ControlType.Button)
                {
                    status = info.AutomationId + "_" + info.Name + "_" + info.IsEnabled + "_" + info.BoundingRectangle ;

                    System.Windows.Point point;
                    bool clickable = ae.TryGetClickablePoint(out point);
                    
                    if (info.IsEnabled && clickable)
                    {
                        _interactiveControls.Add(new InteractiveControl(ae));
                    }
                    else
                    {
                        _othersControls.Add(new InteractiveControl(ae));
                    }
                }
                    /*
                else if (info.ControlType == ControlType.RadioButton) 
                {
                    
                    SelectionItemPattern p = (SelectionItemPattern)ae.GetCurrentPattern(SelectionItemPattern.Pattern);
                    status = info.AutomationId + "_" + info.Name + "_" + info.IsEnabled + "_" + info.BoundingRectangle+"_"+p.Current.IsSelected;

                    System.Windows.Point point;
                    bool clickable = ae.TryGetClickablePoint(out point);

                    if (clickable && info.IsEnabled && (!p.Current.IsSelected)) // I consider the RADIO BUTTON an  interactive control only if it is not selected yet
                    {
                        _interactiveControls.Add(new InteractiveControl(ae));
                    }
                    else
                    {
                        _othersControls.Add(new InteractiveControl(ae));
                    }
                }
                else if (info.ControlType == ControlType.CheckBox)
                {
                    
                    TogglePattern p = (TogglePattern)ae.GetCurrentPattern(TogglePattern.Pattern);
                    status = info.AutomationId + "_" + info.Name + "_" + info.IsEnabled + "_" + info.BoundingRectangle + "_" + p.Current.ToggleState;

                    System.Windows.Point point;
                    bool clickable = ae.TryGetClickablePoint(out point);
                    if (info.IsEnabled && clickable)
                    {
                        _interactiveControls.Add(new InteractiveControl(ae));
                    }
                    else
                    {
                        _othersControls.Add(new InteractiveControl(ae));
                    }
                }
                     */
                else
                {
                    status = info.AutomationId + "_" + info.Name + "_" + info.IsEnabled + "_" + info.BoundingRectangle;
                    _othersControls.Add(new UnknownControl(new IntPtr(info.NativeWindowHandle), info.ClassName, info.AutomationId, info.Name));
                }
                ctrlHash = ctrlHash + status + "|";
            }
            _statusHash = _handle +";"+ _title +";"+ _className +";"+ ctrlHash;

        }

        private static bool WindowControlsEquals(IEnumerable<Control> a, IEnumerable<Control> b)
        {
            foreach (Control ia in a)
            {
                bool found = false;
                foreach (Control ib in b)
                {
                    if (ib.Equals(ia))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode() ^ _className.GetHashCode() ^ _title.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is Window))
                return false;
            else
            {
                Window w = (Window)obj;
                return w._handle == _handle && w._className == _className && _title==w._title && WindowControlsEquals(_interactiveControls,w._interactiveControls) && WindowControlsEquals(w._othersControls,_othersControls);
            }

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

        public IEnumerable<InteractiveControl> InteractiveControls
        {
            get { return _interactiveControls; }
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


        public string StatusHash { get { return _statusHash; } }

        public void Close()
        {
            PostMessage(_handle, 0x0010,0,0);
        }

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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }


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
