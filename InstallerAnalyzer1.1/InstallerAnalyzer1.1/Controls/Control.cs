using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerAnalyzer1_Guest.Controls
{
    public abstract class Control
    {
        protected const int BST_UNCHECKED = 0;
        protected const int BST_CHECKED = 1;
        protected const int BM_GETCHECK = 240;
        protected const int BM_SETCHECK = 241;
        protected const int BM_GETSTATE = 242;                
        protected const int BM_SETSTATE = 243;
        protected const uint BM_CLICK = 0x00F5;

        private IntPtr _handle;
        private string _className;
        private string _id;
        private string _text;

        protected Control(IntPtr handle, string className, string id, string text)
        {
            _handle = handle;
            _id = id;
            _className = className;
            _text = text;
        }

        public string Id
        {
            get
            {
                return _id;

            }
        }

        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        public override string ToString()
        {
            return "Handle: " + _handle + "; ID: " + _id + " Class: " + ClassName + "; Text: " + Text;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Control))
                return false;

            Control c = (Control)obj;
            return (Handle.Equals(c.Handle) && ClassName.Equals(c.ClassName) && Text.Equals(c.Text) && _id == c.Id);

        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode() ^ ClassName.GetHashCode() ^ Text.GetHashCode() ^  _id.GetHashCode();
        }


        
    }
}
