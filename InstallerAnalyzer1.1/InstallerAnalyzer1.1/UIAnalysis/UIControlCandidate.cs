using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public class UIControlCandidate
    {
        public UIControlCandidate() {}

        public override String ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("Type: ");
            if (ControlType != null)
            {
                sb.Append(ControlType);
            }
            else {
                sb.Append("Unknown");
            }

            if (Text!=null) {
                sb.Append("Text ");
                sb.Append(Text);
            }

            return sb.ToString();
        }

        public void Interact()
        {
            // For debugging purposes we enlight the control
            // choosen so we can easily spot it.
            //DBGMark();

            // If the element we have is from UIAutomation, simply interact with it.
            object objPattern;
            
            if (AutoElementRef != null && AutoElementRef.Current.ControlType == System.Windows.Automation.ControlType.Button && AutoElementRef.TryGetCurrentPattern(InvokePattern.Pattern, out objPattern)){
                // For buttons
                InvokePattern invPattern = objPattern as InvokePattern;
                invPattern.Invoke();
            }
            else if (AutoElementRef != null && AutoElementRef.Current.ControlType == System.Windows.Automation.ControlType.RadioButton && AutoElementRef.TryGetCurrentPattern(SelectionItemPatternIdentifiers.Pattern, out objPattern))
            {
                // Radios
                SelectionItemPattern invPattern = objPattern as SelectionItemPattern;
                invPattern.Select();
            }
            else if (AutoElementRef != null && AutoElementRef.Current.ControlType == System.Windows.Automation.ControlType.CheckBox && AutoElementRef.TryGetCurrentPattern(TogglePatternIdentifiers.Pattern, out objPattern))
            {
                // Checkboxes
                TogglePattern invPattern = objPattern as TogglePattern;
                invPattern.Toggle();
            }
            else
            {
                // Let's assume a very simple thing: interaction = mouseclick. We click in the middle of the control.
                // This won't work for combobox, but will work for mostly any Checkbox, radio button, hyperlink and custom buttons.
                // Calculate the center of control
                int x = PositionScreenRelative.Width / 2 + PositionScreenRelative.X;
                int y = PositionScreenRelative.Height / 2 + PositionScreenRelative.Y;

                System.Windows.Forms.Cursor.Position = new Point(x, y);
                LeftClick();
            }
            
        }

        public Rectangle PositionWindowRelative { get; set; }

        public Rectangle PositionScreenRelative { get; set; }

        public string Text { get; set; }

        public bool? IsEnabled { get; set; }

        public bool? HasFocus { get; set; }

        public AutomationElement AutoElementRef { get; set; }

        public int Score { get; set; }
        public System.Windows.Automation.ControlType ControlType { get; set; }

        private static void LeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
        }

        private void DBGMark() {
            IntPtr desktopPtr = GetDC(IntPtr.Zero);
            
            using(Graphics g = Graphics.FromHdc(desktopPtr))
                using (Pen p = new Pen(Color.Red,10))
                    g.DrawRectangle(p, PositionScreenRelative);

            ReleaseDC(IntPtr.Zero, desktopPtr);
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);

    }
}
