using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace InstallerAnalyzer1_Guest
{
    class BasicInteractionPolicy : InteractionPolicy
    {
        private AutomationElement _best;

        public void Interact(Window w)
        {
            var rect = w.WindowLocation;

            AutomationElement winner = null;
            int prevScore = -1;
            foreach (AutomationElement ae in w.Elements)
            {
                // For each element assign a score and select the item with maximum score
                try
                {
                    AutomationElement.AutomationElementInformation info = ae.Current;
                    // Skip handleless buttons: the close button, title bar button, and so on won't be added
                    if (ae.Current.NativeWindowHandle == 0)
                        continue;

                    if (!ae.Current.IsEnabled)
                        continue;

                    int score = 0;

                    // Assign score according to the element type
                    if (info.ControlType == ControlType.Button)
                    {
                        score += 10;
                    }
                    else if (info.ControlType == ControlType.Hyperlink)
                    {
                        score += 5;
                    }
                    else if (info.ControlType == ControlType.Image)
                    {
                        score += 2;
                    }
                    else if (info.ControlType == ControlType.Pane)
                    {
                        score += 1;
                    }
                    else if (info.ControlType == ControlType.Window)
                    {
                        score += 0;
                    }
                    else if (info.ControlType == ControlType.Custom)
                    {
                        score += 0;
                    }
                    else
                    {
                        continue;
                    }

                    // Assign score according to the element text
                    if (info.Name.ToLower().Contains("next"))
                    {
                        score += 10;
                    }

                    if (info.Name.ToLower().Contains("agree"))
                    {
                        score += 10;
                    }

                    if (info.Name.ToLower().Contains("accept"))
                    {
                        score += 10;
                    }

                    if (info.Name.ToLower().Contains("ok"))
                    {
                        score += 10;
                    }

                    if (info.Name.ToLower().Contains("continue"))
                    {
                        score += 10;
                    }

                    // Assign bonus score if the element has focus
                    if (info.HasKeyboardFocus)
                        score += 10;

                    // Assign score according to the element position: 0 to 10 relatevely to the window top-left corner
                    // i.e. maximum score if the element is in the bottom right corner
                    var r = info.BoundingRectangle;
                    double posscore = ((r.X - rect.X) / (double)rect.Width + (r.Y - rect.Y) / (double)rect.Height) * 6.0;
                    score += (int)posscore;

                    // Color? Accelerator key? TODO.

                    if (score > prevScore)
                    {
                        prevScore = score;
                        winner = ae;
                    }

                    string status;
                    status = "Score: " + score + info.AutomationId + "_" + info.Name;
                    Console.WriteLine(status);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "-" + e.StackTrace);
                    continue;
                }
            }

            // Time to interact with winner!
            _best = winner;

            if (_best == null)
            {
                // No valid interaction found....
                Console.WriteLine("Warning: no interactive controls found to interact with...");
                return;
            }
            else
            {
                InvokePattern p;
                var patterns = _best.GetSupportedPatterns();
                if (patterns == null || patterns.Length==0)
                {
                    Console.WriteLine("Warning: control does not have any interaction pattern available");
                    return;
                }

                p = _best.GetCurrentPattern(patterns[0]) as InvokePattern;
                p.Invoke();
            }
            
        }
    }
}
