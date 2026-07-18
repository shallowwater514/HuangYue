using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace HuangYueDemo
{
    internal sealed class BattleMapNode
    {
        public string Name;
        public string Kind;
        public float X;
        public float Y;

        public BattleMapNode(string name, string kind, float x, float y)
        {
            Name = name;
            Kind = kind;
            X = x;
            Y = y;
        }
    }

    internal sealed class BattleFieldUnit
    {
        public string Name;
        public float X;
        public float Y;
        public float Speed;
        public double Strength;
        public double MaxStrength;
        public bool IsEnemy;
        public bool IsCarrier;
        public bool Loaded;
        public bool Active;
        public bool Alive;
        public bool InCombat;
        public bool Blocking;
        public bool AmbushReady;
        public bool PincerActive;
        public bool PincerReported;
        public Color Color;
        public BattleMapNode Target;
        public readonly List<BattleMapNode> Route = new List<BattleMapNode>();
        public int RouteIndex;
        public float CurrentSpeed;
        public string Order;

        public BattleFieldUnit(string name, float x, float y, int strength, float speed,
            bool enemy, bool carrier, Color color)
        {
            Name = name;
            X = x;
            Y = y;
            Strength = strength;
            MaxStrength = strength;
            Speed = speed;
            IsEnemy = enemy;
            IsCarrier = carrier;
            Color = color;
            Active = true;
            Alive = true;
            Order = "原地待命";
        }
    }

    internal sealed class RealtimeState
    {
        public int Food = 4;
        public int Morale = 52;
        public int Support = 38;
        public int Cruelty = 0;
        public double Elapsed = 0;
        public double Deadline = 210;
        public double NextFoodAt = 32;
        public double NextIncidentAt = 24;
        public double MovementDisruptionUntil = 0;
        public double LocalGuidesUntil = 0;
        public int IncidentCount = 0;
        public bool VillageDecided;
        public bool GuardDefeated;
        public bool GranaryDecisionShown;
        public bool GranaryCaptured;
        public bool VanguardReleased;
        public bool ReinforcementReleased;
        public bool LowSupportWarningLogged;
        public bool LowSupportCrisisTriggered;
        public bool Finished;
        public readonly Random IncidentRandom = new Random(1641);
        public readonly List<BattleMapNode> Nodes = new List<BattleMapNode>();
        public readonly List<BattleFieldUnit> Units = new List<BattleFieldUnit>();
        public readonly List<string> Log = new List<string>();

        public BattleMapNode Node(string name)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].Name == name)
                    return Nodes[i];
            return null;
        }

        public BattleFieldUnit Unit(string name)
        {
            for (int i = 0; i < Units.Count; i++)
                if (Units[i].Name == name)
                    return Units[i];
            return null;
        }

        public int PlayerStrength()
        {
            int total = 0;
            for (int i = 0; i < Units.Count; i++)
                if (!Units[i].IsEnemy && Units[i].Alive)
                    total += (int)Math.Ceiling(Units[i].Strength);
            return total;
        }

        public void Normalize()
        {
            Food = Math.Max(0, Math.Min(12, Food));
            Morale = Math.Max(0, Math.Min(100, Morale));
            Support = Math.Max(0, Math.Min(100, Support));
        }
    }

    internal sealed class BattleMapPanel : Panel
    {
        public RealtimeState State;
        public BattleFieldUnit Selected;
        public float AnimationPhase;
        public event Action<BattleFieldUnit> UnitClicked;
        public event Action<BattleMapNode> NodeClicked;

        private readonly int mapPad = 28;

        public BattleMapPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Cursor = Cursors.Cross;
            BackColor = Color.FromArgb(205, 191, 159);
        }

        private PointF ToScreen(float x, float y)
        {
            return new PointF(mapPad + x * Math.Max(1, Width - mapPad * 2),
                mapPad + y * Math.Max(1, Height - mapPad * 2));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (State == null || State.Finished)
                return;

            for (int i = State.Units.Count - 1; i >= 0; i--)
            {
                BattleFieldUnit unit = State.Units[i];
                if (!unit.Active || !unit.Alive)
                    continue;
                PointF p = ToScreen(unit.X, unit.Y);
                if (Distance(p.X, p.Y, e.X, e.Y) <= 25F)
                {
                    if (UnitClicked != null)
                        UnitClicked(unit);
                    return;
                }
            }

            for (int i = 0; i < State.Nodes.Count; i++)
            {
                BattleMapNode node = State.Nodes[i];
                PointF p = ToScreen(node.X, node.Y);
                if (Distance(p.X, p.Y, e.X, e.Y) <= 38F)
                {
                    if (NodeClicked != null)
                        NodeClicked(node);
                    return;
                }
            }
        }

        private float Distance(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx;
            float dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush paper = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(220, 208, 180), Color.FromArgb(185, 169, 137), 90F))
                g.FillRectangle(paper, ClientRectangle);

            DrawPaperTexture(g);
            DrawTerrain(g);
            if (State == null)
                return;
            DrawRoads(g);
            DrawNodes(g);
            DrawOrders(g);
            DrawUnits(g);

            using (Font hintFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular))
            using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(130, 54, 47, 39)))
                g.DrawString("选择一支部队，再点击地图地点下达行军令", hintFont, hintBrush, 18, 14);

            DrawPublicSupportWarning(g);
        }

        private void DrawPublicSupportWarning(Graphics g)
        {
            if (State == null)
                return;
            string warning = null;
            Color color = Color.FromArgb(165, 133, 87, 47);
            if (State.Support < 15)
            {
                warning = "民心崩溃：道路封锁 · 战力下降";
                color = Color.FromArgb(190, 135, 42, 36);
            }
            else if (State.Support < 30)
            {
                warning = "民心低迷：无人带路 · 行军迟缓";
                color = Color.FromArgb(180, 151, 82, 42);
            }
            else if (State.Support >= 65)
            {
                warning = "民心拥护：乡民引路 · 行军加快";
                color = Color.FromArgb(175, 61, 112, 76);
            }
            if (warning == null)
                return;

            using (Font font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(warning, font);
                float x = Width - size.Width - 34;
                using (SolidBrush back = new SolidBrush(Color.FromArgb(220, 42, 36, 30)))
                    g.FillRectangle(back, x - 12, 11, size.Width + 24, 29);
                using (SolidBrush brush = new SolidBrush(color))
                    g.DrawString(warning, font, brush, x, 17);
            }
        }

        private void DrawPaperTexture(Graphics g)
        {
            Random random = new Random(1641);
            using (Pen grain = new Pen(Color.FromArgb(12, 67, 55, 42), 1F))
            {
                for (int i = 0; i < 180; i++)
                {
                    int x = random.Next(Math.Max(1, Width));
                    int y = random.Next(Math.Max(1, Height));
                    g.DrawLine(grain, x, y, x + random.Next(2, 13), y);
                }
            }
        }

        private void DrawTerrain(Graphics g)
        {
            PointF a = ToScreen(0.02F, 0.16F);
            PointF b = ToScreen(0.92F, 0.88F);
            using (Pen river = new Pen(Color.FromArgb(105, 73, 103, 112), 20F))
            {
                river.StartCap = LineCap.Round;
                river.EndCap = LineCap.Round;
                g.DrawBezier(river, a, ToScreen(0.32F, 0.05F), ToScreen(0.56F, 0.95F), b);
            }
            using (Pen riverLight = new Pen(Color.FromArgb(65, 205, 216, 210), 2F))
                g.DrawBezier(riverLight, a, ToScreen(0.32F, 0.05F), ToScreen(0.56F, 0.95F), b);

            using (Pen field = new Pen(Color.FromArgb(28, 73, 68, 46), 1F))
            {
                for (int i = 0; i < 7; i++)
                {
                    PointF p1 = ToScreen(0.08F, 0.28F + i * 0.035F);
                    PointF p2 = ToScreen(0.29F, 0.24F + i * 0.038F);
                    g.DrawLine(field, p1, p2);
                }
                for (int i = 0; i < 6; i++)
                {
                    PointF p1 = ToScreen(0.47F, 0.76F + i * 0.025F);
                    PointF p2 = ToScreen(0.70F, 0.68F + i * 0.031F);
                    g.DrawLine(field, p1, p2);
                }
            }

            using (SolidBrush mountain = new SolidBrush(Color.FromArgb(44, 58, 52, 42)))
            {
                PointF[] ridge = {
                    ToScreen(0.64F, 0.10F), ToScreen(0.69F, 0.02F),
                    ToScreen(0.73F, 0.11F), ToScreen(0.78F, 0.03F),
                    ToScreen(0.83F, 0.14F), ToScreen(0.89F, 0.06F),
                    ToScreen(0.96F, 0.18F), ToScreen(0.96F, 0.01F), ToScreen(0.64F, 0.01F)
                };
                g.FillPolygon(mountain, ridge);
            }
        }

        private void DrawRoads(Graphics g)
        {
            string[,] links = {
                { "黄钺营", "柳沟村" }, { "柳沟村", "周氏庄" },
                { "周氏庄", "官仓" }, { "官仓", "临河县" },
                { "周氏庄", "南渡口" }, { "官仓", "南渡口" },
                { "黄钺营", "周氏庄" }, { "柳沟村", "官仓" }
            };
            using (Pen road = new Pen(Color.FromArgb(100, 100, 82, 59), 2F))
            {
                road.DashStyle = DashStyle.Dash;
                for (int i = 0; i < links.GetLength(0); i++)
                {
                    BattleMapNode first = State.Node(links[i, 0]);
                    BattleMapNode second = State.Node(links[i, 1]);
                    if (first != null && second != null)
                        g.DrawLine(road, ToScreen(first.X, first.Y), ToScreen(second.X, second.Y));
                }
            }
        }

        private void DrawNodes(Graphics g)
        {
            using (Font nameFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (SolidBrush text = new SolidBrush(Color.FromArgb(61, 49, 38)))
            using (Pen ink = new Pen(Color.FromArgb(120, 55, 45, 35), 2F))
            {
                for (int i = 0; i < State.Nodes.Count; i++)
                {
                    BattleMapNode node = State.Nodes[i];
                    PointF p = ToScreen(node.X, node.Y);
                    Color fillColor = Color.FromArgb(160, 211, 196, 159);
                    if (node.Name == "官仓")
                        fillColor = State.GranaryCaptured
                            ? Color.FromArgb(190, 102, 139, 99)
                            : Color.FromArgb(190, 171, 132, 72);
                    if (node.Name == "南渡口")
                        fillColor = Color.FromArgb(175, 105, 139, 132);

                    using (SolidBrush fill = new SolidBrush(fillColor))
                        g.FillEllipse(fill, p.X - 19, p.Y - 19, 38, 38);
                    g.DrawEllipse(ink, p.X - 19, p.Y - 19, 38, 38);
                    DrawNodeIcon(g, node, p, ink);

                    SizeF textSize = g.MeasureString(node.Name, nameFont);
                    using (SolidBrush plate = new SolidBrush(Color.FromArgb(130, 224, 211, 181)))
                        g.FillRectangle(plate, p.X - textSize.Width / 2 - 4, p.Y + 23,
                            textSize.Width + 8, textSize.Height);
                    g.DrawString(node.Name, nameFont, text, p.X - textSize.Width / 2, p.Y + 23);
                }
            }
        }

        private void DrawNodeIcon(Graphics g, BattleMapNode node, PointF p, Pen ink)
        {
            if (node.Kind == "camp")
            {
                PointF[] tent = { new PointF(p.X - 10, p.Y + 8), new PointF(p.X, p.Y - 10), new PointF(p.X + 11, p.Y + 8) };
                g.DrawPolygon(ink, tent);
                g.DrawLine(ink, p.X, p.Y - 10, p.X, p.Y + 8);
            }
            else if (node.Kind == "village" || node.Kind == "manor")
            {
                g.DrawRectangle(ink, p.X - 9, p.Y - 2, 18, 11);
                g.DrawLine(ink, p.X - 12, p.Y - 2, p.X, p.Y - 11);
                g.DrawLine(ink, p.X, p.Y - 11, p.X + 12, p.Y - 2);
            }
            else if (node.Kind == "granary")
            {
                g.DrawRectangle(ink, p.X - 10, p.Y - 9, 20, 18);
                g.DrawLine(ink, p.X - 10, p.Y - 3, p.X + 10, p.Y - 3);
                g.DrawLine(ink, p.X - 4, p.Y - 9, p.X - 4, p.Y + 9);
            }
            else if (node.Kind == "county")
            {
                g.DrawRectangle(ink, p.X - 12, p.Y - 8, 24, 16);
                g.DrawLine(ink, p.X - 12, p.Y - 8, p.X - 12, p.Y - 13);
                g.DrawLine(ink, p.X, p.Y - 8, p.X, p.Y - 13);
                g.DrawLine(ink, p.X + 12, p.Y - 8, p.X + 12, p.Y - 13);
            }
            else
            {
                g.DrawArc(ink, p.X - 11, p.Y - 5, 22, 12, 0, 180);
                g.DrawLine(ink, p.X, p.Y - 8, p.X, p.Y + 6);
            }
        }

        private void DrawOrders(Graphics g)
        {
            for (int i = 0; i < State.Units.Count; i++)
            {
                BattleFieldUnit unit = State.Units[i];
                if (!unit.Active || !unit.Alive || unit.IsEnemy || unit.Target == null || unit.Route.Count == 0)
                    continue;
                using (Pen order = new Pen(Color.FromArgb(150, unit.Color), unit == Selected ? 3F : 2F))
                {
                    order.DashStyle = DashStyle.Dot;
                    order.EndCap = LineCap.ArrowAnchor;
                    PointF from = ToScreen(unit.X, unit.Y);
                    for (int routeIndex = unit.RouteIndex; routeIndex < unit.Route.Count; routeIndex++)
                    {
                        BattleMapNode waypoint = unit.Route[routeIndex];
                        PointF to = ToScreen(waypoint.X, waypoint.Y);
                        g.DrawLine(order, from, to);
                        from = to;
                    }
                }
            }
        }

        private void DrawUnits(Graphics g)
        {
            using (Font unitFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold))
            using (Font countFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            {
                for (int i = 0; i < State.Units.Count; i++)
                {
                    BattleFieldUnit unit = State.Units[i];
                    if (!unit.Active || !unit.Alive)
                        continue;
                    PointF p = ToScreen(unit.X, unit.Y);

                    if (unit == Selected)
                    {
                        float pulse = 27F + (float)Math.Sin(AnimationPhase) * 3F;
                        using (Pen selected = new Pen(Color.FromArgb(190, 229, 196, 95), 3F))
                            g.DrawEllipse(selected, p.X - pulse, p.Y - pulse, pulse * 2, pulse * 2);
                    }

                    if (unit.Blocking)
                    {
                        float blockPulse = 37F + (float)Math.Sin(AnimationPhase * 0.7F) * 2F;
                        using (Pen block = new Pen(Color.FromArgb(195, 196, 139, 67), 2F))
                        {
                            block.DashStyle = DashStyle.Dash;
                            g.DrawEllipse(block, p.X - blockPulse, p.Y - blockPulse,
                                blockPulse * 2, blockPulse * 2);
                        }
                    }

                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(80, 20, 18, 16)))
                        g.FillEllipse(shadow, p.X - 19, p.Y + 11, 40, 12);
                    using (SolidBrush fill = new SolidBrush(unit.Color))
                        g.FillEllipse(fill, p.X - 19, p.Y - 19, 38, 38);
                    using (Pen border = new Pen(unit.IsEnemy ? Color.FromArgb(110, 20, 14, 12) : Color.FromArgb(150, 238, 221, 181), 2F))
                        g.DrawEllipse(border, p.X - 19, p.Y - 19, 38, 38);

                    string mark = unit.IsEnemy ? "官" : (unit.IsCarrier ? "粮" : (unit.Name == "游骑哨" ? "骑" : "义"));
                    SizeF markSize = g.MeasureString(mark, unitFont);
                    using (SolidBrush markBrush = new SolidBrush(Color.FromArgb(238, 235, 218, 183)))
                        g.DrawString(mark, unitFont, markBrush, p.X - markSize.Width / 2, p.Y - markSize.Height / 2);

                    if (unit.Loaded)
                    {
                        using (SolidBrush grain = new SolidBrush(Color.FromArgb(225, 210, 166, 68)))
                            g.FillEllipse(grain, p.X + 12, p.Y - 23, 13, 13);
                    }

                    float barWidth = 46F;
                    float ratio = (float)(unit.Strength / unit.MaxStrength);
                    using (SolidBrush barBack = new SolidBrush(Color.FromArgb(170, 44, 36, 30)))
                        g.FillRectangle(barBack, p.X - barWidth / 2, p.Y - 31, barWidth, 5);
                    using (SolidBrush bar = new SolidBrush(unit.IsEnemy
                        ? Color.FromArgb(205, 151, 52, 45)
                        : Color.FromArgb(205, 92, 136, 91)))
                        g.FillRectangle(bar, p.X - barWidth / 2, p.Y - 31, barWidth * ratio, 5);

                    string caption = unit.Name + " " + Math.Max(0, (int)Math.Ceiling(unit.Strength));
                    SizeF captionSize = g.MeasureString(caption, countFont);
                    using (SolidBrush plate = new SolidBrush(Color.FromArgb(175, 41, 35, 29)))
                        g.FillRectangle(plate, p.X - captionSize.Width / 2 - 4, p.Y + 23,
                            captionSize.Width + 8, captionSize.Height + 1);
                    using (SolidBrush captionBrush = new SolidBrush(Color.FromArgb(230, 229, 213, 179)))
                        g.DrawString(caption, countFont, captionBrush,
                            p.X - captionSize.Width / 2, p.Y + 23);

                    if (unit.InCombat)
                    {
                        using (Pen clash = new Pen(Color.FromArgb(220, 170, 56, 42), 3F))
                        {
                            float swing = (float)Math.Sin(AnimationPhase * 2F) * 4F;
                            g.DrawLine(clash, p.X - 22, p.Y - 22, p.X + 22 + swing, p.Y + 22);
                            g.DrawLine(clash, p.X + 22, p.Y - 22, p.X - 22 - swing, p.Y + 22);
                        }
                    }


                    if (unit.PincerActive && unit.IsEnemy)
                    {
                        string tactic = "夹击";
                        SizeF tacticSize = g.MeasureString(tactic, unitFont);
                        using (SolidBrush tacticBack = new SolidBrush(Color.FromArgb(220, 111, 42, 31)))
                            g.FillRectangle(tacticBack, p.X - tacticSize.Width / 2 - 5,
                                p.Y - 52, tacticSize.Width + 10, tacticSize.Height + 2);
                        using (SolidBrush tacticText = new SolidBrush(Color.FromArgb(242, 221, 163)))
                            g.DrawString(tactic, unitFont, tacticText,
                                p.X - tacticSize.Width / 2, p.Y - 51);
                    }
                }
            }
        }
    }

    internal sealed class RealtimeEventArtPanel : Panel
    {
        public string SceneKind;

        public RealtimeEventArtPanel(string sceneKind)
        {
            SceneKind = sceneKind;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush sky = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(166, 151, 122), Color.FromArgb(52, 47, 41), 90F))
                g.FillRectangle(sky, ClientRectangle);

            using (SolidBrush sun = new SolidBrush(Color.FromArgb(105, 154, 55, 42)))
                g.FillEllipse(sun, Width - 70, 28, 42, 42);
            using (SolidBrush ridge = new SolidBrush(Color.FromArgb(86, 39, 37, 33)))
            {
                PointF[] mountains = {
                    new PointF(0, Height * 0.54F), new PointF(35, Height * 0.39F),
                    new PointF(72, Height * 0.52F), new PointF(118, Height * 0.34F),
                    new PointF(170, Height * 0.53F), new PointF(Width, Height * 0.43F),
                    new PointF(Width, Height), new PointF(0, Height)
                };
                g.FillPolygon(ridge, mountains);
            }

            if (SceneKind == "granary")
                DrawGranary(g);
            else if (SceneKind == "refugees")
                DrawRefugees(g);
            else if (SceneKind == "wounded")
                DrawWounded(g);
            else if (SceneKind == "rumor")
                DrawRumor(g);
            else if (SceneKind == "snow")
                DrawSnow(g);
            else
                DrawVillage(g);

            using (SolidBrush fade = new SolidBrush(Color.FromArgb(35, 227, 210, 175)))
                g.FillRectangle(fade, ClientRectangle);
            using (Pen border = new Pen(Color.FromArgb(135, 92, 71, 49), 2F))
                g.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
        }

        private void DrawVillage(Graphics g)
        {
            using (SolidBrush ground = new SolidBrush(Color.FromArgb(145, 34, 32, 29)))
                g.FillRectangle(ground, 0, Height * 2 / 3, Width, Height / 3);
            using (Pen ink = new Pen(Color.FromArgb(215, 26, 24, 22), 4F))
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = 20 + i * 63;
                    g.DrawRectangle(ink, x, Height - 105 - i * 7, 48, 40);
                    g.DrawLine(ink, x - 5, Height - 105 - i * 7, x + 24, Height - 132 - i * 7);
                    g.DrawLine(ink, x + 24, Height - 132 - i * 7, x + 53, Height - 105 - i * 7);
                }
            }
            DrawFigure(g, 72, Height - 68, 1.0F);
            DrawFigure(g, 111, Height - 62, 0.85F);
            DrawFigure(g, 145, Height - 66, 0.95F);
        }

        private void DrawGranary(Graphics g)
        {
            using (SolidBrush ground = new SolidBrush(Color.FromArgb(160, 32, 29, 26)))
                g.FillRectangle(ground, 0, Height * 3 / 4, Width, Height / 4);
            using (Pen ink = new Pen(Color.FromArgb(220, 25, 23, 21), 5F))
            {
                RectangleF barn = new RectangleF(28, Height - 142, Width - 56, 90);
                g.DrawRectangle(ink, barn.X, barn.Y, barn.Width, barn.Height);
                g.DrawLine(ink, 19, barn.Y, Width / 2, barn.Y - 52);
                g.DrawLine(ink, Width / 2, barn.Y - 52, Width - 19, barn.Y);
                g.DrawLine(ink, Width / 2, barn.Y + 28, Width / 2, barn.Bottom);
            }
            using (SolidBrush sack = new SolidBrush(Color.FromArgb(175, 122, 91, 53)))
            {
                g.FillEllipse(sack, 45, Height - 72, 38, 28);
                g.FillEllipse(sack, 76, Height - 76, 40, 31);
                g.FillEllipse(sack, 112, Height - 71, 38, 27);
            }
        }

        private void DrawRefugees(Graphics g)
        {
            using (Pen road = new Pen(Color.FromArgb(110, 166, 145, 107), 24F))
                g.DrawBezier(road, 0, Height - 24, 55, Height - 105, 145, Height - 50, Width, Height - 145);
            DrawFigure(g, 36, Height - 50, 0.85F);
            DrawFigure(g, 78, Height - 91, 1.05F);
            DrawFigure(g, 126, Height - 83, 0.72F);
            DrawFigure(g, 172, Height - 122, 0.94F);
        }

        private void DrawWounded(Graphics g)
        {
            using (SolidBrush ground = new SolidBrush(Color.FromArgb(150, 31, 29, 27)))
                g.FillRectangle(ground, 0, Height * 3 / 4, Width, Height / 4);
            using (Pen cart = new Pen(Color.FromArgb(220, 24, 22, 20), 4F))
            {
                g.DrawRectangle(cart, 48, Height - 118, 112, 46);
                g.DrawLine(cart, 160, Height - 96, 202, Height - 77);
                g.DrawEllipse(cart, 57, Height - 83, 28, 28);
                g.DrawEllipse(cart, 127, Height - 83, 28, 28);
            }
            DrawFigure(g, 31, Height - 70, 1F);
            DrawFigure(g, 183, Height - 70, 0.9F);
        }

        private void DrawRumor(Graphics g)
        {
            using (SolidBrush wall = new SolidBrush(Color.FromArgb(165, 49, 43, 36)))
                g.FillRectangle(wall, 18, Height - 200, Width - 36, 142);
            using (SolidBrush paper = new SolidBrush(Color.FromArgb(205, 202, 182, 143)))
                g.FillRectangle(paper, 66, Height - 180, 92, 105);
            using (Pen writing = new Pen(Color.FromArgb(155, 70, 52, 38), 2F))
                for (int i = 0; i < 5; i++)
                    g.DrawLine(writing, 82, Height - 158 + i * 14, 143, Height - 158 + i * 14);
            DrawFigure(g, 39, Height - 55, 0.85F);
            DrawFigure(g, 178, Height - 57, 0.9F);
        }

        private void DrawSnow(Graphics g)
        {
            Random random = new Random(17);
            using (SolidBrush snow = new SolidBrush(Color.FromArgb(145, 230, 226, 211)))
                for (int i = 0; i < 55; i++)
                {
                    int x = random.Next(Math.Max(1, Width));
                    int y = random.Next(Math.Max(1, Height));
                    int size = random.Next(2, 5);
                    g.FillEllipse(snow, x, y, size, size);
                }
            DrawFigure(g, 88, Height - 58, 1.05F);
            DrawFigure(g, 143, Height - 62, 0.85F);
        }

        private void DrawFigure(Graphics g, float x, float groundY, float scale)
        {
            using (SolidBrush ink = new SolidBrush(Color.FromArgb(225, 22, 21, 19)))
            using (Pen limb = new Pen(Color.FromArgb(225, 22, 21, 19), 4F * scale))
            {
                float head = 9F * scale;
                g.FillEllipse(ink, x - head / 2, groundY - 53F * scale, head, head);
                g.DrawLine(limb, x, groundY - 44F * scale, x, groundY - 18F * scale);
                g.DrawLine(limb, x, groundY - 34F * scale, x - 11F * scale, groundY - 25F * scale);
                g.DrawLine(limb, x, groundY - 34F * scale, x + 10F * scale, groundY - 27F * scale);
                g.DrawLine(limb, x, groundY - 18F * scale, x - 9F * scale, groundY);
                g.DrawLine(limb, x, groundY - 18F * scale, x + 9F * scale, groundY);
            }
        }
    }

    internal sealed partial class MainForm
    {
        private RealtimeState realtime;
        private BattleMapPanel realtimeMap;
        private Timer realtimeTimer;
        private BattleFieldUnit selectedRealtimeUnit;
        private bool realtimePaused;
        private bool realtimeChoiceOpen;
        private double realtimeSpeed = 1.0;
        private Label rtFood;
        private Label rtTroops;
        private Label rtMorale;
        private Label rtSupport;
        private Label rtClock;
        private Label rtSelected;
        private Label rtObjective;
        private Label rtLog;
        private Button rtMainButton;
        private Button rtScoutButton;
        private Button rtCarrierButton;
        private Button rtPauseButton;
        private Button rtSpeedButton;

        private void StartRealtimeDemo()
        {
            Resize -= CenterTitlePanel;
            StopRealtimeTimer();
            realtime = BuildRealtimeState();
            realtimePaused = false;
            realtimeChoiceOpen = false;
            realtimeSpeed = 1.0;
            BuildRealtimeScreen();
            SelectRealtimeUnit(realtime.Unit("主力营"));
            AddRealtimeLog("目标：夺取官仓，让辎重队装粮后抵达南渡口。");
            AddRealtimeLog("战术：主力与游骑从不同道路接敌，可形成夹击。");

            realtimeTimer = new Timer();
            realtimeTimer.Interval = 33;
            realtimeTimer.Tick += RealtimeTick;
            realtimeTimer.Start();
        }

        private RealtimeState BuildRealtimeState()
        {
            RealtimeState s = new RealtimeState();
            s.Nodes.Add(new BattleMapNode("黄钺营", "camp", 0.12F, 0.74F));
            s.Nodes.Add(new BattleMapNode("柳沟村", "village", 0.28F, 0.43F));
            s.Nodes.Add(new BattleMapNode("周氏庄", "manor", 0.48F, 0.70F));
            s.Nodes.Add(new BattleMapNode("官仓", "granary", 0.65F, 0.29F));
            s.Nodes.Add(new BattleMapNode("临河县", "county", 0.86F, 0.35F));
            s.Nodes.Add(new BattleMapNode("南渡口", "ferry", 0.80F, 0.79F));

            BattleMapNode camp = s.Node("黄钺营");
            BattleMapNode granary = s.Node("官仓");
            BattleMapNode county = s.Node("临河县");
            s.Units.Add(new BattleFieldUnit("主力营", camp.X, camp.Y, 180, 0.050F,
                false, false, Color.FromArgb(176, 120, 48, 41)));
            s.Units.Add(new BattleFieldUnit("游骑哨", camp.X + 0.025F, camp.Y - 0.035F, 46, 0.086F,
                false, false, Color.FromArgb(179, 55, 88, 82)));
            s.Units.Add(new BattleFieldUnit("辎重队", camp.X - 0.018F, camp.Y + 0.035F, 61, 0.034F,
                false, true, Color.FromArgb(182, 134, 100, 57)));
            s.Units.Add(new BattleFieldUnit("仓守乡勇", granary.X, granary.Y, 82, 0F,
                true, false, Color.FromArgb(183, 126, 40, 37)));
            s.Units.Add(new BattleFieldUnit("官军前锋", county.X, county.Y, 128, 0.044F,
                true, false, Color.FromArgb(188, 104, 35, 32)));
            BattleFieldUnit reserve = new BattleFieldUnit("官军别队", county.X + 0.018F, county.Y + 0.035F,
                105, 0.046F, true, false, Color.FromArgb(188, 91, 31, 29));
            reserve.Active = false;
            s.Units.Add(reserve);
            return s;
        }

        private void BuildRealtimeScreen()
        {
            ClearStage();

            TableLayoutPanel frame = new TableLayoutPanel();
            frame.Dock = DockStyle.Fill;
            frame.Padding = new Padding(22, 18, 22, 20);
            frame.BackColor = Color.FromArgb(29, 26, 23);
            frame.ColumnCount = 1;
            frame.RowCount = 3;
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            stage.Controls.Add(frame);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(40, 36, 32);
            frame.Controls.Add(header, 0, 0);

            Label mode = MakeLabel("黄钺 · 白马河军图", 16F, FontStyle.Bold, Paper);
            mode.SetBounds(20, 9, 250, 32);
            header.Controls.Add(mode);

            rtClock = MakeLabel("距官军合围 03:30", 9F, FontStyle.Regular,
                Color.FromArgb(181, 157, 116));
            rtClock.SetBounds(22, 42, 240, 20);
            header.Controls.Add(rtClock);

            FlowLayoutPanel stats = new FlowLayoutPanel();
            stats.FlowDirection = FlowDirection.LeftToRight;
            stats.WrapContents = false;
            stats.BackColor = Color.Transparent;
            stats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stats.SetBounds(340, 7, 620, 61);
            header.Controls.Add(stats);
            rtFood = AddRealtimeStat(stats, "粮秣", "4日", Color.FromArgb(211, 181, 106));
            rtTroops = AddRealtimeStat(stats, "兵力", "287", Color.FromArgb(208, 201, 182));
            rtMorale = AddRealtimeStat(stats, "军心", "52", Color.FromArgb(190, 90, 72));
            rtSupport = AddRealtimeStat(stats, "民望", "38", Color.FromArgb(106, 158, 120));

            rtPauseButton = MakeSmallDarkButton("暂停");
            rtPauseButton.SetBounds(978, 10, 72, 50);
            rtPauseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            rtPauseButton.Click += ToggleRealtimePause;
            header.Controls.Add(rtPauseButton);

            rtSpeedButton = MakeSmallDarkButton("速度 ×1");
            rtSpeedButton.SetBounds(1058, 10, 82, 50);
            rtSpeedButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            rtSpeedButton.Click += ToggleRealtimeSpeed;
            header.Controls.Add(rtSpeedButton);

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(0, 14, 0, 10);
            content.ColumnCount = 2;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            frame.Controls.Add(content, 0, 1);

            Panel mapFrame = new Panel();
            mapFrame.Dock = DockStyle.Fill;
            mapFrame.Padding = new Padding(4);
            mapFrame.BackColor = Color.FromArgb(89, 74, 55);
            content.Controls.Add(mapFrame, 0, 0);

            realtimeMap = new BattleMapPanel();
            realtimeMap.Dock = DockStyle.Fill;
            realtimeMap.State = realtime;
            realtimeMap.UnitClicked += RealtimeUnitClicked;
            realtimeMap.NodeClicked += RealtimeNodeClicked;
            mapFrame.Controls.Add(realtimeMap);

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.Margin = new Padding(14, 0, 0, 0);
            side.Padding = new Padding(16);
            side.BackColor = Color.FromArgb(42, 38, 34);
            content.Controls.Add(side, 1, 0);

            Label troopsTitle = MakeLabel("可 调 动 部 队", 10F, FontStyle.Bold, DarkPaper);
            troopsTitle.SetBounds(16, 12, 280, 26);
            troopsTitle.TextAlign = ContentAlignment.MiddleCenter;
            troopsTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(troopsTitle);

            rtMainButton = MakeUnitButton();
            rtMainButton.SetBounds(16, 46, 280, 54);
            rtMainButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rtMainButton.Click += delegate { SelectRealtimeUnit(realtime.Unit("主力营")); };
            side.Controls.Add(rtMainButton);

            rtScoutButton = MakeUnitButton();
            rtScoutButton.SetBounds(16, 108, 280, 54);
            rtScoutButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rtScoutButton.Click += delegate { SelectRealtimeUnit(realtime.Unit("游骑哨")); };
            side.Controls.Add(rtScoutButton);

            rtCarrierButton = MakeUnitButton();
            rtCarrierButton.SetBounds(16, 170, 280, 54);
            rtCarrierButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rtCarrierButton.Click += delegate { SelectRealtimeUnit(realtime.Unit("辎重队")); };
            side.Controls.Add(rtCarrierButton);

            rtSelected = MakeLabel("", 9.5F, FontStyle.Regular,
                Color.FromArgb(199, 185, 159));
            rtSelected.SetBounds(20, 236, 272, 52);
            rtSelected.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(rtSelected);

            Panel line = new Panel();
            line.BackColor = Color.FromArgb(88, 76, 62);
            line.SetBounds(22, 294, 268, 1);
            line.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(line);

            rtObjective = MakeLabel(
                "军令\r\n① 击败仓守乡勇\r\n② 辎重队进入官仓装粮\r\n③ 护送辎重队抵达南渡口",
                9.5F, FontStyle.Bold, Color.FromArgb(187, 161, 103));
            rtObjective.SetBounds(22, 308, 268, 90);
            rtObjective.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(rtObjective);

            Label reportTitle = MakeLabel("战 场 急 报", 9F, FontStyle.Bold,
                Color.FromArgb(161, 145, 119));
            reportTitle.SetBounds(22, 406, 268, 25);
            reportTitle.TextAlign = ContentAlignment.MiddleCenter;
            reportTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(reportTitle);

            rtLog = MakeLabel("", 8.8F, FontStyle.Regular,
                Color.FromArgb(184, 173, 152));
            rtLog.SetBounds(22, 438, 268, 105);
            rtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(rtLog);

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.LeftToRight;
            footer.WrapContents = false;
            footer.Padding = new Padding(0, 6, 0, 0);
            footer.BackColor = Color.Transparent;
            frame.Controls.Add(footer, 0, 2);

            Button stop = MakeCommandButton("停止当前行军");
            stop.Click += delegate
            {
                if (selectedRealtimeUnit != null && selectedRealtimeUnit.Alive)
                {
                    ClearUnitRoute(selectedRealtimeUnit);
                    selectedRealtimeUnit.Blocking = false;
                    selectedRealtimeUnit.AmbushReady = false;
                    selectedRealtimeUnit.Order = "原地待命";
                    AddRealtimeLog(selectedRealtimeUnit.Name + "停止行军。");
                    UpdateRealtimeUi();
                    realtimeMap.Invalidate();
                }
            };
            footer.Controls.Add(stop);

            Button block = MakeCommandButton("设伏阻击");
            block.Click += delegate { SetBlockingOrder(selectedRealtimeUnit); };
            footer.Controls.Add(block);

            Button escort = MakeCommandButton("主力护送辎重队");
            escort.Click += delegate { OrderMainToCarrier(); };
            footer.Controls.Add(escort);

            Button reset = MakeCommandButton("重新开始战场");
            reset.Click += delegate { StartRealtimeDemo(); };
            footer.Controls.Add(reset);

            Button back = MakeCommandButton("返回标题");
            back.Click += delegate { StopRealtimeTimer(); ShowTitleScreen(); };
            footer.Controls.Add(back);

            UpdateRealtimeUi();
        }

        private Label AddRealtimeStat(FlowLayoutPanel parent, string name, string value, Color valueColor)
        {
            Panel panel = new Panel();
            panel.Size = new Size(132, 57);
            panel.Margin = new Padding(3, 0, 3, 0);
            panel.BackColor = Color.FromArgb(49, 44, 39);
            Label n = MakeLabel(name, 8F, FontStyle.Regular, Color.FromArgb(142, 129, 109));
            n.SetBounds(4, 3, 124, 19);
            n.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(n);
            Label v = MakeLabel(value, 14F, FontStyle.Bold, valueColor);
            v.SetBounds(4, 22, 124, 29);
            v.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(v);
            parent.Controls.Add(panel);
            return v;
        }

        private Button MakeSmallDarkButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.ForeColor = Color.FromArgb(211, 198, 169);
            button.BackColor = Color.FromArgb(57, 50, 43);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(95, 80, 62);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private Button MakeUnitButton()
        {
            Button button = new Button();
            button.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(12, 0, 4, 0);
            button.ForeColor = Color.FromArgb(217, 203, 175);
            button.BackColor = Color.FromArgb(53, 47, 41);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(91, 77, 61);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(68, 59, 49);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private Button MakeCommandButton(string text)
        {
            Button button = MakeSmallDarkButton(text);
            button.Size = new Size(178, 32);
            button.Margin = new Padding(0, 0, 10, 0);
            return button;
        }

        private void RealtimeUnitClicked(BattleFieldUnit unit)
        {
            if (!unit.IsEnemy)
                SelectRealtimeUnit(unit);
            else
                AddRealtimeLog("探知敌情：" + unit.Name + "约" + (int)Math.Ceiling(unit.Strength) + "人。");
        }

        private void SelectRealtimeUnit(BattleFieldUnit unit)
        {
            if (unit == null || !unit.Alive)
                return;
            selectedRealtimeUnit = unit;
            if (realtimeMap != null)
            {
                realtimeMap.Selected = unit;
                realtimeMap.Invalidate();
            }
            UpdateRealtimeUi();
        }

        private void RealtimeNodeClicked(BattleMapNode node)
        {
            if (realtime == null || realtime.Finished || realtimeChoiceOpen || selectedRealtimeUnit == null)
                return;
            if (!selectedRealtimeUnit.Alive)
                return;
            if (selectedRealtimeUnit.InCombat)
            {
                realtime.Morale -= 2;
                AddRealtimeLog(selectedRealtimeUnit.Name + "脱离交战，军心略降。");
            }
            SetUnitRoute(selectedRealtimeUnit, node);
            selectedRealtimeUnit.Order = "前往" + node.Name;
            AddRealtimeLog("令" + selectedRealtimeUnit.Name + "前往" + node.Name + "。");
            realtime.Normalize();
            UpdateRealtimeUi();
            realtimeMap.Invalidate();
        }

        private void OrderMainToCarrier()
        {
            BattleFieldUnit main = realtime.Unit("主力营");
            BattleFieldUnit carrier = realtime.Unit("辎重队");
            if (main == null || carrier == null || !main.Alive || !carrier.Alive)
                return;
            BattleMapNode nearest = FindNearestNode(carrier.X, carrier.Y);
            SetUnitRoute(main, nearest);
            main.Order = "护送辎重队至" + nearest.Name;
            SelectRealtimeUnit(main);
            AddRealtimeLog("主力营向辎重队靠拢。");
        }

        private BattleMapNode FindNearestNode(float x, float y)
        {
            BattleMapNode best = realtime.Nodes[0];
            double bestDistance = 999;
            for (int i = 0; i < realtime.Nodes.Count; i++)
            {
                double dx = realtime.Nodes[i].X - x;
                double dy = realtime.Nodes[i].Y - y;
                double d = dx * dx + dy * dy;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = realtime.Nodes[i];
                }
            }
            return best;
        }

        private string[,] RealtimeRoadLinks()
        {
            return new string[,] {
                { "黄钺营", "柳沟村" }, { "柳沟村", "周氏庄" },
                { "周氏庄", "官仓" }, { "官仓", "临河县" },
                { "周氏庄", "南渡口" }, { "官仓", "南渡口" },
                { "黄钺营", "周氏庄" }, { "柳沟村", "官仓" }
            };
        }

        private List<BattleMapNode> FindRealtimeRoute(BattleMapNode start, BattleMapNode destination)
        {
            List<BattleMapNode> route = new List<BattleMapNode>();
            if (start == null || destination == null)
                return route;

            Dictionary<BattleMapNode, double> distances = new Dictionary<BattleMapNode, double>();
            Dictionary<BattleMapNode, BattleMapNode> previous = new Dictionary<BattleMapNode, BattleMapNode>();
            HashSet<BattleMapNode> unvisited = new HashSet<BattleMapNode>();
            for (int i = 0; i < realtime.Nodes.Count; i++)
            {
                BattleMapNode node = realtime.Nodes[i];
                distances[node] = node == start ? 0 : double.MaxValue;
                unvisited.Add(node);
            }

            string[,] links = RealtimeRoadLinks();
            while (unvisited.Count > 0)
            {
                BattleMapNode current = null;
                double currentDistance = double.MaxValue;
                foreach (BattleMapNode node in unvisited)
                {
                    if (distances[node] < currentDistance)
                    {
                        current = node;
                        currentDistance = distances[node];
                    }
                }
                if (current == null || currentDistance == double.MaxValue)
                    break;
                unvisited.Remove(current);
                if (current == destination)
                    break;

                for (int i = 0; i < links.GetLength(0); i++)
                {
                    BattleMapNode neighbor = null;
                    if (links[i, 0] == current.Name)
                        neighbor = realtime.Node(links[i, 1]);
                    else if (links[i, 1] == current.Name)
                        neighbor = realtime.Node(links[i, 0]);
                    if (neighbor == null || !unvisited.Contains(neighbor))
                        continue;

                    double dx = current.X - neighbor.X;
                    double dy = current.Y - neighbor.Y;
                    double candidate = currentDistance + Math.Sqrt(dx * dx + dy * dy);
                    if (candidate < distances[neighbor])
                    {
                        distances[neighbor] = candidate;
                        previous[neighbor] = current;
                    }
                }
            }

            BattleMapNode step = destination;
            route.Add(step);
            while (step != start && previous.ContainsKey(step))
            {
                step = previous[step];
                route.Insert(0, step);
            }
            if (route.Count == 0 || route[0] != start)
            {
                route.Clear();
                route.Add(destination);
            }
            return route;
        }

        private void SetUnitRoute(BattleFieldUnit unit, BattleMapNode destination)
        {
            if (unit == null || destination == null)
                return;
            unit.Route.Clear();
            unit.RouteIndex = 0;
            unit.Target = destination;
            unit.Blocking = false;
            unit.AmbushReady = false;

            BattleMapNode start = FindNearestNode(unit.X, unit.Y);
            List<BattleMapNode> route = FindRealtimeRoute(start, destination);
            for (int i = 0; i < route.Count; i++)
            {
                BattleMapNode waypoint = route[i];
                double dx = waypoint.X - unit.X;
                double dy = waypoint.Y - unit.Y;
                if (i == 0 && Math.Sqrt(dx * dx + dy * dy) < 0.060)
                    continue;
                unit.Route.Add(waypoint);
            }
            if (unit.Route.Count == 0)
                unit.Route.Add(destination);
        }

        private void ClearUnitRoute(BattleFieldUnit unit)
        {
            if (unit == null)
                return;
            unit.Target = null;
            unit.Route.Clear();
            unit.RouteIndex = 0;
            unit.CurrentSpeed = 0;
        }

        private void SetBlockingOrder(BattleFieldUnit unit)
        {
            if (realtime == null || realtime.Finished || realtimeChoiceOpen ||
                unit == null || !unit.Alive)
                return;
            if (unit.IsCarrier)
            {
                AddRealtimeLog("辎重队无法设伏，请选择主力营或游骑哨。");
                return;
            }
            ClearUnitRoute(unit);
            unit.Blocking = true;
            unit.AmbushReady = true;
            unit.Order = "设伏阻击";
            AddRealtimeLog(unit.Name + "就地设伏，将截停进入附近的敌军。");
            UpdateRealtimeUi();
            realtimeMap.Invalidate();
        }

        private void ToggleRealtimePause(object sender, EventArgs e)
        {
            if (realtimeChoiceOpen || realtime.Finished)
                return;
            realtimePaused = !realtimePaused;
            rtPauseButton.Text = realtimePaused ? "继续" : "暂停";
            AddRealtimeLog(realtimePaused ? "战场已暂停。" : "战场继续。");
        }

        private void ToggleRealtimeSpeed(object sender, EventArgs e)
        {
            realtimeSpeed = realtimeSpeed < 1.5 ? 2.0 : 1.0;
            rtSpeedButton.Text = realtimeSpeed > 1.5 ? "速度 ×2" : "速度 ×1";
        }

        private void RealtimeTick(object sender, EventArgs e)
        {
            if (realtime == null || realtime.Finished || realtimePaused || realtimeChoiceOpen)
                return;

            double dt = 0.033 * realtimeSpeed;
            realtime.Elapsed += dt;
            realtimeMap.AnimationPhase += (float)(0.065 * realtimeSpeed);

            ReleaseEnemyForces();
            ResetCombatFlags();
            DetectRealtimeEngagements();
            MoveRealtimeUnits(dt);
            DetectRealtimeEngagements();
            ResolveRealtimeCombat(dt);
            HandleGranaryAfterBattle();
            ConsumeRealtimeFood();
            CheckPublicSupportEffects();
            TriggerUnexpectedIncident();
            CheckRealtimeEnd();
            realtime.Normalize();
            UpdateRealtimeUi();
            realtimeMap.Invalidate();
        }

        private void ReleaseEnemyForces()
        {
            if (!realtime.VanguardReleased && realtime.Elapsed >= 42)
            {
                realtime.VanguardReleased = true;
                BattleFieldUnit vanguard = realtime.Unit("官军前锋");
                SetUnitRoute(vanguard, realtime.Node("柳沟村"));
                vanguard.Order = "扫荡柳沟村";
                AddRealtimeLog("官军前锋出城，正向柳沟村进发！");
            }
            if (!realtime.ReinforcementReleased && realtime.Elapsed >= 118)
            {
                realtime.ReinforcementReleased = true;
                BattleFieldUnit reserve = realtime.Unit("官军别队");
                reserve.Active = true;
                SetUnitRoute(reserve, realtime.Node("南渡口"));
                reserve.Order = "截断南渡口";
                AddRealtimeLog("官军别队出现，企图抢占南渡口！");
            }
        }

        private void ResetCombatFlags()
        {
            for (int i = 0; i < realtime.Units.Count; i++)
            {
                realtime.Units[i].InCombat = false;
                realtime.Units[i].PincerActive = false;
            }
        }

        private double UnitDistance(BattleFieldUnit first, BattleFieldUnit second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void DetectRealtimeEngagements()
        {
            for (int i = 0; i < realtime.Units.Count; i++)
            {
                BattleFieldUnit player = realtime.Units[i];
                if (!player.Active || !player.Alive || player.IsEnemy)
                    continue;
                for (int j = 0; j < realtime.Units.Count; j++)
                {
                    BattleFieldUnit enemy = realtime.Units[j];
                    if (!enemy.Active || !enemy.Alive || !enemy.IsEnemy)
                        continue;
                    double interceptionRange = player.Blocking && !player.IsCarrier ? 0.075 :
                        (player.IsCarrier ? 0.040 : 0.045);
                    if (UnitDistance(player, enemy) > interceptionRange)
                        continue;

                    player.InCombat = true;
                    enemy.InCombat = true;
                    if (player.Blocking && player.AmbushReady)
                    {
                        player.AmbushReady = false;
                        enemy.Strength -= Math.Min(8, Math.Max(3, enemy.Strength * 0.08));
                        realtime.Morale += 2;
                        AddRealtimeLog(player.Name + "伏兵齐出，截住" + enemy.Name + "！");
                        if (enemy.Strength <= 0)
                            DefeatRealtimeEnemy(enemy);
                    }
                }
            }

            BattleFieldUnit main = realtime.Unit("主力营");
            BattleFieldUnit scout = realtime.Unit("游骑哨");
            if (main == null || scout == null || !main.Alive || !scout.Alive)
                return;
            for (int i = 0; i < realtime.Units.Count; i++)
            {
                BattleFieldUnit enemy = realtime.Units[i];
                if (!enemy.Active || !enemy.Alive || !enemy.IsEnemy ||
                    !main.InCombat || !scout.InCombat)
                    continue;
                double mainRange = main.Blocking ? 0.075 : 0.050;
                double scoutRange = scout.Blocking ? 0.075 : 0.050;
                if (UnitDistance(main, enemy) > mainRange || UnitDistance(scout, enemy) > scoutRange)
                    continue;

                double mainX = main.X - enemy.X;
                double mainY = main.Y - enemy.Y;
                double scoutX = scout.X - enemy.X;
                double scoutY = scout.Y - enemy.Y;
                double mainLength = Math.Sqrt(mainX * mainX + mainY * mainY);
                double scoutLength = Math.Sqrt(scoutX * scoutX + scoutY * scoutY);
                if (mainLength < 0.001 || scoutLength < 0.001)
                    continue;
                double directionDot = (mainX * scoutX + mainY * scoutY) /
                    (mainLength * scoutLength);
                if (directionDot >= 0.78)
                    continue;

                main.PincerActive = true;
                scout.PincerActive = true;
                enemy.PincerActive = true;
                if (!enemy.PincerReported)
                {
                    enemy.PincerReported = true;
                    realtime.Morale += 4;
                    AddRealtimeLog("夹击形成：主力营与游骑哨从两路压住" + enemy.Name + "！");
                }
            }
        }

        private void MoveRealtimeUnits(double dt)
        {
            for (int i = 0; i < realtime.Units.Count; i++)
            {
                BattleFieldUnit unit = realtime.Units[i];
                if (!unit.Active || !unit.Alive || unit.Target == null || unit.Speed <= 0 ||
                    unit.Route.Count == 0 || unit.InCombat)
                    continue;
                if (unit.RouteIndex >= unit.Route.Count)
                    unit.RouteIndex = unit.Route.Count - 1;
                BattleMapNode waypoint = unit.Route[unit.RouteIndex];
                float dx = waypoint.X - unit.X;
                float dy = waypoint.Y - unit.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                float movementFactor = 1F;
                if (!unit.IsEnemy)
                {
                    if (realtime.Support < 15)
                        movementFactor *= 0.68F;
                    else if (realtime.Support < 30)
                        movementFactor *= 0.82F;
                    else if (realtime.Support >= 65)
                        movementFactor *= 1.08F;
                    if (realtime.Elapsed < realtime.MovementDisruptionUntil)
                        movementFactor *= 0.72F;
                    if (realtime.Elapsed < realtime.LocalGuidesUntil)
                        movementFactor *= 1.18F;
                }
                float desiredSpeed = unit.Speed * movementFactor;
                bool finalWaypoint = unit.RouteIndex == unit.Route.Count - 1;
                if (finalWaypoint && distance < 0.055F)
                    desiredSpeed *= Math.Max(0.25F, distance / 0.055F);
                float acceleration = Math.Max(0.020F, unit.Speed * 3.2F) * (float)dt;
                if (unit.CurrentSpeed < desiredSpeed)
                    unit.CurrentSpeed = Math.Min(desiredSpeed, unit.CurrentSpeed + acceleration);
                else
                    unit.CurrentSpeed = Math.Max(desiredSpeed, unit.CurrentSpeed - acceleration);
                float step = unit.CurrentSpeed * (float)dt;
                if (distance <= step || distance < 0.002F)
                {
                    unit.X = waypoint.X;
                    unit.Y = waypoint.Y;
                    if (!finalWaypoint)
                    {
                        unit.RouteIndex++;
                    }
                    else
                    {
                        BattleMapNode arrived = unit.Target;
                        ClearUnitRoute(unit);
                        unit.Order = "驻于" + arrived.Name;
                        HandleRealtimeArrival(unit, arrived);
                    }
                }
                else
                {
                    unit.X += dx / distance * step;
                    unit.Y += dy / distance * step;
                }
            }
        }

        private void HandleRealtimeArrival(BattleFieldUnit unit, BattleMapNode node)
        {
            if (unit.IsEnemy)
            {
                if (unit.Name == "官军前锋" && node.Name == "柳沟村")
                {
                    realtime.Support -= 10;
                    SetUnitRoute(unit, realtime.Node("黄钺营"));
                    unit.Order = "进逼黄钺营";
                    AddRealtimeLog("官军进入柳沟村，村中起火。民望下降。");
                }
                else if (node.Name == "黄钺营")
                {
                    realtime.Morale -= 12;
                    SetUnitRoute(unit, realtime.Node("南渡口"));
                    unit.Order = "截断退路";
                    AddRealtimeLog("黄钺营遭袭，军心动摇！");
                }
                return;
            }

            AddRealtimeLog(unit.Name + "抵达" + node.Name + "。");
            if (node.Name == "柳沟村" && !realtime.VillageDecided)
            {
                realtime.VillageDecided = true;
                ShowRealtimeDecision(
                    "柳沟村的冬粮",
                    "村民愿意开门，但他们的存粮也只够过冬。你可以立下借契，也可以趁官军到来前强征。",
                    "依约借粮",
                    "所得不多：粮秣 +1，民望 +12，军心 +3",
                    delegate
                    {
                        realtime.Food += 1; realtime.Support += 12; realtime.Morale += 3;
                        AddRealtimeLog("赵先生写下借契，柳沟村借出冬粮。");
                    },
                    "强征冬粮",
                    "可解燃眉：粮秣 +3，民望 -18，军心 +5",
                    delegate
                    {
                        realtime.Food += 3; realtime.Support -= 18; realtime.Morale += 5; realtime.Cruelty += 2;
                        AddRealtimeLog("军士搬空七户冬粮，村门自此紧闭。");
                    });
            }

            if (node.Name == "官仓" && unit.IsCarrier && realtime.GranaryCaptured && !unit.Loaded)
            {
                unit.Loaded = true;
                unit.Order = "已装粮，等待军令";
                AddRealtimeLog("辎重队装粮完毕，请立即撤往南渡口！");
            }

            if (node.Name == "南渡口" && unit.IsCarrier && unit.Loaded)
                FinishRealtimeBattle(true, "粮车过河");
        }

        private void ResolveRealtimeCombat(double dt)
        {
            for (int i = 0; i < realtime.Units.Count; i++)
            {
                BattleFieldUnit player = realtime.Units[i];
                if (!player.Active || !player.Alive || player.IsEnemy || player.IsCarrier)
                    continue;
                for (int j = 0; j < realtime.Units.Count; j++)
                {
                    BattleFieldUnit enemy = realtime.Units[j];
                    if (!enemy.Active || !enemy.Alive || !enemy.IsEnemy)
                        continue;
                    double combatRange = player.Blocking ? 0.075 : 0.045;
                    if (UnitDistance(player, enemy) > combatRange)
                        continue;

                    player.InCombat = true;
                    enemy.InCombat = true;
                    double playerPower = Math.Max(0.6,
                        player.Strength * (0.45 + realtime.Morale / 100.0) * 0.020);
                    double enemyPower = Math.Max(0.5, enemy.Strength * 0.022);
                    if (realtime.Support >= 60)
                        playerPower *= 1.10;
                    else if (realtime.Support < 15)
                        playerPower *= 0.75;
                    else if (realtime.Support < 30)
                        playerPower *= 0.90;
                    if (realtime.Food <= 0)
                        playerPower *= 0.60;
                    if (player.Blocking)
                        playerPower *= 1.12;
                    if (enemy.PincerActive)
                    {
                        playerPower *= 1.45;
                        enemyPower *= 0.58;
                    }
                    enemy.Strength -= playerPower * dt;
                    player.Strength -= enemyPower * dt;

                    if (enemy.Strength <= 0)
                        DefeatRealtimeEnemy(enemy);
                    if (player.Strength <= 0)
                    {
                        player.Strength = 0;
                        player.Alive = false;
                        ClearUnitRoute(player);
                        player.Blocking = false;
                        player.AmbushReady = false;
                        realtime.Morale -= 15;
                        AddRealtimeLog(player.Name + "已溃散！");
                    }
                }
            }

            BattleFieldUnit carrier = realtime.Unit("辎重队");
            if (carrier != null && carrier.Alive)
            {
                for (int i = 0; i < realtime.Units.Count; i++)
                {
                    BattleFieldUnit enemy = realtime.Units[i];
                    if (!enemy.Active || !enemy.Alive || !enemy.IsEnemy)
                        continue;
                    double dx = carrier.X - enemy.X;
                    double dy = carrier.Y - enemy.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) <= 0.040)
                    {
                        carrier.InCombat = true;
                        enemy.InCombat = true;
                        carrier.Strength -= Math.Max(0.8, enemy.Strength * 0.035) * dt;
                        if (carrier.Strength <= 0)
                        {
                            carrier.Strength = 0;
                            carrier.Alive = false;
                            ClearUnitRoute(carrier);
                            AddRealtimeLog("辎重队被冲散，粮车尽失！");
                        }
                    }
                }
            }
        }

        private void DefeatRealtimeEnemy(BattleFieldUnit enemy)
        {
            if (enemy == null || !enemy.Alive)
                return;
            enemy.Strength = 0;
            enemy.Alive = false;
            ClearUnitRoute(enemy);
            enemy.Blocking = false;
            enemy.AmbushReady = false;
            realtime.Morale += 5;
            AddRealtimeLog(enemy.Name + "溃散，军心上升。");
            if (enemy.Name == "仓守乡勇")
                realtime.GuardDefeated = true;
        }

        private void HandleGranaryAfterBattle()
        {
            if (realtime.GuardDefeated && !realtime.GranaryDecisionShown && !realtimeChoiceOpen)
            {
                realtime.GranaryDecisionShown = true;
                ShowRealtimeDecision(
                    "官仓已破",
                    "仓门打开后，饥民也跟了进来。军中需要粮，村民同样需要。官军前锋正在逼近。",
                    "军民分粮",
                    "粮秣 +3，民望 +15，装车速度不变",
                    delegate
                    {
                        realtime.Food += 3; realtime.Support += 15;
                        FinalizeGranaryCapture();
                        AddRealtimeLog("官仓开仓分粮，军民各取一半。");
                    },
                    "尽数装车",
                    "粮秣 +6，民望 -15，军心 +4",
                    delegate
                    {
                        realtime.Food += 6; realtime.Support -= 15; realtime.Morale += 4; realtime.Cruelty += 2;
                        FinalizeGranaryCapture();
                        AddRealtimeLog("官仓粮食尽数装入军车，饥民被挡在门外。");
                    });
            }
        }

        private void FinalizeGranaryCapture()
        {
            realtime.GranaryCaptured = true;
            BattleFieldUnit carrier = realtime.Unit("辎重队");
            BattleMapNode granary = realtime.Node("官仓");
            if (carrier != null && carrier.Alive &&
                Math.Abs(carrier.X - granary.X) < 0.01F && Math.Abs(carrier.Y - granary.Y) < 0.01F)
            {
                carrier.Loaded = true;
                carrier.Order = "已装粮，等待军令";
                AddRealtimeLog("辎重队就地装粮完毕，请撤向南渡口！");
            }
        }

        private void ConsumeRealtimeFood()
        {
            if (realtime.Elapsed < realtime.NextFoodAt)
                return;
            realtime.NextFoodAt += 32;
            realtime.Food -= 1;
            if (realtime.Food > 0)
                AddRealtimeLog("全军消耗一日粮秣，尚余" + realtime.Food + "日。");
            else
            {
                realtime.Morale -= 8;
                AddRealtimeLog("军中断粮！军心下降，部队开始减员。");
                for (int i = 0; i < realtime.Units.Count; i++)
                {
                    BattleFieldUnit unit = realtime.Units[i];
                    if (!unit.IsEnemy && unit.Alive)
                        unit.Strength = Math.Max(0, unit.Strength - 5);
                }
            }
        }

        private void CheckPublicSupportEffects()
        {
            if (realtime.Support < 30 && !realtime.LowSupportWarningLogged)
            {
                realtime.LowSupportWarningLogged = true;
                AddRealtimeLog("民心低迷：村民闭门，无人愿为军队带路。");
            }
            else if (realtime.Support >= 35)
            {
                realtime.LowSupportWarningLogged = false;
                if (realtime.Support >= 45)
                    realtime.LowSupportCrisisTriggered = false;
            }

            if (realtime.Support < 15 && !realtime.LowSupportCrisisTriggered && !realtimeChoiceOpen)
            {
                realtime.LowSupportCrisisTriggered = true;
                ShowUnexpectedIncident(
                    "support",
                    "乡民封锁粮道",
                    "沿路村庄敲锣示警，拆桥、推车堵路。军士抓住一名少年，他怀里藏着官军悬赏的告示。民心已经不只是一个数字。",
                    "开仓安抚",
                    "分出至多两日粮：民望 +18，军心 +2",
                    delegate
                    {
                        realtime.Food -= Math.Min(2, realtime.Food);
                        realtime.Support += 18;
                        realtime.Morale += 2;
                        AddRealtimeLog("军中开仓赈济，封路的乡民渐渐散去。");
                    },
                    "搜捕首事者",
                    "强行开路：民望 -10，军心 +4，主力损失8人",
                    delegate
                    {
                        ApplyUnitLoss("主力营", 8);
                        realtime.Support -= 10;
                        realtime.Morale += 4;
                        realtime.Cruelty += 2;
                        realtime.MovementDisruptionUntil = realtime.Elapsed + 25;
                        AddRealtimeLog("军士入村搜捕，粮道暂通，沿途村落尽空。");
                    });
            }
        }

        private void TriggerUnexpectedIncident()
        {
            if (realtimeChoiceOpen || realtime.IncidentCount >= 4 || realtime.Elapsed < realtime.NextIncidentAt)
                return;

            realtime.IncidentCount++;
            realtime.NextIncidentAt += 34 + realtime.IncidentRandom.Next(0, 17);

            if (realtime.Support < 28)
            {
                ShowLowSupportIncident();
                return;
            }
            if (realtime.Food <= 1)
            {
                ShowFoodCrisisIncident();
                return;
            }
            if (287 - realtime.PlayerStrength() >= 42)
            {
                ShowWoundedIncident();
                return;
            }
            if (realtime.Support >= 65)
            {
                ShowVolunteerIncident();
                return;
            }

            int roll = realtime.IncidentRandom.Next(0, 3);
            if (roll == 0)
                ShowRefugeeIncident();
            else if (roll == 1)
                ShowRumorIncident();
            else
                ShowSnowIncident();
        }

        private void ShowLowSupportIncident()
        {
            ShowUnexpectedIncident(
                "support",
                "带路人不见了",
                "清晨出发时，昨日雇来的三名向导全都不见。前方小路又被倒下的树木堵住。斥候说，附近百姓正在暗中替官军传递消息。",
                "赔粮并重新雇人",
                "粮秣 -1，民望 +14，并获得一段时间的乡民引路",
                delegate
                {
                    realtime.Food -= 1;
                    realtime.Support += 14;
                    realtime.LocalGuidesUntil = realtime.Elapsed + 42;
                    AddRealtimeLog("重新雇得向导，部队暂时恢复行军速度。");
                },
                "命军士强行开路",
                "主力损失6人，民望 -10，军心 +3，道路仍会受阻",
                delegate
                {
                    ApplyUnitLoss("主力营", 6);
                    realtime.Support -= 10;
                    realtime.Morale += 3;
                    realtime.Cruelty += 1;
                    realtime.MovementDisruptionUntil = realtime.Elapsed + 20;
                    AddRealtimeLog("军士伐木开路，附近村民连夜逃走。");
                });
        }

        private void ShowFoodCrisisIncident()
        {
            ShowUnexpectedIncident(
                "snow",
                "锅中已经见底",
                "司粮官把最后一只空粮袋翻给你看。若下一批粮不能及时运来，今夜便有人吃不上饭。游骑哨尚有几匹负伤的战马。",
                "杀马充饥",
                "游骑哨损失12人，粮秣 +2，军心 -3",
                delegate
                {
                    ApplyUnitLoss("游骑哨", 12);
                    realtime.Food += 2;
                    realtime.Morale -= 3;
                    AddRealtimeLog("伤马被杀，军中多出两日口粮，游骑却慢慢沉默。");
                },
                "全军减半配给",
                "粮秣 +1，军心 -9，民望 +4",
                delegate
                {
                    realtime.Food += 1;
                    realtime.Morale -= 9;
                    realtime.Support += 4;
                    AddRealtimeLog("主将与士卒同食半碗稀粥，军心虽降，百姓有所耳闻。");
                });
        }

        private void ShowWoundedIncident()
        {
            ShowUnexpectedIncident(
                "wounded",
                "伤兵车停在路上",
                "一辆伤兵车陷进泥地，后面的队伍全被堵住。军医说其中六人尚可救活，但需要粮、水和半个时辰。官军的旗号已经越来越近。",
                "停车救治",
                "粮秣 -1，军心 +9，民望 +6，主力恢复6人",
                delegate
                {
                    realtime.Food -= 1;
                    realtime.Morale += 9;
                    realtime.Support += 6;
                    RestoreUnit("主力营", 6);
                    realtime.MovementDisruptionUntil = realtime.Elapsed + 10;
                    AddRealtimeLog("全军停车抬出伤兵，六人重新归队。");
                },
                "把车推到路旁",
                "继续行军：军心 -8，民望 -10",
                delegate
                {
                    realtime.Morale -= 8;
                    realtime.Support -= 10;
                    realtime.Cruelty += 1;
                    AddRealtimeLog("队伍从伤兵车旁经过，没有人回头。");
                });
        }

        private void ShowVolunteerIncident()
        {
            ShowUnexpectedIncident(
                "village",
                "十八名乡勇来投",
                "因军中分粮守纪，柳沟村与邻村共有十八名青年携矛来投。他们熟悉河道和小路，但家中也在等粮。",
                "编入游骑哨",
                "粮秣 -1，游骑哨增加18人，军心 +6",
                delegate
                {
                    realtime.Food -= 1;
                    AddVolunteers("游骑哨", 18);
                    realtime.Morale += 6;
                    AddRealtimeLog("十八名乡勇编入游骑哨。");
                },
                "请他们为全军引路",
                "民望 +5，较长时间内全军行军加快",
                delegate
                {
                    realtime.Support += 5;
                    realtime.LocalGuidesUntil = realtime.Elapsed + 55;
                    AddRealtimeLog("乡勇没有入伍，而是沿途为全军引路。");
                });
        }

        private void ShowRefugeeIncident()
        {
            ShowUnexpectedIncident(
                "refugees",
                "流民跟上了队伍",
                "二十余名流民从荒村中走出，远远跟着辎重队。他们说愿意拉车、抬伤员，只求每天能分到一碗粥。",
                "开栅收留",
                "粮秣 -1，辎重队增加20人，民望 +12，军心 +2",
                delegate
                {
                    realtime.Food -= 1;
                    AddVolunteers("辎重队", 20);
                    realtime.Support += 12;
                    realtime.Morale += 2;
                    AddRealtimeLog("流民加入辎重队，队伍变得更长。");
                },
                "不许他们靠近",
                "保持行军速度，民望 -8",
                delegate
                {
                    realtime.Support -= 8;
                    AddRealtimeLog("守卫举起长矛，流民停在路边，继续望着粮车。");
                });
        }

        private void ShowRumorIncident()
        {
            ShowUnexpectedIncident(
                "rumor",
                "告示贴进了营中",
                "有人把官军悬赏告示贴在营门：献出你的首级，可免本村赋税。军中又传言主将私藏了粮食。几名士卒已经围住司粮官。",
                "公开全部粮册",
                "军心 -2，民望 +10，谣言平息",
                delegate
                {
                    realtime.Morale -= 2;
                    realtime.Support += 10;
                    AddRealtimeLog("粮袋当众清点，军中看见主将所得与士卒相同。");
                },
                "斩杀传播者",
                "军心 +5，民望 -12",
                delegate
                {
                    realtime.Morale += 5;
                    realtime.Support -= 12;
                    realtime.Cruelty += 1;
                    AddRealtimeLog("传播告示者被斩，营中再无人议论粮册。");
                });
        }

        private void ShowSnowIncident()
        {
            ShowUnexpectedIncident(
                "snow",
                "北风卷雪",
                "白马河上忽然起了大雪。道路很快被掩没，辎重车轮不断打滑。停下整队会失去时间，强行军则会有人掉队。",
                "扎营避雪",
                "官军合围提前18秒，军心 +5",
                delegate
                {
                    realtime.Deadline -= 18;
                    realtime.Morale += 5;
                    AddRealtimeLog("全军背风扎营，官军却趁雪逼近。");
                },
                "顶雪强行军",
                "主力、游骑各损失5人，军心 -4",
                delegate
                {
                    ApplyUnitLoss("主力营", 5);
                    ApplyUnitLoss("游骑哨", 5);
                    realtime.Morale -= 4;
                    AddRealtimeLog("队伍顶雪前进，十人掉队，再未归营。");
                });
        }

        private void ApplyUnitLoss(string unitName, int amount)
        {
            BattleFieldUnit unit = realtime.Unit(unitName);
            if (unit == null || !unit.Alive)
                return;
            unit.Strength = Math.Max(0, unit.Strength - amount);
            if (unit.Strength <= 0)
            {
                unit.Alive = false;
                ClearUnitRoute(unit);
                unit.Blocking = false;
                unit.AmbushReady = false;
            }
        }

        private void RestoreUnit(string unitName, int amount)
        {
            BattleFieldUnit unit = realtime.Unit(unitName);
            if (unit == null || !unit.Alive)
                return;
            unit.Strength = Math.Min(unit.MaxStrength, unit.Strength + amount);
        }

        private void AddVolunteers(string unitName, int amount)
        {
            BattleFieldUnit unit = realtime.Unit(unitName);
            if (unit == null || !unit.Alive)
                return;
            unit.MaxStrength += amount;
            unit.Strength += amount;
        }

        private void CheckRealtimeEnd()
        {
            if (realtime.Finished)
                return;
            BattleFieldUnit carrier = realtime.Unit("辎重队");
            BattleFieldUnit main = realtime.Unit("主力营");
            BattleFieldUnit scout = realtime.Unit("游骑哨");
            if (carrier == null || !carrier.Alive)
            {
                FinishRealtimeBattle(false, "粮车尽失");
                return;
            }
            if ((main == null || !main.Alive) && (scout == null || !scout.Alive))
            {
                FinishRealtimeBattle(false, "诸营皆溃");
                return;
            }
            if (realtime.Morale <= 0)
            {
                FinishRealtimeBattle(false, "军心离散");
                return;
            }
            if (realtime.Elapsed >= realtime.Deadline)
                FinishRealtimeBattle(false, "官军合围");
        }

        private void ShowRealtimeDecision(string title, string body,
            string firstTitle, string firstHint, Action firstAction,
            string secondTitle, string secondHint, Action secondAction)
        {
            ShowRealtimePopup("战场决策", GuessEventScene(title), title, body,
                firstTitle, firstHint, firstAction, secondTitle, secondHint, secondAction);
        }

        private void ShowUnexpectedIncident(string sceneKind, string title, string body,
            string firstTitle, string firstHint, Action firstAction,
            string secondTitle, string secondHint, Action secondAction)
        {
            ShowRealtimePopup("突发事件", sceneKind, title, body,
                firstTitle, firstHint, firstAction, secondTitle, secondHint, secondAction);
        }

        private string GuessEventScene(string title)
        {
            if (title.Contains("仓"))
                return "granary";
            if (title.Contains("母") || title.Contains("流民"))
                return "refugees";
            if (title.Contains("伤"))
                return "wounded";
            return "village";
        }

        private void ShowRealtimePopup(string kicker, string sceneKind, string title, string body,
            string firstTitle, string firstHint, Action firstAction,
            string secondTitle, string secondHint, Action secondAction)
        {
            realtimeChoiceOpen = true;
            realtimePaused = true;

            Panel box = new Panel();
            box.Size = new Size(820, 460);
            box.Left = (ClientSize.Width - box.Width) / 2;
            box.Top = (ClientSize.Height - box.Height) / 2;
            box.Anchor = AnchorStyles.None;
            box.BackColor = Color.FromArgb(225, 211, 181);
            box.BorderStyle = BorderStyle.FixedSingle;
            stage.Controls.Add(box);
            box.BringToFront();

            Label seal = MakeLabel(kicker == "突发事件" ? "变" : "决", 20F, FontStyle.Bold, Red);
            seal.TextAlign = ContentAlignment.MiddleCenter;
            seal.SetBounds(286, 25, 48, 48);
            box.Controls.Add(seal);

            Label typeLabel = MakeLabel(kicker, 9F, FontStyle.Bold, Red);
            typeLabel.SetBounds(342, 22, 160, 23);
            box.Controls.Add(typeLabel);

            Label heading = MakeLabel(title, 22F, FontStyle.Bold, Ink);
            heading.SetBounds(342, 46, 430, 45);
            box.Controls.Add(heading);

            Label description = MakeLabel(body, 11F, FontStyle.Regular,
                Color.FromArgb(62, 53, 44));
            description.SetBounds(342, 99, 430, 93);
            box.Controls.Add(description);

            RealtimeEventArtPanel art = new RealtimeEventArtPanel(sceneKind);
            art.SetBounds(28, 28, 232, 362);
            box.Controls.Add(art);

            Label artCaption = MakeLabel("局势会因你的选择继续变化", 8.5F,
                FontStyle.Regular, Color.FromArgb(112, 94, 71));
            artCaption.TextAlign = ContentAlignment.MiddleCenter;
            artCaption.SetBounds(28, 398, 232, 25);
            box.Controls.Add(artCaption);

            Button first = MakeChoiceButton();
            first.Text = "【一】 " + firstTitle + "\r\n      " + firstHint;
            first.SetBounds(342, 207, 430, 78);
            box.Controls.Add(first);

            Button second = MakeChoiceButton();
            second.Text = "【二】 " + secondTitle + "\r\n      " + secondHint;
            second.SetBounds(342, 300, 430, 78);
            box.Controls.Add(second);

            Label pauseNote = MakeLabel("事件发生时战场已经自动暂停", 8.5F,
                FontStyle.Regular, Color.FromArgb(126, 109, 84));
            pauseNote.TextAlign = ContentAlignment.MiddleRight;
            pauseNote.SetBounds(342, 391, 430, 24);
            box.Controls.Add(pauseNote);

            Action close = delegate
            {
                realtime.Normalize();
                stage.Controls.Remove(box);
                box.Dispose();
                realtimeChoiceOpen = false;
                realtimePaused = false;
                rtPauseButton.Text = "暂停";
                UpdateRealtimeUi();
            };
            first.Click += delegate { firstAction(); close(); };
            second.Click += delegate { secondAction(); close(); };
        }

        private void FinishRealtimeBattle(bool victory, string title)
        {
            if (realtime.Finished)
                return;
            realtime.Finished = true;
            StopRealtimeTimer();
            realtime.Normalize();

            Panel result = new Panel();
            result.Size = new Size(720, 500);
            result.Left = (ClientSize.Width - result.Width) / 2;
            result.Top = (ClientSize.Height - result.Height) / 2;
            result.Anchor = AnchorStyles.None;
            result.BackColor = Color.FromArgb(228, 215, 187);
            result.BorderStyle = BorderStyle.FixedSingle;
            stage.Controls.Add(result);
            result.BringToFront();

            Label era = MakeLabel(victory ? "白马河战事暂歇" : "临河之众败散", 10F,
                FontStyle.Regular, Color.FromArgb(132, 107, 77));
            era.TextAlign = ContentAlignment.MiddleCenter;
            era.SetBounds(30, 28, 660, 28);
            result.Controls.Add(era);

            Label heading = MakeLabel(title, 29F, FontStyle.Bold,
                victory ? Color.FromArgb(91, 111, 78) : Red);
            heading.TextAlign = ContentAlignment.MiddleCenter;
            heading.SetBounds(30, 62, 660, 66);
            result.Controls.Add(heading);

            string resultText;
            if (victory)
            {
                if (realtime.Support >= 60 && realtime.Cruelty < 3)
                    resultText = "粮车已过南渡口。沿途村民替你们指路、抬伤兵，也分得了一部分官仓粮。你赢下的不是一块地图，而是让一些人多活过了一个冬天。";
                else if (realtime.Support < 30 || realtime.Cruelty >= 4)
                    resultText = "粮车已过南渡口，军中暂时不会断粮。但沿路村门紧闭，没有人替伤兵停步。你夺到了粮，也开始变成百姓眼中的另一支兵。";
                else
                    resultText = "粮车已过南渡口。官军未能合围，但这一路仍留下许多死伤。胜利只是争得几日喘息，下一次选择很快就会到来。";
            }
            else
            {
                resultText = "官军重新控制白马河一带，军簿只记‘击溃流贼’。那些死在路旁、渡口和粮车边的人，没有留下姓名。";
            }

            Label body = MakeLabel(resultText, 11F, FontStyle.Regular,
                Color.FromArgb(58, 49, 41));
            body.SetBounds(70, 150, 580, 86);
            body.TextAlign = ContentAlignment.MiddleCenter;
            result.Controls.Add(body);

            int playerLeft = realtime.PlayerStrength();
            int losses = Math.Max(0, 287 - playerLeft);
            Label numbers = MakeLabel(
                "战后存者　" + playerLeft + "人　　　死散　" + losses + "人\r\n" +
                "余粮　" + realtime.Food + "日　　　军心　" + realtime.Morale +
                "　　　民望　" + realtime.Support,
                11F, FontStyle.Bold, Color.FromArgb(122, 91, 54));
            numbers.TextAlign = ContentAlignment.MiddleCenter;
            numbers.SetBounds(58, 260, 604, 72);
            result.Controls.Add(numbers);

            Label note = MakeLabel(
                victory ? "你可以重新开始，尝试不同的调兵顺序和粮食分配。" : "失败并不只发生在倒计时结束时，它也发生在每一次被放弃的人身上。",
                9.5F, FontStyle.Regular, Color.FromArgb(118, 101, 79));
            note.TextAlign = ContentAlignment.MiddleCenter;
            note.SetBounds(50, 344, 620, 30);
            result.Controls.Add(note);

            Button replay = MakePrimaryButton("重新部署");
            replay.SetBounds(208, 400, 150, 50);
            replay.Click += delegate { StartRealtimeDemo(); };
            result.Controls.Add(replay);

            Button story = MakeChoiceButton();
            story.Text = "返回标题";
            story.TextAlign = ContentAlignment.MiddleCenter;
            story.SetBounds(374, 400, 140, 50);
            story.Click += delegate { ShowTitleScreen(); };
            result.Controls.Add(story);
        }

        private void UpdateRealtimeUi()
        {
            if (realtime == null || rtFood == null)
                return;
            rtFood.Text = realtime.Food + "日";
            rtTroops.Text = realtime.PlayerStrength().ToString();
            rtMorale.Text = realtime.Morale.ToString();
            if (realtime.Support < 15)
            {
                rtSupport.Text = realtime.Support + " 崩溃";
                rtSupport.ForeColor = Color.FromArgb(211, 66, 57);
            }
            else if (realtime.Support < 30)
            {
                rtSupport.Text = realtime.Support + " 低迷";
                rtSupport.ForeColor = Color.FromArgb(211, 132, 63);
            }
            else if (realtime.Support >= 65)
            {
                rtSupport.Text = realtime.Support + " 拥护";
                rtSupport.ForeColor = Color.FromArgb(96, 176, 112);
            }
            else
            {
                rtSupport.Text = realtime.Support.ToString();
                rtSupport.ForeColor = Color.FromArgb(106, 158, 120);
            }
            int remain = Math.Max(0, (int)Math.Ceiling(realtime.Deadline - realtime.Elapsed));
            rtClock.Text = "距官军合围 " + (remain / 60).ToString("00") + ":" + (remain % 60).ToString("00");

            UpdateUnitButton(rtMainButton, realtime.Unit("主力营"));
            UpdateUnitButton(rtScoutButton, realtime.Unit("游骑哨"));
            UpdateUnitButton(rtCarrierButton, realtime.Unit("辎重队"));

            if (selectedRealtimeUnit != null)
            {
                rtSelected.Text = "当前选择：" + selectedRealtimeUnit.Name + "\r\n军令：" + selectedRealtimeUnit.Order;
            }

            BattleFieldUnit carrier = realtime.Unit("辎重队");
            string first = realtime.GuardDefeated ? "✓" : "①";
            string second = carrier != null && carrier.Loaded ? "✓" : "②";
            string third = "③";
            rtObjective.Text = "军令\r\n" + first + " 击败仓守乡勇\r\n" +
                second + " 辎重队进入官仓装粮\r\n" + third + " 护送辎重队抵达南渡口";
        }

        private void UpdateUnitButton(Button button, BattleFieldUnit unit)
        {
            if (button == null || unit == null)
                return;
            button.Text = unit.Name + "　" + Math.Max(0, (int)Math.Ceiling(unit.Strength)) + "人\r\n" + unit.Order;
            button.Enabled = unit.Alive;
            if (!unit.Alive)
                button.Text = unit.Name + "　已溃散";
            button.FlatAppearance.BorderColor = unit == selectedRealtimeUnit
                ? Color.FromArgb(202, 168, 83)
                : Color.FromArgb(91, 77, 61);
            button.FlatAppearance.BorderSize = unit == selectedRealtimeUnit ? 2 : 1;
        }

        private void AddRealtimeLog(string message)
        {
            if (realtime == null)
                return;
            realtime.Log.Add(message);
            while (realtime.Log.Count > 5)
                realtime.Log.RemoveAt(0);
            if (rtLog != null)
            {
                StringBuilder b = new StringBuilder();
                for (int i = 0; i < realtime.Log.Count; i++)
                {
                    if (i > 0)
                        b.Append("\r\n");
                    b.Append("· ");
                    b.Append(realtime.Log[i]);
                }
                rtLog.Text = b.ToString();
            }
        }

        private void StopRealtimeTimer()
        {
            if (realtimeTimer != null)
            {
                realtimeTimer.Stop();
                realtimeTimer.Dispose();
                realtimeTimer = null;
            }
        }
    }
}
