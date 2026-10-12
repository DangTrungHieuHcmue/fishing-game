using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace game_câu_cá
{
    public partial class Form1 : Form
    {
        // =====================================================================
        //  CONFIG  (everything you might want to tweak is in this section)
        // =====================================================================

        // ================= DEV SWITCH =================
        const bool Dev = true;   // false = normal game

        // ---------- world layout ----------
        const int ViewW = 1600;               // window width
        const int WorldW = 7000;         // about a 20 s boat ride (5 px per tick)
        const int ScreenH = 680;
        const int WaterTop = 320;
        const int DockTop = WaterTop - 25;
        const int DockW = 720;                // the dock is a small platform on the left
        const int HookSize = 12;
        const int LeftLimit = 10;
        const int RightLimit = WorldW - 80;

        // ---------- depth zones (sizes are in SCREENS) ----------
        const int ZoneCount = 4;
        int[] zoneScreens = { 3, 4, 6, 12 };
        string[] zoneNames = { "Surface", "Mid", "Deep", "Abyss" };
        Color[] zoneColors =
        {
            Color.CornflowerBlue,
            Color.RoyalBlue,
            Color.FromArgb(25, 50, 160),
            Color.FromArgb(10, 20, 70)
        };
        int[] zoneTop = new int[ZoneCount + 1];   // filled in the constructor (zoneTop[ZoneCount] = seabed)

        // ---------- fish behaviours ----------
        const int Solo = 0;      // swims alone
        const int School = 1;    // swims in a small group
        const int Hunter = 2;    // chases and eats smaller fish

        Species[][] zoneSpecies =
        {
            new[] {  // Surface
                new Species("Minnow",   10, Color.Silver,         2.8f, 0.5f, School),
                new Species("Sardine",  9,  Color.LightSteelBlue, 3.2f, 0.3f, School),
                new Species("Guppy",    8,  Color.Violet,         3.0f, 0.2f, School),
                new Species("Carp",     16, Color.Orange,         2.0f, 3f,   Solo),
                new Species("Perch",    14, Color.YellowGreen,    2.4f, 2f,   Solo),
                new Species("Goldfish", 13, Color.DarkOrange,     1.8f, 1.5f, Solo),
                new Species("Bluegill", 12, Color.DeepSkyBlue,    2.6f, 1f,   Solo) },
            new[] {  // Mid
                new Species("Bass",     18, Color.OliveDrab,      2.4f, 6f,   Solo),
                new Species("Trout",    17, Color.LightSeaGreen,  2.6f, 5f,   School),
                new Species("Salmon",   22, Color.Salmon,         2.0f, 10f,  School),
                new Species("Mackerel", 15, Color.Teal,           3.2f, 4f,   School),
                new Species("Pike",     24, Color.DarkOliveGreen, 2.8f, 12f,  Hunter),
                new Species("Catfish",  20, Color.Sienna,         1.6f, 9f,   Solo),
                new Species("Snapper",  19, Color.IndianRed,      2.2f, 7f,   Solo) },
            new[] {  // Deep
                new Species("Tuna",      28, Color.SteelBlue,      2.6f, 30f, School),
                new Species("Mahi",      26, Color.MediumSeaGreen, 2.8f, 25f, School),
                new Species("Swordfish", 32, Color.DarkTurquoise,  3.0f, 60f, Hunter),
                new Species("Marlin",    36, Color.DodgerBlue,     3.2f, 90f, Hunter),
                new Species("Grouper",   30, Color.SandyBrown,     1.8f, 45f, Solo),
                new Species("Barracuda", 28, Color.LightSlateGray, 3.4f, 35f, Hunter),
                new Species("Manta",     40, Color.DarkSlateBlue,  1.6f, 80f, Solo) },
            new[] {  // Abyss (last number = respawn cooldown in seconds = how rare it is)
                new Species("Anglerfish", 30, Color.MediumPurple, 1.8f, 50f,  Solo,   60),
                new Species("Viperfish",  26, Color.DarkCyan,     2.4f, 30f,  Solo,   60),
                new Species("Fangtooth",  22, Color.SlateGray,    2.0f, 25f,  Solo,   90),
                new Species("Gulper Eel", 36, Color.Indigo,       1.4f, 90f,  Solo,   180),
                new Species("Hammerhead", 40, Color.DimGray,      2.2f, 120f, Hunter, 180),
                new Species("Oarfish",    46, Color.Crimson,      1.2f, 150f, Solo,   240) }
        };

        Species legendary = new Species("Legendary Fish", 36, Color.Gold, 1.8f, 200f, Solo, 300);

        // ---------- fish counts ----------
        int[] fishPerZone = { 450, 200, 100, 0 };      // Surface / Mid / Deep (the Abyss uses its own system below)
        int[] baseCount = new int[ZoneCount];       // how many fish each zone normally holds

        // Abyss: 10 on the map, the rest wait in reserve, each with its own respawn cooldown
        const int AbyssActive = 10;
        int[] abyssCounts = { 8, 7, 5, 4, 4, 1 };   // same order as zoneSpecies[3]; the Legendary is added once on top
        const int CdDiv = Dev ? 6 : 1;              // dev = cooldowns 6x shorter

        // ---------- anti-clustering ----------
        float[] spawnGap = { 55f, 70f, 100f, 200f };
        const int CellSize = 70;

        // ---------- prices: a fixed amount per species (weight only matters for the rod) ----------
        Dictionary<string, int> fishPrice = new Dictionary<string, int>
        {
            // Surface
            { "Minnow", 3 },  { "Sardine", 3 }, { "Guppy", 3 },
            { "Bluegill", 5 }, { "Goldfish", 6 }, { "Perch", 7 }, { "Carp", 8 },
            // Mid
            { "Bass", 35 }, { "Trout", 30 }, { "Salmon", 60 }, { "Mackerel", 25 },
            { "Pike", 70 }, { "Catfish", 55 }, { "Snapper", 40 },
            // Deep
            { "Tuna", 250 }, { "Mahi", 220 }, { "Swordfish", 500 }, { "Marlin", 800 },
            { "Grouper", 400 }, { "Barracuda", 300 }, { "Manta", 700 },
            // Abyss
            { "Anglerfish", 700 }, { "Viperfish", 450 }, { "Fangtooth", 350 },
            { "Gulper Eel", 1300 }, { "Hammerhead", 1700 }, { "Oarfish", 2100 },
            { "Legendary Fish", 8000 }
        };

        // ---------- hunters ----------
        const float HuntRange = 420f;
        const float PreyMaxRatio = 0.5f;   // hunters only eat fish up to half their weight

        // ---------- hazards ----------
        int[] jellyCount = { 18, 35, 44, 35 };
        int[] mineCount = { 0, 22, 13, 9 };
        const int HazardCap = 10;          // never more than this many hazards on one screen
        const int HitImmunity = 90;        // about 1.5 seconds
        const int JellyW = 40, JellyH = 50;
        const int MineW = JellyW * 2, MineH = JellyH * 2;

        // ---------- cliffs (rock walls on the left / right edges) ----------
        const float CliffSkipChance = 0.4f;   // chance that a cliff is skipped (open water instead)
        float[] coralChance = { 0.9f, 0.7f, 0.4f, 0.2f };
        Color[] coralColors = { Color.HotPink, Color.Coral, Color.MediumOrchid, Color.LightSalmon, Color.Tomato };
        const float CoralBiteChance = 0.05f;
        const float CoralBiteValueLeft = 0.75f;

        // ---------- treasure chests ----------
        const int ChestW = 44, ChestH = 32;
        const float RockChestChance = 0.20f;     // per cliff, rolled again on every cast
        const float SeabedChestChance = 0.30f;   // per cast
        const float RockChestPercent = 0.10f;              // reward = 10% of your next upgrade...
        int[] rockChestCap = { 1500, 3500, 7000, 10000 };  // ...but never more than this (per zone)

        // ---------- line ----------
        const float BaseLine = 200f;
        const int MaxUpgrades = 30;

        // ---------- bait ----------
        const int GlowPrice = 5000;
        const float GlowRadius = 240f;
        const float BaseLightRadius = 80f;

        // ---------- rods ----------
        string[] rodNames =
        {
            "Basic Rod", "Bamboo Rod", "Fiberglass Rod", "Graphite Rod", "Carbon Rod",
            "Steel Rod", "Titanium Rod", "Pro Rod", "Expert Rod", "Master Rod"
        };
        float[] rodCapacity = { 10f, 20f, 35f, 60f, 100f, 150f, 220f, 320f, 450f, 700f };
        int[] rodPrice = { 0, 150, 500, 1500, 4000, 10000, 25000, 60000, 150000, 400000 };
        int[] rodMinZone = { 0, 0, 0, 0, 1, 1, 2, 2, 2, 2 };   // higher rods ignore shallow zones
        const float MinWeightFrac = 0.02f;                       // ...and fish lighter than 2% of the rod's capacity

        // ---------- boat ----------
        const int BoatPrice = 1000;
        const int BoatW = 160;
        const float BoatSpeed = 5f;
        const int MooredX = DockW + 5;            // where the boat is parked
        const float BoatCapMul = 1.5f;            // +50% weight capacity while on the boat
        const int BoatExtraSlots = 3;             // +3 fish on the hook while on the boat
        // fish get heavier the farther you fish from the dock (by hook X)
        const int NearEnd = 1500;                 // the dock can only reach this far
        const int MidEnd = 4200;
        const float NearMul = 0.6f, MidMul = 1.0f, FarMul = 1.6f;
        const float RoamChance = 0.03f;           // a big fish sometimes roams into the near area

        // ---------- predators ----------
        string[] predNames = { "Piranha", "Eel", "Shark", "???" };
        float[] predSpeed = { 9f, 5f, 7f, 6f };
        int[] predW = { 30, 60, 80, 100 };
        int[] predH = { 16, 14, 30, 40 };
        int[,] predMinutes = { { 1, 3 }, { 2, 5 }, { 3, 6 }, { 4, 8 } };
        const int PredDiv = Dev ? 12 : 1;
        const int WarnTime = Dev ? 60 : 180;
        const int GraceTime = 300;

        // ---------- weather ----------
        const int WeatherSeconds = Dev ? 60 : 600;      // a new random weather every 10 minutes

        // ---------- darkness ----------
        float[] darkLevels = { 0f, 0.35f, 0.72f, 0.93f, 0.97f };

        // ---------- player / casting ----------
        const float WalkSpeed = 4f;
        const int NearDistance = 5;
        const int RodPivotYOffset = 45;
        const int RodLen = 45;
        const float MaxThrow = 600f;

        // ---------- hook movement ----------
        const float SinkSpeed = 2f;
        const float FastSinkSpeed = 6f;
        const float ReelSpeed = 10f;
        const float TapReelDist = 70f;
        const float HoldReelSpeed = 3f;
        const float SinkAccel = 0.03f;
        const float MaxSinkBonus = 5f;

        // ---------- zone banner ----------
        const int ZoneBannerTime = 180;                 // about 3 seconds

        // =====================================================================
        //  TYPES
        // =====================================================================

        class Species
        {
            public string Name;
            public int Size;
            public Color Color;
            public float Speed;
            public float Weight;      // kg
            public int Behavior;
            public int CdSec;         // Abyss only: respawn cooldown in seconds

            public Species(string name, int size, Color color, float speed, float weight, int behavior, int cdSec = 0)
            {
                Name = name; Size = size; Color = color; Speed = speed;
                Weight = weight; Behavior = behavior; CdSec = cdSec;
            }
        }

        // one of the Abyss fish: a few are active on the map, the rest wait in reserve
        class AbyssSlot
        {
            public Species Sp;
            public Fish Active;      // the fish currently swimming (null = in reserve)
            public int Cooldown;     // ticks until it may come back
        }

        class Fish
        {
            public string Name;
            public int Zone;
            public float X, Y, Speed, BaseSpeed;
            public int Size, Value, Behavior;
            public float Weight;
            public float Roll;       // random 0..1, decides if a "roaming big fish" is big
            public float Phase;      // random offset for wobble / tie-breaks
            public Color Color;
            public bool Hooked;
            public bool Bitten;      // a coral fish took a bite out of it
            public bool Extra;       // spawned by the weather, leaves when the weather clears
            public bool Dead;        // eaten this tick
            public Fish Target;      // prey (hunters only)
            public AbyssSlot Slot;   // Abyss fish only
            public bool FacingRight { get { return Speed > 0; } }
            public Rectangle Bounds { get { return new Rectangle((int)X, (int)Y, Size * 2, Size); } }
        }

        class Predator
        {
            public string Name;
            public float X, Y, Speed;
            public int W, H, Zone;
            public bool Done;
            public Rectangle Bounds { get { return new Rectangle((int)X, (int)Y, W, H); } }
        }

        class Hazard
        {
            public int Kind;            // 0 = jellyfish, 1 = sea mine
            public int Zone;
            public float X, Y, BaseY, Speed, Phase;
            public int W, H;
            public Rectangle Bounds { get { return new Rectangle((int)X, (int)Y, W, H); } }
        }

        class Chest
        {
            public float X, Y;
            public int W, H, Zone;
            public bool OnRock;      // true = sits on a cliff, false = on the seabed
            public bool Opened;
            public Rectangle Bounds { get { return new Rectangle((int)X, (int)Y, W, H); } }
        }

        class Coral
        {
            public Rectangle Rect;
            public Rectangle Bite;   // area where the coral fish can reach your hook
            public Color Color;      // accent color for the coral tips
            public int Seed;         // makes every patch look different (but the same every frame)
            public bool HasBiter;
            public bool Touching;
            public int Flash;
        }

        // a stepped rock wall on the left or right edge of the map
        class Cliff
        {
            public int Side;                                  // 0 = left, 1 = right
            public bool NoChest;                              // the cliff under the dock never gets a chest
            public List<Rectangle> Steps = new List<Rectangle>();
            public Point[] Poly;
            public Rectangle Bounds;
        }

        class DevButton
        {
            public string Label;
            public Func<string> Dynamic;   // optional: label that changes (e.g. ON/OFF)
            public Action Click;
        }

        enum State { Idle, Charging, Throwing, Sinking, Reeling }
        enum Weather { Clear, Rain, Storm }

        // =====================================================================
        //  GAME STATE
        // =====================================================================

        State state = State.Idle;
        Weather weather = Weather.Clear;

        Random rng = new Random();
        Timer timer = new Timer();
        int frame = 0;
        string message = "Hold SPACE to charge, release to throw!";

        // ---------- economy ----------
        int bagCount = 0;
        int bagValue = 0;
        int money = Dev ? 1000000 : 0;
        int chestsOpened = 0;

        // ---------- gear ----------
        int rodLevel = 0;
        int lineUpgrades = 0;
        bool hasGlowBait = false;
        bool boatOwned = false;
        bool onBoat = false;
        float boatX = MooredX;

        // ---------- world objects ----------
        List<Fish> fishes = new List<Fish>();
        List<Predator> predators = new List<Predator>();
        List<Hazard> hazards = new List<Hazard>();
        List<Chest> chests = new List<Chest>();
        List<Rectangle> rocks = new List<Rectangle>();   // every step of every cliff (collisions)
        List<Cliff> cliffs = new List<Cliff>();          // the same rocks grouped for drawing
        List<Coral> corals = new List<Coral>();
        List<AbyssSlot> abyssSlots = new List<AbyssSlot>();
        List<AbyssSlot> readyTmp = new List<AbyssSlot>();
        List<Fish> eatenTmp = new List<Fish>();

        // ---------- anti-clustering grid ----------
        int gridW, gridH;
        List<Fish>[] grid;
        List<int> usedCells = new List<int>();

        // ---------- respawns (batched once per second so groups can re-form) ----------
        int[] pending = new int[ZoneCount];
        int[] pendingExtra = new int[ZoneCount];
        int respawnTimer = 0;
        bool extrasPresent = false;

        // ---------- hazards ----------
        int hitTicks = 0;

        // ---------- line ----------
        bool atLineEnd = false;

        // ---------- predators ----------
        int[] predElapsed = new int[ZoneCount];
        int[] predNeeded = new int[ZoneCount];
        bool[] predBusy = new bool[ZoneCount];
        int warnTicks = 0;
        int warnZone = -1;
        int graceTicks = 0;

        // ---------- weather ----------
        int weatherTicks = WeatherSeconds * 60;
        float[] rainX = new float[220];
        float[] rainY = new float[220];
        Pen rainPen = new Pen(Color.FromArgb(150, 200, 220, 255), 1);

        // ---------- darkness ----------
        float[] darkDepth;
        float darkLevel = 0f;
        float lightRadius = 0f;
        bool devSeeAll = Dev;   // Dev mode: no darkness, every fish visible (toggle with F)

        // ---------- camera ----------
        float cameraX = 0f;
        float cameraY = 0f;

        // ---------- player / rod ----------
        float playerX = 250;
        float rodAngle = 25f;
        int facing = 1;

        // ---------- throwing ----------
        float power = 0f;
        float powerDir = 1f;
        float lastPower = 0f;
        float throwT = 0f;
        float throwStartX, throwStartY, throwTargetX, throwArc;

        // ---------- hook ----------
        float hookX, hookY;
        bool leftPressed, rightPressed, downPressed, upPressed;
        float tapReel = 0f;
        float sinkBonus = 0f;

        // ---------- zone banner ----------
        int zoneBannerTicks = 0;
        int zoneBannerZone = -1;
        bool[] zoneSeen = new bool[ZoneCount];          // zones already announced on this cast

        // ---------- gear shop UI ----------
        bool shopOpen = false;
        int shopTab = 0;          // 0 = rod, 1 = line, 2 = bait
        int shopSel = 0;
        Point mousePos = new Point(-1, -1);

        // ---------- dev panel ----------
        List<DevButton> devButtons = new List<DevButton>();
        bool devOpen = true;

        // ---------- images (set these later) ----------
        Image sellImage = null;
        Image gearImage = null;
        Image dockImage = null;
        Image coralImage = null;   // if set, it is drawn instead of the code-drawn coral

        // ---------- coral palette (muted, like a pixel-art reef) ----------
        Color[] moundColors = { Color.FromArgb(34, 62, 52), Color.FromArgb(46, 72, 60), Color.FromArgb(58, 62, 80) };
        Color[] branchColors = { Color.FromArgb(176, 156, 108), Color.FromArgb(150, 158, 96), Color.FromArgb(190, 170, 130) };
        Color[] weedColors = { Color.FromArgb(70, 140, 84), Color.FromArgb(92, 158, 96) };

        // ---------- cached drawing objects ----------
        Dictionary<Color, SolidBrush> brushCache = new Dictionary<Color, SolidBrush>();
        Pen rockPen = new Pen(Color.FromArgb(45, 55, 75), 2);
        Pen dividerPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1);
        Pen glowPen = new Pen(Color.White, 2);
        Pen rodPen = new Pen(Color.Black, 3);
        Pen goldPen = new Pen(Color.Gold, 3);
        Pen jellyTentPen = new Pen(Color.FromArgb(170, 210, 150, 255), 2);
        Pen minePen = new Pen(Color.Orange, 3);
        Pen predPen = new Pen(Color.Red, 3);
        Font fontTiny = new Font("Arial", 8, FontStyle.Bold);
        Font fontLabel = new Font("Arial", 9, FontStyle.Bold);
        Font fontSmall = new Font("Arial", 10, FontStyle.Bold);
        Font fontRow = new Font("Arial", 11, FontStyle.Bold);
        Font fontHud = new Font("Arial", 12, FontStyle.Bold);
        Font fontTitle = new Font("Arial", 14, FontStyle.Bold);
        Font fontBanner = new Font("Arial", 40, FontStyle.Bold);
        StringFormat centerFmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        SolidBrush GetBrush(Color c)
        {
            SolidBrush b;
            if (!brushCache.TryGetValue(c, out b))
            {
                b = new SolidBrush(c);
                brushCache[c] = b;
            }
            return b;
        }

        // =====================================================================
        //  CONSTRUCTOR
        // =====================================================================

        public Form1()
        {
            InitializeComponent();

            this.ClientSize = new Size(ViewW, ScreenH);
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.KeyPreview = true;

            // load your images here later, for example:
            // sellImage = Image.FromFile("sell.png");
            // gearImage = Image.FromFile("gear.png");
            // dockImage = Image.FromFile("dock.png");
            // coralImage = Image.FromFile("coral.png");

            // zone boundaries (must come first, a lot of things use them)
            zoneTop[0] = WaterTop;
            for (int z = 0; z < ZoneCount; z++)
                zoneTop[z + 1] = zoneTop[z] + zoneScreens[z] * ScreenH;

            darkDepth = new float[]
            {
                0f,
                ZoneTopY(1) - WaterTop,
                ZoneTopY(2) - WaterTop,
                ZoneTopY(3) - WaterTop,
                WorldBottom - WaterTop
            };

            PlaceHookAtTip();

            for (int i = 0; i < rainX.Length; i++)
            {
                rainX[i] = (float)rng.NextDouble();
                rainY[i] = (float)rng.NextDouble();
            }

            SpawnRocks();          // also places the rock chests

            gridW = WorldW / CellSize + 2;
            gridH = WorldBottom / CellSize + 2;
            grid = new List<Fish>[gridW * gridH];

            for (int z = 0; z < ZoneCount - 1; z++)
            {
                baseCount[z] = fishPerZone[z];
                SpawnBatch(z, baseCount[z], true, false);
            }
            baseCount[ZoneCount - 1] = AbyssActive;
            BuildAbyssSlots();
            ProcessAbyss(true, AbyssActive);

            for (int z = 0; z < ZoneCount; z++)
                predNeeded[z] = RollPredTime(z);

            SpawnHazards();
            RerollChests();

            cameraX = ClampCamX(playerX - ClientSize.Width / 2f);

            if (Dev) BuildDevButtons();

            timer.Interval = 16;
            timer.Tick += GameLoop;
            timer.Start();

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseWheel += Form1_MouseWheel;
        }

        // =====================================================================
        //  LAYOUT HELPERS
        // =====================================================================

        int ZoneTopY(int z) { return zoneTop[z]; }
        int ZoneBottomY(int z) { return zoneTop[z + 1]; }
        int WorldBottom { get { return zoneTop[ZoneCount]; } }

        int ZoneAtY(float y)
        {
            for (int z = 0; z < ZoneCount; z++)
                if (y < ZoneBottomY(z)) return z;
            return ZoneCount - 1;
        }

        // zone the hook is in (-1 = above the water)
        int CurrentZone()
        {
            for (int z = ZoneCount - 1; z >= 0; z--)
                if (hookY >= ZoneTopY(z)) return z;
            return -1;
        }

        Rectangle SellBox { get { return new Rectangle(10, DockTop - 100, 150, 100); } }
        Rectangle GearBox { get { return new Rectangle(170, DockTop - 100, 150, 100); } }
        Rectangle DockRect { get { return new Rectangle(0, DockTop, DockW, 35); } }
        Rectangle BoatRect { get { return new Rectangle((int)boatX, DockTop, BoatW, 30); } }
        Rectangle CharacterRect { get { return new Rectangle((int)playerX - 12, DockTop - 40, 24, 40); } }

        float ClampCamX(float x)
        {
            float maxCamX = Math.Max(0, WorldW - ClientSize.Width);
            return Math.Max(0f, Math.Min(x, maxCamX));
        }

        PointF RodTip()
        {
            double a = rodAngle * Math.PI / 180.0;
            float x = playerX + facing * (float)(Math.Sin(a) * RodLen);
            float y = (WaterTop - RodPivotYOffset) - (float)(Math.Cos(a) * RodLen);
            return new PointF(x, y);
        }

        void PlaceHookAtTip()
        {
            PointF tip = RodTip();
            hookX = tip.X - HookSize / 2f;
            hookY = tip.Y + 45;
        }

        string WeatherName()
        {
            if (weather == Weather.Rain) return "Rain";
            if (weather == Weather.Storm) return "Storm";
            return "Clear";
        }

        // =====================================================================
        //  LINE / ROD / BAIT
        // =====================================================================

        int UpgradeCost { get { return 100 + 60 * lineUpgrades + 4 * lineUpgrades * lineUpgrades; } }

        // every upgrade adds the same amount; 30 upgrades reaches the seabed
        float LinePerUpgrade
        {
            get { return (WorldBottom - WaterTop - HookSize - 2 - BaseLine) / MaxUpgrades; }
        }

        float LineLength { get { return BaseLine + Math.Min(lineUpgrades, MaxUpgrades) * LinePerUpgrade; } }
        float MaxLineLength { get { return BaseLine + MaxUpgrades * LinePerUpgrade; } }

        // 1 = Surface only ... 4 = reaches the Abyss
        int depthLevel
        {
            get
            {
                int lvl = 1;
                float len = LineLength;
                for (int z = 1; z < ZoneCount; z++)
                    if (len >= ZoneTopY(z) - WaterTop - 0.5f) lvl = z + 1;
                return lvl;
            }
        }

        // the boat gives more weight capacity and more hook slots
        float RodCapacity { get { return rodCapacity[rodLevel] * (onBoat ? BoatCapMul : 1f); } }
        int MaxHooked { get { return 3 + rodLevel + (onBoat ? BoatExtraSlots : 0); } }

        float HookedWeight()
        {
            float w = 0f;
            foreach (Fish f in fishes)
                if (f.Hooked) w += f.Weight;
            return w;
        }

        int HookedCount()
        {
            int n = 0;
            foreach (Fish f in fishes)
                if (f.Hooked) n++;
            return n;
        }

        List<Fish> HookedList()
        {
            var list = new List<Fish>();
            foreach (Fish f in fishes)
                if (f.Hooked) list.Add(f);
            return list;
        }

        // cost of the cheapest thing you could upgrade next (-1 = everything is maxed)
        int NextUpgradeCost()
        {
            int best = -1;
            if (rodLevel < rodNames.Length - 1) best = rodPrice[rodLevel + 1];
            if (lineUpgrades < MaxUpgrades)
            {
                int u = UpgradeCost;
                if (best < 0 || u < best) best = u;
            }
            return best;
        }

        void BuyGlow()
        {
            if (hasGlowBait) { message = "You already own the Glow Bait."; return; }
            if (money < GlowPrice) { message = "Need $" + GlowPrice + " for the Glow Bait."; return; }
            money -= GlowPrice;
            hasGlowBait = true;
            message = "Bought the Glow Bait! A big circle of light now follows your hook.";
        }

        void BuyRod() { BuyRodAt(rodLevel + 1); }

        void BuyRodAt(int index)
        {
            if (index < 0 || index >= rodNames.Length) return;

            if (index <= rodLevel) { message = "You already own the " + rodNames[index] + "."; return; }
            if (index > rodLevel + 1) { message = "Buy the " + rodNames[rodLevel + 1] + " first."; return; }

            int cost = rodPrice[index];
            if (money < cost) { message = "Need $" + cost + " for the " + rodNames[index] + "."; return; }

            money -= cost;
            rodLevel = index;
            shopSel = Math.Min(rodLevel + 1, rodNames.Length - 1);
            message = "Bought the " + rodNames[rodLevel] + "! It holds " + RodCapacity + " kg.";
        }

        void Upgrade()
        {
            if (lineUpgrades >= MaxUpgrades)
            {
                message = "Your line is already fully upgraded!";
                return;
            }

            int cost = UpgradeCost;
            if (money < cost)
            {
                message = "Need $" + cost + " to upgrade your line.";
                return;
            }

            int levelBefore = depthLevel;
            money -= cost;
            lineUpgrades++;

            if (depthLevel > levelBefore)
                message = "Line upgraded! You can now reach the " + zoneNames[depthLevel - 1] + " zone!";
            else
                message = "Line extended to " + (int)(LineLength / 10) + " m! (" + lineUpgrades + "/" + MaxUpgrades + ")";
        }

        // ---------- rod filter: which fish will this rod hook? ----------
        // zone rule + weight rule. If the zone rule would hide every zone your line can reach,
        // the rod falls back to the deepest zone you can reach and ignores the weight rule.
        bool CanHook(Fish f, float weight)
        {
            int rodMin = rodMinZone[rodLevel];
            int reach = depthLevel - 1;
            int minZone = Math.Min(rodMin, reach);

            if (f.Zone < minZone) return false;
            if (minZone < rodMin) return true;
            return weight >= RodCapacity * MinWeightFrac;
        }

        // =====================================================================
        //  BOAT
        // =====================================================================

        bool BoatDocked { get { return boatX <= MooredX + 1f; } }

        bool NearBoat()
        {
            return !onBoat && Math.Abs(playerX - (MooredX + BoatW / 2f)) < 140f;
        }

        bool IsQuestFish(string name)
        {
            return name == "Pike" || name == "Salmon" || name == "Catfish";
        }

        // fish are heavier the farther from the dock you hook them
        float RegionMul(Fish f)
        {
            float x = hookX;
            if (x < NearEnd) return f.Roll < RoamChance ? FarMul : NearMul;
            if (x < MidEnd) return MidMul;
            return FarMul;
        }

        string AreaName(float x)
        {
            if (x < NearEnd) return "Near (x" + NearMul.ToString("0.0") + " weight)";
            if (x < MidEnd) return "Middle (x" + MidMul.ToString("0.0") + " weight)";
            return "Far (x" + FarMul.ToString("0.0") + " weight)";
        }

        void UseBoat()
        {
            if (onBoat)
            {
                if (!BoatDocked) { message = "Sail back to the dock to get off."; return; }
                onBoat = false;
                playerX = DockW - 30;
                message = "You got off the boat.";
                return;
            }

            if (boatOwned)
            {
                onBoat = true;
                playerX = boatX + BoatW / 2f;
                message = "A/D = sail   Hold SPACE = cast   On the boat you carry more!   E at the dock = get off";
                return;
            }

            if (money >= BoatPrice)
            {
                money -= BoatPrice;
                boatOwned = true;
                message = "Bought the boat! Press E again to board it.";
            }
            else
                message = "The boat costs $" + BoatPrice + " - or land a Pike, Salmon or Catfish and it's yours for free!";
        }

        // =====================================================================
        //  CLIFFS + CORAL
        // =====================================================================

        bool RectHitsRock(Rectangle r)
        {
            for (int i = 0; i < rocks.Count; i++)
                if (rocks[i].IntersectsWith(r)) return true;
            return false;
        }

        bool HookHitsRock(float x, float y)
        {
            return RectHitsRock(new Rectangle((int)x, (int)y, HookSize, HookSize));
        }

        // short (1-2 steps) 45%, medium (3-4) 35%, long (5-8) 20%
        int RollCliffSteps()
        {
            int r = rng.Next(100);
            if (r < 45) return rng.Next(1, 3);
            if (r < 80) return rng.Next(3, 5);
            return rng.Next(5, 9);
        }

        // narrow 30%, medium 40%, wide 30%  (rolled separately from the length)
        int RollCliffWidth()
        {
            int r = rng.Next(100);
            if (r < 30) return rng.Next(180, 321);
            if (r < 70) return rng.Next(320, 561);
            return rng.Next(600, 1001);
        }

        void SpawnRocks()
        {
            rocks.Clear();
            cliffs.Clear();
            corals.Clear();

            int limit = WorldBottom - 130;   // keep the seabed clear

            for (int side = 0; side < 2; side++)
            {
                int y = WaterTop + 150 + rng.Next(0, 120);

                while (y + 90 < limit)
                {
                    int steps = RollCliffSteps();
                    int stepH = rng.Next(90, 151);
                    steps = Math.Min(steps, (limit - y) / stepH);
                    if (steps < 1) break;

                    // some cliffs are skipped: open water instead
                    if (rng.NextDouble() < CliffSkipChance)
                    {
                        y += steps * stepH + rng.Next(300, 600);
                        continue;
                    }

                    Cliff c = BuildCliff(side, y, steps, stepH, RollCliffWidth());
                    if (side == 0 && !cliffs.Exists(k => k.Side == 0)) c.NoChest = true;   // first left cliff = the one under the dock
                    cliffs.Add(c);
                    AddCoralOn(c.Steps[0]);

                    y = c.Bounds.Bottom + rng.Next(500, 1000);
                }
            }
        }

        Cliff BuildCliff(int side, int y, int steps, int stepH, int w)
        {
            Cliff c = new Cliff();
            c.Side = side;

            int yy = y;
            for (int i = 0; i < steps; i++)
            {
                int x = side == 0 ? 0 : WorldW - w;
                Rectangle r = new Rectangle(x, yy, w, stepH);
                c.Steps.Add(r);
                rocks.Add(r);
                yy += stepH;
                w = Math.Max(140, w - rng.Next(40, 111));
            }

            BuildCliffShape(c);
            return c;
        }

        void BuildCliffShape(Cliff c)
        {
            var pts = new List<Point>();
            int baseX = c.Side == 0 ? 0 : WorldW;

            pts.Add(new Point(baseX, c.Steps[0].Y));
            for (int i = 0; i < c.Steps.Count; i++)
            {
                Rectangle r = c.Steps[i];
                int edge = c.Side == 0 ? r.Right : r.Left;
                pts.Add(new Point(edge, r.Y));
                pts.Add(new Point(edge, r.Bottom));
            }
            pts.Add(new Point(baseX, c.Steps[c.Steps.Count - 1].Bottom));
            c.Poly = pts.ToArray();

            Rectangle b = c.Steps[0];
            for (int i = 1; i < c.Steps.Count; i++) b = Rectangle.Union(b, c.Steps[i]);
            c.Bounds = b;
        }

        // a wide, flat coral patch lying on top of the cliff (drawn in code, or with coralImage)
        void AddCoralOn(Rectangle r)
        {
            int zone = ZoneAtY(r.Y);
            if (rng.NextDouble() > coralChance[zone]) return;

            Coral c = new Coral();
            int cw = (int)(r.Width * (0.7 + rng.NextDouble() * 0.25));
            int ch = rng.Next(40, 66);
            int cx = r.X + rng.Next(0, Math.Max(1, r.Width - cw));
            c.Rect = new Rectangle(cx, r.Y - ch + 6, cw, ch);
            c.Bite = Rectangle.Inflate(c.Rect, 30, 25);
            c.Color = coralColors[rng.Next(coralColors.Length)];
            c.Seed = rng.Next();
            c.HasBiter = rng.Next(2) == 0;
            corals.Add(c);
        }

        // the chest sits on the top ledge, at the open end of the cliff
        void AddRockChest(Cliff c)
        {
            Rectangle top = c.Steps[0];
            Chest ch = new Chest();
            ch.W = ChestW;
            ch.H = ChestH;
            ch.X = c.Side == 0 ? top.Right - ChestW - 8 : top.Left + 8;
            ch.Y = top.Y - ChestH;
            ch.Zone = ZoneAtY(top.Y);
            ch.OnRock = true;
            chests.Add(ch);
        }

        void UpdateCorals(bool hookActive, Rectangle hookRect)
        {
            for (int i = 0; i < corals.Count; i++)
            {
                Coral c = corals[i];
                if (c.Flash > 0) c.Flash--;

                bool inside = hookActive && c.HasBiter && c.Bite.IntersectsWith(hookRect);
                if (!inside) { c.Touching = false; continue; }
                if (c.Touching) continue;       // one roll per pass
                c.Touching = true;

                List<Fish> hooked = HookedList();
                if (hooked.Count == 0) continue;
                if (rng.NextDouble() >= CoralBiteChance) continue;

                Fish bitten = hooked[rng.Next(hooked.Count)];
                int before = bitten.Value;
                bitten.Value = (int)(bitten.Value * CoralBiteValueLeft);
                bitten.Bitten = true;
                c.Flash = 45;
                message = "A coral fish bit your " + bitten.Name + "! (-$" + (before - bitten.Value) + ")";
            }
        }

        // =====================================================================
        //  TREASURE CHESTS
        // =====================================================================

        void SpawnSeabedChest()
        {
            Chest c = new Chest();
            c.W = ChestW;
            c.H = ChestH;
            c.X = rng.Next(60, WorldW - 60 - c.W);
            c.Y = WorldBottom - c.H - 2;
            c.Zone = ZoneCount - 1;
            c.OnRock = false;
            chests.Add(c);
        }

        // seabed: $6,000-14,000 (10% jackpot x4). Cliff: 10% of your next upgrade, capped by the zone.
        int RollChestReward(Chest c)
        {
            if (!c.OnRock)
            {
                int reward = rng.Next(6000, 14001);
                if (rng.Next(10) == 0) reward *= 4;
                return reward;
            }

            int cap = rockChestCap[c.Zone];
            int next = NextUpgradeCost();
            if (next < 0) return cap;                        // everything maxed: pay the cap
            return Math.Min(cap, (int)(next * RockChestPercent));
        }

        // new random chests every time you cast
        void RerollChests()
        {
            chests.Clear();

            foreach (Cliff c in cliffs)
            {
                if (c.NoChest) continue;   // the cliff under the dock never gets a chest
                if (rng.NextDouble() < RockChestChance) AddRockChest(c);
            }

            if (rng.NextDouble() < SeabedChestChance) SpawnSeabedChest();
        }

        void UpdateChests(bool hookActive, Rectangle hookRect)
        {
            if (!hookActive) return;

            foreach (Chest c in chests)
            {
                if (c.Opened || !c.Bounds.IntersectsWith(hookRect)) continue;

                int reward = RollChestReward(c);
                c.Opened = true;
                money += reward;
                chestsOpened++;
                message = "TREASURE! You opened a chest and found $" + reward + "!";
            }
        }

        // =====================================================================
        //  FISH SPAWNING
        // =====================================================================

        Species PickSpecies(int zone)
        {
            Species[] options = zoneSpecies[zone];
            return options[rng.Next(options.Length)];
        }

        Fish MakeFish(int zone, Species sp, bool extra)
        {
            var f = new Fish();
            f.Name = sp.Name;
            f.Zone = zone;
            f.Size = sp.Size;
            f.Behavior = sp.Behavior;
            f.Color = sp.Color;
            f.Extra = extra;
            f.Phase = (float)(rng.NextDouble() * 6.28);
            f.Roll = (float)rng.NextDouble();

            // weight varies per fish (it only matters for the rod), the price is fixed per species
            f.Weight = sp.Weight * (0.7f + (float)rng.NextDouble() * 0.6f);
            int price;
            f.Value = fishPrice.TryGetValue(sp.Name, out price) ? price : 1;
            return f;
        }

        // is there no other fish of this zone within "gap" pixels of this spot?
        bool SpotFree(int zone, float x, float y, float gap)
        {
            for (int i = 0; i < fishes.Count; i++)
            {
                Fish o = fishes[i];
                if (o.Zone != zone) continue;
                if (Math.Abs(o.X - x) < gap && Math.Abs(o.Y - y) < gap) return false;
            }
            return true;
        }

        // spawns n fish in a zone; school species arrive as small groups (3-5)
        void SpawnBatch(int zone, int n, bool anywhere, bool extra)
        {
            int made = 0;
            while (made < n)
            {
                Species sp = PickSpecies(zone);
                int group = 1;
                if (sp.Behavior == School)
                    group = Math.Min(n - made, rng.Next(3, 6));
                made += SpawnGroup(zone, sp, group, anywhere, extra);
            }
        }

        int SpawnGroup(int zone, Species sp, int count, bool anywhere, bool extra)
        {
            float speed = sp.Speed + (float)rng.NextDouble() * 0.8f;
            bool goRight = rng.Next(2) == 0;

            int minY = ZoneTopY(zone) + 12;
            int maxY = Math.Max(minY + 1, ZoneBottomY(zone) - sp.Size - 6);

            // try a few spots, prefer one with free space around it
            float baseX = 0f, baseY = 0f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                baseY = rng.Next(minY, maxY);
                baseX = anywhere ? rng.Next(0, WorldW) : (goRight ? -sp.Size * 3 : WorldW + sp.Size);
                if (SpotFree(zone, baseX, baseY, spawnGap[zone])) break;
            }

            for (int i = 0; i < count; i++)
            {
                Fish f = MakeFish(zone, sp, extra);

                // group members swim the same way at nearly the same speed, but not exactly
                float mySpeed = count > 1 ? speed * (0.95f + (float)rng.NextDouble() * 0.1f) : speed;
                f.BaseSpeed = mySpeed;
                f.Speed = goRight ? mySpeed : -mySpeed;

                if (count > 1)
                {
                    f.Y = Math.Max(minY, Math.Min(baseY + rng.Next(-50, 51), maxY));
                    if (anywhere) f.X = baseX + rng.Next(-100, 101);
                    else f.X = baseX + (goRight ? -rng.Next(0, 200) : rng.Next(0, 200));
                }
                else
                {
                    f.Y = baseY;
                    f.X = baseX;
                }

                fishes.Add(f);
            }
            return count;
        }

        // called whenever a fish leaves the game (caught, eaten, stolen...)
        void OnFishRemoved(Fish f)
        {
            if (f.Slot != null)
            {
                // Abyss fish: its slot goes on cooldown (rarer fish take longer)
                f.Slot.Active = null;
                f.Slot.Cooldown = f.Slot.Sp.CdSec * 60 / CdDiv;
            }
            else if (!f.Extra)
                pending[f.Zone]++;
        }

        // ---------- Abyss: 10 active, the rest in reserve ----------
        void BuildAbyssSlots()
        {
            abyssSlots.Clear();
            Species[] list = zoneSpecies[ZoneCount - 1];
            for (int s = 0; s < list.Length; s++)
                for (int i = 0; i < abyssCounts[s]; i++)
                {
                    AbyssSlot slot = new AbyssSlot();
                    slot.Sp = list[s];
                    abyssSlots.Add(slot);
                }

            AbyssSlot leg = new AbyssSlot();
            leg.Sp = legendary;
            abyssSlots.Add(leg);
        }

        void ProcessAbyss(bool anywhere, int maxSpawns)
        {
            int active = 0;
            readyTmp.Clear();
            for (int i = 0; i < abyssSlots.Count; i++)
            {
                AbyssSlot s = abyssSlots[i];
                if (s.Active != null) active++;
                else if (s.Cooldown <= 0) readyTmp.Add(s);
            }

            int spawned = 0;
            while (active < AbyssActive && readyTmp.Count > 0 && spawned < maxSpawns)
            {
                int k = rng.Next(readyTmp.Count);
                SpawnSlotFish(readyTmp[k], anywhere);
                readyTmp.RemoveAt(k);
                active++;
                spawned++;
            }
        }

        void SpawnSlotFish(AbyssSlot s, bool anywhere)
        {
            Species sp = s.Sp;
            int zone = ZoneCount - 1;

            Fish f = MakeFish(zone, sp, false);
            f.Slot = s;
            s.Active = f;

            bool goRight = rng.Next(2) == 0;
            float speed = sp.Speed + (float)rng.NextDouble() * 0.8f;
            f.BaseSpeed = speed;
            f.Speed = goRight ? speed : -speed;

            int minY = ZoneTopY(zone) + 12;
            int maxY = Math.Max(minY + 1, ZoneBottomY(zone) - sp.Size - 6);
            float x = 0f, y = 0f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                y = rng.Next(minY, maxY);
                x = anywhere ? rng.Next(0, WorldW) : (goRight ? -sp.Size * 3 : WorldW + sp.Size);
                if (SpotFree(zone, x, y, spawnGap[zone])) break;
            }
            f.X = x;
            f.Y = y;

            fishes.Add(f);
        }

        void TickAbyssCooldowns()
        {
            for (int i = 0; i < abyssSlots.Count; i++)
            {
                AbyssSlot s = abyssSlots[i];
                if (s.Active == null && s.Cooldown > 0) s.Cooldown--;
            }
        }

        void ProcessRespawns()
        {
            for (int z = 0; z < ZoneCount - 1; z++)
            {
                int n = Math.Min(pending[z], 40);
                if (n > 0)
                {
                    pending[z] -= n;
                    SpawnBatch(z, n, false, false);
                }

                if (weather == Weather.Clear) { pendingExtra[z] = 0; continue; }

                int e = Math.Min(pendingExtra[z], 25);
                if (e > 0)
                {
                    pendingExtra[z] -= e;
                    SpawnBatch(z, e, false, true);
                    extrasPresent = true;
                }
            }

            ProcessAbyss(false, 2);
        }

        void ExpireExtras()
        {
            Rectangle view = new Rectangle((int)cameraX - 150, (int)cameraY - 150,
                                           ClientSize.Width + 300, ClientSize.Height + 300);
            int removed = 0;
            bool any = false;

            for (int i = fishes.Count - 1; i >= 0; i--)
            {
                Fish f = fishes[i];
                if (!f.Extra) continue;

                if (!f.Hooked && removed < 25 && !view.IntersectsWith(f.Bounds))
                {
                    fishes.RemoveAt(i);
                    removed++;
                }
                else any = true;
            }
            extrasPresent = any;
        }

        int CountExtra(int zone)
        {
            int n = 0;
            foreach (Fish f in fishes)
                if (f.Extra && f.Zone == zone) n++;
            return n;
        }

        // =====================================================================
        //  WEATHER
        // =====================================================================

        void SetWeather(Weather w)
        {
            weather = w;
            weatherTicks = WeatherSeconds * 60;

            if (w == Weather.Clear)
            {
                for (int z = 0; z < ZoneCount; z++) pendingExtra[z] = 0;
                message = "The weather clears up.";
                return;
            }

            // rain = +25% fish, storm = +50% fish (the Abyss stays at its fixed 10)
            float boost = w == Weather.Rain ? 0.25f : 0.5f;
            for (int z = 0; z < ZoneCount; z++)
            {
                if (z == ZoneCount - 1) { pendingExtra[z] = 0; continue; }
                pendingExtra[z] = Math.Max(0, (int)(baseCount[z] * boost) - CountExtra(z));
            }

            message = w == Weather.Rain
                ? "It starts to rain - more fish are showing up!"
                : "A storm is rolling in! Visibility is low, but there are lots of fish.";
        }

        // fully random every time: 40% Clear, 35% Rain, 25% Storm
        void NextWeather()
        {
            int roll = rng.Next(100);
            Weather next = roll < 40 ? Weather.Clear
                         : roll < 75 ? Weather.Rain
                         : Weather.Storm;

            if (next == weather)
            {
                weatherTicks = WeatherSeconds * 60;
                message = "The weather stays the same.";
                return;
            }
            SetWeather(next);
        }

        // =====================================================================
        //  HAZARDS
        // =====================================================================

        int CountHazardsNear(float x, float y, Hazard exclude)
        {
            int n = 0;
            float halfW = ClientSize.Width / 2f;
            float halfH = ClientSize.Height / 2f;
            for (int i = 0; i < hazards.Count; i++)
            {
                Hazard o = hazards[i];
                if (o == exclude) continue;
                if (Math.Abs(o.X - x) < halfW && Math.Abs(o.Y - y) < halfH) n++;
            }
            return n;
        }

        void SpawnHazards()
        {
            for (int z = 0; z < ZoneCount; z++)
            {
                for (int i = 0; i < jellyCount[z]; i++) AddHazard(0, z, false);
                for (int i = 0; i < mineCount[z]; i++) AddHazard(1, z, false);
            }
        }

        bool AddHazard(int kind, int zone, bool avoidHook)
        {
            var h = new Hazard();
            h.Kind = kind;
            h.Zone = zone;
            h.W = kind == 0 ? JellyW : MineW;
            h.H = kind == 0 ? JellyH : MineH;

            int minY = Math.Max(ZoneTopY(zone) + 70, WaterTop + 150);   // never right at the surface
            int maxY = Math.Max(minY + 1, ZoneBottomY(zone) - h.H - 70);

            bool placed = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                h.X = rng.Next(LeftLimit, Math.Max(LeftLimit + 1, WorldW - 80));
                h.BaseY = rng.Next(minY, maxY);
                h.Y = h.BaseY;

                if (RectHitsRock(Rectangle.Inflate(h.Bounds, 10, 60))) continue;
                if (avoidHook && Math.Abs(h.X - hookX) < 600 && Math.Abs(h.BaseY - hookY) < 450) continue;
                if (CountHazardsNear(h.X, h.BaseY, null) >= HazardCap) continue;
                placed = true;
                break;
            }
            if (!placed) return false;

            h.Phase = (float)(rng.NextDouble() * Math.PI * 2);
            h.Speed = kind == 0 ? (rng.Next(2) == 0 ? 0.8f : -0.8f) : 0f;
            hazards.Add(h);
            return true;
        }

        int LoseHooked(int count)
        {
            List<Fish> hooked = HookedList();
            count = Math.Min(count, hooked.Count);
            for (int i = 0; i < count; i++)
            {
                Fish f = hooked[rng.Next(hooked.Count)];
                hooked.Remove(f);
                fishes.Remove(f);
                OnFishRemoved(f);
            }
            return count;
        }

        void UpdateHazards(bool hookActive)
        {
            if (hitTicks > 0) hitTicks--;

            Rectangle hook = new Rectangle((int)hookX - 4, (int)hookY - 4, HookSize + 8, HookSize + 8);

            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = hazards[i];
                h.Phase += 0.03f;

                if (h.Kind == 0)
                {
                    // drift sideways, but turn around instead of piling into a full screen
                    float nx = h.X + h.Speed;
                    if (CountHazardsNear(nx, h.BaseY, h) >= HazardCap) h.Speed = -h.Speed;
                    else h.X = nx;

                    if (h.X > WorldW) h.X = -h.W;
                    if (h.X < -h.W) h.X = WorldW;
                    h.Y = h.BaseY + (float)Math.Sin(h.Phase) * 50f;
                }
                else
                    h.Y = h.BaseY + (float)Math.Sin(h.Phase) * 6f;

                if (!hookActive || hitTicks > 0) continue;
                if (!h.Bounds.IntersectsWith(hook)) continue;

                hitTicks = HitImmunity;
                sinkBonus = 0f;
                tapReel = 0f;

                float before = hookY;

                if (h.Kind == 0)
                {
                    int lost = LoseHooked(1);
                    hookY = Math.Max(WaterTop + 5, hookY - 80);
                    message = lost > 0 ? "Jellyfish shock! You lost a fish." : "Jellyfish shock!";
                }
                else
                {
                    int lost = LoseHooked((int)Math.Ceiling(HookedCount() * 0.5));
                    hookY = Math.Max(WaterTop + 5, hookY - 150);
                    message = lost > 0 ? "BOOM! The mine blew away " + lost + " fish!" : "BOOM! Sea mine!";
                    hazards.RemoveAt(i);
                    AddHazard(1, h.Zone, true);   // a new mine appears somewhere else in that zone
                }

                if (HookHitsRock(hookX, hookY)) hookY = before;   // never get knocked into a rock
            }
        }

        // =====================================================================
        //  PREDATORS
        // =====================================================================

        int RollPredTime(int z)
        {
            int sec = rng.Next(predMinutes[z, 0] * 60, predMinutes[z, 1] * 60 + 1) / PredDiv;
            return Math.Max(3, sec) * 60;
        }

        // the Piranha is harmless once your line is strong enough (10 upgrades), the Eel at 20
        bool PredImmune(int z)
        {
            if (z == 0) return lineUpgrades >= 10;
            if (z == 1) return lineUpgrades >= 20;
            return false;
        }

        float PiranhaStealChance() { return Math.Max(0f, 1f - 0.1f * lineUpgrades); }
        float EelCutChance() { return Math.Max(0f, 0.60f - 0.03f * lineUpgrades); }
        float SharkFraction() { return Math.Max(0.30f, 0.75f - 0.015f * lineUpgrades); }

        void ClearPredator(int z)
        {
            predBusy[z] = false;
            predElapsed[z] = 0;
            predNeeded[z] = RollPredTime(z);
        }

        void ResetPredatorState()
        {
            predators.Clear();
            warnTicks = 0;
            for (int z = 0; z < ZoneCount; z++)
                if (predBusy[z]) ClearPredator(z);
        }

        void SpawnPredator(int zone)
        {
            var p = new Predator();
            p.Name = predNames[zone];
            p.Zone = zone;
            p.W = predW[zone];
            p.H = predH[zone];

            // predators swim in from just off the edge of the screen
            bool fromLeft = rng.Next(2) == 0;
            p.Speed = fromLeft ? predSpeed[zone] : -predSpeed[zone];
            float edge = ClientSize.Width / 2f + p.W;
            p.X = fromLeft ? hookX - edge : hookX + edge;
            p.Y = hookY - p.H / 2f;

            predators.Add(p);
            message = p.Name + " is coming!";
        }

        void PredatorSteal(Predator p, int take)
        {
            List<Fish> hooked = HookedList();
            if (hooked.Count == 0) return;

            take = Math.Min(take, hooked.Count);
            for (int i = 0; i < take; i++)
            {
                Fish f = hooked[rng.Next(hooked.Count)];
                hooked.Remove(f);
                fishes.Remove(f);
                OnFishRemoved(f);
            }

            message = p.Name + " stole " + take + " fish!";
        }

        void PredatorAttack(Predator p)
        {
            int hooked = HookedCount();

            if (p.Zone == 0)
            {
                // Piranha: steals 1 fish, less likely with a stronger line
                if (rng.NextDouble() < PiranhaStealChance()) PredatorSteal(p, 1);
                else message = "The Piranha tried to bite your catch but missed!";
            }
            else if (p.Zone == 1)
            {
                // Eel: never steals, it only tries to cut the line
                if (rng.NextDouble() < EelCutChance()) SnapLine("The Eel");
                else message = "The Eel tried to cut your line but it held!";
            }
            else if (p.Zone == 2)
            {
                // Shark: takes a part of your catch, less with a stronger line
                int take = Math.Max(1, (int)Math.Round(hooked * SharkFraction()));
                PredatorSteal(p, take);
            }
            else
            {
                // ???: takes your whole catch, always
                PredatorSteal(p, hooked);
            }
        }

        void UpdatePredators(bool hookActive, Rectangle hookRect)
        {
            bool protectedNow = graceTicks > 0;
            int hookZone = hookActive ? CurrentZone() : -1;

            // the timer of a zone only runs while your hook is in that zone
            if (hookZone >= 0 && !protectedNow && !PredImmune(hookZone) && !predBusy[hookZone])
            {
                predElapsed[hookZone]++;
                if (predElapsed[hookZone] >= predNeeded[hookZone] && warnTicks == 0 && predators.Count == 0)
                {
                    predBusy[hookZone] = true;
                    warnTicks = WarnTime;
                    warnZone = hookZone;
                    message = "WARNING: something is coming! Reel up or leave this zone!";
                }
            }

            if (warnTicks > 0)
            {
                if (!hookActive || hookZone != warnZone || protectedNow)
                {
                    warnTicks = 0;
                    ClearPredator(warnZone);
                    if (hookActive) message = "You got away!";
                }
                else
                {
                    warnTicks--;
                    if (warnTicks == 0 && predators.Count == 0)
                        SpawnPredator(warnZone);
                }
            }

            for (int i = predators.Count - 1; i >= 0; i--)
            {
                Predator p = predators[i];

                if (!hookActive || hookZone != p.Zone)
                {
                    ClearPredator(p.Zone);
                    predators.RemoveAt(i);
                    continue;
                }

                p.X += p.Speed;

                if (!p.Done)
                {
                    float targetY = hookY - p.H / 2f;
                    float minY = ZoneTopY(p.Zone);
                    float maxY = ZoneBottomY(p.Zone) - p.H;
                    targetY = Math.Max(minY, Math.Min(targetY, maxY));
                    p.Y += (targetY - p.Y) * 0.4f;
                }

                // an empty hook is safe: the predator just swims past
                if (!protectedNow && !p.Done && HookedCount() > 0
                    && Rectangle.Inflate(p.Bounds, 0, 15).IntersectsWith(hookRect))
                {
                    PredatorAttack(p);
                    if (state == State.Idle) return;   // the line was cut, everything got reset
                    p.Done = true;
                }

                // gone once it has swum well past the screen -> a new random wait starts
                if (Math.Abs(p.X - hookX) > ClientSize.Width + p.W * 3)
                {
                    ClearPredator(p.Zone);
                    predators.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        //  HUNTER FISH + SEPARATION
        // =====================================================================

        void HuntStep(Fish f, int index)
        {
            if (f.Target != null && (f.Target.Dead || f.Target.Hooked)) f.Target = null;

            if (f.Target == null)
            {
                f.Speed = Math.Sign(f.Speed) * f.BaseSpeed;

                // look for prey only every 10 ticks (staggered between hunters)
                if ((index + frame) % 10 != 0) return;

                float best = HuntRange * HuntRange;
                foreach (Fish p in fishes)
                {
                    if (p == f || p.Hooked || p.Dead || p.Behavior == Hunter || p.Zone != f.Zone) continue;
                    if (p.Weight > f.Weight * PreyMaxRatio) continue;

                    float dx = p.X - f.X, dy = p.Y - f.Y;
                    float d = dx * dx + dy * dy;
                    if (d < best) { best = d; f.Target = p; }
                }
                return;
            }

            Fish t = f.Target;
            float tx = t.X - f.X, ty = t.Y - f.Y;
            if (tx * tx + ty * ty > HuntRange * HuntRange * 2.25f) { f.Target = null; return; }

            // chase
            f.Speed = (t.X > f.X ? 1f : -1f) * f.BaseSpeed * 1.7f;
            f.Y += Math.Sign(t.Y - f.Y) * Math.Min(Math.Abs(t.Y - f.Y), 2.5f);
            f.Y = Math.Max(ZoneTopY(f.Zone) + 6, Math.Min(f.Y, ZoneBottomY(f.Zone) - f.Size - 6));

            if (f.Bounds.IntersectsWith(t.Bounds))
            {
                t.Dead = true;
                eatenTmp.Add(t);
                f.Target = null;
            }
        }

        // gently pushes overlapping fish apart (up / down) so they don't pile up
        void SeparateFish(Rectangle area)
        {
            for (int i = 0; i < usedCells.Count; i++) grid[usedCells[i]].Clear();
            usedCells.Clear();

            for (int i = 0; i < fishes.Count; i++)
            {
                Fish f = fishes[i];
                if (f.Dead || f.Hooked) continue;

                int px = (int)f.X, py = (int)f.Y;
                if (!area.Contains(px, py)) continue;

                int cx = px / CellSize, cy = py / CellSize;
                if (cx < 0 || cy < 0 || cx >= gridW || cy >= gridH) continue;

                int key = cy * gridW + cx;
                List<Fish> cell = grid[key];
                if (cell == null) { cell = new List<Fish>(4); grid[key] = cell; }
                if (cell.Count == 0) usedCells.Add(key);
                cell.Add(f);
            }

            for (int u = 0; u < usedCells.Count; u++)
            {
                int key = usedCells[u];
                int cx = key % gridW, cy = key / gridW;
                List<Fish> cell = grid[key];

                for (int i = 0; i < cell.Count; i++)
                {
                    Fish a = cell[i];
                    if (a.Target != null) continue;     // hunters that are chasing keep their course
                    Rectangle ra = a.Bounds;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = cx + dx, ny = cy + dy;
                            if (nx < 0 || ny < 0 || nx >= gridW || ny >= gridH) continue;

                            List<Fish> other = grid[ny * gridW + nx];
                            if (other == null) continue;

                            for (int j = 0; j < other.Count; j++)
                            {
                                Fish b = other[j];
                                if (b == a || b.Zone != a.Zone) continue;
                                if (!ra.IntersectsWith(b.Bounds)) continue;

                                float dir = a.Y > b.Y ? 1f : (a.Y < b.Y ? -1f : (a.Phase > b.Phase ? 1f : -1f));
                                a.Y += dir * 0.9f;
                            }
                        }
                    }

                    a.Y = Math.Max(ZoneTopY(a.Zone) + 6, Math.Min(a.Y, ZoneBottomY(a.Zone) - a.Size - 6));
                }
            }
        }

        // =====================================================================
        //  CASTING
        // =====================================================================

        float GetTargetX()
        {
            float minDist = 50f;
            float dist = minDist + power * (MaxThrow - minDist);
            float x = playerX + facing * dist;
            return Math.Max(LeftLimit, Math.Min(x, RightLimit - HookSize));
        }

        void StartCharging()
        {
            state = State.Charging;
            power = 0f;
            powerDir = 1f;
            message = "A/D = turn   Release SPACE to throw!";
        }

        void ThrowRod()
        {
            lastPower = power;
            state = State.Throwing;
            throwT = 0f;
            throwStartX = hookX;
            throwStartY = hookY;
            throwTargetX = GetTargetX();
            throwArc = 60 + power * 80;
            message = "";
        }

        void FinishCast()
        {
            var counts = new Dictionary<string, int>();
            int caught = 0, value = 0;
            bool questFish = false;

            for (int i = fishes.Count - 1; i >= 0; i--)
            {
                if (!fishes[i].Hooked) continue;

                Fish f = fishes[i];
                caught++;
                value += f.Value;
                if (IsQuestFish(f.Name)) questFish = true;

                int c;
                counts.TryGetValue(f.Name, out c);
                counts[f.Name] = c + 1;

                fishes.RemoveAt(i);
                OnFishRemoved(f);
            }

            bagCount += caught;
            bagValue += value;

            if (caught == 0)
                message = "No fish this time. Hold SPACE to throw again!";
            else
            {
                var parts = new List<string>();
                foreach (var kv in counts) parts.Add(kv.Key + " x" + kv.Value);
                message = "Caught: " + string.Join(", ", parts.ToArray()) + "!";
            }

            // boat quest: land a Pike, Salmon or Catfish
            if (questFish && !boatOwned)
            {
                boatOwned = true;
                message += " QUEST DONE - the boat is yours! Walk to the end of the dock and press E.";
            }

            ResetPredatorState();
            tapReel = 0f;
            atLineEnd = false;
            state = State.Idle;
        }

        void SnapLine(string who)
        {
            for (int i = fishes.Count - 1; i >= 0; i--)
            {
                if (!fishes[i].Hooked) continue;
                Fish f = fishes[i];
                fishes.RemoveAt(i);
                OnFishRemoved(f);
            }

            ResetPredatorState();
            tapReel = 0f;
            atLineEnd = false;
            state = State.Idle;
            graceTicks = GraceTime;
            message = who + " snapped your line! You lost your catch. (Protected for 5s)";
        }

        // pulls the hook up; if a rock ledge is above it, it slides sideways out from under it instead
        void MoveHookUp(float dist)
        {
            float ny = hookY - dist;
            if (HookHitsRock(hookX, ny))
            {
                hookX += (hookX < WorldW / 2f) ? 6f : -6f;
                hookX = Math.Max(LeftLimit, Math.Min(hookX, RightLimit - HookSize));
            }
            else
                hookY = ny;
        }

        // =====================================================================
        //  SHOPS + INTERACTION
        // =====================================================================

        void Sell()
        {
            if (bagCount == 0) { message = "You have no fish to sell."; return; }
            money += bagValue;
            message = "Sold " + bagCount + " fish for $" + bagValue + "!";
            bagCount = 0;
            bagValue = 0;
        }

        void BuyCurrent()
        {
            if (shopTab == 0) BuyRodAt(shopSel);
            else if (shopTab == 1) Upgrade();
            else BuyGlow();
        }

        void OpenGear()
        {
            shopOpen = true;
            shopTab = 0;
            shopSel = Math.Min(rodLevel + 1, rodNames.Length - 1);
            message = "";
        }

        // 0 = sell, 1 = gear shop, -1 = none
        int NearestShop()
        {
            Rectangle[] boxes = { SellBox, GearBox };
            int best = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < boxes.Length; i++)
            {
                float centerX = boxes[i].X + boxes[i].Width / 2f;
                float d = Math.Abs(playerX - centerX);
                if (d < boxes[i].Width / 2f + NearDistance && d < bestDist)
                {
                    best = i;
                    bestDist = d;
                }
            }
            return best;
        }

        // 0 = sell, 1 = gear shop, 2 = boat, -1 = nothing in reach
        int NearestSpot()
        {
            if (onBoat) return BoatDocked ? 2 : -1;
            int shop = NearestShop();
            if (shop >= 0) return shop;
            return NearBoat() ? 2 : -1;
        }

        void Interact()
        {
            if (state != State.Idle) return;
            if (onBoat) { UseBoat(); return; }

            int spot = NearestSpot();
            if (spot == 0) Sell();
            else if (spot == 1) OpenGear();
            else if (spot == 2) UseBoat();
        }

        // =====================================================================
        //  DEV PANEL
        // =====================================================================

        void DevMoney() { money += 1000000; message = "DEV: +$1,000,000"; }

        void DevRod()
        {
            if (rodLevel < rodNames.Length - 1)
            {
                rodLevel++;
                message = "DEV: " + rodNames[rodLevel] + " (" + RodCapacity + " kg)";
            }
        }

        void DevLine()
        {
            lineUpgrades = MaxUpgrades;
            message = "DEV: line fully upgraded (reaches the seabed)";
        }

        void DevGlow() { hasGlowBait = true; message = "DEV: Glow Bait unlocked"; }
        void DevBoat() { boatOwned = true; message = "DEV: boat unlocked"; }

        void DevSeeAll()
        {
            devSeeAll = !devSeeAll;
            message = "DEV: see all fish " + (devSeeAll ? "ON" : "OFF");
        }

        void DevPredator()
        {
            int pz = (state == State.Sinking || state == State.Reeling) ? CurrentZone() : -1;
            if (pz >= 0 && !PredImmune(pz) && !predBusy[pz])
            {
                predElapsed[pz] = predNeeded[pz];
                message = "DEV: the predator of this zone is about to show up";
            }
            else
                message = "DEV: hook must be in the water, in a zone whose predator isn't immune";
        }

        void AddDev(string label, Action click, Func<string> dynamic = null)
        {
            devButtons.Add(new DevButton { Label = label, Click = click, Dynamic = dynamic });
        }

        void BuildDevButtons()
        {
            AddDev("+$1,000,000", DevMoney);
            AddDev("Free rod upgrade", DevRod);
            AddDev("Buy next rod", BuyRod);
            AddDev("Max line", DevLine);
            AddDev("Glow bait", DevGlow);
            AddDev("Unlock boat", DevBoat);
            AddDev("", DevSeeAll, () => "See all fish: " + (devSeeAll ? "ON" : "OFF"));
            AddDev("Next weather", NextWeather);
            AddDev("Predator now", DevPredator);
        }

        Rectangle DevPanel
        {
            get
            {
                int h = devOpen ? 34 + devButtons.Count * 30 + 6 : 28;
                return new Rectangle(ClientSize.Width - 165, 106, 155, h);
            }
        }
        Rectangle DevHeader { get { return new Rectangle(DevPanel.X, DevPanel.Y, DevPanel.Width, 28); } }
        Rectangle DevBtn(int i) { return new Rectangle(DevPanel.X + 8, DevPanel.Y + 34 + i * 30, DevPanel.Width - 16, 26); }

        // returns true if the click was on the dev panel
        bool HandleDevClick(Point p)
        {
            if (!DevPanel.Contains(p)) return false;

            if (DevHeader.Contains(p)) { devOpen = !devOpen; return true; }
            if (!devOpen) return true;

            for (int i = 0; i < devButtons.Count; i++)
                if (DevBtn(i).Contains(p)) { devButtons[i].Click(); break; }
            return true;
        }

        void DrawDevPanel(Graphics g)
        {
            Rectangle p = DevPanel;
            g.FillRectangle(GetBrush(Color.FromArgb(225, 25, 25, 35)), p);
            g.DrawRectangle(Pens.Orange, p);
            g.DrawString("DEV  " + (devOpen ? "[-]" : "[+]"), fontSmall, Brushes.Orange, DevHeader, centerFmt);

            if (!devOpen) return;

            for (int i = 0; i < devButtons.Count; i++)
            {
                DevButton b = devButtons[i];
                string text = b.Dynamic != null ? b.Dynamic() : b.Label;
                DrawButton(g, DevBtn(i), text, Color.FromArgb(255, 70, 70, 100), fontLabel);
            }
        }

        // =====================================================================
        //  INPUT
        // =====================================================================

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (shopOpen)
            {
                if (e.KeyCode == Keys.Escape) shopOpen = false;
                else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) shopTab = Math.Max(0, shopTab - 1);
                else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) shopTab = Math.Min(2, shopTab + 1);
                else if (shopTab == 0 && (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)) shopSel = Math.Max(0, shopSel - 1);
                else if (shopTab == 0 && (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)) shopSel = Math.Min(rodNames.Length - 1, shopSel + 1);
                else if (e.KeyCode == Keys.Enter) BuyCurrent();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) leftPressed = true;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) rightPressed = true;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) downPressed = true;

            if (e.KeyCode == Keys.E) Interact();

            if ((e.KeyCode == Keys.W || e.KeyCode == Keys.Up) && !upPressed)
            {
                upPressed = true;
                if (state == State.Sinking) { tapReel = TapReelDist; sinkBonus = 0f; }
            }

            if (Dev) HandleDevKeys(e);

            if (e.KeyCode == Keys.Space)
            {
                if (state == State.Idle)
                    StartCharging();
                else if (state == State.Sinking)
                {
                    state = State.Reeling;
                    message = "Reeling up!";
                }
                e.SuppressKeyPress = true;
            }
        }

        void HandleDevKeys(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.M) DevMoney();
            if (e.KeyCode == Keys.B) BuyRod();
            if (e.KeyCode == Keys.R) DevRod();
            if (e.KeyCode == Keys.L) DevLine();
            if (e.KeyCode == Keys.G) DevGlow();
            if (e.KeyCode == Keys.V) DevBoat();
            if (e.KeyCode == Keys.F) DevSeeAll();
            if (e.KeyCode == Keys.T) NextWeather();
            if (e.KeyCode == Keys.P) DevPredator();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) leftPressed = false;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) rightPressed = false;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) downPressed = false;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) upPressed = false;

            if (e.KeyCode == Keys.Space && state == State.Charging)
                ThrowRod();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            mousePos = e.Location;
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!shopOpen || shopTab != 0) return;
            if (e.Delta > 0) shopSel = Math.Max(0, shopSel - 1);
            else if (e.Delta < 0) shopSel = Math.Min(rodNames.Length - 1, shopSel + 1);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (Dev && HandleDevClick(e.Location)) return;

            // clicking a shop box / the boat (while standing next to it)
            if (!shopOpen)
            {
                if (state != State.Idle) return;

                Point w = new Point((int)(e.X + cameraX), (int)(e.Y + cameraY));
                if (onBoat) { if (BoatRect.Contains(w)) UseBoat(); return; }

                int near = NearestSpot();
                if (near == 0 && SellBox.Contains(w)) Sell();
                else if (near == 1 && GearBox.Contains(w)) OpenGear();
                else if (near == 2 && BoatRect.Contains(w)) UseBoat();
                return;
            }

            // gear menu
            if (GearClose.Contains(e.Location)) { shopOpen = false; return; }

            for (int i = 0; i < 3; i++)
                if (GearTab(i).Contains(e.Location)) { shopTab = i; return; }

            if (shopTab == 0)
            {
                for (int i = 0; i < rodNames.Length; i++)
                    if (GearRow(i).Contains(e.Location)) { shopSel = i; return; }
            }

            if (GearBuyBtn.Contains(e.Location)) { BuyCurrent(); return; }

            if (!GearPanel.Contains(e.Location)) shopOpen = false;
        }

        // =====================================================================
        //  GAME LOOP
        // =====================================================================

        private void GameLoop(object sender, EventArgs e)
        {
            frame++;
            if (graceTicks > 0) graceTicks--;

            TickAbyssCooldowns();
            UpdateWalking();
            UpdatePower();
            UpdateRod();
            UpdateThrow();
            UpdateHook();
            UpdateCamera();

            Rectangle hookRect = new Rectangle((int)hookX - 12, (int)hookY - 12, HookSize + 24, HookSize + 24);
            bool hookActive = state == State.Sinking || state == State.Reeling;

            UpdateZoneBanner(hookActive);

            UpdateFish(hookActive, hookRect);
            UpdateWorldTimers();

            UpdateHazards(hookActive);
            UpdatePredators(hookActive, hookRect);
            UpdateChests(hookActive, hookRect);
            UpdateCorals(hookActive, hookRect);

            Invalidate();
        }

        // walking on the dock / sailing the boat (only when idle) and turning
        void UpdateWalking()
        {
            if (state == State.Idle)
            {
                if (shopOpen) return;

                if (onBoat)
                {
                    if (leftPressed) { boatX -= BoatSpeed; facing = -1; }
                    if (rightPressed) { boatX += BoatSpeed; facing = 1; }
                    boatX = Math.Max(MooredX, Math.Min(boatX, WorldW - 120 - BoatW));
                    playerX = boatX + BoatW / 2f;
                }
                else
                {
                    boatX = MooredX;
                    if (leftPressed) { playerX -= WalkSpeed; facing = -1; }
                    if (rightPressed) { playerX += WalkSpeed; facing = 1; }
                    playerX = Math.Max(20, Math.Min(playerX, DockW - 20));
                }
            }
            else if (state == State.Charging)
            {
                if (leftPressed) facing = -1;
                if (rightPressed) facing = 1;
            }
        }

        // power meter bounces back and forth
        void UpdatePower()
        {
            if (state != State.Charging) return;

            power += 0.02f * powerDir;
            if (power >= 1f) { power = 1f; powerDir = -1f; }
            if (power <= 0f) { power = 0f; powerDir = 1f; }
        }

        void UpdateRod()
        {
            float targetAngle;
            float ease;
            switch (state)
            {
                case State.Charging:
                    targetAngle = 25f - power * 75f;
                    ease = 0.5f;
                    break;
                case State.Throwing:
                    targetAngle = throwT < 0.5f ? 75f : 45f;
                    ease = 0.35f;
                    break;
                case State.Sinking:
                case State.Reeling:
                    targetAngle = 45f;
                    ease = 0.15f;
                    break;
                default:
                    targetAngle = 25f;
                    ease = 0.2f;
                    break;
            }
            rodAngle += (targetAngle - rodAngle) * ease;

            // hook dangles from the rod until it is thrown
            if (state == State.Idle || state == State.Charging)
                PlaceHookAtTip();
        }

        void UpdateThrow()
        {
            if (state != State.Throwing) return;

            throwT += 0.03f;
            float t = Math.Min(throwT, 1f);

            hookX = throwStartX + (throwTargetX - throwStartX) * t;
            hookY = throwStartY + (WaterTop - throwStartY) * t - throwArc * 4f * t * (1f - t);

            if (throwT >= 1f)
            {
                state = State.Sinking;
                hookX = throwTargetX;
                hookY = WaterTop + 5;
                sinkBonus = 0f;
                tapReel = 0f;
                atLineEnd = false;
                RerollChests();
                Array.Clear(zoneSeen, 0, ZoneCount);
                zoneBannerTicks = 0;
                message = "A/D = steer   S = sink faster   W = tap/hold up   SPACE = reel up";
            }
        }

        void UpdateHook()
        {
            if (state != State.Sinking && state != State.Reeling) return;

            float oldX = hookX, oldY = hookY;

            if (leftPressed) hookX -= 4;
            if (rightPressed) hookX += 4;
            hookX = Math.Max(LeftLimit, Math.Min(hookX, RightLimit - HookSize));
            if (HookHitsRock(hookX, hookY)) hookX = oldX;   // rocks block sideways movement

            if (state != State.Sinking) tapReel = 0f;

            if (state == State.Sinking)
                SinkStep(oldY);
            else
            {
                MoveHookUp(ReelSpeed);
                if (hookY <= WaterTop) FinishCast();
            }
        }

        void SinkStep(float oldY)
        {
            float depthLimitY = WaterTop + LineLength;
            bool movedUp = false;

            if (tapReel > 0f)
            {
                float step = Math.Min(tapReel, ReelSpeed);
                MoveHookUp(step);
                tapReel -= step;
                movedUp = true;
            }
            else if (upPressed)
            {
                sinkBonus = 0f;                 // holding W resets the build-up
                MoveHookUp(HoldReelSpeed);
                movedUp = true;
            }
            else
            {
                float sinkNow = SinkSpeed + sinkBonus;
                sinkBonus = Math.Min(MaxSinkBonus, sinkBonus + SinkAccel);
                hookY += downPressed ? FastSinkSpeed : sinkNow;
            }

            if (hookY <= WaterTop)
            {
                FinishCast();   // pulled all the way out of the water: the cast is over
                return;
            }

            // rocks block sinking: the hook stops, steer around (A/D)
            if (!movedUp && hookY > oldY && HookHitsRock(hookX, hookY))
            {
                hookY = oldY;
                sinkBonus = 0f;
                if (warnTicks == 0) message = "A rock is in the way! Steer around it (A/D) or reel up.";
            }

            // end of the line: the hook just stops here
            if (hookY >= depthLimitY)
            {
                hookY = depthLimitY;
                if (!atLineEnd)
                {
                    atLineEnd = true;
                    message = "End of your line! Reel up with SPACE or W.";
                }
            }
            else atLineEnd = false;
        }

        void UpdateCamera()
        {
            // vertical
            float targetCam = 0f;
            if (state == State.Sinking || state == State.Reeling)
                targetCam = hookY - ClientSize.Height * 0.4f;

            float maxCam = Math.Max(0, WorldBottom - ClientSize.Height);
            targetCam = Math.Max(0f, Math.Min(targetCam, maxCam));
            cameraY += (targetCam - cameraY) * 0.15f;

            // horizontal
            float targetCamX;
            if (state == State.Charging)
                targetCamX = (playerX + GetTargetX()) / 2f - ClientSize.Width / 2f;   // show the player and where the cast will land
            else if (state == State.Throwing || state == State.Sinking || state == State.Reeling)
                targetCamX = hookX + HookSize / 2f - ClientSize.Width / 2f;
            else
                targetCamX = playerX - ClientSize.Width / 2f;

            cameraX += (ClampCamX(targetCamX) - cameraX) * 0.15f;
        }

        // announces each zone once per cast, in the middle of the screen
        void UpdateZoneBanner(bool hookActive)
        {
            if (zoneBannerTicks > 0) zoneBannerTicks--;
            if (!hookActive) return;

            int z = CurrentZone();
            if (z >= 0 && !zoneSeen[z])
            {
                zoneSeen[z] = true;
                zoneBannerZone = z;
                zoneBannerTicks = ZoneBannerTime;
            }
        }

        // fish movement + hunting + catching
        void UpdateFish(bool hookActive, Rectangle hookRect)
        {
            // hunters only think when they are near the screen (keeps the game fast)
            Rectangle activeArea = new Rectangle((int)cameraX - 500, (int)cameraY - 500,
                                                 ClientSize.Width + 1000, ClientSize.Height + 1000);

            for (int i = 0; i < fishes.Count; i++)
            {
                Fish f = fishes[i];
                if (f.Dead) continue;

                if (f.Hooked)
                {
                    f.X = hookX - f.Size;
                    f.Y = hookY;
                    continue;
                }

                if (f.Behavior == Hunter && activeArea.IntersectsWith(f.Bounds))
                    HuntStep(f, i);

                f.X += f.Speed;
                f.Y += (float)Math.Sin((frame + f.Phase * 60f) * 0.04f) * 0.25f;   // small up/down wobble

                if (f.Speed > 0 && f.X > WorldW) f.X = -f.Size * 3;
                if (f.Speed < 0 && f.X < -f.Size * 3) f.X = WorldW;

                if (hookActive && f.Bounds.IntersectsWith(hookRect))
                    TryHook(f);
            }

            // keep fish near the screen from piling up
            if (frame % 2 == 0)
            {
                Rectangle sepArea = new Rectangle((int)cameraX - 200, (int)cameraY - 200,
                                                  ClientSize.Width + 400, ClientSize.Height + 400);
                SeparateFish(sepArea);
            }

            // remove fish that got eaten
            if (eatenTmp.Count > 0)
            {
                foreach (Fish d in eatenTmp)
                {
                    fishes.Remove(d);
                    OnFishRemoved(d);
                }
                eatenTmp.Clear();
            }
        }

        // the hook touched a fish: check the rod filter, the slots and the capacity
        void TryHook(Fish f)
        {
            float w = f.Weight * RegionMul(f);   // fish are heavier far from the dock

            if (!CanHook(f, w)) return;          // this rod ignores it

            if (HookedCount() >= MaxHooked)
            {
                if (warnTicks == 0) message = "Hook is full! Reel up.";
                return;
            }

            if (HookedWeight() + w > RodCapacity)
            {
                if (warnTicks == 0) message = f.Name + " (" + w.ToString("0.0") + " kg) is too heavy for your rod!";
                return;
            }

            f.Weight = w;   // the price stays fixed, only the weight changes
            f.Hooked = true;
        }

        // weather + respawns
        void UpdateWorldTimers()
        {
            weatherTicks--;
            if (weatherTicks <= 0) NextWeather();

            respawnTimer++;
            if (respawnTimer >= (weather == Weather.Clear ? 60 : 30))
            {
                respawnTimer = 0;
                ProcessRespawns();
            }
            if (weather == Weather.Clear && extrasPresent) ExpireExtras();
        }

        // =====================================================================
        //  DRAWING: WORLD OBJECTS
        // =====================================================================

        void DrawFish(Graphics g, Fish f, int alpha)
        {
            Rectangle r = f.Bounds;
            bool full = alpha >= 255;
            if (!full) alpha = Math.Max(17, alpha / 17 * 17);

            SolidBrush b = GetBrush(full ? f.Color : Color.FromArgb(alpha, f.Color));
            g.FillEllipse(b, r);

            Point[] tail;
            if (f.FacingRight)
                tail = new[] {
                    new Point(r.Left + 2, r.Top + r.Height / 2),
                    new Point(r.Left - f.Size / 2, r.Top),
                    new Point(r.Left - f.Size / 2, r.Bottom) };
            else
                tail = new[] {
                    new Point(r.Right - 2, r.Top + r.Height / 2),
                    new Point(r.Right + f.Size / 2, r.Top),
                    new Point(r.Right + f.Size / 2, r.Bottom) };
            g.FillPolygon(b, tail);

            int eyeX = f.FacingRight ? r.Right - f.Size / 2 : r.Left + f.Size / 4;
            Brush eye = full ? Brushes.Black : (Brush)GetBrush(Color.FromArgb(alpha, 0, 0, 0));
            g.FillEllipse(eye, eyeX, r.Top + f.Size / 4, 4, 4);

            // a bite mark if a coral fish got to it
            if (f.Bitten)
                g.FillEllipse(GetBrush(Color.DarkRed), r.X + r.Width / 2 - 4, r.Y + r.Height / 2 - 2, 9, 6);

            if (full && f.Name == legendary.Name)
                g.DrawEllipse(glowPen, r);
        }

        // big stepped rock wall drawn as one shape (blue-tinted rock, sandy top ledge)
        void DrawCliff(Graphics g, Cliff c)
        {
            g.FillPolygon(GetBrush(Color.FromArgb(82, 100, 126)), c.Poly);

            // dark strip under every overhang
            for (int i = 0; i < c.Steps.Count - 1; i++)
            {
                Rectangle r = c.Steps[i], n = c.Steps[i + 1];
                int x0 = c.Side == 0 ? n.Right : r.Left;
                int x1 = c.Side == 0 ? r.Right : n.Left;
                g.FillRectangle(GetBrush(Color.FromArgb(70, 0, 0, 20)), x0, r.Bottom - 10, x1 - x0, 10);
            }

            // pale sandy strip along the top ledge
            Rectangle top = c.Steps[0];
            g.FillRectangle(GetBrush(Color.FromArgb(206, 200, 172)), top.X, top.Y, top.Width, 12);
            g.FillRectangle(GetBrush(Color.FromArgb(150, 145, 122)), top.X, top.Y + 12, top.Width, 4);

            g.DrawPolygon(rockPen, c.Poly);
        }

        // reef drawn in code: dark round mounds, blocky branching coral and swaying seaweed.
        // set coralImage and that picture is drawn instead.
        void DrawCoral(Graphics g, Coral c)
        {
            Rectangle r = c.Rect;

            if (coralImage != null)
                g.DrawImage(coralImage, r);
            else
            {
                Random rnd = new Random(c.Seed);   // same seed = same look every frame
                int bottom = r.Bottom;
                int x = r.X + 4;

                while (x < r.Right - 10)
                {
                    int kind = rnd.Next(10);

                    if (kind < 4)
                    {
                        // dark round mound with speckles
                        int w = rnd.Next(28, 60);
                        if (x + w > r.Right) w = r.Right - x;
                        int h = rnd.Next(14, Math.Max(16, r.Height - 10));

                        g.FillPie(GetBrush(moundColors[rnd.Next(moundColors.Length)]), x, bottom - h, w, h * 2, 180, 180);
                        for (int k = 0; k < 8; k++)
                        {
                            int sx = x + rnd.Next(4, Math.Max(5, w - 6));
                            int sy = bottom - rnd.Next(3, h);
                            Color sc = rnd.Next(2) == 0 ? Color.FromArgb(78, 110, 92) : Color.FromArgb(20, 40, 34);
                            g.FillRectangle(GetBrush(sc), sx, sy, 3, 3);
                        }
                        x += w + rnd.Next(2, 14);
                    }
                    else if (kind < 7)
                    {
                        // blocky branching coral with colored tips
                        int h = rnd.Next(22, Math.Max(24, r.Height + 6));
                        SolidBrush bb = GetBrush(branchColors[rnd.Next(branchColors.Length)]);
                        SolidBrush tip = GetBrush(c.Color);
                        int armY = bottom - h / 2;

                        g.FillRectangle(bb, x + 8, bottom - h, 4, h);          // stem
                        g.FillRectangle(bb, x, armY, 12, 4);                    // left arm
                        g.FillRectangle(bb, x, armY - 10, 4, 12);               // left fork
                        g.FillRectangle(bb, x + 12, armY - 8, 10, 4);           // right arm
                        g.FillRectangle(bb, x + 18, armY - 16, 4, 12);          // right fork
                        g.FillRectangle(tip, x + 7, bottom - h - 3, 6, 4);
                        g.FillRectangle(tip, x - 1, armY - 13, 6, 4);
                        g.FillRectangle(tip, x + 17, armY - 19, 6, 4);

                        x += 26 + rnd.Next(2, 12);
                    }
                    else
                    {
                        // tall seaweed, a stack of small blocks that sways
                        int segs = rnd.Next(5, 10);
                        float ph = (float)rnd.NextDouble() * 6f;
                        SolidBrush wb = GetBrush(weedColors[rnd.Next(weedColors.Length)]);

                        for (int s = 0; s < segs; s++)
                        {
                            int sway = (int)(Math.Sin(frame * 0.05 + ph + s * 0.5) * s * 0.8);
                            g.FillRectangle(wb, x + sway, bottom - (s + 1) * 7, 5, 8);
                        }
                        x += 14 + rnd.Next(4, 14);
                    }
                }
            }

            if (c.Flash > 0)
            {
                // the coral fish darting out
                int dart = (45 - c.Flash) / 2;
                Rectangle body = new Rectangle(r.X + r.Width / 2 - 12 + dart, r.Y + r.Height / 2 - 6, 24, 12);
                g.FillEllipse(Brushes.OrangeRed, body);
                g.FillPolygon(Brushes.OrangeRed, new[] {
                    new Point(body.X + 2, body.Y + 6),
                    new Point(body.X - 8, body.Y),
                    new Point(body.X - 8, body.Bottom) });
                g.FillEllipse(Brushes.Black, body.Right - 8, body.Y + 3, 4, 4);
            }
        }

        void DrawChest(Graphics g, Chest c)
        {
            Rectangle r = c.Bounds;
            int lidH = r.Height / 3;
            Rectangle body = new Rectangle(r.X, r.Y + lidH, r.Width, r.Height - lidH);

            SolidBrush wood = GetBrush(Color.SaddleBrown);
            SolidBrush lid = GetBrush(Color.Sienna);

            g.FillRectangle(wood, body);
            g.DrawRectangle(goldPen, body);

            if (!c.Opened)
            {
                // soft glow so chests are easy to spot in the dark
                g.FillEllipse(GetBrush(Color.FromArgb(60, 255, 215, 0)), r.X - 14, r.Y - 14, r.Width + 28, r.Height + 28);
                g.FillRectangle(wood, body);
                g.DrawRectangle(goldPen, body);

                Rectangle top = new Rectangle(r.X, r.Y, r.Width, lidH);
                g.FillRectangle(lid, top);
                g.DrawRectangle(goldPen, top);

                g.FillRectangle(Brushes.Gold, r.X + r.Width / 2 - 4, r.Y + lidH - 4, 8, 10);
            }
            else
            {
                // gold inside, lid swung open
                g.FillRectangle(Brushes.Gold, r.X + 4, r.Y + lidH - 6, r.Width - 8, 8);
                Point[] open =
                {
                    new Point(r.X, r.Y + lidH),
                    new Point(r.Right, r.Y + lidH),
                    new Point(r.Right - 6, r.Y - 10),
                    new Point(r.X + 6, r.Y - 10)
                };
                g.FillPolygon(lid, open);
                g.DrawPolygon(goldPen, open);
            }
        }

        // jellyfish drawn in code: pulsing dome, inner glow and wavy tentacles (same hitbox as before)
        void DrawJelly(Graphics g, Hazard h)
        {
            int bellH = (int)(h.H * 0.55f);
            float pulse = (float)Math.Sin(h.Phase * 3f) * 2f;
            int bw = h.W + (int)pulse;
            int bx = (int)(h.X - pulse / 2f);
            int by = (int)h.Y;

            // tentacles first (under the dome)
            float y0 = h.Y + bellH - 2;
            float yEnd = h.Y + h.H;
            float yMid = y0 + (yEnd - y0) / 2f;
            for (int i = 0; i < 5; i++)
            {
                float tx = h.X + 5 + i * (h.W - 10) / 4f;
                float sway1 = (float)Math.Sin(h.Phase * 3f + i) * 3f;
                float sway2 = (float)Math.Sin(h.Phase * 3f + i + 1.5f) * 5f;
                g.DrawLine(jellyTentPen, tx, y0, tx + sway1, yMid);
                g.DrawLine(jellyTentPen, tx + sway1, yMid, tx + sway2, yEnd);
            }

            // dome + inner glow + two little spots
            g.FillPie(GetBrush(Color.FromArgb(185, 190, 110, 240)), bx, by, bw, bellH * 2, 180, 180);
            g.FillPie(GetBrush(Color.FromArgb(120, 240, 190, 255)), bx + 6, by + 4, bw - 12, (bellH - 4) * 2, 180, 180);
            g.FillEllipse(GetBrush(Color.FromArgb(200, 255, 255, 255)), bx + bw / 3 - 2, by + bellH / 2, 4, 4);
            g.FillEllipse(GetBrush(Color.FromArgb(200, 255, 255, 255)), bx + 2 * bw / 3 - 2, by + bellH / 2 + 3, 3, 3);
        }

        void DrawHazards(Graphics g, Rectangle view)
        {
            foreach (Hazard h in hazards)
            {
                if (!view.IntersectsWith(h.Bounds)) continue;

                if (h.Kind == 0)
                    DrawJelly(g, h);
                else
                {
                    g.FillRectangle(GetBrush(Color.FromArgb(140, 60, 20, 0)), h.Bounds);
                    g.DrawRectangle(minePen, h.Bounds);
                    g.DrawString("Mine", fontTiny, Brushes.White, h.X, h.Y - 14);
                }
            }
        }

        void DrawPredators(Graphics g)
        {
            foreach (Predator p in predators)
            {
                g.FillRectangle(GetBrush(Color.FromArgb(70, 255, 0, 0)), p.Bounds);
                g.DrawRectangle(predPen, p.Bounds);
                g.DrawString(p.Name, fontLabel, Brushes.White, p.X, p.Y - 16);
            }
        }

        void DrawShopBox(Graphics g, Rectangle r, Image img, string label, bool near)
        {
            if (img != null)
                g.DrawImage(img, r);
            else
            {
                g.FillRectangle(Brushes.Peru, r);
                g.DrawString(label, fontHud, Brushes.Black, r, centerFmt);
            }

            using (Pen p = new Pen(near ? Color.Gold : Color.Black, near ? 4 : 2))
                g.DrawRectangle(p, r);
        }

        // small dock on the left: planks + posts holding it up
        void DrawDock(Graphics g)
        {
            for (int x = 30; x < DockW; x += 130)
                g.FillRectangle(GetBrush(Color.FromArgb(70, 45, 20)), x, DockTop + 35, 10, 70);

            if (dockImage != null) g.DrawImage(dockImage, DockRect);
            else
            {
                g.FillRectangle(Brushes.SaddleBrown, DockRect);
                g.DrawRectangle(Pens.Black, DockRect);
            }
        }

        void DrawBoat(Graphics g, bool near)
        {
            int x = (int)boatX;
            int top = DockTop;
            Point[] hull =
            {
                new Point(x, top),
                new Point(x + BoatW, top),
                new Point(x + BoatW - 22, top + 28),
                new Point(x + 22, top + 28)
            };

            g.FillPolygon(GetBrush(boatOwned ? Color.Sienna : Color.DimGray), hull);
            using (Pen p = new Pen(near ? Color.Gold : Color.Black, near ? 4 : 2))
                g.DrawPolygon(p, hull);

            if (!boatOwned)
                g.DrawString("BOAT", fontLabel, Brushes.White, new Rectangle(x, top, BoatW, 28), centerFmt);
        }

        string SpotPrompt(int spot)
        {
            if (spot == 0) return "Press E (or click) to SELL";
            if (spot == 1) return "Press E (or click) to open the GEAR shop";
            if (onBoat) return "Press E to get off the boat";
            if (boatOwned) return "Press E to board your boat";
            return "Press E to buy the boat ($" + BoatPrice + ") - or land a Pike, Salmon or Catfish for free";
        }

        // =====================================================================
        //  DRAWING: WEATHER + DARKNESS
        // =====================================================================

        void DrawRain(Graphics g)
        {
            if (weather == Weather.Clear) return;

            int skyH = (int)(WaterTop - cameraY);
            if (skyH <= 4) return;

            int count = weather == Weather.Storm ? 220 : 110;
            float fall = weather == Weather.Storm ? 22f : 16f;

            g.SetClip(new Rectangle(0, 0, ClientSize.Width, skyH));
            for (int i = 0; i < count; i++)
            {
                float x = rainX[i] * ClientSize.Width;
                float y = (rainY[i] * skyH + frame * fall) % skyH;
                g.DrawLine(rainPen, x, y, x - 5, y + 16);
            }
            g.ResetClip();
        }

        // how dark it is at a given world Y (0 = bright, ~1 = pitch black)
        float DepthDarkness(float y)
        {
            float d = y - WaterTop;
            if (d <= 0f) return 0f;

            for (int i = 0; i < darkDepth.Length - 1; i++)
            {
                if (d <= darkDepth[i + 1])
                {
                    float t = (d - darkDepth[i]) / (darkDepth[i + 1] - darkDepth[i]);
                    return darkLevels[i] + (darkLevels[i + 1] - darkLevels[i]) * t;
                }
            }
            return darkLevels[darkLevels.Length - 1];
        }

        // darker the deeper you go; storms limit your view near the surface;
        // the Glow Bait makes the circle of light around the hook much bigger
        void UpdateLighting()
        {
            float centerY = cameraY + ClientSize.Height / 2f;
            float depthDark = DepthDarkness(centerY);

            float stormF = 0f;
            if (weather == Weather.Storm)
                stormF = Math.Max(0f, 1f - (centerY - WaterTop) / 1500f);

            darkLevel = Math.Min(0.96f, depthDark + stormF * 0.78f);

            float r = hasGlowBait ? GlowRadius : BaseLightRadius;
            if (stormF > 0.02f) r = Math.Max(r, 250f);
            lightRadius = r;
        }

        // where the light is, in world coordinates (follows the hook, or the player on the dock / boat)
        PointF LightWorldPos()
        {
            if (state == State.Idle || state == State.Charging)
                return new PointF(playerX, DockTop - 20);
            return new PointF(hookX + HookSize / 2f, hookY + HookSize / 2f);
        }

        void DrawDarkness(Graphics g)
        {
            int alpha = (int)Math.Min(245f, darkLevel * 255f);
            if (alpha <= 2) return;

            float radius = lightRadius;
            Rectangle screen = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

            PointF lp = LightWorldPos();
            float lx = lp.X - cameraX;
            float ly = lp.Y - cameraY;

            using (GraphicsPath ell = new GraphicsPath())
            {
                ell.AddEllipse(lx - radius, ly - radius, radius * 2, radius * 2);

                // everything outside the circle
                using (GraphicsPath full = new GraphicsPath())
                {
                    full.AddRectangle(screen);
                    full.AddPath(ell, false);
                    g.FillPath(GetBrush(Color.FromArgb(alpha, 0, 0, 0)), full);
                }

                // soft edge inside the circle
                using (PathGradientBrush pg = new PathGradientBrush(ell))
                {
                    pg.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    pg.SurroundColors = new Color[] { Color.FromArgb(alpha, 0, 0, 0) };
                    pg.FocusScales = new PointF(0.3f, 0.3f);
                    g.FillPath(pg, ell);
                }
            }
        }

        // the zone name in the middle of the screen, once per zone per cast
        void DrawZoneBanner(Graphics g)
        {
            if (zoneBannerTicks <= 0 || zoneBannerZone < 0) return;

            int elapsed = ZoneBannerTime - zoneBannerTicks;
            float a = Math.Min(1f, Math.Min(elapsed / 20f, zoneBannerTicks / 30f));   // fade in, fade out
            int alpha = (int)(255 * a);

            Rectangle r = new Rectangle(0, ClientSize.Height / 2 - 60, ClientSize.Width, 120);
            Rectangle shadow = new Rectangle(r.X + 3, r.Y + 3, r.Width, r.Height);
            string text = zoneNames[zoneBannerZone].ToUpper();

            g.DrawString(text, fontBanner, GetBrush(Color.FromArgb(alpha / 2, 0, 0, 0)), shadow, centerFmt);
            g.DrawString(text, fontBanner, GetBrush(Color.FromArgb(alpha, 255, 255, 255)), r, centerFmt);
        }

        // =====================================================================
        //  DRAWING: GEAR SHOP
        // =====================================================================

        Rectangle GearPanel { get { return new Rectangle(ClientSize.Width / 2 - 380, 90, 760, 530); } }
        Rectangle GearClose { get { return new Rectangle(GearPanel.Right - 40, GearPanel.Y + 10, 28, 28); } }
        Rectangle GearTab(int i) { return new Rectangle(GearPanel.X + 15 + i * 150, GearPanel.Y + 52, 140, 32); }
        Rectangle GearRow(int i) { return new Rectangle(GearPanel.X + 15, GearPanel.Y + 100 + i * 30, GearPanel.Width - 30, 28); }
        Rectangle GearBuyBtn { get { return new Rectangle(GearPanel.X + GearPanel.Width / 2 - 190, GearPanel.Y + 410, 380, 40); } }

        string PredatorInfo(int z)
        {
            if (z == 0)
                return "Piranha (Surface): " + (PredImmune(0) ? "IMMUNE" : (int)Math.Round(PiranhaStealChance() * 100) + "% to steal a fish")
                       + "     [immune at 10 upgrades]";
            if (z == 1)
                return "Eel (Mid): " + (PredImmune(1) ? "IMMUNE" : (int)Math.Round(EelCutChance() * 100) + "% to cut your line")
                       + "     [immune at 20 upgrades]";
            if (z == 2)
                return "Shark (Deep): takes " + (int)Math.Round(SharkFraction() * 100) + "% of your catch     [never immune]";
            return "??? (Abyss): takes your whole catch     [nothing protects you]";
        }

        void DrawButton(Graphics g, Rectangle r, string text, Color col, Font font)
        {
            g.FillRectangle(GetBrush(col), r);
            if (r.Contains(mousePos))
                g.FillRectangle(GetBrush(Color.FromArgb(50, 255, 255, 255)), r);
            g.DrawRectangle(Pens.White, r);
            g.DrawString(text, font, Brushes.White, r, centerFmt);
        }

        // what the rod does: shown on the rod tab
        string RodFilterText(int i)
        {
            int mz = rodMinZone[i];
            string zones = mz == 0 ? "all zones" : "skips " + (mz == 1 ? "Surface" : "Surface + Mid");
            return zones;
        }

        void DrawGearShop(Graphics g)
        {
            Rectangle p = GearPanel;
            g.FillRectangle(GetBrush(Color.FromArgb(240, 30, 30, 40)), p);
            g.DrawRectangle(Pens.White, p);

            g.DrawString("GEAR SHOP      Money: $" + money, fontTitle, Brushes.White, p.X + 15, p.Y + 14);
            DrawButton(g, GearClose, "X", GearClose.Contains(mousePos) ? Color.Firebrick : Color.DarkRed, fontRow);

            string[] tabNames = { "ROD", "LINE", "BAIT" };
            for (int i = 0; i < 3; i++)
            {
                Color tc = i == shopTab ? Color.FromArgb(255, 184, 134, 11) : Color.FromArgb(255, 70, 70, 90);
                DrawButton(g, GearTab(i), tabNames[i], tc, fontRow);
            }

            string btnText;
            Color btnCol;

            if (shopTab == 0) DrawRodTab(g, p, out btnText, out btnCol);
            else if (shopTab == 1) DrawLineTab(g, p, out btnText, out btnCol);
            else DrawBaitTab(g, p, out btnText, out btnCol);

            DrawButton(g, GearBuyBtn, btnText, btnCol, fontRow);

            // summary of everything you own
            g.DrawString("Rod: " + rodNames[rodLevel] + " (" + RodCapacity + " kg, " + MaxHooked + " fish)" +
                         "     Line: " + (int)(LineLength / 10) + " m (" + lineUpgrades + "/" + MaxUpgrades + ")" +
                         "     Glow bait: " + (hasGlowBait ? "yes" : "no"),
                         fontSmall, Brushes.White, p.X + 15, p.Bottom - 70);
            g.DrawString(message, fontSmall, Brushes.Gold, p.X + 15, p.Bottom - 46);
            g.DrawString("Mouse: click a tab or row, then BUY.   Keys: A/D = tab, W/S = select, Enter = buy, Esc = close",
                         fontSmall, Brushes.LightGray, p.X + 15, p.Bottom - 24);
        }

        void DrawRodTab(Graphics g, Rectangle p, out string btnText, out Color btnCol)
        {
            for (int i = 0; i < rodNames.Length; i++)
            {
                Rectangle r = GearRow(i);

                if (i == shopSel)
                    g.FillRectangle(GetBrush(Color.FromArgb(90, 255, 215, 0)), r);
                else if (r.Contains(mousePos))
                    g.FillRectangle(GetBrush(Color.FromArgb(35, 255, 255, 255)), r);

                string status;
                Brush color;
                if (i < rodLevel) { status = "OWNED"; color = Brushes.Gray; }
                else if (i == rodLevel) { status = "EQUIPPED"; color = Brushes.LimeGreen; }
                else if (i == rodLevel + 1)
                {
                    status = "$" + rodPrice[i];
                    color = money >= rodPrice[i] ? Brushes.Gold : Brushes.OrangeRed;
                }
                else { status = "LOCKED"; color = Brushes.DimGray; }

                g.DrawString((i + 1) + ". " + rodNames[i], fontRow, color, r.X + 8, r.Y + 5);
                g.DrawString(rodCapacity[i] + " kg, " + (3 + i) + " fish, " + RodFilterText(i), fontRow, color, r.X + 230, r.Y + 5);
                g.DrawString(status, fontRow, color, r.X + 600, r.Y + 5);
            }

            if (shopSel < rodLevel) { btnText = "OWNED"; btnCol = Color.Gray; }
            else if (shopSel == rodLevel) { btnText = "EQUIPPED"; btnCol = Color.SeaGreen; }
            else if (shopSel == rodLevel + 1)
            {
                btnText = "BUY " + rodNames[shopSel] + "  -  $" + rodPrice[shopSel];
                btnCol = money >= rodPrice[shopSel] ? Color.SeaGreen : Color.Firebrick;
            }
            else
            {
                btnText = "LOCKED - buy the " + rodNames[rodLevel + 1] + " first";
                btnCol = Color.DimGray;
            }
        }

        void DrawLineTab(Graphics g, Rectangle p, out string btnText, out Color btnCol)
        {
            int meters = (int)(LineLength / 10);
            int maxMeters = (int)(MaxLineLength / 10);
            g.DrawString("Line length: " + meters + " m / " + maxMeters + " m", fontTitle, Brushes.White, p.X + 20, p.Y + 110);
            g.DrawString("Upgrades: " + lineUpgrades + " / " + MaxUpgrades + "        Reaches: " + zoneNames[depthLevel - 1] + " zone",
                         fontRow, Brushes.Gold, p.X + 20, p.Y + 145);

            // depth bar: zones to scale, with how far your line reaches
            Rectangle bar = new Rectangle(p.X + 20, p.Y + 215, p.Width - 40, 30);
            float total = WorldBottom - WaterTop;
            for (int z = 0; z < ZoneCount; z++)
            {
                float x0 = bar.X + (ZoneTopY(z) - WaterTop) / total * bar.Width;
                float x1 = bar.X + (ZoneBottomY(z) - WaterTop) / total * bar.Width;
                g.FillRectangle(GetBrush(zoneColors[z]), x0, bar.Y, x1 - x0, bar.Height);
                g.DrawString(zoneNames[z], fontSmall, Brushes.White, x0 + 3, bar.Bottom + 4);
            }
            float reach = bar.X + Math.Min(1f, LineLength / total) * bar.Width;
            g.FillRectangle(GetBrush(Color.FromArgb(110, 255, 255, 255)), bar.X, bar.Y, reach - bar.X, bar.Height);
            g.DrawLine(goldPen, reach, bar.Y - 6, reach, bar.Bottom + 6);
            g.DrawRectangle(Pens.White, bar);

            g.DrawString("Every upgrade adds " + (int)(LinePerUpgrade / 10) + " m of line and makes predators weaker:", fontSmall, Brushes.White, p.X + 20, p.Y + 290);
            for (int z = 0; z < ZoneCount; z++)
            {
                Brush pc = (z <= 1 && PredImmune(z)) ? Brushes.LimeGreen : Brushes.LightGray;
                g.DrawString(PredatorInfo(z), fontSmall, pc, p.X + 20, p.Y + 315 + z * 22);
            }

            if (lineUpgrades >= MaxUpgrades)
            {
                btnText = "LINE MAXED";
                btnCol = Color.Gray;
            }
            else
            {
                btnText = "UPGRADE LINE  (+" + (int)(LinePerUpgrade / 10) + " m)  -  $" + UpgradeCost;
                btnCol = money >= UpgradeCost ? Color.SeaGreen : Color.Firebrick;
            }
        }

        void DrawBaitTab(Graphics g, Rectangle p, out string btnText, out Color btnCol)
        {
            Rectangle r = GearRow(0);
            g.FillRectangle(GetBrush(Color.FromArgb(90, 255, 215, 0)), r);

            Brush c = hasGlowBait ? Brushes.LimeGreen : (money >= GlowPrice ? Brushes.Gold : Brushes.OrangeRed);
            g.DrawString("1. Glow Bait  (PREMIUM)", fontRow, c, r.X + 8, r.Y + 5);
            g.DrawString(hasGlowBait ? "OWNED" : "$" + GlowPrice, fontRow, c, r.X + 560, r.Y + 5);

            g.DrawString("A big circle of light follows your hook. Fish only show up when they swim into it.", fontSmall, Brushes.White, p.X + 20, p.Y + 150);
            g.DrawString("Without it you only see a tiny circle around the hook once it gets dark.", fontSmall, Brushes.White, p.X + 20, p.Y + 175);
            g.DrawString("One-time purchase: you keep it forever, no need to buy it again.", fontSmall, Brushes.White, p.X + 20, p.Y + 200);

            btnText = hasGlowBait ? "OWNED" : "BUY GLOW BAIT  -  $" + GlowPrice;
            btnCol = hasGlowBait ? Color.Gray : (money >= GlowPrice ? Color.SeaGreen : Color.Firebrick);
        }

        // =====================================================================
        //  PAINT
        // =====================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            UpdateLighting();

            // what part of the world is on screen (with a margin) - used to skip drawing things off screen
            Rectangle view = new Rectangle((int)cameraX - 150, (int)cameraY - 150,
                                           ClientSize.Width + 300, ClientSize.Height + 300);

            // ===== WORLD (scrolls with the camera) =====
            g.TranslateTransform(-cameraX, -cameraY);
            DrawWorld(g, view);

            // ===== SCREEN (fixed HUD) =====
            g.ResetTransform();
            DrawRain(g);
            if (!devSeeAll) DrawDarkness(g);
            DrawPowerBar(g);
            DrawHud(g);
            DrawZoneBanner(g);
            if (Dev) DrawDevPanel(g);

            if (shopOpen) DrawGearShop(g);   // on top of everything
        }

        void DrawWorld(Graphics g, Rectangle view)
        {
            DrawSkyAndWater(g, view);

            // cliffs + coral (edges of the map)
            foreach (Cliff c in cliffs)
                if (view.IntersectsWith(c.Bounds)) DrawCliff(g, c);

            foreach (Coral c in corals)
                if (view.IntersectsWith(Rectangle.Inflate(c.Rect, 0, 70))) DrawCoral(g, c);

            // treasure chests (under the fish)
            foreach (Chest c in chests)
                if (view.IntersectsWith(c.Bounds)) DrawChest(g, c);

            DrawAllFish(g, view);
            DrawHazards(g, view);
            DrawPredators(g);

            // dock, shop spots, boat
            int spot = state == State.Idle ? NearestSpot() : -1;
            DrawDock(g);
            DrawShopBox(g, SellBox, sellImage, "SELL", spot == 0);
            DrawShopBox(g, GearBox, gearImage, "GEAR", spot == 1);
            DrawBoat(g, spot == 2);

            // character, rod, line + hook
            g.FillRectangle(Brushes.OrangeRed, CharacterRect);

            PointF tip = RodTip();
            float pivotY = WaterTop - RodPivotYOffset;
            g.DrawLine(rodPen, playerX, pivotY, tip.X, tip.Y);
            g.DrawLine(Pens.White, tip.X, tip.Y, hookX + HookSize / 2f, hookY);
            g.FillEllipse(Brushes.Silver, hookX, hookY, HookSize, HookSize);

            // prompt above the shop / boat
            if (spot >= 0 && !shopOpen)
            {
                string prompt = SpotPrompt(spot);
                SizeF size = g.MeasureString(prompt, fontRow);
                float px = Math.Max(5, Math.Min(playerX - size.Width / 2, WorldW - size.Width - 5));
                g.DrawString(prompt, fontRow, Brushes.DarkGreen, px, DockTop - 125);
            }
        }

        void DrawSkyAndWater(Graphics g, Rectangle view)
        {
            // sky (changes with the weather)
            Color skyColor = weather == Weather.Storm ? Color.FromArgb(70, 80, 95)
                           : weather == Weather.Rain ? Color.FromArgb(130, 150, 170)
                           : Color.LightSkyBlue;
            g.FillRectangle(GetBrush(skyColor), Rectangle.Intersect(new Rectangle(0, 0, WorldW, WaterTop), view));

            // water zones (only the part that is on screen)
            for (int z = 0; z < ZoneCount; z++)
            {
                Rectangle band = new Rectangle(0, ZoneTopY(z), WorldW, ZoneBottomY(z) - ZoneTopY(z));
                if (band.Bottom < view.Top || band.Top > view.Bottom) continue;

                g.FillRectangle(GetBrush(zoneColors[z]), Rectangle.Intersect(band, view));
                g.DrawLine(dividerPen, view.Left, band.Y, view.Right, band.Y);
            }
        }

        // once it is dark, only fish inside the circle of light can be seen
        void DrawAllFish(Graphics g, Rectangle view)
        {
            float outsideA = Math.Max(0f, Math.Min(1f, 1f - (darkLevel - 0.35f) / 0.2f));
            if (devSeeAll) outsideA = 1f;
            PointF lp = LightWorldPos();
            float lr2 = lightRadius * lightRadius;

            foreach (Fish f in fishes)
            {
                if (f.Dead || !view.IntersectsWith(f.Bounds)) continue;

                int a = 255;
                if (outsideA < 0.999f && !f.Hooked)
                {
                    float dx = f.X + f.Size - lp.X;
                    float dy = f.Y + f.Size / 2f - lp.Y;
                    if (dx * dx + dy * dy > lr2)
                    {
                        if (outsideA <= 0.03f) continue;     // too dark: hidden
                        a = (int)(255 * outsideA);           // fading out
                    }
                }
                DrawFish(g, f, a);
            }
        }

        void DrawPowerBar(Graphics g)
        {
            if (state == State.Idle || state == State.Sinking || state == State.Reeling) return;

            float shown = state == State.Charging ? power : lastPower;
            int barW = 30, barH = 200;
            Rectangle pb = new Rectangle(ClientSize.Width - barW - 20 - (Dev ? 170 : 0), 260, barW, barH);

            g.FillRectangle(Brushes.White, pb);

            int fillH = (int)(barH * shown);
            Brush fill = shown < 0.4f ? Brushes.LimeGreen
                       : shown < 0.75f ? Brushes.Gold
                       : Brushes.OrangeRed;
            g.FillRectangle(fill, pb.X, pb.Bottom - fillH, barW, fillH);
            g.DrawRectangle(Pens.Black, pb);

            g.DrawString("POWER", fontSmall, Brushes.White, pb.X - 8, pb.Y - 22);
            g.DrawString((int)(shown * 100) + "%", fontSmall, Brushes.White, pb.X + 2, pb.Bottom + 5);
        }

        void DrawHud(Graphics g)
        {
            g.FillRectangle(GetBrush(Color.FromArgb(150, 255, 255, 255)), 0, 0, ClientSize.Width, 98);

            g.DrawString(message, fontHud, Brushes.DarkRed, 10, 6);

            if (graceTicks > 0)
                g.DrawString("LINE PROTECTED: " + (graceTicks / 60 + 1) + "s", fontSmall, Brushes.DarkGreen,
                             ClientSize.Width - 200, 6);

            bool hookActive = state == State.Sinking || state == State.Reeling;
            int z = CurrentZone();
            string zoneText = z >= 0 ? zoneNames[z] : "Above water";
            int depthM = (int)Math.Max(0, hookY - WaterTop) / 10;
            string boatText = boatOwned ? (onBoat ? "sailing" : "owned") : "no";

            g.DrawString("Bag: " + bagCount + " fish ($" + bagValue + ")   Money: $" + money +
                         "   Depth: " + depthM + " m (" + zoneText + ")   Chests: " + chestsOpened +
                         "   Weather: " + WeatherName() + (weather != Weather.Clear ? " (more fish!)" : ""),
                         fontSmall, Brushes.Black, 10, 30);
            g.DrawString("Rod: " + rodNames[rodLevel] + " (" + HookedWeight().ToString("0.0") + "/" + RodCapacity + " kg, " +
                         HookedCount() + "/" + MaxHooked + " fish)" +
                         "   Line: " + (int)(LineLength / 10) + " m (" + lineUpgrades + "/" + MaxUpgrades + ")" +
                         "   Boat: " + boatText +
                         "   Area: " + AreaName(hookActive ? hookX : playerX) +
                         (hasGlowBait ? "   Glow bait: ON" : ""),
                         fontSmall, Brushes.Black, 10, 52);
            g.DrawString("A/D = walk / sail   Hold SPACE = charge, release = throw   S = sink faster   W = tap/hold up   SPACE = reel up   E / click = shop / boat" +
                         (Dev ? "   [DEV] panel on the right" : ""),
                         fontSmall, Brushes.Black, 10, 74);
        }
    }
}