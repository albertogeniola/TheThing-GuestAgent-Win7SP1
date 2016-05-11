using InstallerAnalyzer1_Guest.UIAnalysis.RankingPolicy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    public interface IUIRanker
    {
        /// <summary>
        /// Given a Window, this method scans the window
        /// and organizes the resultset so that best items are top-ranked, according
        /// to the policy specified.
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        CandidateSet Rank(IRankingPolicy policy, Window w);

        bool WaitReaction(Window curWindow, CandidateSet previousRes, int REACTION_TIMEOUT);
    }
}
