using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Winter
{
    class ArtworkColor
    {
        public static Bitmap getColor(string image)
        {
            Bitmap ret = null;
            using (var fs = new System.IO.FileStream(image, System.IO.FileMode.Open))
            {
                try
                {
                    var orig = new Bitmap(fs);
                    Bitmap bmp = new Bitmap(1, 1);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        // updated: the Interpolation mode needs to be set to 
                        // HighQualityBilinear or HighQualityBicubic or this method
                        // doesn't work at all.  With either setting, the results are
                        // slightly different from the averaging method.
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawRectangle(new Pen(new SolidBrush(Color.Black)), 0, 0, bmp.Width, bmp.Height);
                        g.DrawImage(orig, new Rectangle(0, 0, 1, 1));
                    }
                    Color pixel = bmp.GetPixel(0, 0);
                    ret = bmp;
                } catch(Exception e)
                {
                    Console.WriteLine(e);
                    ret = new Bitmap(1, 1);
                }
                
                return ret;
            }           
        }
    }
}
