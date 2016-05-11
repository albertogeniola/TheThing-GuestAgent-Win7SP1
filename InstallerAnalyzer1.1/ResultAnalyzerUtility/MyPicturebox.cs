using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InstallerAnalyzer1_Guest.UIAnalysis;
using System.Windows.Automation;

namespace ResultAnalyzerUtility
{
    public partial class MyPicturebox : PictureBox
    {
        private int cursor_x = 0;
        private int cursor_y = 0;
        private Brush _brush;
        private Pen _pen, _bestPen,_infoBrushPen;
        private ScreenInfo screenData;
        private Pen _polygobnPen, _separatorPen;
        private UIControlCandidate _best;
        private Brush _infoBrush, _infoBrushText;
        private Font _infoFont;

        public MyPicturebox()
        {
            _brush = new SolidBrush(Color.DarkGray);
            _pen = new Pen(_brush, 1);
            _polygobnPen = new Pen(Color.Blue, 1F);
            _bestPen = new Pen(Color.Red, 3);
            _infoBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
            _infoBrushText = new SolidBrush(Color.White);
            _infoBrushPen = new Pen(Color.White,1);
            _separatorPen = new Pen(Color.LightGray, 0.5F);
            _infoFont = new Font(FontFamily.GenericSansSerif, 9);
            InitializeComponent();
            MouseMove += mouse_hover;
        }

        private void mouse_hover(object sender, MouseEventArgs e)
        {
            cursor_x = e.X;
            cursor_y = e.Y;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // This will paint the original image
            base.OnPaint(e);

            // Let's add cursor loc
            // VerticalLine
            e.Graphics.DrawLine(_pen, cursor_x, 0, cursor_x, this.Height);
            // Horizontal
            e.Graphics.DrawLine(_pen, 0, cursor_y, this.Width, cursor_y);

            if (screenData.cs != null)
            {
                List<UIControlCandidate> hovered = new List<UIControlCandidate>();

                // Display shapes
                foreach (UIControlCandidate i in this.screenData.cs)
                {
                    Pen p = i == _best ? _bestPen : _polygobnPen;

                    // If the cursor is inside, highlight the button
                    p.Width = i.PositionWindowRelative.Contains(cursor_x, cursor_y) ? 3 : 1;

                    // Draw enclosing rectangle
                    e.Graphics.DrawRectangle(p, i.PositionWindowRelative);
                    
                    // If we are currently hovering an item, display related data
                    if (i.PositionWindowRelative.Contains(cursor_x, cursor_y))
                        hovered.Add(i);

                    /*
                    // Check if the best item was Visual Recognition item. If so, draw an X where the click event was fired
                    if (i == _best && i.ControlTypeId == -1) {
                        int x = i.PositionScreenRelative.Width / 2 + i.PositionScreenRelative.X;
                        int y = i.PositionScreenRelative.Height / 2 + i.PositionScreenRelative.Y;
                    }*/
                }

                int _max_width = this.Width/2;
                int margin = 5;
                int box_width = 0;
                int box_height = 2 * margin+hovered.Count*margin;

                // Calculate maximums
                foreach (var item in hovered)
                {
                    string data = GetCandidateInfo(item);
                    // Measure the string
                    SizeF size = e.Graphics.MeasureString(data, _infoFont, _max_width);

                    if (size.Width > box_width)
                        box_width = (int)size.Width+2*margin;

                    box_height += (int)size.Height;
                }
                
                int startx=0, starty=0;
                if (cursor_x > (Width / 2))
                    startx = cursor_x - box_width;
                else
                    startx = cursor_x;

                if (cursor_y > (Height / 2))
                    starty = cursor_y - box_height;
                else
                    starty = cursor_y;
                
                if (hovered.Count > 0)
                {
                    Rectangle infoRect = new Rectangle(startx, starty, box_width, box_height);
                    // Draw info alongside the cursor
                    e.Graphics.FillRectangle(_infoBrush, infoRect);
                    e.Graphics.DrawRectangle(_infoBrushPen, infoRect);

                    // Margin
                    startx += margin;
                    starty += margin;

                    float text_y = starty;
                    
                    // Write info for each item
                    foreach (var item in hovered) {
                        string data = GetCandidateInfo(item);
                        // Measure the string
                        SizeF size = e.Graphics.MeasureString(data, _infoFont,box_width);
                        e.Graphics.DrawString(data, _infoFont, _infoBrushText, startx, text_y);
                        text_y += size.Height+margin;
                        if (item != hovered.Last())
                        {
                            e.Graphics.DrawLine(_separatorPen, startx - margin, text_y, startx + box_width - margin, text_y);
                            text_y += margin;
                        }
                    }
                }
            }
        }

        public string GetCandidateInfo(UIControlCandidate item) {
            return string.Format("Score: {0}\nUIAutomation: {1}\nType: {2}", item.Score, (item.ControlTypeId != -1), item.ControlTypeId != -1 ? item.GuessedControlType.ProgrammaticName : "Recognized by Visual Filter");
        }

        public void SetScreenData(ScreenInfo item)
        {
            this.screenData = item;
            this.Image = Image.FromFile(item.clean_image);
            _best = findBestItem(item.cs);
        }

        private UIControlCandidate findBestItem(CandidateSet cs)
        {
            UIControlCandidate tmp = null;
            foreach (var i in cs) {
                if (tmp == null) {
                    tmp = i;
                    continue;
                }
                
                if (i.Score > tmp.Score) {
                    tmp = i;
                }
            }

            return tmp;
        }
    }
}
