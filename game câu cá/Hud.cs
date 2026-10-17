using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace game_câu_cá
{

    public partial class Form1
    {

        bool helpOpen = false;
        string lastMessage = "";
        int toastTicks = 0;
        const int ToastTime = 240;

        Font fontMoney = new Font("Arial", 18, FontStyle.Bold);
        Pen cogPen = new Pen(Color.White, 5);
        Pen sunPen = new Pen(Color.Gold, 2);
        Pen dropPen = new Pen(Color.FromArgb(120, 190, 255), 2);


        Rectangle WeatherBox { get { return new Rectangle(ClientSize.Width / 2 - 115, 10, 230, 46); } }
        Rectangle MoneyBox { get { return new Rectangle(8, ClientSize.Height - 86, 250, 78); } }
        Rectangle HelpIconRect { get { return new Rectangle(ClientSize.Width - 118, 10, 46, 46); } }
        Rectangle CogIconRect { get { return new Rectangle(ClientSize.Width - 62, 10, 46, 46); } }

        Rectangle HelpPanel { get { return new Rectangle(ClientSize.Width / 2 - 430, ClientSize.Height / 2 - 280, 860, 560); } }
        Rectangle HelpClose { get { return new Rectangle(HelpPanel.Right - 40, HelpPanel.Y + 10, 28, 28); } }

        void OpenHelp()
        {
            helpOpen = true;
            ClearKeys();
        }

        void DrawInterface(Graphics g)
        {
            DrawMoneyBox(g);
            DrawWeatherBox(g);
            DrawIconButton(g, HelpIconRect, true);
            DrawIconButton(g, CogIconRect, false);
            DrawToast(g);

            if (graceTicks > 0)
            {
                Rectangle r = new Rectangle(WeatherBox.X - 215, 10, 205, 46);
                DrawPanel(g, r);
                g.DrawString("LINE PROTECTED: " + (graceTicks / 60 + 1) + "s", fontSmall, Brushes.LimeGreen, r, centerFmt);
            }
        }

        void DrawPanel(Graphics g, Rectangle r)
        {
            g.FillRectangle(GetBrush(Color.FromArgb(195, 18, 22, 36)), r);
            g.DrawRectangle(Pens.White, r);
        }


        void DrawMoneyBox(Graphics g)
        {
            Rectangle r = MoneyBox;
            DrawPanel(g, r);

            g.DrawString("$" + money.ToString("N0"), fontMoney, Brushes.Gold, r.X + 10, r.Y + 5);

            int z = CurrentZone();
            string zoneText = z >= 0 ? zoneNames[z] : "Above water";
            int depthM = (int)Math.Max(0, hookY - WaterTop) / 10;

            g.DrawString("Bag: " + bagCount + " fish ($" + bagValue + ")", fontSmall, Brushes.White, r.X + 10, r.Y + 38);
            g.DrawString("Depth: " + depthM + " m (" + zoneText + ")", fontSmall, Brushes.LightGray, r.X + 10, r.Y + 56);
        }


        void DrawWeatherBox(Graphics g)
        {
            Rectangle r = WeatherBox;
            DrawPanel(g, r);

            float cx = r.X + 28, cy = r.Y + r.Height / 2f;

            if (weather == Weather.Clear)
            {
                g.FillEllipse(Brushes.Gold, cx - 9, cy - 9, 18, 18);
                for (int a = 0; a < 360; a += 45)
                {
                    double rad = a * Math.PI / 180.0;
                    g.DrawLine(sunPen, cx + (float)Math.Cos(rad) * 12, cy + (float)Math.Sin(rad) * 12,
                                       cx + (float)Math.Cos(rad) * 18, cy + (float)Math.Sin(rad) * 18);
                }
            }
            else if (weather == Weather.Rain)
            {
                DrawCloud(g, cx, cy - 6, Color.FromArgb(200, 205, 215));
                for (int i = 0; i < 3; i++)
                    g.DrawLine(dropPen, cx - 10 + i * 9, cy + 7, cx - 13 + i * 9, cy + 15);
            }
            else
            {
                DrawCloud(g, cx, cy - 6, Color.FromArgb(110, 115, 130));
                g.FillPolygon(Brushes.Gold, new PointF[]
                {
                    new PointF(cx + 2, cy + 2), new PointF(cx - 5, cy + 9), new PointF(cx - 1, cy + 9),
                    new PointF(cx - 4, cy + 17), new PointF(cx + 7, cy + 7), new PointF(cx + 2, cy + 7)
                });
            }

            Rectangle text = new Rectangle(r.X + 52, r.Y, r.Width - 56, r.Height);
            g.DrawString(WeatherName(), fontHud, Brushes.White, text, centerFmt);
        }

        void DrawCloud(Graphics g, float cx, float cy, Color c)
        {
            SolidBrush b = GetBrush(c);
            g.FillEllipse(b, cx - 17, cy - 3, 16, 12);
            g.FillEllipse(b, cx - 9, cy - 11, 18, 17);
            g.FillEllipse(b, cx + 1, cy - 4, 16, 12);
            g.FillRectangle(b, cx - 10, cy + 1, 20, 8);
        }


        void DrawIconButton(Graphics g, Rectangle r, bool help)
        {
            bool hover = r.Contains(mousePos);
            g.FillEllipse(GetBrush(hover ? Color.FromArgb(235, 70, 76, 110) : Color.FromArgb(200, 18, 22, 36)), r);
            g.DrawEllipse(Pens.White, r);

            if (help)
                g.DrawString("?", fontTitle, Brushes.White, r, centerFmt);
            else
            {
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                for (int a = 0; a < 360; a += 45)
                {
                    double rad = a * Math.PI / 180.0;
                    g.DrawLine(cogPen, cx + (float)Math.Cos(rad) * 8, cy + (float)Math.Sin(rad) * 8,
                                       cx + (float)Math.Cos(rad) * 13, cy + (float)Math.Sin(rad) * 13);
                }
                g.FillEllipse(Brushes.White, cx - 9, cy - 9, 18, 18);
                g.FillEllipse(GetBrush(Color.FromArgb(255, 40, 44, 62)), cx - 4, cy - 4, 8, 8);
            }
        }


        void DrawToast(Graphics g)
        {
            if (toastTicks <= 0 || message == "" || shopOpen) return;

            float a = Math.Min(1f, toastTicks / 40f);
            SizeF sz = g.MeasureString(message, fontHud, 900);

            Rectangle r = new Rectangle((int)(ClientSize.Width / 2f - sz.Width / 2f - 14), WeatherBox.Bottom + 8,
                                        (int)sz.Width + 28, (int)sz.Height + 14);

            g.FillRectangle(GetBrush(Color.FromArgb((int)(195 * a), 18, 22, 36)), r);
            g.DrawString(message, fontHud, GetBrush(Color.FromArgb((int)(255 * a), 255, 230, 140)), r, centerFmt);
        }


        void DrawHelp(Graphics g)
        {
            g.FillRectangle(GetBrush(Color.FromArgb(150, 0, 0, 0)), 0, 0, ClientSize.Width, ClientSize.Height);

            Rectangle p = HelpPanel;
            g.FillRectangle(GetBrush(Color.FromArgb(245, 30, 30, 42)), p);
            g.DrawRectangle(Pens.White, p);
            DrawButton(g, HelpClose, "X", HelpClose.Contains(mousePos) ? Color.Firebrick : Color.DarkRed, fontRow);

            g.DrawString("HOW TO PLAY", fontTitle, Brushes.White, new Rectangle(p.X, p.Y + 14, p.Width, 34), centerFmt);

            string[,] rows =
            {
                { "A / D",            "Duy chuyển xung quanh , lái tàu , đổi hướng khi câu cá " },
                { "Hold SPACE",       "ném cần , giữ càng lâu ném càng xa " },
                { "SPACE (in water)", "Kéo cần lên " },
                { "W",                "giữ hoặc ấn nhẹ để kéo cần câu đi lên" },
                { "S",                "Mỗi đi xuống nhanh hơn " },
                { "E  or  click",     "Sử dụng cửa hàng , mua bán cá / nâng cấp" },
                { "H",                "Tắt mở hướng dẫn chơi" },
                { "Esc",              "để vào setting điều chỉnh âm thanh , xem tiến trình , lưu game " }
            };

            int y = p.Y + 62;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                g.DrawString(rows[i, 0], fontRow, Brushes.Gold, p.X + 30, y);
                g.DrawString(rows[i, 1], fontRow, Brushes.White, p.X + 210, y);
                y += 30;
            }

            y += 12;
            g.DrawString("TIPS", fontRow, Brushes.Gold, p.X + 30, y);
            y += 28;

            string[] tips =
            {
                "Bán cá của bạn ở tiệm đầu tiên , các nâng cấp về cần câu ,dây và mồi ở của hàng thứ hai.",
                "jelly fish và sea mine sẽ làm bạn mất cá hãy né chúng khi có thể.",
                "Mỗi khu vực đều có những mối đe dọa riêng của nó hãy thu cần khi còn có thể."
            };
            foreach (string t in tips)
            {
                g.DrawString(t, fontSmall, Brushes.LightGray, new Rectangle(p.X + 30, y, p.Width - 60, 40));
                y += 36;
            }

            g.DrawString("Esc / Enter / H to close", fontTiny, Brushes.LightGray,
                         new Rectangle(p.X, p.Bottom - 26, p.Width, 18), centerFmt);
        }
        Rectangle WorldEventBox
        {
            get { return new Rectangle(ClientSize.Width - 270, 85, 250, 100); }
        }

        void DrawWorldEventBox(Graphics g)
        {
            Rectangle r = WorldEventBox;
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(220, 18, 22, 36)))
                g.FillRectangle(bg, r);

            using (Pen border = new Pen(Color.White, 2))
                g.DrawString(
                "Sự kiện",
                fontRow,
                Brushes.Gold,
                r.X + 10,
                r.Y + 8
            );
            g.DrawString(
                "Events: Không có",
                fontSmall,
                Brushes.White,
                r.X + 10,
                r.Y + 38
            );

            
        }
    }
}
