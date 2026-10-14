using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace game_câu_cá
{

    public partial class Form1
    {
        const int MaxErrorStreak = 300;            
        const long MaxLogBytes = 200 * 1024;       
        static readonly object logLock = new object();
        static readonly Dictionary<string, DateTime> lastLogged = new Dictionary<string, DateTime>();

        int errorStreak = 0;
        bool stoppedByErrors = false;

        public static void LogError(string where, Exception ex)
        {
            try
            {
                string text = ex == null ? "(unknown error)" : ex.ToString();
                string key = where + "|" + (ex == null ? "" : ex.GetType().Name + ex.Message);

                lock (logLock)
                {
                    DateTime last;
                    if (lastLogged.TryGetValue(key, out last) && (DateTime.Now - last).TotalSeconds < 5) return;
                    lastLogged[key] = DateTime.Now;

                    string path = Path.Combine(Application.StartupPath, "error_log.txt");
                    if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes) File.Delete(path);

                    File.AppendAllText(path,
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + where + "\r\n" + text + "\r\n\r\n");
                }
            }
            catch { }   
        }

        void Safe(string where, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                LogError(where, ex);
                message = "Error in " + where + " (see error_log.txt)";
            }
        }


        private void GameLoop(object sender, EventArgs e)
        {
            try
            {
                FixNumbers();
                GameLoopCore();
                errorStreak = 0;
            }
            catch (Exception ex)
            {
                errorStreak++;
                LogError("GameLoop", ex);
                RecoverFromError();

                if (errorStreak >= MaxErrorStreak && !stoppedByErrors)
                {
                    stoppedByErrors = true;
                    timer.Stop();
                    try
                    {
                        MessageBox.Show("The game stopped because the same error kept happening.\n\nDetails are in error_log.txt next to the game.",
                                        "Fishing game", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch { }
                }
            }
        }

        void RecoverFromError()
        {
            try
            {
                ClearKeys();
                ResetPredatorState();
                tapReel = 0f;
                atLineEnd = false;
                state = State.Idle;
                PlaceHookAtTip();
                message = "Something went wrong, the game recovered. (see error_log.txt)";
                Invalidate();
            }
            catch (Exception ex2) { LogError("RecoverFromError", ex2); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                PaintCore(e);
            }
            catch (Exception ex)
            {
                LogError("OnPaint", ex);
                try
                {
                    e.Graphics.ResetTransform();
                    e.Graphics.ResetClip();
                    e.Graphics.DrawString("A drawing error happened. See error_log.txt", fontSmall, Brushes.White, 10, 10);
                }
                catch { }
            }
        }

        static bool BadNumber(float v) { return float.IsNaN(v) || float.IsInfinity(v); }

        void FixNumbers()
        {
            if (BadNumber(playerX)) playerX = 250;
            if (BadNumber(boatX)) boatX = MooredX;
            if (BadNumber(cameraX)) cameraX = 0f;
            if (BadNumber(cameraY)) cameraY = IdleCamY();
            if (BadNumber(hookX) || BadNumber(hookY)) PlaceHookAtTip();
            if (BadNumber(power)) power = 0f;
            if (BadNumber(rodAngle)) rodAngle = 25f;
            if (BadNumber(sinkBonus)) sinkBonus = 0f;
            if (BadNumber(tapReel)) tapReel = 0f;

            if (money < 0) money = int.MaxValue;   
            if (rodLevel < 0) rodLevel = 0;
            if (rodLevel > rodNames.Length - 1) rodLevel = rodNames.Length - 1;
            if (lineUpgrades < 0) lineUpgrades = 0;
            if (lineUpgrades > MaxUpgrades) lineUpgrades = MaxUpgrades;
        }
    }
}
