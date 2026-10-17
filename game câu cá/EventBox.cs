using System;
using System.Collections.Generic;
using System.Drawing;

namespace game_câu_cá
{
    public partial class Form1
    {
        const int EventNoteTicks = 10 * 60;

        class EventNote
        {
            public EventKind Kind;
            public int Born;
            public string Detail;
        }

        class EventRow
        {
            public string Title, Desc;
            public Color Col;
            public int Left, Total;
        }

        List<EventNote> eventNotes = new List<EventNote>();
        void NoteEvent(EventKind k, string detail = "")
        {
            eventNotes.Add(new EventNote
            {
                Kind = k,
                Born = miniTicks,
                Detail = detail
            });

            if (eventNotes.Count > 6)
                eventNotes.RemoveAt(0);
        }
        void EventInfo(
            EventKind k,
            string detail,
            out string title,
            out string desc,
            out Color col)
        {
            switch (k)
            {
                case EventKind.FishFrenzy:
                    title = "Tiền vào như nước";
                    desc = "x2 điểm bán cá";
                    col = Color.FromArgb(50, 170, 90);
                    break;

                case EventKind.MarketCrash:
                    title = "Tích trữ chờ thời";
                    desc = "mất 0,5 giá trị khi bán";
                    col = Color.FromArgb(200, 60, 60);
                    break;

                case EventKind.Storm:
                    title = "Bão";
                    desc = "Giảm tầm nhìn";
                    col = Color.FromArgb(110, 120, 170);
                    break;

                case EventKind.GoldenFish:
                    title = "Cá hiếm";
                    desc = "Bắt nó: +$" + GoldenValue.ToString("N0");
                    col = Color.Gold;
                    break;

                case EventKind.TreasureDrop:
                    title = "Rương kho báu";
                    desc = "Một chiếc rương đã xuất hiện!";
                    col = Color.FromArgb(220, 150, 60);
                    break;

                case EventKind.JellySwarm:
                    title = "Bầy sứa di cư";
                    desc = "Né những chú sứa đi qua";
                    col = Color.FromArgb(170, 110, 230);
                    break;

                default:
                    title = "Thú săn mồi";
                    desc = string.IsNullOrEmpty(detail)
                        ? "Một thứ gì đấy sắp đến"
                        : detail + " sắp đến!";
                    col = Color.FromArgb(225, 70, 70);
                    break;
            }
        }
        List<EventRow> CollectEventRows()
        {
            List<EventRow> rows = new List<EventRow>();

            string title, desc;
            Color color;

            if (priceTicks > 0)
            {
                EventInfo(
                    priceMult > 1f
                        ? EventKind.FishFrenzy
                        : EventKind.MarketCrash,
                    "",
                    out title, out desc, out color);

                rows.Add(new EventRow
                {
                    Title = title,
                    Desc = desc,
                    Col = color,
                    Left = priceTicks,
                    Total = PriceEventTicks
                });
            }

            if (stormTicks > 0)
            {
                EventInfo(
                    EventKind.Storm, "",
                    out title, out desc, out color);

                rows.Add(new EventRow
                {
                    Title = title,
                    Desc = desc,
                    Col = color,
                    Left = stormTicks,
                    Total = StormEventTicks
                });
            }

            if (goldenTicks > 0 && golden != null)
            {
                EventInfo(
                    EventKind.GoldenFish, "",
                    out title, out desc, out color);

                rows.Add(new EventRow
                {
                    Title = title,
                    Desc = desc,
                    Col = color,
                    Left = goldenTicks,
                    Total = GoldenTicks
                });
            }

            if (swarmTicks > 0)
            {
                EventInfo(
                    EventKind.JellySwarm, "",
                    out title, out desc, out color);

                rows.Add(new EventRow
                {
                    Title = title,
                    Desc = desc,
                    Col = color,
                    Left = swarmTicks,
                    Total = SwarmTicks
                });
            }
            for (int i = eventNotes.Count - 1; i >= 0; i--)
            {
                EventNote note = eventNotes[i];
                int age = miniTicks - note.Born;

                if (age < 0 || age >= EventNoteTicks)
                {
                    eventNotes.RemoveAt(i);
                    continue;
                }

                EventInfo(
                    note.Kind, note.Detail,
                    out title, out desc, out color);

                rows.Add(new EventRow
                {
                    Title = title,
                    Desc = desc,
                    Col = color,
                    Left = EventNoteTicks - age,
                    Total = EventNoteTicks
                });
            }

            return rows;
        }
        void DrawEventBox(Graphics g)
        {
            List<EventRow> rows = CollectEventRows();
            Rectangle r = WorldEventBox;

            using (SolidBrush bg = new SolidBrush(
                Color.FromArgb(220, 18, 22, 36)))
            using (Pen border = new Pen(Color.White, 2))
            {
                g.FillRectangle(bg, r);
                g.DrawRectangle(border, r);
            }

            g.DrawString(
                "Sự Kiện",
                fontRow,
                Brushes.Gold,
                r.X + 10,
                r.Y + 8);

            int y = r.Y + 35;

            if (rows.Count == 0)
            {
                g.DrawString(
                    "Không có sự kiện",
                    fontSmall,
                    Brushes.LightGray,
                    r.X + 10,
                    y);

                y += 24;
            }
            else
            {
                foreach (EventRow row in rows)
                {
                    int seconds = (row.Left + 59) / 60;

                    g.DrawString(
                        row.Title,
                        fontSmall,
                        GetBrush(row.Col),
                        r.X + 10,
                        y);

                    g.DrawString(
                        row.Desc + "  " + seconds + "s",
                        fontTiny,
                        Brushes.White,
                        r.X + 10,
                        y + 17);

                    y += 38;

                    if (y > r.Bottom - 25)
                        break;
                }
            }
            string foot;
            Brush footBrush;

            if (pendingEvent != EventKind.None)
            {
                foot = "Event waiting: lower your line";
                footBrush = Brushes.Gold;
            }
            else
            {
                int elapsed = MiniSeconds * 60 - miniTicks;
                int nextIn = Math.Max(0, eventNextTick - elapsed);
                int seconds = (nextIn + 59) / 60;

                foot = "Next event: "
                    + (seconds / 60) + ":"
                    + (seconds % 60).ToString("00");

                footBrush = Brushes.LightGray;
            }

            g.DrawString(
                foot,
                fontTiny,
                footBrush,
                r.X + 10,
                r.Bottom - 18);
        }
    }
}