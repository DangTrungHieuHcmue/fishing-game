using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace game_câu_cá
{
    public partial class Form1
    {
        enum GameMode { Menu, Casual, Mini, MiniResult }
        GameMode gameMode = GameMode.Casual;

        const int MiniSeconds = 600;               
        const int MiniPriceMul = 5;               

        int miniTicks = 0;                          
        int miniFinalScore = 0;
        int miniBest = 0;
        bool miniBestLoaded = false;
        bool miniNewBest = false;
        int MiniScore { get { return totalEarned; } }
        bool miniStashed = false;
        int sMoney, sRod, sLine, sBagCount, sBagValue;
        int sCaught, sEarned, sLost, sChests, sPlay;
        bool sGlow, sBoat;
        void BeginMini()
        {
            if (!miniStashed)
            {
                sMoney = money; sRod = rodLevel; sLine = lineUpgrades;
                sGlow = hasGlowBait; sBoat = boatOwned;
                sBagCount = bagCount; sBagValue = bagValue;
                sCaught = totalCaught; sEarned = totalEarned; sLost = fishLost;
                sChests = chestsOpened; sPlay = playTicks;
                miniStashed = true;
            }
            LoadMiniBest();
            money = 0;
            rodLevel = 0;
            lineUpgrades = 0;
            hasGlowBait = false;
            boatOwned = true;
            bagCount = 0;
            bagValue = 0;
            totalCaught = 0;
            totalEarned = 0;
            fishLost = 0;
            chestsOpened = 0;
            playTicks = 0;

            miniNewBest = false;
            miniTicks = MiniSeconds * 60;
            gameMode = GameMode.Mini;

            ResetToDock();
            message = "MINI GAME: 10 phút để câu cá , sự kiện mỗi 1p30s , giá trị cá x5 kiếm nhiều điểm nhất có thể";
        }
        void LeaveMini()
        {
            if (!miniStashed) return;

            money = sMoney; rodLevel = sRod; lineUpgrades = sLine;
            hasGlowBait = sGlow; boatOwned = sBoat;
            bagCount = sBagCount; bagValue = sBagValue;
            totalCaught = sCaught; totalEarned = sEarned; fishLost = sLost;
            chestsOpened = sChests; playTicks = sPlay;
            miniStashed = false;
        }
        void UpdateMiniTimer()
        {
            if (gameMode != GameMode.Mini) return;

            miniTicks--;
            if (miniTicks <= 0) EndMini();
        }

        void EndMini()
        {
            if (state == State.Sinking || state == State.Reeling) FinishCast();
            if (bagCount > 0) Sell();

            miniFinalScore = MiniScore;
            ResetToDock();

            if (miniFinalScore > miniBest)
            {
                miniBest = miniFinalScore;
                miniNewBest = true;
                SaveMiniBest();
            }
            gameMode = GameMode.MiniResult;
        }
        void BackToMenu()
        {
            if (gameMode == GameMode.Mini || gameMode == GameMode.MiniResult) LeaveMini();

            gameMode = GameMode.Casual;
            ResetToDock();
            menuSel = 1;
            menuOpen = true;
        }
        void ResetToDock()
        {
            foreach (Fish f in fishes) f.Hooked = false;
            ResetPredatorState();

            shopOpen = false;
            pauseOpen = false;
            helpOpen = false;
            popup = 0;
            state = State.Idle;
            onBoat = false;
            boatX = MooredX;
            playerX = 250;
            graceTicks = 0;
            tapReel = 0f;
            atLineEnd = false;
            booms.Clear();
            ClearKeys();

            PlaceHookAtTip();
            cameraX = ClampCamX(playerX - ClientSize.Width / 2f);
            cameraY = IdleCamY();
        }

        string MiniBestPath { get { return Path.Combine(Application.StartupPath, "mini_best.txt"); } }

        void LoadMiniBest()
        {
            if (miniBestLoaded) return;
            miniBestLoaded = true;

            try
            {
                int v;
                if (File.Exists(MiniBestPath) && int.TryParse(File.ReadAllText(MiniBestPath).Trim(), out v))
                    miniBest = v;
            }
            catch { }
        }

        void SaveMiniBest()
        {
            try { File.WriteAllText(MiniBestPath, miniBest.ToString()); }
            catch { }
        }

        void HandleModeKey(KeyEventArgs e)
        {
            if (gameMode == GameMode.MiniResult)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) BeginMini();     
                else if (e.KeyCode == Keys.Escape) BackToMenu();
            }
            else if (gameMode == GameMode.Menu)
            {
                BackToMenu();                                                              
            }
        }

        void HandleModeClick(Point p)
        {
            if (gameMode == GameMode.MiniResult)
            {
                if (ResultAgainBtn.Contains(p)) BeginMini();
                else if (ResultMenuBtn.Contains(p)) BackToMenu();
            }
            else if (gameMode == GameMode.Menu)
            {
                BackToMenu();
            }
        }

        Rectangle ResultPanel { get { return new Rectangle(ClientSize.Width / 2 - 260, ClientSize.Height / 2 - 170, 520, 340); } }
        Rectangle ResultAgainBtn { get { return new Rectangle(ResultPanel.X + 40, ResultPanel.Bottom - 80, 205, 50); } }
        Rectangle ResultMenuBtn { get { return new Rectangle(ResultPanel.Right - 245, ResultPanel.Bottom - 80, 205, 50); } }
        void DrawMiniHud(Graphics g)
        {
            if (gameMode != GameMode.Mini) return;

            Rectangle r = new Rectangle(10, 10, 240, 80);
            g.FillRectangle(GetBrush(Color.FromArgb(195, 18, 22, 36)), r);
            g.DrawRectangle(Pens.White, r);

            int secs = (miniTicks + 59) / 60;
            string time = (secs / 60) + ":" + (secs % 60).ToString("00");

            g.DrawString("Mini Game  (fish x" + MiniPriceMul + ")", fontSmall, Brushes.Gold, r.X + 10, r.Y + 6);
            g.DrawString("Score: $" + MiniScore.ToString("N0"), fontTitle, Brushes.White, r.X + 10, r.Y + 24);
            g.DrawString("Time: " + time, fontRow, secs <= 10 ? Brushes.OrangeRed : Brushes.LightGray, r.X + 10, r.Y + 54);
        }
        void DrawModeScreens(Graphics g)
        {
            if (gameMode != GameMode.MiniResult) return;

            g.FillRectangle(GetBrush(Color.FromArgb(170, 0, 0, 0)), 0, 0, ClientSize.Width, ClientSize.Height);

            Rectangle p = ResultPanel;
            g.FillRectangle(GetBrush(Color.FromArgb(248, 30, 30, 42)), p);
            g.DrawRectangle(Pens.White, p);
            g.DrawString("MONEY MADE", fontSmall, Brushes.LightGray, new Rectangle(p.X, p.Y + 62, p.Width, 24), centerFmt);
            g.DrawString("$" + miniFinalScore.ToString("N0"), fontBanner, Brushes.Gold, new Rectangle(p.X, p.Y + 86, p.Width, 80), centerFmt);

            if (miniNewBest)
                g.DrawString("Best score!", fontRow, Brushes.LimeGreen, new Rectangle(p.X, p.Y + 176, p.Width, 28), centerFmt);
            else
                g.DrawString("Best: $" + miniBest.ToString("N0"), fontRow, Brushes.LightGray, new Rectangle(p.X, p.Y + 176, p.Width, 28), centerFmt);

            DrawButton(g, ResultAgainBtn, "Chơi lại", Color.SeaGreen, fontRow);
            DrawButton(g, ResultMenuBtn, "Main menu", Color.FromArgb(255, 70, 70, 100), fontRow);
            g.DrawString("Enter = chơi lại     Esc = main menu", fontTiny, Brushes.LightGray,
                         new Rectangle(p.X, p.Bottom - 22, p.Width, 18), centerFmt);
        }
    }
}