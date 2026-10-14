using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace game_câu_cá
{
   
    public partial class Form1
    {
        const float FishSpriteScale = 3f;                         
        float[] predSpriteScale = { 1.8f, 1.7f, 1.6f, 1.0f };      
        string[] predSprite = { "piranha", "eel", "shark", "kraken" };

      
        Dictionary<string, string> speciesSprite = new Dictionary<string, string>
        {
          
            { "Minnow", "fish_02" },   { "Sardine", "fish_08" }, { "Guppy", "fish_17" },
            { "Carp", "fish_07" },     { "Perch", "fish_23" },   { "Goldfish", "fish1" },
            { "Bluegill", "fish_11" },
          
            { "Bass", "fish_04" },     { "Trout", "fish_03" },   { "Salmon", "salmon" },
            { "Mackerel", "fish_10" }, { "Pike", "fish4" },      { "Catfish", "mudfish" },
            { "Snapper", "fish_15" },
         
            { "Tuna", "fish_06" },     { "Mahi", "fish_25" },    { "Swordfish", "fish_26" },
            { "Marlin", "fish_27" },   { "Grouper", "fish_22" }, { "Barracuda", "fish_12" },
            { "Manta", "fish_30" },
         
            { "Anglerfish", "anglerfish" }, { "Viperfish", "fish_28" }, { "Fangtooth", "fish_18" },
            { "Gulper Eel", "fish_21" },    { "Hammerhead", "shark" },  { "Oarfish", "eel" },
            { "Legendary Fish", "solarfish" }
        };

        Dictionary<string, Image> spriteImg = new Dictionary<string, Image>();
        Dictionary<int, ImageAttributes> alphaAttrs = new Dictionary<int, ImageAttributes>();

        
        Dictionary<string, RectangleF> spriteContent = new Dictionary<string, RectangleF>();

       
        static bool SpriteLooksLeft(string key)
        {
            return key.StartsWith("fish_") || key == "piranha" || key == "eel";
        }

        void LoadSprites()
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "Images");
                if (!Directory.Exists(dir)) return;

                foreach (string f in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
                {
                    string key = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                    try
                    {
                        using (Image tmp = Image.FromFile(f))
                            spriteImg[key] = new Bitmap(tmp);  
                    }
                    catch { }
                }
            }
            catch { }
        }

        ImageAttributes AlphaAttr(int alpha)
        {
            ImageAttributes ia;
            if (!alphaAttrs.TryGetValue(alpha, out ia))
            {
                ColorMatrix m = new ColorMatrix();
                m.Matrix33 = alpha / 255f;
                ia = new ImageAttributes();
                ia.SetColorMatrix(m, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                alphaAttrs[alpha] = ia;
            }
            return ia;
        }

      
        RectangleF ContentFrac(string key, Image img)
        {
            RectangleF rf;
            if (spriteContent.TryGetValue(key, out rf)) return rf;

            rf = new RectangleF(0f, 0f, 1f, 1f);   

            try
            {
                Bitmap bmp = img as Bitmap;
                if (bmp != null)
                {
                    int w = bmp.Width, h = bmp.Height;
                    int minX = w, minY = h, maxX = -1, maxY = -1;

                    BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        int stride = Math.Abs(data.Stride);
                        byte[] buf = new byte[stride * h];
                        Marshal.Copy(data.Scan0, buf, 0, buf.Length);

                        for (int y = 0; y < h; y++)
                        {
                            int row = y * stride;
                            for (int x = 0; x < w; x++)
                            {
                                if (buf[row + x * 4 + 3] <= 24) continue;  
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }
                        }
                    }
                    finally { bmp.UnlockBits(data); }

                    if (maxX >= minX && maxY >= minY)
                        rf = new RectangleF(minX / (float)w, minY / (float)h,
                                            (maxX - minX + 1) / (float)w, (maxY - minY + 1) / (float)h);
                }
            }
            catch { }

            spriteContent[key] = rf;
            return rf;
        }

       
        bool DrawSprite(Graphics g, string key, float cx, float cy, float width, bool movingRight, int alpha)
        {
            Image img;
            if (key == null || !spriteImg.TryGetValue(key.ToLowerInvariant(), out img)) return false;
            if (img == null || img.Width <= 0 || img.Height <= 0 || width < 1f) return false;

            alpha = Math.Max(0, Math.Min(255, alpha));
            GraphicsState st = null;
            try
            {
                float height = Math.Max(1f, width * img.Height / img.Width);
                bool flip = SpriteLooksLeft(key.ToLowerInvariant()) ? movingRight : !movingRight;

                st = g.Save();
                g.InterpolationMode = width < img.Width ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                g.TranslateTransform(cx, cy);
                if (flip) g.ScaleTransform(-1, 1);

                Rectangle dest = new Rectangle((int)(-width / 2f), (int)(-height / 2f), (int)width, (int)height);
                if (alpha >= 255)
                    g.DrawImage(img, dest);
                else
                    g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, AlphaAttr(alpha));
                return true;
            }
            catch (Exception ex)
            {
                LogError("DrawSprite " + key, ex);
                return false;
            }
            finally
            {
                if (st != null) { try { g.Restore(st); } catch { } }
            }
        }

      
        bool DrawFishSprite(Graphics g, Fish f, int alpha)
        {
            string key;
            if (!speciesSprite.TryGetValue(f.Name, out key)) return false;

            Rectangle r = f.Bounds;
            if (!DrawSprite(g, key, r.X + r.Width / 2f, r.Y + r.Height / 2f,
                            f.Size * FishSpriteScale, f.FacingRight, alpha))
                return false;

            if (f.Bitten)   
                g.FillEllipse(GetBrush(Color.DarkRed), r.X + r.Width / 2 - 4, r.Y + r.Height / 2 - 2, 9, 6);

            if (alpha >= 255 && f.Name == legendary.Name)
                g.DrawEllipse(glowPen, r.X - 10, r.Y - 14, r.Width + 20, r.Height + 28);

            return true;
        }

    
        bool DrawPredatorSprite(Graphics g, Predator p)
        {
            return DrawSprite(g, predSprite[p.Zone], p.X + p.W / 2f, p.Y + p.H / 2f,
                              p.W * predSpriteScale[p.Zone], p.Speed > 0, 255);
        }
        bool PredatorSpriteRect(Predator p, out Rectangle rect)
        {
            rect = Rectangle.Empty;

            string key = predSprite[p.Zone].ToLowerInvariant();
            Image img;
            if (!spriteImg.TryGetValue(key, out img)) return false;

           
            float w = p.W * predSpriteScale[p.Zone];
            float h = w * img.Height / img.Width;
            float left = p.X + p.W / 2f - w / 2f;
            float top = p.Y + p.H / 2f - h / 2f;

            RectangleF c = ContentFrac(key, img);
            bool movingRight = p.Speed > 0;
            bool flip = SpriteLooksLeft(key) ? movingRight : !movingRight;
            float fx = flip ? 1f - (c.X + c.Width) : c.X;

            const float pad = 2f;   
            rect = new Rectangle((int)(left + fx * w - pad), (int)(top + c.Y * h - pad),
                                 (int)(c.Width * w + pad * 2f), (int)(c.Height * h + pad * 2f));
            return true;
        }
    }
}