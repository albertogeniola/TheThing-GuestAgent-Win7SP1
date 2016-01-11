using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace InstallerAnalyzer1_Guest.Controls
{
    public class InteractiveControl : Control
    {

        private AutomationElement _ae;

        public InteractiveControl(AutomationElement ae)
            : base(new IntPtr(ae.Current.NativeWindowHandle), ae.Current.ClassName, ae.Current.AutomationId, ae.Current.Name)
        {
            _ae = ae;
        }

        /// <summary>
        /// /// Does specific interaction with the control. 
        /// </summary>
        /// <param name="parameter">Particular parameter to pass to the control for interacting.</param>
        public void Interact(int interactionId)
        {
            object ap = _ae.GetCurrentPattern(AutomationPattern.LookupById(interactionId));

            if (ap is SelectionItemPattern)
            {
                SelectionItemPattern p = (((SelectionItemPattern)ap));
                // If is a multiselection, first try to add
                try
                {
                    if (p.Current.IsSelected)
                        p.RemoveFromSelection();
                    else if (!p.Current.IsSelected)
                        p.AddToSelection();
                }
                catch (Exception e)
                {
                    p.Select();
                    
                }
            }
            else if (ap is TogglePattern)
            {
                ((TogglePattern)ap).Toggle();
            }
            else if (ap is InvokePattern)
            {
                ((InvokePattern)ap).Invoke();
            }

            
        }

        public System.Windows.Rect GetBounds()
        {
            System.Windows.Rect boundingRect = (System.Windows.Rect)_ae.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            return boundingRect;
        }

        public bool IsFocused()
        {
            return (bool)_ae.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty);
        }
        
        public AutomationPattern[] GetPossibleInteractions()
        {
            return _ae.GetSupportedPatterns();
        }
    }
}
