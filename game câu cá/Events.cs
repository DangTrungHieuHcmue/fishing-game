using System;
using System.Collections.Generic;
using System.Drawing;

namespace game_câu_cá
{
    public partial class Form1
    {
        enum EventKind { None, FishFrenzy, GoldenFish, TreasureDrop, JellySwarm, Storm, PredatorAlert, MarketCrash }

        const int EventEveryTicks = 90 * 60;
        const int PriceEventTicks = 30 * 60;
        const int StormEventTicks = 30 * 60;
        const int GoldenTicks = 8 * 60;
        const int SwarmTicks = 15 * 60;
        const int GoldenValue = 3000;

        EventKind eventLast = EventKind.None;
        EventKind pendingEvent = EventKind.None;
        int eventNextTick = EventEveryTicks;

        float priceMult = 1f;
        int priceTicks = 0;
        int stormTicks = 0;

        Fish golden = null;
        int goldenTicks = 0;

        List<Hazard> swarmJellies = new List<Hazard>();
        int swarmTicks = 0;

        void ResetEvents()
        {
            eventLast = EventKind.None;
            pendingEvent = EventKind.None;
            eventNextTick = EventEveryTicks;
            priceMult = 1f;
            priceTicks = 0;
            stormTicks = 0;
            golden = null;
            goldenTicks = 0;
            swarmJellies.Clear();
            swarmTicks = 0;
        }

        bool HookInWater
        {
            get { return state == State.Sinking || state == State.Reeling; }
        }

        bool EventNeedsHook(EventKind k)
        {
            return k == EventKind.GoldenFish || k == EventKind.TreasureDrop
                || k == EventKind.JellySwarm || k == EventKind.PredatorAlert;
        }

        bool CanFireNow(EventKind k)
        {
            if (!EventNeedsHook(k)) return true;
            if (!HookInWater) return false;

            if (k == EventKind.GoldenFish)
            {
                int minZone = Math.Min(rodMinZone[rodLevel], depthLevel - 1);
                return CurrentZone() >= minZone;
            }
            return true;
        }

        void UpdateEvents()
        {
            int elapsed = MiniSeconds * 60 - miniTicks;

            if (elapsed >= eventNextTick)
            {
                eventNextTick += EventEveryTicks;
                TriggerRandomEvent();
            }

            if (pendingEvent != EventKind.None && CanFireNow(pendingEvent))
            {
                EventKind k = pendingEvent;
                pendingEvent = EventKind.None;
                Fire(k);
            }

            TickEventEffects();
        }

        void TriggerRandomEvent()
        {
            EventKind k;
            do { k = (EventKind)rng.Next(1, 8); }
            while (k == eventLast);

            eventLast = k;
            Dispatch(k);
        }

        void Dispatch(EventKind k)
        {
            if (CanFireNow(k)) Fire(k);
            else pendingEvent = k;
        }

        void Fire(EventKind k)
        {
            switch (k)
            {
                case EventKind.FishFrenzy:
                    priceMult = 2f;
                    priceTicks = PriceEventTicks;
                    message = "SỰ KIỆN: Cá cực hot! Điểm x2 trong 30 giây!";
                    break;

                case EventKind.MarketCrash:
                    priceMult = 0.5f;
                    priceTicks = PriceEventTicks;
                    message = "SỰ KIỆN: Thị trường sụp đổ! Điểm chỉ còn một nửa trong 30 giây.";
                    break;

                case EventKind.Storm:
                    SetWeather(Weather.Storm);
                    stormTicks = StormEventTicks;
                    message = "SỰ KIỆN: Bão ập tới! Tầm nhìn giảm mạnh.";
                    break;

                case EventKind.GoldenFish:
                    SpawnGolden();
                    message = "SỰ KIỆN: Một con cá vàng xuất hiện gần lưỡi câu! Nhanh tay, chỉ có 8 giây!";
                    break;

                case EventKind.TreasureDrop:
                    SpawnEventChest();
                    message = "SỰ KIỆN: Một chiếc rương xuất hiện gần bạn!";
                    break;

                case EventKind.JellySwarm:
                    SpawnSwarm();
                    message = "SỰ KIỆN: Bầy sứa kéo đến quanh lưỡi câu!";
                    break;

                case EventKind.PredatorAlert:
                    if (!FirePredator()) FireOther();
                    break;
            }
        }

        void FireOther()
        {
            EventKind[] others =
            {
                EventKind.FishFrenzy, EventKind.GoldenFish, EventKind.TreasureDrop,
                EventKind.JellySwarm, EventKind.Storm, EventKind.MarketCrash
            };
            Dispatch(others[rng.Next(others.Length)]);
        }

        bool FirePredator()
        {
            int zone = CurrentZone();
            if (zone < 0 || PredImmune(zone)) return false;
            if (predators.Count > 0 || warnTicks > 0 || predBusy[zone] || graceTicks > 0) return false;

            predBusy[zone] = true;
            SpawnPredator(zone);
            message = "SỰ KIỆN: " + predNames[zone] + " lao tới ngay lập tức!";
            return true;
        }

