using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace InstallerAnalyzer1_Host
{
    public class TodoJob
    {
        private static readonly string[] CONTROL_SEP = new String[] { NetworkProtocol.Protocol.URI_CONTROL_SEP };
        private static readonly string[] PATH_SEP = new String[] {NetworkProtocol.Protocol.URI_PATH_SEP};
        private static readonly string[] START_PATH_SEP = new String[] {NetworkProtocol.Protocol.URI_APP_START};

        private string _actionPath;
        private int _id;
        private string[] _actions;
        private int currentAction=0;

        public struct Action {
            public string controlId;
            public int interactionType;
            public string relativePath;
        }

        public TodoJob(int id, string actionPath)
        { 
            _actionPath = actionPath;
            _id = id;
            _actions = actionPath.Split(START_PATH_SEP,StringSplitOptions.RemoveEmptyEntries);
            if (_actions.Length > 0)
            {
                _actions = _actions[0].Split(PATH_SEP, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public bool HasNextAction()
        {
            return currentAction < _actions.Length;
        }
        
        public Action NextAction()
        {
            Action res = new Action();
            string current = _actions[currentAction];
            res.relativePath = current;
            currentAction++;

            res.controlId = current.Split('=')[0];
            res.interactionType = int.Parse(current.Split('=')[1]);

            return res;

        }
        
        public int Id
        {
            get { return _id; }
        }
        public string ActionPath
        {
            get { return _actionPath; }
        }

        public void Skip(string followedPath)
        {
            TodoJob tmp = new TodoJob(-1, followedPath);
            while (tmp.HasNextAction())
            {
                Action cTmp = tmp.NextAction();
                Action mTmp = NextAction();

                if (!cTmp.relativePath.Equals(mTmp.relativePath))
                {
                    throw new ArgumentException("Paths are different!\n "+_actionPath+" - " + tmp._actionPath);
                }
            }
            
        }
    }
}
