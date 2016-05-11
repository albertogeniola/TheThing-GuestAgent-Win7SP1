using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    [Serializable()]
    public class CandidateSet:SortedSet<UIControlCandidate>
    {
        public CandidateSet() : base(new UIScoreComparer()) { }

        public UIControlCandidate PopTopCandidate() {
            if (this.Count < 1)
                throw new NoMoreCandidateException();

            var result = this.First();
            Remove(result);
            
            return result;
        }

        class UIScoreComparer : IComparer<UIControlCandidate> {
            public int Compare(UIControlCandidate x, UIControlCandidate y)
            {
                return y.Score - x.Score;
            }
        }

        public UIControlCandidate this[int index]
        {
            get
            {
                if (index >= this.Count)
                    throw new IndexOutOfRangeException();
                else
                    return this.ElementAt(index);
            }
        }

        public string Hash { get; set; }
        

    }
}
