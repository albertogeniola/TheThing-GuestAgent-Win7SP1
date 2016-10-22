using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using InstallerAnalyzer1_Guest.UIAnalysis.RankingPolicy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using Tesseract;

namespace InstallerAnalyzer1_Guest.UIAnalysis
{
    //TODO: Improve these methods by using multithreading!
    //TODO: Check for loops
    public class NativeAndVisualRanker:IUIRanker, IDisposable
    {
        public const int POLLING_INTERVAL = 5000;
        private bool disposed = false;
        private TesseractEngine _engine;
        private AndCondition _cond;
        private int _min_width = 0;
        private int _min_height = 0;

        /// <summary>
        /// Constructor. Instantiates the OCR Engine. Must use Dispose when done to ensure memory save.
        /// </summary>
        public NativeAndVisualRanker(int min_width=30, int min_height=20) {
            _engine = new TesseractEngine("tessdata", "eng", EngineMode.TesseractAndCube);
            Condition bc = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
            Condition sb = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ProgressBar);
            Condition cc = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox);
            Condition rc = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.RadioButton);
            Condition hc = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Hyperlink);
            Condition enCond = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
            OrCondition orc = new OrCondition(bc, cc, rc, hc, sb);
            _cond = new AndCondition(orc, enCond);

            _min_width = min_width;
            _min_height = min_height;
        }


        /// <summary>
        /// Given a Window, this method will perform a full scan of the window using both UIAutomation and VisualRecognition
        /// of the UI-Window. This method may take some time to be executed.
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public CandidateSet Rank(IRankingPolicy policy, Window w)
        {
            CandidateSet res = new CandidateSet();

            try
            {
                // Scan all elements with UI Atuomation. Those are the "preferred ones"
                ScanWithUIAutomation(ref res, w, policy);

                using (Bitmap bitmap = w.GetWindowsScreenshot())
                {
                    if (bitmap == null)
                        return res;
                    // Scan with Visual recognition. 
                    ScanWithVisualRecognition(ref res, bitmap, false, _min_width, _min_height, w, policy);
                    ScanWithVisualRecognition(ref res, bitmap, true, _min_width, _min_height, w, policy);
                    
                    // Calculate an hash starting from the bitmap and save it to the candidate set.
                    res.Hash = CalculateHash(bitmap);
                }
            }
            catch (Exception e) { 
                // Many things may go wrong here. Assume this will fail.
                res.Clear();
                res.Hash = null;
            }

            return res;
            
        }

        //TODO //FIXME
        public static string CalculateHash(Bitmap original, string save_filtered_to=null)
        {
            // Hash calculation is very discriminating. If a pixel changes in color, the whole hash will be different.
            // For us this is too rigid: we might support some "noise" or "error", manly because of animation of buttons.
            // For this reason, it is a good idea to apply some filter to the image in order to flattern common pizels.
            // A way to do this is to lower the image quality
            // Apply some common filter and then calculate the hash.
            if (original == null)
                return null;

            using (Bitmap bmp = Grayscale.CommonAlgorithms.BT709.Apply(original))
            {
                // Apply the Threshold
                var threshold = new SISThreshold();
                threshold.ApplyInPlace(bmp);
                
                Erosion f = new Erosion();
                f.ApplyInPlace(bmp);

                ImageConverter converter = new ImageConverter();
                byte[] rawImageData = converter.ConvertTo(bmp, typeof(byte[])) as byte[];

                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] hash = md5.ComputeHash(rawImageData);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2"));
                }

                string strhash = sb.ToString();

                if (save_filtered_to != null)
                    bmp.Save(string.Format(save_filtered_to, strhash + "_filtered"));

                return strhash;
            }
        }

        public static string CalculateHash(Window w) {
            using (Bitmap bitmap = w.GetWindowsScreenshot())
            {
                if (bitmap == null) {
                    return null;
                }
                return CalculateHash(bitmap);
            }
        }

        public bool WaitReaction(Window curWindow, CandidateSet previousRes, int REACTION_TIMEOUT)
        {
            long waited = 0;
            int attempts = 0;
            try
            
            {
                string c_hash = CalculateHash(curWindow);
                while (c_hash!=null && c_hash.CompareTo(previousRes.Hash) == 0 && waited < REACTION_TIMEOUT)
                {
                    Thread.Sleep(POLLING_INTERVAL);
                    waited += POLLING_INTERVAL;
                    c_hash = CalculateHash(curWindow);
                    Console.WriteLine("Prev hash : " + previousRes.Hash + " vs New hash: " + c_hash);
                }

            }
            catch (Exception e) {
                //TODO: this is ugly and bad. Fixme when you have time.
                attempts++;
                if (attempts > 3)
                    throw e;
            }

            return waited < REACTION_TIMEOUT;
            
        }

        private void ScanWithUIAutomation(ref CandidateSet res, Window w, IRankingPolicy policy)
        {
            AutomationElementCollection elements = AutomationElement.FromHandle(w.Handle).FindAll(TreeScope.Descendants, _cond);
            foreach (var el in elements) {
                var ae = (el as AutomationElement).Current;
                var windowLoc = w.WindowLocation;
                if (double.IsInfinity(ae.BoundingRectangle.Top) || double.IsInfinity(ae.BoundingRectangle.Left) || double.IsInfinity(ae.BoundingRectangle.Bottom) || double.IsInfinity(ae.BoundingRectangle.Right))
                    continue;

                UIControlCandidate u = new UIControlCandidate();
                var r = ae.BoundingRectangle;
                u.PositionScreenRelative = new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
                u.PositionWindowRelative = new Rectangle((int)r.X - windowLoc.X, (int)r.Y - windowLoc.Y, (int)r.Width, (int)r.Height);
                u.Text = ae.Name;
                u.IsEnabled = ae.IsEnabled;
                u.AutoElementRef = el as AutomationElement;
                u.Score = policy.RankElement(u);
                res.Add(u);
            }
        }

        /// <summary>
        /// Given the Window UI Screenshot, tries to detect all the buttons in it and returns all of them.
        /// </summary>
        /// <param name="res">Where to put candidate controls</param>
        /// <param name="bitmap">Bitmap containing the pixels of the UI to analyze</param>
        /// <param name="shouldInvert">If true, the bitmap will be inverted before analysis</param>
        /// <param name="minWidth">Minimum width of patterns to recognize</param>
        /// <param name="minHeight">Minimum height of patterns to recognize</param>
        /// <returns>void</returns>
        private void ScanWithVisualRecognition(ref CandidateSet res, Bitmap original, bool shouldInvert, int minWidth, int minHeight,Window w, IRankingPolicy policy)
        {
            // Setup the Blob counter
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.BackgroundThreshold = Color.FromArgb(10, 10, 10);
            blobCounter.FilterBlobs = true;
            blobCounter.CoupledSizeFiltering = false;
            blobCounter.MinHeight = minHeight;
            blobCounter.MinWidth = minWidth;

            List<Blob> blobs = new List<Blob>();

            // Button scanning
            // Apply the grayscale. This is needed for AForge's filters
            using (Bitmap grey_scaled = Grayscale.CommonAlgorithms.BT709.Apply(original))
            {
                // Invert the image if requested
                if (shouldInvert)
                {
                    Invert invert = new Invert();
                    invert.ApplyInPlace(grey_scaled);
                }

                //grey_scaled.Save("C:\\users\\alberto geniola\\desktop\\dbg\\gray_scaled_" + toinvert + ".bmp");

                using (Bitmap t1 = new Threshold(64).Apply(grey_scaled))
                {
                    using (var tmp = new Bitmap(t1.Width, t1.Height))
                    {
                        using (Graphics g = Graphics.FromImage(tmp))
                        {
                            g.DrawImage(t1, 0, 0);
                        }

                        // The blob counter will analyze the bitmap looking for shapes
                        blobCounter.ProcessImage(tmp);
                        var tmparr = blobCounter.GetObjectsInformation();
                        blobs.AddRange(tmparr);
                    }
                }

                using (Bitmap t2 = new SISThreshold().Apply(grey_scaled))
                {
                    using (var tmp = new Bitmap(t2.Width, t2.Height))
                    {
                        using (Graphics g = Graphics.FromImage(tmp))
                        {
                            g.DrawImage(t2, 0, 0);
                        }
                        //tmp.Save("C:\\users\\alberto geniola\\desktop\\dbg\\t2_" + toinvert + ".bmp");
                        // The blob counter will analyze the bitmap looking for shapes
                        blobCounter.ProcessImage(tmp);
                        var tmparr = blobCounter.GetObjectsInformation();
                        blobs.AddRange(tmparr);
                    }
                }
            }
            
            // Let's analyze every single shape
            for (int i = 0, n = blobs.Count; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                if (edgePoints.Count > 1)
                {
                    IntPoint p0, p1;
                    PointsCloud.GetBoundingRectangle(edgePoints, out p0, out p1);
                    var r = new Rectangle(p0.X, p0.Y, p1.X - p0.X, p1.Y - p0.Y);

                    // Skip any shape representing the border of the whole window ( +10px padding)
                    if (r.Width >= (original.Width - 10))
                        continue;
                    
                    // This is most-likely a button!
                    // Crop the image and pass it to the OCR engine for text recognition
                    using (Bitmap button = new Bitmap(r.Width, r.Height))
                    {
                        // Scan the original shape
                        String txt = null;
                        using (var g1 = Graphics.FromImage(button))
                        {
                            g1.DrawImage(original, 0, 0, r, GraphicsUnit.Pixel);
                        }

                        // Process OCR on that image
                        txt = scanButton(button);

                        if (String.IsNullOrEmpty(txt))
                        {
                            using (Bitmap tmp = Grayscale.CommonAlgorithms.BT709.Apply(button))
                            {
                                if (shouldInvert)
                                    new Invert().ApplyInPlace(tmp);

                                new SISThreshold().ApplyInPlace(tmp);
                                txt = scanButton(tmp);
                            }
                        }

                        // If still nothing is found, repeat the analysis with the second version of the filter
                        if (String.IsNullOrEmpty(txt))
                        {
                            using (Bitmap tmp = Grayscale.CommonAlgorithms.BT709.Apply(button))
                            {
                                if (shouldInvert)
                                    new Invert().ApplyInPlace(tmp);
                                new Threshold(64).ApplyInPlace(tmp);
                                txt = scanButton(tmp);
                            }
                        }
                        
                        // At this point we should have a result. Add it to list if it does not overlap any UIAutomated element
                        UIControlCandidate t = new UIControlCandidate();
                        t.PositionWindowRelative = r;
                        var winLoc = w.WindowLocation;
                        t.PositionScreenRelative = new Rectangle(r.X + winLoc.X, r.Y + winLoc.Y, r.Width, r.Height);
                        t.Text = txt;
                        t.Score = policy.RankElement(t);
                                
                        // If the item falls into the same area of a UI element, ignore it.
                        bool overlaps = false;
                        foreach (var el in res)
                        {
                            if (el.AutoElementRef != null && el.PositionScreenRelative.IntersectsWith(t.PositionScreenRelative))
                            {
                                overlaps = true;
                                break;
                            }
                        }
                        if (!overlaps)
                            res.Add(t);
                    }
                }

            }
        }

        private string scanButton(Bitmap b)
        {
            // Copy constructor to avoid memory problems
            using(Bitmap tmp = new Bitmap(b))
                lock (_engine)
                {
                    try
                    {
                        using (var page = _engine.Process(tmp,PageSegMode.SingleLine))
                        {
                            string txt = page.GetText();

                            if (txt == null)
                                return "";
                            else
                                return txt.Trim().Trim('\n');
                            
                        }
                    }
                    catch (Exception e)
                    {
                        return "";
                    }
                }
        }
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    _engine.Dispose();
                
                disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~NativeAndVisualRanker()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }
    }
}
