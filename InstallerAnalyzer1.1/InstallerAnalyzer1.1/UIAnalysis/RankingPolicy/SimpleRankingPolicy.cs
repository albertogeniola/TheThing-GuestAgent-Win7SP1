using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

namespace InstallerAnalyzer1_Guest.UIAnalysis.RankingPolicy
{
    public class SimpleRankingPolicy:IRankingPolicy
    {
        private readonly char[] TEXT_SEPARATORS = new char[]{ ' ', '\t', '\n', '\r' };
        // Heavly Penalize disabled items
        private const int CONTROL_TYPE_DISABLED_SCORE = -1000;
        
        // Give some bonus to the focused item
        private const int CONTROL_TYPE_FCUSED_SCORE = 30;
        
        // High precedence to items with text exactly matching one of the whitelisted words
        private const int WORD_WHITE_EXACT_SCORE = 280;
        private const int WORD_BLACK_EXACT_SCORE = 290;

        // Prefer buttons!
        private const int CONTROL_TYPE_BUTTON_SCORE = 50;
        
        // Penalize buttons with no text
        private const int CONTROL_NO_TEXT_SCORE = -30;

        // Some points are earned even if the items does not match exactly our word list but a combination of them
        private const int WORD_WHITE_CONTAINED_SCORE = 25;
        private const int WORD_BLACK_CONTAINED_SCORE = 30;

        // Also consider checkboxes. If unchecked they are relevant!
        private const int CONTROL_TYPE_CHECKBOX_SCORE = 15;
        private const int CONTROL_TYPE_CHECKBOX_UNCHECKED_SCORE = 50;

        // Do not forget about radios
        private const int CONTROL_TYPE_RADIO_SCORE = 15;

        // Give some points considering the position of the element
        private const int POSITION_AXSIS_SCORE = 10;
        private const int CONTROL_TYPE_HYPERLINK_SCORE = 10;
        
        // Penalize already checked cb
        private const int CONTROL_TYPE_CHECKBOX_CHECKED_PENALTY = 100;

        // MAKE THEM LOWER CASE!
        private static string[] WHITE_LIST = new string[] { "next", "continue", "agree", "accept", "ok", "install", "finish", "run", "done", "yes", "i agree", "i accept", "accept and install", "next >" };
        private static string[] BLACK_LIST = new string[] { "forward by small amount", "back by small amount", "back by large amount", "forward by large amount", "disagree", "cancel", "abort", "exit", "back", "<", "decline", "quit", "minimize", "no", "close", "pause", "x", "_", "do not accept", "< back" };
        

        public int RankElement(UIControlCandidate control)
        {
            int score = 0;
            score += RankEnabled(control);
            score += RankText(control);
            score += RankPosition(control);
            score += RankControlType(control);
            score += RankFocus(control);
            return score;
        }

        private int RankText(UIControlCandidate control)
        {
            if (string.IsNullOrEmpty(control.Text) || string.IsNullOrEmpty(control.Text.Trim()))
                return CONTROL_NO_TEXT_SCORE;
            int score = 0;

            string text = control.Text.ToLower().Trim();
            string[] words = text.Split(TEXT_SEPARATORS);

            // Assign score according to the element text
            foreach (string s in WHITE_LIST)
            {
                if (text.CompareTo(s) == 0)
                {
                    score += WORD_WHITE_EXACT_SCORE;
                    return score;
                }

                if (words.Contains(s))
                    score += WORD_WHITE_CONTAINED_SCORE;
            }

            
            foreach (string s in BLACK_LIST)
            {
                if (text.CompareTo(s) == 0)
                {
                    score -= WORD_BLACK_EXACT_SCORE;
                    return score;
                }

                if (words.Contains(s.ToLower()))
                    score -= WORD_BLACK_CONTAINED_SCORE;

            }
            

            return score;
        }

        private int RankPosition(UIControlCandidate control) {
            if (control.PositionScreenRelative == null)
                return 0;

            var b = control.PositionScreenRelative;
            Rectangle resolution = Screen.PrimaryScreen.Bounds;

            double posscore = (int)(((b.X - resolution.X) / (double)resolution.Width + (b.Y - resolution.Y) / (double)resolution.Height) * POSITION_AXSIS_SCORE);

            return (int)posscore;
        }

        private int RankControlType(UIControlCandidate control) {
            if (control.GuessedControlType == null)
                return 0;

            if (control.GuessedControlType == ControlType.Button)
                return CONTROL_TYPE_BUTTON_SCORE;
            else if (control.GuessedControlType == ControlType.CheckBox)
            {
                int bonus = 0;
                // Assign a bonus if the checkbox is unchecked
                object objPattern;
                if (control.AutoElementRef.TryGetCurrentPattern(TogglePatternIdentifiers.Pattern, out objPattern)) {
                    if ((objPattern as TogglePattern).Current.ToggleState == ToggleState.Off)
                    {
                        bonus += CONTROL_TYPE_CHECKBOX_UNCHECKED_SCORE;
                    }
                    else {
                        bonus -= CONTROL_TYPE_CHECKBOX_CHECKED_PENALTY;
                    }
                }
                return CONTROL_TYPE_CHECKBOX_SCORE + bonus;
            }
            else if (control.GuessedControlType == ControlType.RadioButton) {
                return CONTROL_TYPE_RADIO_SCORE;
            }
            else if (control.GuessedControlType == ControlType.Hyperlink)
                return CONTROL_TYPE_HYPERLINK_SCORE;
            else
                return 0;
        }

        private int RankFocus(UIControlCandidate control) {
            if (control.HasFocus == null)
                return 0;

            if (control.HasFocus==true)
                return CONTROL_TYPE_FCUSED_SCORE;
            return 0;
        }

        private int RankEnabled(UIControlCandidate control)
        {
            // If we truly know it is enabled, asign a positive bonus
            if (control.IsEnabled==false)
                return CONTROL_TYPE_DISABLED_SCORE;

            return 0;
        }
    }
}
