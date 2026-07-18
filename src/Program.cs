using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace HuangYueDemo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class GameState
    {
        public int Day = 1;
        public int Food = 3;
        public int People = 287;
        public int Morale = 48;
        public int Support = 35;
        public int Disease = 12;
        public int Cruelty = 0;
        public readonly HashSet<string> Flags = new HashSet<string>();
        public readonly List<string> Records = new List<string>();

        public bool Has(string flag)
        {
            return Flags.Contains(flag);
        }

        public void Mark(string flag)
        {
            Flags.Add(flag);
        }

        public void Normalize()
        {
            Food = Math.Max(0, Math.Min(10, Food));
            People = Math.Max(0, People);
            Morale = Math.Max(0, Math.Min(100, Morale));
            Support = Math.Max(0, Math.Min(100, Support));
            Disease = Math.Max(0, Math.Min(100, Disease));
        }
    }

    internal sealed class StoryChoice
    {
        public string Title;
        public string Hint;
        public Action<GameState> Apply;

        public StoryChoice(string title, string hint, Action<GameState> apply)
        {
            Title = title;
            Hint = hint;
            Apply = apply;
        }
    }

    internal sealed class StoryEvent
    {
        public string Date;
        public string Place;
        public string Title;
        public string Body;
        public StoryChoice First;
        public StoryChoice Second;

        public StoryEvent(string date, string place, string title, string body,
            StoryChoice first, StoryChoice second)
        {
            Date = date;
            Place = place;
            Title = title;
            Body = body;
            First = first;
            Second = second;
        }
    }

    internal sealed partial class MainForm : Form
    {
        private readonly Color Ink = Color.FromArgb(40, 34, 29);
        private readonly Color Red = Color.FromArgb(139, 45, 38);
        private readonly Color Paper = Color.FromArgb(224, 211, 181);
        private readonly Color DarkPaper = Color.FromArgb(198, 180, 145);

        private GameState state;
        private List<StoryEvent> events;
        private int eventIndex;

        private Panel stage;
        private Label dayLabel;
        private Label placeLabel;
        private Label titleLabel;
        private Label bodyLabel;
        private Label foodValue;
        private Label peopleValue;
        private Label moraleValue;
        private Label supportValue;
        private Label diseaseValue;
        private Label lastRecordLabel;
        private Button choiceOne;
        private Button choiceTwo;

        public MainForm()
        {
            Text = "黄钺：七日粮（Demo v0.4）";
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(960, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 22, 20);
            ForeColor = Ink;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;
            ShowTitleScreen();
        }

        private void ClearStage()
        {
            Controls.Clear();
            stage = new Panel();
            stage.Dock = DockStyle.Fill;
            stage.BackColor = Color.FromArgb(29, 26, 23);
            Controls.Add(stage);
        }

        private Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", size, style);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoSize = false;
            return label;
        }

        private void ShowTitleScreen()
        {
            ClearStage();

            InkLandscapePanel landscape = new InkLandscapePanel();
            landscape.Dock = DockStyle.Fill;
            stage.Controls.Add(landscape);

            Panel veil = new Panel();
            veil.BackColor = Color.FromArgb(215, 30, 27, 24);
            veil.Size = new Size(640, 610);
            veil.Location = new Point((ClientSize.Width - veil.Width) / 2, 72);
            veil.Anchor = AnchorStyles.Top;
            landscape.Controls.Add(veil);

            Label era = MakeLabel("大明崇祯十四年 · 河南", 12F, FontStyle.Regular,
                Color.FromArgb(185, 166, 132));
            era.TextAlign = ContentAlignment.MiddleCenter;
            era.SetBounds(20, 48, 600, 30);
            veil.Controls.Add(era);

            Label logo = MakeLabel("黄  钺", 48F, FontStyle.Bold, Paper);
            logo.TextAlign = ContentAlignment.MiddleCenter;
            logo.SetBounds(20, 90, 600, 88);
            veil.Controls.Add(logo);

            Panel redLine = new Panel();
            redLine.BackColor = Red;
            redLine.SetBounds(216, 185, 208, 3);
            veil.Controls.Add(redLine);

            Label subtitle = MakeLabel("七 日 粮", 18F, FontStyle.Regular, DarkPaper);
            subtitle.TextAlign = ContentAlignment.MiddleCenter;
            subtitle.SetBounds(20, 200, 600, 40);
            veil.Controls.Add(subtitle);

            Label intro = MakeLabel(
                "大旱之后，你带着二百八十七名饥民与义军来到临河县。\r\n" +
                "军中之粮，只够三日。\r\n\r\n" +
                "你要让他们活下去。\r\n只是活下去，往往也要别人付出代价。",
                11.5F, FontStyle.Regular, Color.FromArgb(206, 194, 169));
            intro.TextAlign = ContentAlignment.MiddleCenter;
            intro.SetBounds(70, 258, 500, 150);
            veil.Controls.Add(intro);

            Button start = MakePrimaryButton("进入实时战场（新）");
            start.SetBounds(170, 420, 300, 56);
            start.Click += delegate { StartRealtimeDemo(); };
            veil.Controls.Add(start);

            Button story = MakeChoiceButton();
            story.Text = "游玩七日文字版";
            story.TextAlign = ContentAlignment.MiddleCenter;
            story.SetBounds(205, 490, 230, 44);
            story.Click += delegate { StartGame(); };
            veil.Controls.Add(story);

            Label note = MakeLabel("个人制作原型 · 县名与人物均为虚构", 9F,
                FontStyle.Regular, Color.FromArgb(132, 122, 105));
            note.TextAlign = ContentAlignment.MiddleCenter;
            note.SetBounds(20, 557, 600, 28);
            veil.Controls.Add(note);

            Resize += CenterTitlePanel;
        }

        private void CenterTitlePanel(object sender, EventArgs e)
        {
            if (stage == null || stage.Controls.Count == 0)
                return;
            Control landscape = stage.Controls[0];
            if (landscape.Controls.Count == 0)
                return;
            Control veil = landscape.Controls[0];
            veil.Left = Math.Max(20, (ClientSize.Width - veil.Width) / 2);
        }

        private Button MakePrimaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            button.ForeColor = Color.FromArgb(239, 225, 196);
            button.BackColor = Red;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(174, 77, 62);
            button.FlatAppearance.BorderSize = 1;
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void StartGame()
        {
            Resize -= CenterTitlePanel;
            state = new GameState();
            events = BuildEvents();
            eventIndex = 0;
            BuildGameScreen();
            PresentEvent();
        }

        private void BuildGameScreen()
        {
            ClearStage();

            TableLayoutPanel frame = new TableLayoutPanel();
            frame.Dock = DockStyle.Fill;
            frame.Padding = new Padding(28, 22, 28, 24);
            frame.BackColor = Color.FromArgb(31, 28, 25);
            frame.ColumnCount = 1;
            frame.RowCount = 3;
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            stage.Controls.Add(frame);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(39, 35, 31);
            frame.Controls.Add(header, 0, 0);

            dayLabel = MakeLabel("第一日", 21F, FontStyle.Bold, Paper);
            dayLabel.SetBounds(24, 14, 180, 38);
            header.Controls.Add(dayLabel);

            placeLabel = MakeLabel("临河县", 9.5F, FontStyle.Regular,
                Color.FromArgb(170, 154, 126));
            placeLabel.SetBounds(26, 52, 240, 22);
            header.Controls.Add(placeLabel);

            FlowLayoutPanel stats = new FlowLayoutPanel();
            stats.FlowDirection = FlowDirection.LeftToRight;
            stats.WrapContents = false;
            stats.BackColor = Color.Transparent;
            stats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            stats.SetBounds(436, 10, 650, 66);
            header.Controls.Add(stats);

            foodValue = AddStat(stats, "余粮", "3日", Color.FromArgb(211, 181, 106));
            peopleValue = AddStat(stats, "部众", "287", Color.FromArgb(204, 200, 184));
            moraleValue = AddStat(stats, "军心", "48", Color.FromArgb(185, 91, 73));
            supportValue = AddStat(stats, "民望", "35", Color.FromArgb(111, 157, 123));
            diseaseValue = AddStat(stats, "疫病", "12", Color.FromArgb(153, 126, 157));

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(0, 18, 0, 10);
            content.BackColor = Color.Transparent;
            content.ColumnCount = 2;
            content.RowCount = 1;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            frame.Controls.Add(content, 0, 1);

            Panel paper = new Panel();
            paper.Dock = DockStyle.Fill;
            paper.Padding = new Padding(38, 28, 38, 28);
            paper.BackColor = Color.FromArgb(218, 204, 175);
            content.Controls.Add(paper, 0, 0);

            Label scrollMark = MakeLabel("录", 24F, FontStyle.Bold,
                Color.FromArgb(151, 54, 45));
            scrollMark.TextAlign = ContentAlignment.MiddleCenter;
            scrollMark.SetBounds(38, 26, 48, 48);
            paper.Controls.Add(scrollMark);

            titleLabel = MakeLabel("", 22F, FontStyle.Bold, Ink);
            titleLabel.SetBounds(100, 28, 610, 52);
            titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            paper.Controls.Add(titleLabel);

            bodyLabel = MakeLabel("", 12F, FontStyle.Regular,
                Color.FromArgb(54, 47, 40));
            bodyLabel.SetBounds(42, 96, 674, 184);
            bodyLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            paper.Controls.Add(bodyLabel);

            Panel divider = new Panel();
            divider.BackColor = Color.FromArgb(151, 135, 105);
            divider.SetBounds(42, 286, 674, 1);
            divider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            paper.Controls.Add(divider);

            choiceOne = MakeChoiceButton();
            choiceOne.SetBounds(42, 310, 674, 86);
            choiceOne.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            choiceOne.Click += delegate { Choose(0); };
            paper.Controls.Add(choiceOne);

            choiceTwo = MakeChoiceButton();
            choiceTwo.SetBounds(42, 410, 674, 86);
            choiceTwo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            choiceTwo.Click += delegate { Choose(1); };
            paper.Controls.Add(choiceTwo);

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.Margin = new Padding(18, 0, 0, 0);
            side.BackColor = Color.FromArgb(42, 38, 34);
            content.Controls.Add(side, 1, 0);

            Label sideTitle = MakeLabel("军 中 记 事", 12F, FontStyle.Bold, DarkPaper);
            sideTitle.TextAlign = ContentAlignment.MiddleCenter;
            sideTitle.SetBounds(18, 26, 320, 32);
            sideTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(sideTitle);

            Panel sideLine = new Panel();
            sideLine.BackColor = Color.FromArgb(91, 79, 65);
            sideLine.SetBounds(28, 67, 300, 1);
            sideLine.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            side.Controls.Add(sideLine);

            lastRecordLabel = MakeLabel(
                "军中存粮只够三日。\r\n\r\n陈二娘带着两个孩子缩在营火旁。\r\n\r\n石头问：‘明日还有粥么？’",
                10.5F, FontStyle.Regular, Color.FromArgb(196, 184, 162));
            lastRecordLabel.SetBounds(28, 88, 300, 360);
            lastRecordLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            side.Controls.Add(lastRecordLabel);

            Label prototype = MakeLabel("试玩原型 v0.1", 8.5F, FontStyle.Regular,
                Color.FromArgb(111, 100, 86));
            prototype.TextAlign = ContentAlignment.BottomRight;
            prototype.SetBounds(24, 474, 306, 28);
            prototype.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            side.Controls.Add(prototype);

            Label footer = MakeLabel(
                "所有选择都会被记录。史书只写一句，承受它的却是一个个具体的人。",
                9.5F, FontStyle.Regular, Color.FromArgb(157, 143, 120));
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.Dock = DockStyle.Fill;
            frame.Controls.Add(footer, 0, 2);
        }

        private Label AddStat(FlowLayoutPanel parent, string name, string value, Color valueColor)
        {
            Panel box = new Panel();
            box.Size = new Size(118, 60);
            box.Margin = new Padding(4, 0, 4, 0);
            box.BackColor = Color.FromArgb(48, 43, 38);

            Label nameLabel = MakeLabel(name, 8.5F, FontStyle.Regular,
                Color.FromArgb(145, 132, 112));
            nameLabel.TextAlign = ContentAlignment.MiddleCenter;
            nameLabel.SetBounds(5, 5, 108, 20);
            box.Controls.Add(nameLabel);

            Label valueLabel = MakeLabel(value, 15F, FontStyle.Bold, valueColor);
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;
            valueLabel.SetBounds(5, 25, 108, 29);
            box.Controls.Add(valueLabel);

            parent.Controls.Add(box);
            return valueLabel;
        }

        private Button MakeChoiceButton()
        {
            Button button = new Button();
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(142, 123, 92);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 187, 151);
            button.BackColor = Color.FromArgb(210, 195, 164);
            button.ForeColor = Ink;
            button.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(18, 5, 14, 5);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void PresentEvent()
        {
            StoryEvent current = events[eventIndex];
            state.Day = eventIndex + 1;
            dayLabel.Text = NumberToChineseDay(state.Day);
            placeLabel.Text = current.Date + "　·　" + current.Place;
            titleLabel.Text = current.Title;
            bodyLabel.Text = current.Body;
            choiceOne.Text = "【一】 " + current.First.Title + "\r\n      " + current.First.Hint;
            choiceTwo.Text = "【二】 " + current.Second.Title + "\r\n      " + current.Second.Hint;
            choiceOne.Enabled = true;
            choiceTwo.Enabled = true;
            UpdateStats();
        }

        private string NumberToChineseDay(int day)
        {
            string[] names = { "", "第一日", "第二日", "第三日", "第四日", "第五日", "第六日", "第七日" };
            return day >= 1 && day <= 7 ? names[day] : "第" + day + "日";
        }

        private void Choose(int index)
        {
            choiceOne.Enabled = false;
            choiceTwo.Enabled = false;
            StoryEvent current = events[eventIndex];
            StoryChoice selected = index == 0 ? current.First : current.Second;
            selected.Apply(state);
            state.Normalize();
            UpdateStats();
            UpdateRecordPanel();

            eventIndex++;
            if (eventIndex >= events.Count)
            {
                ShowEnding();
                return;
            }

            Timer pause = new Timer();
            pause.Interval = 420;
            pause.Tick += delegate
            {
                pause.Stop();
                pause.Dispose();
                PresentEvent();
            };
            pause.Start();
        }

        private void UpdateStats()
        {
            foodValue.Text = state.Food + "日";
            peopleValue.Text = state.People.ToString();
            moraleValue.Text = state.Morale.ToString();
            supportValue.Text = state.Support.ToString();
            diseaseValue.Text = state.Disease.ToString();
        }

        private void UpdateRecordPanel()
        {
            StringBuilder text = new StringBuilder();
            int start = Math.Max(0, state.Records.Count - 4);
            for (int i = start; i < state.Records.Count; i++)
            {
                if (text.Length > 0)
                    text.Append("\r\n\r\n");
                text.Append("· ");
                text.Append(state.Records[i]);
            }
            lastRecordLabel.Text = text.ToString();
        }

        private List<StoryEvent> BuildEvents()
        {
            List<StoryEvent> list = new List<StoryEvent>();

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初三", "临河县北·乱葬岗",
                "仓门之外",
                "探子来报：官仓今晚要把最后一批粮运往府城。\r\n" +
                "柳沟村尚有一点冬粮，但那也是村民留给老人和孩子的口粮。\r\n\r\n" +
                "韩九按着刀说：‘等天亮，咱们就只能吃树皮了。’",
                new StoryChoice("夜袭官仓", "粮多，但守仓者也是被征来的乡勇。",
                    delegate(GameState s)
                    {
                        s.Food += 5; s.People -= 18; s.Morale += 8; s.Support += 4;
                        s.Mark("raided_granary"); s.Mark("zhou_helped");
                        s.Records.Add("夜袭官仓，得粮百余石，死十八人。仓吏周禾生暗开了西门。");
                    }),
                new StoryChoice("向柳沟村借粮", "所得有限，并许诺来年加倍归还。",
                    delegate(GameState s)
                    {
                        s.Food += 2; s.Morale -= 3; s.Support += 14;
                        s.Mark("borrowed_grain");
                        s.Records.Add("柳沟村借出冬粮。赵先生写下借契，按了你的手印。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初四", "白马河畔·流民营",
                "营外的母子",
                "三十六名流民跟了上来，其中已有七人发热。守营者不肯开栅。\r\n" +
                "陈二娘抱着幼子站在最前面。她说自己能做饭，也能替伤兵洗布。\r\n\r\n" +
                "军医只说了一句：‘让他们进来，疫气也会进来。’",
                new StoryChoice("开栅收容", "多三十六张嘴，也不把他们留在寒夜里。",
                    delegate(GameState s)
                    {
                        s.People += 36; s.Food -= 1; s.Support += 15; s.Disease += 20;
                        s.Mark("sheltered_chen");
                        s.Records.Add("开栅收容流民三十六人。陈二娘入营，幼子当夜高热。");
                    }),
                new StoryChoice("隔营留粥", "不准入营，在河湾搭棚，每日送一锅稀粥。",
                    delegate(GameState s)
                    {
                        s.Food -= 1; s.Support += 5; s.Disease += 5; s.Morale += 2;
                        s.Mark("left_chen_outside");
                        s.Records.Add("流民留在河湾。军中每日送粥一锅，次晨少了九个人。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初五", "柳沟村",
                "被夺走的半袋粟",
                "三名士卒闯入柳沟村，抢走一户人家的半袋粟，还打伤了拦门的老人。\r\n" +
                "韩九说军中刚有粮，不能为了一个村民坏了弟兄义气。\r\n\r\n" +
                "村民都站在祠堂外，等你处置。",
                new StoryChoice("依军令重惩", "归还粮食，杖责首犯，逐出队伍。",
                    delegate(GameState s)
                    {
                        s.People -= 1; s.Food -= 1; s.Morale -= 8; s.Support += 17;
                        s.Mark("punished_looters");
                        s.Records.Add("惩治劫粮者，归还半袋粟。韩九自此与你生隙。");
                    }),
                new StoryChoice("袒护士卒", "乱世之中，先保住愿意替你作战的人。",
                    delegate(GameState s)
                    {
                        s.Food += 1; s.Morale += 10; s.Support -= 22; s.Cruelty += 2;
                        s.Mark("protected_looters");
                        s.Records.Add("劫粮者未受惩处。当夜，柳沟村有七户人家逃走。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初六", "周氏庄园",
                "高墙之内",
                "周氏庄园的地窖里藏着粮。庄门外却聚着数百名佃户，他们同样等着开仓。\r\n" +
                "寨墙不高，强攻可破；赵先生则说，可以用先前写下的借契说服佃户开门。\r\n\r\n" +
                "日落之前，必须有所决断。",
                new StoryChoice("破门开仓", "先夺下粮食，之后再谈如何分配。",
                    delegate(GameState s)
                    {
                        s.Food += 5; s.People -= 27; s.Morale += 7; s.Support += 3; s.Disease += 4;
                        s.Cruelty += 1; s.Mark("stormed_manor"); s.Mark("stone_wounded");
                        s.Records.Add("强攻周庄，死二十七人。石头左肩中箭，地窖粮食尽数运走。");
                    }),
                new StoryChoice("与佃户立约", "佃户开门，粮食由军中与庄户平分。",
                    delegate(GameState s)
                    {
                        s.Food += s.Has("borrowed_grain") ? 4 : 3;
                        s.Morale -= 5; s.Support += 12; s.Mark("shared_manor_grain");
                        s.Records.Add(s.Has("borrowed_grain")
                            ? "赵先生出示柳沟借契，佃户遂开庄门。军民平分窖粮。"
                            : "与佃户约法分粮。虽所得不多，庄中无人死伤。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初七", "临河县南门",
                "城中的万人",
                "县城即将封门。县令要把税粮和壮丁一起送走，城南已有饥民冲撞门禁。\r\n" +
                "周禾生知道运粮车经过的小路；陈二娘却求你打开南门，让城中百姓逃生。\r\n\r\n" +
                "两件事只能做一件。",
                new StoryChoice("截夺税粮车", "以小队伏击运粮队，补足军中粮草。",
                    delegate(GameState s)
                    {
                        int loss = s.Has("zhou_helped") ? 11 : 23;
                        s.Food += 4; s.People -= loss; s.Morale += 7; s.Support -= 4;
                        s.Mark("seized_tax_grain");
                        s.Records.Add("截得税粮二十七车，伏击中死" + loss + "人。周禾生此后失踪。");
                    }),
                new StoryChoice("接应百姓出城", "弃掉两日军粮，在南门外开出一条路。",
                    delegate(GameState s)
                    {
                        s.Food -= 2; s.People += 80; s.Morale -= 4; s.Support += 18; s.Disease += 9;
                        s.Mark("opened_south_gate");
                        s.Records.Add("接应八十名百姓出城。队伍更长了，粮袋也更空了。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初八", "白马河渡口",
                "追兵已至",
                "官军前锋已能望见旗号。渡河还需两个时辰，队尾尽是老人、伤兵和孩子。\r\n" +
                "韩九主张烧桥先走；石头把一根长矛插在冻土里，说总得有人守住渡口。\r\n\r\n" +
                "风从北面来，带着马蹄声。",
                new StoryChoice("留下断后", "选出能战者守住渡口，直到百姓过河。",
                    delegate(GameState s)
                    {
                        int loss = s.Has("stone_wounded") ? 56 : 45;
                        s.People -= loss; s.Food -= 1; s.Morale += 8; s.Support += 10;
                        s.Mark("defended_ford");
                        s.Records.Add("白马河断后，死" + loss + "人。石头留在最后一排。");
                    }),
                new StoryChoice("烧桥先行", "保住主力，桥另一头的人只能各安天命。",
                    delegate(GameState s)
                    {
                        s.People -= 8; s.Food -= 1; s.Morale += 3; s.Support -= 15; s.Cruelty += 2;
                        s.Mark("burned_bridge");
                        s.Records.Add("桥火燃起时，尚有数十人在北岸。哭喊声直到后半夜才停。");
                    })));

            list.Add(new StoryEvent(
                "崇祯十四年·冬月初九", "白马河南岸",
                "第七日",
                "你们暂时甩开了追兵。西面有大股义军经过，愿收下所有能拿兵器的人。\r\n" +
                "南面群山中有几处废村，也许能让百姓熬过这个冬天。\r\n\r\n" +
                "七日已尽。赵先生铺开纸，等你说出最后的决定。",
                new StoryChoice("投奔大军", "留下老弱，把能战者带入更大的战争。",
                    delegate(GameState s)
                    {
                        s.Morale += 12; s.Mark("joined_rebellion");
                        s.Records.Add("能战者随你西行。老弱留在白马河南岸，自寻生路。");
                    }),
                new StoryChoice("护送百姓入山", "不争一城一地，先让这支队伍活过冬天。",
                    delegate(GameState s)
                    {
                        s.Food -= 2; s.Morale -= 5; s.Support += 12; s.Mark("escorted_people");
                        s.Records.Add("全队转向南山。有人说这是退却，也有人第一次看见了活路。");
                    })));

            return list;
        }

        private void ShowEnding()
        {
            ClearStage();

            EndingScenePanel scene = new EndingScenePanel();
            scene.Dock = DockStyle.Fill;
            stage.Controls.Add(scene);

            TableLayoutPanel ending = new TableLayoutPanel();
            ending.Dock = DockStyle.Fill;
            ending.Padding = new Padding(58, 34, 58, 36);
            ending.BackColor = Color.Transparent;
            ending.ColumnCount = 2;
            ending.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39F));
            ending.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61F));
            scene.Controls.Add(ending);

            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.BackColor = Color.FromArgb(214, 27, 24, 21);
            ending.Controls.Add(left, 0, 0);

            Label finalEra = MakeLabel("大明崇祯十四年 · 冬", 10F, FontStyle.Regular,
                Color.FromArgb(171, 153, 124));
            finalEra.TextAlign = ContentAlignment.MiddleCenter;
            finalEra.SetBounds(20, 34, 350, 25);
            finalEra.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            left.Controls.Add(finalEra);

            string endingTitle = GetEndingTitle();
            Label finalTitle = MakeLabel(endingTitle, 28F, FontStyle.Bold, Paper);
            finalTitle.TextAlign = ContentAlignment.MiddleCenter;
            finalTitle.SetBounds(20, 70, 350, 70);
            finalTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            left.Controls.Add(finalTitle);

            Panel mark = new Panel();
            mark.BackColor = Red;
            mark.SetBounds(142, 148, 106, 3);
            mark.Anchor = AnchorStyles.Top;
            left.Controls.Add(mark);

            Label chronicle = MakeLabel(GetChronicle(), 11F, FontStyle.Regular,
                Color.FromArgb(205, 193, 169));
            chronicle.SetBounds(42, 178, 306, 280);
            chronicle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            left.Controls.Add(chronicle);

            Label totals = MakeLabel(
                "余粮　" + state.Food + "日\r\n" +
                "存者　" + state.People + "人\r\n" +
                "民望　" + state.Support + "\r\n" +
                "疫病　" + state.Disease,
                11F, FontStyle.Bold, Color.FromArgb(185, 160, 102));
            totals.TextAlign = ContentAlignment.MiddleCenter;
            totals.SetBounds(60, 470, 270, 116);
            totals.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            left.Controls.Add(totals);

            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.Margin = new Padding(18, 0, 0, 0);
            right.BackColor = Color.FromArgb(224, 211, 181);
            ending.Controls.Add(right, 1, 0);

            Label fateTitle = MakeLabel("乱 世 录 · 人 物 小 传", 15F, FontStyle.Bold, Ink);
            fateTitle.SetBounds(38, 30, 610, 42);
            fateTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            right.Controls.Add(fateTitle);

            TextBox fates = new TextBox();
            fates.Multiline = true;
            fates.ReadOnly = true;
            fates.ScrollBars = ScrollBars.Vertical;
            fates.BorderStyle = BorderStyle.None;
            fates.BackColor = Color.FromArgb(224, 211, 181);
            fates.ForeColor = Color.FromArgb(51, 44, 37);
            fates.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);
            fates.Text = GetFates();
            fates.SetBounds(40, 82, 600, 462);
            fates.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            right.Controls.Add(fates);

            Button again = MakePrimaryButton("再写一遍这七日");
            again.SetBounds(405, 572, 220, 50);
            again.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            again.Click += delegate { StartGame(); };
            right.Controls.Add(again);

            Button quit = MakeChoiceButton();
            quit.Text = "退出";
            quit.TextAlign = ContentAlignment.MiddleCenter;
            quit.SetBounds(40, 572, 120, 50);
            quit.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            quit.Click += delegate { Close(); };
            right.Controls.Add(quit);

            FadeIn(ending);
        }

        private string GetEndingTitle()
        {
            if (state.Food <= 0 || state.Disease >= 80 || state.People < 100)
                return "饥 疫 之 后";
            if (state.Cruelty >= 4 || state.Support < 20)
                return "黄 钺 易 主";
            if (state.Has("joined_rebellion"))
                return "西 去 无 名";
            return "南 山 微 火";
        }

        private string GetChronicle()
        {
            if (state.Food <= 0)
            {
                return "是岁河南大饥，野无青草，十室九空。临河一带斗米千钱，道殣相望。至腊月，或有父子相弃、甚至人相食者。\r\n\r\n黄钺军行七日而粮尽，众遂离散。";
            }
            if (state.Disease >= 80)
            {
                return "是冬疫疠大作，自流民营传至白马河。死者无棺，生者不敢近。军虽未败于阵，十停中已去其六七。\r\n\r\n后人但记‘群盗自溃’，未载其姓名。";
            }
            if (state.Cruelty >= 4 || state.Support < 20)
            {
                return "初，众以饥而聚，誓开仓活民。及兵势稍盛，所过复征粮夺食，百姓闭户，不敢迎之。\r\n\r\n昔举黄钺以诛暴，未得一城，而暴政之形已生于军中。";
            }
            if (state.Has("joined_rebellion"))
            {
                return "临河之众渡白马河，能战者西从大军。其后攻城陷阵，多死于无名之地。州县文书统称之为‘贼’，姓名无一得录。\r\n\r\n然乱世未平，饥者亦未饱。";
            }
            return "临河之众不争城池，护老弱入南山。是冬苦寒，仍多有死者；幸村火未绝，至来春尚有人下山播种。\r\n\r\n史不载其事，惟山中旧碑有‘七日粮’三字。";
        }

        private string GetFates()
        {
            StringBuilder b = new StringBuilder();

            b.Append("陈二娘，年三十七。\r\n");
            if (state.Has("sheltered_chen") && state.Has("escorted_people") && state.Disease < 70)
                b.Append("本临河县佃户。携二子入军，后随众入南山。幼子熬过疫病，来春母子三人皆在。\r\n\r\n");
            else if (state.Has("sheltered_chen") && state.Disease >= 70)
                b.Append("携二子入营。幼子先染疫，二娘照料病者数日，亦殁。长子后不知所终。\r\n\r\n");
            else if (state.Has("left_chen_outside"))
                b.Append("曾于白马河外求入营，不许。次晨携子离去。后于官军簿册中见同名妇人，真伪不可考。\r\n\r\n");
            else
                b.Append("乱后失其踪迹。\r\n\r\n");

            b.Append("石头，年十七。\r\n");
            if (state.Has("defended_ford"))
                b.Append(state.Has("stone_wounded")
                    ? "周庄中箭，仍留守白马河渡口。战后尸骨不得收，矛折于岸边。\r\n\r\n"
                    : "自请守白马河渡口，使队尾得渡。其后未归，同行者皆以为已死。\r\n\r\n");
            else if (state.Has("joined_rebellion"))
                b.Append("随军西行，三年间转战七县。最后一次名册记其为百人队旗手，年二十。\r\n\r\n");
            else
                b.Append("随众入山。来春为村民修好第一张犁，后改名石生。\r\n\r\n");

            b.Append("赵先生，年四十二。\r\n");
            if (state.Has("shared_manor_grain"))
                b.Append("以借契说服周庄佃户开仓。后将七日所见写成薄册，纸残过半，今只存十三行。\r\n\r\n");
            else if (state.Has("protected_looters"))
                b.Append("见军中袒护劫掠者，自此不再替军中写告示。渡河后独自离去。\r\n\r\n");
            else
                b.Append("一路记录死者姓名。册中共二百一十七人，末页没有落款。\r\n\r\n");

            b.Append("韩九，年三十一。\r\n");
            if (state.Has("punished_looters"))
                b.Append("因军令之争与你生隙，但渡河时仍率十余人殿后。此后投奔别部，死于次年攻城。\r\n\r\n");
            else if (state.Has("burned_bridge"))
                b.Append("主张焚桥，保全军中主力。后来每逢醉酒，便说北岸的哭声仍在耳边。\r\n\r\n");
            else
                b.Append("善战而性烈。七日之后仍在军中，再无确切记载。\r\n\r\n");

            b.Append("周禾生，年十九。\r\n");
            if (state.Has("zhou_helped"))
                b.Append("原为官仓小吏，私开西门，使饥民得粮。后为运粮队认出，是否被杀，无人得知。\r\n\r\n");
            else
                b.Append("原为官仓小吏。临河县封城之后仍在仓中当值，县志只记其‘从贼失踪’。\r\n\r\n");

            b.Append("——黄钺虽在，死者无名。");
            return b.ToString();
        }

        private void FadeIn(Control target)
        {
            target.Visible = true;
        }
    }

    internal sealed class InkLandscapePanel : Panel
    {
        public InkLandscapePanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush sky = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(47, 43, 38), Color.FromArgb(19, 18, 17), 90F))
                g.FillRectangle(sky, ClientRectangle);

            int h = Height;
            int w = Width;
            Point[] far = {
                new Point(0, h * 58 / 100), new Point(w * 12 / 100, h * 42 / 100),
                new Point(w * 23 / 100, h * 55 / 100), new Point(w * 40 / 100, h * 34 / 100),
                new Point(w * 55 / 100, h * 56 / 100), new Point(w * 72 / 100, h * 39 / 100),
                new Point(w, h * 61 / 100), new Point(w, h), new Point(0, h)
            };
            using (SolidBrush mountain = new SolidBrush(Color.FromArgb(50, 13, 13, 12)))
                g.FillPolygon(mountain, far);

            using (Pen river = new Pen(Color.FromArgb(45, 190, 174, 143), 2F))
                g.DrawBezier(river, -20, h * 76 / 100, w * 34 / 100, h * 69 / 100,
                    w * 64 / 100, h * 91 / 100, w + 20, h * 78 / 100);

            using (SolidBrush sun = new SolidBrush(Color.FromArgb(80, 150, 55, 43)))
                g.FillEllipse(sun, w - 250, 95, 86, 86);
        }
    }

    internal sealed class EndingScenePanel : Panel
    {
        public EndingScenePanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush bg = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(52, 45, 38), Color.FromArgb(18, 17, 16), 90F))
                g.FillRectangle(bg, ClientRectangle);

            using (Pen tree = new Pen(Color.FromArgb(100, 10, 9, 8), 8F))
            {
                int x = Width - 135;
                int y = Height - 80;
                g.DrawLine(tree, x, y, x - 12, y - 245);
                tree.Width = 4F;
                g.DrawLine(tree, x - 10, y - 185, x - 70, y - 255);
                g.DrawLine(tree, x - 10, y - 170, x + 35, y - 232);
                g.DrawLine(tree, x - 28, y - 210, x - 35, y - 284);
            }

            using (SolidBrush ground = new SolidBrush(Color.FromArgb(125, 14, 13, 12)))
                g.FillRectangle(ground, 0, Height - 100, Width, 100);
        }
    }
}
