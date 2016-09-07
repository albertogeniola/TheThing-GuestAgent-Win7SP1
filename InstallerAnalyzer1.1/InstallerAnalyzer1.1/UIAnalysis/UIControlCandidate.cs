using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public class UIControlCandidate
    {
        public UIControlCandidate() {
            // Trick: this enables UIAutomation so it gets initialized. 
            AutomationElement e = AutomationElement.RootElement;
        }
        
        public Rectangle PositionWindowRelative { get; set; }

        public Rectangle PositionScreenRelative { get; set; }

        public string Text { get; set; }

        public bool? IsEnabled { get; set; }

        public bool? HasFocus { get; set; }
        
        public int Score { get; set; }

        [XmlIgnore]
        public AutomationElement AutoElementRef { get; set; }

        [XmlIgnore]
        private ControlType _ctrlType;

        [XmlIgnore]
        public ControlType GuessedControlType {
            get {
                if (AutoElementRef != null)
                    return AutoElementRef.Current.ControlType;
                else
                    return _ctrlType;
            }

            set {
                _ctrlType = value;
            }
        }
        [XmlElement("ControlTypeId")]
        public int ControlTypeId
        {
            get
            {
                if (AutoElementRef != null)
                {
                    return GuessedControlType.Id;
                }
                else if (this._ctrlType != null)
                {
                    return this._ctrlType.Id;
                }
                else {
                    return -1;
                }
            }
            set
            {
                if (value == -1)
                    this.GuessedControlType = null;
                else
                {
                    _ctrlType = ControlType.LookupById(value);
                }
            }
        }

        public void Interact()
        {
            // For debugging purposes we enlight the control
            // choosen so we can easily spot it.
            //DBGMark();

            // If the element we have is from UIAutomation, simply interact with it.
            object objPattern;

            if (AutoElementRef != null && AutoElementRef.Current.ControlType == System.Windows.Automation.ControlType.Button && AutoElementRef.TryGetCurrentPattern(InvokePattern.Pattern, out objPattern))
            {
                System.Windows.Point pt = new System.Windows.Point();
                if (AutoElementRef.TryGetClickablePoint(out pt))
                {
                    System.Windows.Forms.Cursor.Position = new Point((int)pt.X+1, (int)pt.Y+1);
                    LeftClick();
                }
                else {
                    // NOTE! The following approach won't work for some strange UIs (probably bugged). We use the classic way of sendkeys to solve this problem once for all
                    // For buttons
                    InvokePattern invPattern = objPattern as InvokePattern;
                    invPattern.Invoke();
                }

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

        private static void LeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
            Thread.Sleep(1000);
            mouse_event(MOUSEEVENTF_LEFTUP, System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y, 0, 0);
            Thread.Sleep(1000);
        }

        public bool IsBest { get; set; }

        [XmlIgnore]
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        [XmlIgnore]
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Type: ");
            if (GuessedControlType != null)
            {
                sb.Append(GuessedControlType);
            }
            else
            {
                sb.Append("Unknown");
            }

            if (Text != null)
            {
                sb.Append("Text ");
                sb.Append(Text);
            }

            return sb.ToString();
        }
    }
}
