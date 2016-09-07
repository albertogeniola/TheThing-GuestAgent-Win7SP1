using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using AForge.Imaging;
using AForge;
using AForge.Math.Geometry;
using Tesseract;

namespace UiAnalysisTest
{
    public partial class Form1 : Form
    {
        int minHeight = 14;
        int minWidth = 14;
        string _fname;
        
        public Form1(string fname)
        {
            InitializeComponent();
            _fname = fname;
            process_image(not_inverted, false);
            process_image(inverted, true);
        }

        private Pen _marker = new Pen(Color.Red,3);
        private Brush _b = new SolidBrush(Color.Red);
        private Font _f = new Font(FontFamily.GenericSerif, 10);


        private void process_image(PictureBox box, bool toinvert) {

            Bitmap original = (Bitmap)Bitmap.FromFile(_fname);
            //original.Save("C:\\users\\alberto geniola\\desktop\\dbg\\original_"+toinvert+".bmp");

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
                if (toinvert)
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

                        for (int i = 0, n = tmparr.Length; i < n; i++)
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

                                using (var g = Graphics.FromImage(tmp))
                                {
                                    g.DrawRectangle(_marker, r);
                                }
                            }
                        }

                        //tmp.Save("C:\\users\\alberto geniola\\desktop\\dbg\\t1_" + toinvert + ".bmp");

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

                        for (int i = 0, n = tmparr.Length; i < n; i++)
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

                                using (var g = Graphics.FromImage(tmp))
                                {
                                    g.DrawRectangle(_marker, r);
                                }
                            }
                        }

                        //tmp.Save("C:\\users\\alberto geniola\\desktop\\dbg\\t1_" + toinvert + ".bmp");
                    }
                }
            }
            
              
            Bitmap test = (Bitmap)original.Clone();

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

                    using (var g = Graphics.FromImage(test))
                    {
                        g.DrawRectangle(_marker, r);
                    }

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
                                if (toinvert)
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
                                if (toinvert)
                                    new Invert().ApplyInPlace(tmp);
                                new Threshold(64).ApplyInPlace(tmp);
                                txt = scanButton(tmp);
                            }
                        }
                        
                        if (!String.IsNullOrEmpty(txt))
                        {
                            using (var g = Graphics.FromImage(test))
                            {
                                g.FillRectangle(new SolidBrush(Color.White), r);
                                g.DrawString(txt, _f, _b, r.X, r.Y);
                            }
                        }
                            
                        /*
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
                        */
                    }
                }

                box.Image = test;
                
            }
        }

        TesseractEngine _engine = new TesseractEngine("tessdata", "eng", EngineMode.TesseractAndCube);
        private string scanButton(Bitmap b)
        {
            // Copy constructor to avoid memory problems
            using (Bitmap tmp = new Bitmap(b))
                lock (_engine)
                {
                    try
                    {
                        using (var page = _engine.Process(tmp,PageSegMode.SingleBlock))
                        {
                            return page.GetText();
                        }
                    }
                    catch (Exception e)
                    {
                        return "";
                    }
                }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Dispose();
        }
    }
}
