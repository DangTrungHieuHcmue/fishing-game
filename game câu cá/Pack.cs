using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace game_câu_cá
{
    public partial class Form1
    {
        const int FisherFrame = 48;      
        const float FisherScale = 2f;    
        bool packReady = false;
        const int BoatFeetDrop = 36;      
        const int BoatClipAdjust = 0;

        bool FisherRowing
        {
            get { return state == State.Idle && onBoat && (leftPressed || rightPressed) && !shopOpen; }
        }

        float FisherBaseY { get { return DockTop + (FisherRowing ? BoatFeetDrop : 0); } }

        float BoatHullBottomY()
        {
            Image img = Pk("pack_boat");
            if (img == null) return WaterTop + BoatClipAdjust;
            return DockTop - 10 + img.Height * 2 + BoatClipAdjust;
        }

        const float FishFeetX = 22.4f;                                                   
        static readonly float[] hookFeetX = { 22.3f, 23.2f, 22.2f, 21.7f, 16.1f, 16.1f }; 

        const float FishTipX = 43f, FishTipY = 25f;
        static readonly float[] hookTipX = { 43f, 41f, 37f, 45f, 25f, 24f };
        static readonly float[] hookTipY = { 25f, 28f, 28f, 19f, 4f, 3f };

        Image Pk(string key)
        {
            Image img;
            return spriteImg.TryGetValue(key, out img) ? img : null;
        }

        void LoadPackExtras()
        {
            packReady = Pk("pack_fisherman_idle") != null && Pk("pack_fisherman_walk") != null
                     && Pk("pack_fisherman_fish") != null && Pk("pack_fisherman_hook") != null;

            Image hut = Pk("pack_hut");
            if (hut != null) gearImage = hut;    

            Image sell = Pk("pack_sell");
            if (sell != null) sellImage = sell;  
        }

        PointF PackRodTip()
        {
            float tx = FishTipX, ty = FishTipY, feet = FishFeetX;

            if (state == State.Throwing)
            {
                int fr = 3 + Math.Min(2, (int)(throwT * 3f));
                tx = hookTipX[fr]; ty = hookTipY[fr]; feet = hookFeetX[fr];
            }

            return new PointF(playerX + facing * (tx - feet) * FisherScale,
                   FisherBaseY - (47.5f - ty) * FisherScale);
        }

        void DrawFisherFrame(Graphics g, Image sheet, int fr, float feetX, bool flip)
        {
            GraphicsState st = g.Save();
            try
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                if (onBoat)
                {
                    float bottom = BoatHullBottomY();
                    g.SetClip(new RectangleF(-100000f, -100000f, 200000f + WorldW, 100000f + bottom));
                }

                if (flip)
                {
                    g.TranslateTransform(playerX, 0);
                    g.ScaleTransform(-1, 1);
                    g.TranslateTransform(-playerX, 0);
                }

                int size = (int)(FisherFrame * FisherScale);
                Rectangle dest = new Rectangle((int)Math.Round(playerX - feetX * FisherScale),
                                               (int)Math.Round(FisherBaseY - 47.5f * FisherScale), size, size);
                g.DrawImage(sheet, dest, fr * FisherFrame, 0, FisherFrame, FisherFrame, GraphicsUnit.Pixel);
            }
            catch (Exception ex) { LogError("DrawFisherFrame", ex); }
            finally { g.Restore(st); }
        }
        void DrawFisherman(Graphics g)
        {
            string sheetName;
            int fr;
            float feet;

            if (state == State.Charging)
            {
                sheetName = "fish"; fr = 0; feet = FishFeetX;
            }
            else if (state == State.Throwing)
            {
                sheetName = "hook"; fr = 3 + Math.Min(2, (int)(throwT * 3f)); feet = hookFeetX[fr];
            }
            else if (state == State.Sinking || state == State.Reeling)
            {
                sheetName = "fish"; fr = (frame / 10) % 4; feet = FishFeetX;
            }
            else
            {
                bool moving = (leftPressed || rightPressed) && !shopOpen;
                if (onBoat && moving) { sheetName = "row"; fr = (frame / 8) % 4; feet = 14f; }
                else if (!onBoat && moving) { sheetName = "walk"; fr = (frame / 6) % 6; feet = 16.2f; }
                else { sheetName = "idle"; fr = (frame / 12) % 4; feet = 16f; }
            }

            Image sheet = Pk("pack_fisherman_" + sheetName);
            if (sheet == null) { sheet = Pk("pack_fisherman_idle"); fr = 0; feet = 16f; }
            if (sheet == null) return;

            DrawFisherFrame(g, sheet, fr, feet, facing < 0);
        }
        void DrawFishermanScene(Graphics g)
        {
            DrawFisherman(g);

            if (state == State.Idle) return;    

            PointF tip = RodTip();
            g.DrawLine(Pens.White, tip.X, tip.Y, hookX + HookSize / 2f, hookY);
            g.FillEllipse(Brushes.Silver, hookX, hookY, HookSize, HookSize);
        }
        bool DrawBoatSprite(Graphics g, bool near)
        {
            Image img = Pk("pack_boat");
            if (img == null) return false;

            const int scale = 2;
            int w = img.Width * scale, h = img.Height * scale;
            int x = (int)boatX + (BoatW - w) / 2;
            int y = DockTop - 5 * scale;        

            GraphicsState st = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (onBoat && facing < 0)          
            {
                float cx = boatX + BoatW / 2f;
                g.TranslateTransform(cx, 0);
                g.ScaleTransform(-1, 1);
                g.TranslateTransform(-cx, 0);
            }

            Rectangle dest = new Rectangle(x, y, w, h);
            if (boatOwned) g.DrawImage(img, dest);
            else g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, AlphaAttr(130));
            g.Restore(st);

            if (near)
                using (Pen p = new Pen(Color.Gold, 3)) g.DrawRectangle(p, BoatRect);

            if (!boatOwned)
                g.DrawString("BOAT", fontLabel, Brushes.White, new Rectangle(x, y + h / 2 - 10, w, 20), centerFmt);

            return true;
        }

        void DrawShopImage(Graphics g, Rectangle r, Image img, bool near)
        {
            GraphicsState st = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            float s = Math.Min(r.Width / (float)img.Width, r.Height / (float)img.Height);
            int w = (int)(img.Width * s), h = (int)(img.Height * s);
            Rectangle dest = new Rectangle(r.X + (r.Width - w) / 2, r.Bottom - h, w, h);
            g.DrawImage(img, dest);
            g.Restore(st);

            if (near)
                using (Pen p = new Pen(Color.Gold, 3)) g.DrawRectangle(p, dest);
        }

        bool DrawChestSprite(Graphics g, Chest c)
        {
            Image img = Pk("pack_chest");
            if (img == null) return false;

            Rectangle r = c.Bounds;
            int w = img.Width * 2, h = img.Height * 2;
            Rectangle dest = new Rectangle(r.X + (r.Width - w) / 2, r.Bottom - h, w, h);

            if (!c.Opened)
                g.FillEllipse(GetBrush(Color.FromArgb(60, 255, 215, 0)), r.X - 14, r.Y - 14, r.Width + 28, r.Height + 28);

            GraphicsState st = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            if (!c.Opened) g.DrawImage(img, dest);
            else g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, AlphaAttr(120));
            g.Restore(st);

            if (c.Opened)   
                g.FillEllipse(Brushes.Gold, dest.X + 8, dest.Y - 2, w - 16, 8);

            return true;
        }
    }
}