        void TickEventEffects()
        {
            if (priceTicks > 0)
            {
                priceTicks--;
                if (priceTicks == 0) priceMult = 1f;
            }

            if (stormTicks > 0)
            {
                stormTicks--;
                if (stormTicks == 0) SetWeather(Weather.Clear);
            }

            if (goldenTicks > 0)
            {
                goldenTicks--;
                if (goldenTicks == 0)
                {
                    if (golden != null && !golden.Hooked) fishes.Remove(golden);
                    golden = null;
                }
            }

            if (swarmTicks > 0)
            {
                swarmTicks--;
                if (swarmTicks == 0)
                {
                    foreach (Hazard h in swarmJellies) hazards.Remove(h);
                    swarmJellies.Clear();
                }
            }
        }

        void SpawnGolden()
        {
            if (!speciesSprite.ContainsKey("Golden Fish")) speciesSprite["Golden Fish"] = "solarfish";

            int zone = Math.Max(0, CurrentZone());
            Species sp = new Species("Golden Fish", 30, Color.Gold, 3.2f, 12f, Solo);
            Fish f = MakeFish(zone, sp, true);
            f.Value = GoldenValue;
            f.Weight = 12f;

            float startX = hookX + (rng.Next(2) == 0 ? -260f : 260f);
            f.X = Math.Max(0f, Math.Min(startX, WorldW - 120f));
            f.BaseSpeed = 3.2f;
            f.Speed = f.X < hookX ? 3.2f : -3.2f;

            int minY = ZoneTopY(zone) + 12;
            int maxY = Math.Max(minY + 1, ZoneBottomY(zone) - sp.Size - 6);
            f.Y = Math.Max(minY, Math.Min(hookY - sp.Size / 2f, maxY));

            fishes.Add(f);
            golden = f;
            goldenTicks = GoldenTicks;
        }

        void SpawnEventChest()
        {
            Chest c = new Chest();
            c.W = ChestW;
            c.H = ChestH;
            c.X = Math.Max(10f, Math.Min(hookX + rng.Next(-140, 141) - ChestW / 2f, WorldW - ChestW - 10f));
            c.Y = Math.Min(hookY + rng.Next(30, 90), WorldBottom - ChestH - 2f);
            c.Zone = ZoneAtY(c.Y);
            c.OnRock = false;
            chests.Add(c);
        }

        void SpawnSwarm()
        {
            int zone = Math.Max(0, CurrentZone());
            int count = zone == 0 ? 4 : zone == 1 ? 3 : zone == 2 ? 2 : 1;

            int minY = Math.Max(ZoneTopY(zone) + 40, WaterTop + 60);
            int maxY = Math.Max(minY + 1, ZoneBottomY(zone) - JellyH - 40);

            for (int i = 0; i < count; i++)
            {
                Hazard h = new Hazard();
                h.Kind = 0;
                h.Zone = zone;
                h.W = JellyW;
                h.H = JellyH;

                bool placed = false;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    float side = rng.Next(2) == 0 ? -1f : 1f;
                    h.X = Math.Max(LeftLimit, Math.Min(hookX + side * rng.Next(140, 420), WorldW - 80f));
                    h.BaseY = Math.Max(minY, Math.Min(hookY + rng.Next(-120, 121) - JellyH / 2f, maxY));
                    h.Y = h.BaseY;

                    if (RectHitsRock(Rectangle.Inflate(h.Bounds, 10, 60))) continue;
                    placed = true;
                    break;
                }
                if (!placed) continue;

                h.Phase = (float)(rng.NextDouble() * Math.PI * 2);
                h.Speed = h.X < hookX ? 1.1f : -1.1f;
                hazards.Add(h);
                swarmJellies.Add(h);
            }

            swarmTicks = SwarmTicks;
        }

        void DevEvent()
        {
            if (gameMode != GameMode.Mini)
            {
                message = "DEV: sự kiện chỉ chạy trong mini game";
                return;
            }
            TriggerRandomEvent();
        }

        void DrawEventChip(Graphics g)
        {
            if (priceTicks <= 0) return;

            int secs = (priceTicks + 59) / 60;
            bool good = priceMult > 1f;

            Rectangle r = new Rectangle(8, 78, 300, 34);
            g.FillRectangle(GetBrush(good ? Color.FromArgb(220, 30, 110, 60) : Color.FromArgb(220, 130, 35, 35)), r);
            g.DrawRectangle(Pens.White, r);
            string label = good ? "CÁ CỰC HOT  x2 ĐIỂM  " + secs + "s" : "THỊ TRƯỜNG SỤP  x0.5 ĐIỂM  " + secs + "s";
            g.DrawString(label, fontSmall, Brushes.White, r, centerFmt);
        }
    }
}
