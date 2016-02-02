using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public class UIControlCandidate
    {
        private Rectangle _position;

        public UIControlCandidate()
        {
        }

        public void Interact()
        {
            throw new NotImplementedException();
        }

        public Rectangle BoundingPosition { get; set; }

        public string Text { get; set; }
    
        public  bool? IsEnabled { get; set; }

        public  bool? HasFocus { get; set; }

        public int Score {get;set;}
        public  System.Windows.Automation.ControlType ControlType { get; set; }}
}
