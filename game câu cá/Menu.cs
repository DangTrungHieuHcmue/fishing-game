using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace game_câu_cá
{
    public partial class Form1
    {

        string menuTitle = "Fisch";                         string menuSubtitle = "";                   
        Color menuTitleColor = Color.Black;                                  Color menuTitleOutline = Color.FromArgb(235, 235, 245);              const string MenuLogoName = "menu_logo";
        string[] menuLabels = { "Casual", "Mini Game", "Load", "Settings", "Exit" };
        const string MenuBackgroundName = "menu_background";
        const int MenuDimAlpha = 0;                 
        bool menuOpen = true;                               int menuSel = 0;                                    Action[] menuActions;                               Image menuBg = null;
        Image menuLogo = null;
        string menuNote = "";
        int menuNoteTicks = 0;
        Point lastMenuMouse = new Point(-1, -1);

        Font fontMenuBtn = new Font("Arial", 18, FontStyle.Bold);
        Font fontMenuTitle = new Font("Arial Black", 72, FontStyle.Regular);           Font fontMenuSub = new Font("Arial", 12, FontStyle.Bold);

        const int MenuBtnH = 48;
        const int MenuBtnGap = 10;
        const int MenuPadX = 30;
        const int MenuPadY = 28;
        Rectangle MenuTitleArea
        {
            get
            {
                int w = (int)Math.Min(ClientSize.Width * 0.60f, 900);
                int top = (int)(ClientSize.Height * 0.04f);
                int bottom = (int)(ClientSize.Height * 0.37f);
                return new Rectangle(ClientSize.Width / 2 - w / 2, top, w, bottom - top);
            }
        }
        Rectangle MenuPanel
        {
            get
            {
                int n = menuLabels.Length;
                int w = (int)Math.Max(440, Math.Min(640, ClientSize.Width * 0.34f));
                int h = MenuPadY * 2 + n * MenuBtnH + (n - 1) * MenuBtnGap;

                int top = (int)(ClientSize.Height * 0.40f);
                top = Math.Min(top, ClientSize.Height - h - 20);                   top = Math.Max(top, 10);

                return new Rectangle(ClientSize.Width / 2 - w / 2, top, w, h);
            }
        }

        Rectangle MenuBtn(int i)
        {
            Rectangle p = MenuPanel;
            return new Rectangle(p.X + MenuPadX, p.Y + MenuPadY + i * (MenuBtnH + MenuBtnGap),
                                 p.Width - MenuPadX * 2, MenuBtnH);
        }
        void LoadMenu()
        {
            menuActions = new Action[]
            {
                StartCasualMode,
                StartMiniGame,
                MenuLoad,
                OpenOptions,
                () => Close()
            };

            menuBg = LoadMenuImage(MenuBackgroundName);
            menuLogo = LoadMenuImage(MenuLogoName);
        }
        Image LoadMenuImage(string name)
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "Images");
                if (!Directory.Exists(dir)) return null;

                string[] exts = { ".png", ".jpg", ".jpeg", ".bmp" };
                foreach (string ext in exts)
                {
                    string[] files = Directory.GetFiles(dir, name + ext, SearchOption.AllDirectories);
                    if (files.Length == 0) continue;

                    using (Image tmp = Image.FromFile(files[0]))
                        return new Bitmap(tmp);                         }
            }
            catch (Exception ex) { LogError("LoadMenuImage " + name, ex); }
            return null;
        }

        void MenuActivate(int i)
        {
            if (menuActions == null || i < 0 || i >= menuActions.Length) return;
            menuActions[i]();
        }
        void StartMiniGame()
        {
            BeginMini();
            menuOpen = false;
            ClearKeys();
        }

        void StartCasualMode()
        {
            gameMode = GameMode.Casual;
            menuOpen = false;
            ClearKeys();
        }

        void MenuLoad()
        {
            bool hadSave = File.Exists(SavePath);
            LoadGame();                                 
            if (hadSave) { gameMode = GameMode.Casual; menuOpen = false; ClearKeys(); }
            else { menuNote = pauseMsg; menuNoteTicks = 150; }
        }
        void OpenOptions()
        {
            pauseOpen = true;
            pauseView = 1;
            pauseMsg = "";
            ClearKeys();
        }
        void MenuTick()
        {
            if (menuNoteTicks > 0) menuNoteTicks--;
            if (mousePos != lastMenuMouse)
            {
                lastMenuMouse = mousePos;
                for (int i = 0; i < menuLabels.Length; i++)
                    if (MenuBtn(i).Contains(mousePos)) menuSel = i;
            }
        }

        void HandleMenuKey(KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            if (pauseOpen)                                          {
                if (e.KeyCode == Keys.Escape) ClosePause();
                return;
            }

            int n = menuLabels.Length;
            if (menuSel >= n) menuSel = 0;

            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) menuSel = (menuSel + n - 1) % n;
            else if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) menuSel = (menuSel + 1) % n;
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) MenuActivate(menuSel);
        }

        void HandleMenuClick(Point p)
        {
            if (pauseOpen)
            {
                HandlePauseClick(p);
                if (pauseOpen && pauseView == 0) pauseOpen = false;                   return;
            }

            for (int i = 0; i < menuLabels.Length; i++)
                if (MenuBtn(i).Contains(p)) { MenuActivate(i); return; }
        }

        void DrawMainMenu(Graphics g)
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            GraphicsState st = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (menuBg != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                float s = Math.Max(W / (float)menuBg.Width, H / (float)menuBg.Height);                   float dw = menuBg.Width * s, dh = menuBg.Height * s;
                g.DrawImage(menuBg, (W - dw) / 2f, (H - dh) / 2f, dw, dh);

                if (MenuDimAlpha > 0)
                    g.FillRectangle(GetBrush(Color.FromArgb(MenuDimAlpha, 0, 0, 0)), 0, 0, W, H);
            }
            else
            {
                using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                           Color.FromArgb(24, 52, 110), Color.FromArgb(6, 12, 34), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }
            Rectangle ta = MenuTitleArea;
            bool hasSub = menuSubtitle != "";
            Rectangle titleBox = hasSub ? new Rectangle(ta.X, ta.Y, ta.Width, ta.Height - 32) : ta;
            DrawMenuTitle(g, titleBox);
            if (hasSub)
            {
                Rectangle sr = new Rectangle(ta.X, ta.Bottom - 30, ta.Width, 30);
                MenuText(g, menuSubtitle, fontMenuSub, GetBrush(Color.FromArgb(160, 0, 0, 0)), new Rectangle(sr.X + 2, sr.Y + 2, sr.Width, sr.Height));
                MenuText(g, menuSubtitle, fontMenuSub, Brushes.White, sr);
            }
            Rectangle panel = MenuPanel;
            using (GraphicsPath pp = MenuRound(panel, 16))
            {
                int[] glowW = { 14, 10, 6, 3 };
                int[] glowA = { 16, 28, 44, 70 };
                for (int k = 0; k < glowW.Length; k++)
                    using (Pen gp = new Pen(Color.FromArgb(glowA[k], 90, 170, 230), glowW[k]) { LineJoin = LineJoin.Round })
                        g.DrawPath(gp, pp);

                using (LinearGradientBrush pb = new LinearGradientBrush(panel,
                           Color.FromArgb(238, 40, 50, 64), Color.FromArgb(238, 14, 18, 26), 90f))
                    g.FillPath(pb, pp);

                using (Pen frame = new Pen(Color.FromArgb(255, 80, 150, 195), 2))
                    g.DrawPath(frame, pp);
            }
            for (int k = 0; k < 3; k++)
            {
                int ny = panel.Y + panel.Height / 4 + k * (panel.Height / 4) - 10;
                g.FillRectangle(GetBrush(Color.FromArgb(255, 80, 150, 195)), panel.X - 4, ny, 8, 20);
                g.FillRectangle(GetBrush(Color.FromArgb(255, 80, 150, 195)), panel.Right - 4, ny, 8, 20);
            }
            for (int i = 0; i < menuLabels.Length; i++)
            {
                Rectangle r = MenuBtn(i);
                bool sel = menuSel == i;

                using (GraphicsPath bp = MenuRound(r, 9))
                {
                    using (LinearGradientBrush fb = new LinearGradientBrush(r,
                               sel ? Color.FromArgb(255, 40, 78, 108) : Color.FromArgb(255, 30, 36, 46),
                               sel ? Color.FromArgb(255, 14, 30, 46) : Color.FromArgb(255, 6, 8, 12), 90f))
                        g.FillPath(fb, bp);

                    using (Pen bpen = new Pen(sel ? Color.FromArgb(255, 130, 215, 255) : Color.FromArgb(255, 62, 92, 118), sel ? 3 : 2))
                        g.DrawPath(bpen, bp);
                }

                MenuText(g, menuLabels[i], fontMenuBtn, Brushes.White, new Rectangle(r.X + 10, r.Y, r.Width - 20, r.Height));
            }
            if (menuNoteTicks > 0 && menuNote != "")
            {
                float a = Math.Min(1f, menuNoteTicks / 40f);
                Rectangle nr = new Rectangle(0, Math.Min(H - 36, panel.Bottom + 10), W, 30);
                MenuText(g, menuNote, fontMenuSub, GetBrush(Color.FromArgb((int)(120 * a), 0, 0, 0)),
                         new Rectangle(nr.X + 2, nr.Y + 2, nr.Width, nr.Height));
                MenuText(g, menuNote, fontMenuSub, GetBrush(Color.FromArgb((int)(255 * a), 255, 255, 255)), nr);
            }

            g.Restore(st);
            if (pauseOpen) DrawPauseMenu(g);
        }
        GraphicsPath MenuRound(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        void DrawMenuTitle(Graphics g, Rectangle area)
        {
            if (menuLogo != null)
            {
                float k = Math.Min(area.Width / (float)menuLogo.Width, area.Height / (float)menuLogo.Height);
                float w = menuLogo.Width * k, h = menuLogo.Height * k;
                GraphicsState ls = g.Save();
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(menuLogo, area.X + (area.Width - w) / 2f, area.Y + (area.Height - h) / 2f, w, h);
                g.Restore(ls);
                return;
            }

            DrawTitleText(g, menuTitle, area, 9f);
        }
        void DrawTitleText(Graphics g, string text, Rectangle area, float edgeWidth)
        {
            string[] lines = text.Split('\n');
            float em = fontMenuTitle.Size * g.DpiY / 72f;                 float lineH = em * 1.1f;

            using (GraphicsPath tp = new GraphicsPath())
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center })
            {
                for (int i = 0; i < lines.Length; i++)
                    tp.AddString(lines[i].TrimEnd('\r'), fontMenuTitle.FontFamily, (int)fontMenuTitle.Style, em,
                                 new RectangleF(0, i * lineH, 1000f, em * 3f), sf);   
                RectangleF bb = tp.GetBounds();
                if (bb.Width < 1 || bb.Height < 1) return;

                float s = Math.Min(Math.Min(area.Width / bb.Width, area.Height / bb.Height), 1.5f);

                GraphicsState ts = g.Save();
                g.TranslateTransform(area.X + area.Width / 2f, area.Y + area.Height / 2f);
                g.ScaleTransform(s, s);
                g.TranslateTransform(-(bb.X + bb.Width / 2f), -(bb.Y + bb.Height / 2f));

                using (Pen edge = new Pen(menuTitleOutline, edgeWidth / s) { LineJoin = LineJoin.Round })
                    g.DrawPath(edge, tp);
                using (SolidBrush fill = new SolidBrush(menuTitleColor))
                    g.FillPath(fill, tp);

                g.Restore(ts);
            }
        }
        void MenuText(Graphics g, string text, Font font, Brush brush, Rectangle r)
        {
            SizeF sz = g.MeasureString(text, font);
            float s = sz.Width > r.Width ? r.Width / sz.Width : 1f;

            GraphicsState st = g.Save();
            g.TranslateTransform(r.X + r.Width / 2f, r.Y + r.Height / 2f);
            g.ScaleTransform(s, s);
            g.DrawString(text, font, brush, 0, 0, centerFmt);
            g.Restore(st);
        }
    }
}