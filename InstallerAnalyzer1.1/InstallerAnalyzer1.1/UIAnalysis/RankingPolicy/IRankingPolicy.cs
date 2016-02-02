using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace InstallerAnalyzer1_Guest.UIAnalysis.RankingPolicy
{
    public interface IRankingPolicy
    {
        /// <summary>
        /// Given an UIElement, calculates the score it should get.
        /// This score can be used by Rankers to decide which items 
        /// should be used for interaction.
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        int RankElement(UIControlCandidate w);
    }
}
