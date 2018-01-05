using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Drawing;
using System.Windows.Forms;

namespace vutils
{
    public class VSlider : Form
    {
        public VSlider()
        {
            this.Height = 0;
            this.Width = 500;
            Text = "VSlider";
            FormBorderStyle = FormBorderStyle.FixedSingle;

            KeyPreview = true;
            KeyDown += VSlider_KeyDown;

            MaximizeBox = false;
        }
        public static VSlider RunAsync()
        {
            var re = new VSlider();
            var th = new Thread(new ParameterizedThreadStart(async));
            th.TrySetApartmentState(ApartmentState.MTA);
            th.Start(re);
            return re;
        }
        static void async(object me)
        {
            var re = (VSlider)me;
            Application.Run(re);
        }

        private void VSlider_KeyDown(object sender, KeyEventArgs e)
        {
            var butt = Controls.Cast<ButtonCtrl>().FirstOrDefault(o => o.BackColor == ButtonCtrl.COLOR_ACTIVATED);
            if(butt == null) { return; }

            switch (e.KeyCode)
            {
                case Keys.Add:
                    butt.M.Incr();
                    break;
                case Keys.Subtract:
                    butt.M.Decr();
                    break;

                default: return;
            }

            butt.Refresh();
        }

        public delegate bool ParsingDelegate<T>(string str, out T re);
        public IManipulator AddWatch(Action<int> act, int current = 0, int lower = 0, int upper = 100, int delta = 1)
        {
            Manipulator<int> m = null;
            int oldvalue = current;
            void diffQ(int x) { if (x != oldvalue) { act(oldvalue = x); } }

            m = new Manipulator<int>(diffQ, current, lower, upper, delta, int.TryParse);
            return AddWatch(m);
        }
        public IManipulator AddWatch(Action<double> act, double current = 0.5, double lower = 0.0, double upper = 1.0, double delta = 0.01)
        {
            var m = new Manipulator<double>(act, current, lower, upper, delta, double.TryParse);
            return AddWatch(m);
        }
        public IManipulator AddWatch<T>(Action<T> act, T current, T lower, T upper, T delta, ParsingDelegate<T> parser)
        {
            var m = new Manipulator<T>(act, current, lower, upper, delta, parser);
            return AddWatch(m);
        }
        const int DELTAY = 5, STARTY = 10, DELTAX = 5;
        public IManipulator AddWatch(IManipulator m)
        {
            //back:
            //try { this.Invoke((Action)invoked); }
            //catch { Thread.Sleep(100); goto back; }

            while (!IsHandleCreated) { Thread.Sleep(1); }
            this.Invoke((Action)invoked);

            return m;
            void invoked()
            {
                var newButt = new ButtonCtrl(this, m);

                int topY = STARTY + (newButt.Height + DELTAY) * Controls.Count;
                newButt.Location = new Point(DELTAX, topY);
                Controls.Add(newButt);

                int botY = topY + newButt.Height + DELTAY;
                this.Height = Math.Max(botY + STARTY + 32, Height);
            }
        }

        class Manipulator<T> : IManipulator
        {
            public T current;
            protected T lower, upper, delta;
            protected readonly Action<T> refresh;
            protected readonly ParsingDelegate<T> parser;

            public Manipulator(Action<T> f, T current, T lower, T upper, T delta, ParsingDelegate<T> parsingFunc)
            {
                this.current = current;
                this.lower = lower;
                this.upper = upper;
                this.delta = delta;
                this.refresh = f;
                this.parser = parsingFunc;
                this.Name = refresh.Method.Name;

                mu = new EventWaitHandle(true, EventResetMode.AutoReset);
                loop();
            }

            protected virtual T set(double rate)
            {
                dynamic l = lower;
                dynamic diff = upper - l;
                dynamic mult = diff * rate;
                return (T)(lower + mult);
            }
            public virtual double GetRate()
            {
                dynamic curr = current;
                dynamic l = lower;
                dynamic diff = (double)(upper - l);
                dynamic currl = (T)curr - l;

                double re = currl / (diff);
                return re;
            }

            public virtual void Incr()
            {
                dynamic c = current;
                current = c + delta;
                refreshHanle();
            }
            public virtual void Decr()
            {
                dynamic c = current;
                current = c - delta;
                refreshHanle();
            }
            public virtual void SetRate(double rate)
            {
                current = set(rate);
                refreshHanle();
            }
            public virtual bool SetCurrent(string value)
            {
                if(parser(value, out var c))
                {
                    current = c;
                    refreshHanle();
                    return true;
                }
                else { return false; }
            }
            public virtual bool SetMin(string value)
            {
                if (parser(value, out var o))
                {
                    lower = o;
                    return true;
                }
                else { return false; }
            }
            public virtual bool SetMax(string value)
            {
                if (parser(value, out var o))
                {
                    upper = o;
                    return true;
                }
                else { return false; }
            }
            public virtual bool SetDelta(string value)
            {
                if (parser(value, out var o))
                {
                    delta = o;
                    return true;
                }
                else { return false; }
            }

            readonly EventWaitHandle mu;
            void refreshHanle() { mu.Set(); }
            void loop()
            {
                new Thread(async).Start();
                void async()
                {
                    while (!disposed)
                    {
                        mu.WaitOne();
                        refresh(current);

                        Thread.Sleep(DelayMS);
                    }
                }
            }

            public int DelayMS { get; set; } = 100;
            public string Name { get; set; }
            public string GetCurrentString() => current.ToString();
            public string GetMinString()   => lower.ToString();
            public string GetMaxString()   => upper.ToString();
            public string GetDeltaString() => delta.ToString();

            bool disposed = false;
            public void Dispose() { disposed = true; }
        }
        public interface IManipulator : IDisposable
        {
            string Name { get; set; }
            int DelayMS { get; set; }

            void Incr();
            void Decr();

            void SetRate(double rate);
            bool SetCurrent(string value);
            bool SetMin(string value);
            bool SetMax(string value);
            bool SetDelta(string value);

            double GetRate();
            string GetCurrentString();
            string GetMinString();
            string GetMaxString();
            string GetDeltaString();
        }
        class ButtonCtrl : Control
        {
            public readonly IManipulator M;
            readonly ManiZone maniZone;
            readonly DescreteZone descreteZone;
            readonly VSlider parent;
            readonly BufferedGraphics bG;
            const int HEIGHT = 50, BOTTOM_HEIGHT = (int)(HEIGHT * (3.0 / 5.0));
            protected override void OnGotFocus(EventArgs e) { }
            public ButtonCtrl(VSlider p, IManipulator m)
            {
                CheckForIllegalCrossThreadCalls = false;
                parent = p;
                M = m;

                Size = new Size(parent.Width - 2 * DELTAX - 15, HEIGHT);
                BottomSize = new Size(Width, BOTTOM_HEIGHT);
                BackColor = Color.White;
                DoubleBuffered = false;
                bG = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), ClientRectangle);
                bG.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                
                maniZone = new ManiZone(this);
                maniZone.MouseEnter += ButtonCtrl_MouseEnter;
                maniZone.MouseLeave += ButtonCtrl_MouseLeave;
                maniZone.MouseUp += ButtonCtrl_MouseUp;
                Controls.Add(maniZone);
                descreteZone = new DescreteZone(this);
                descreteZone.MouseEnter += ButtonCtrl_MouseEnter;
                descreteZone.MouseLeave += ButtonCtrl_MouseLeave;
                descreteZone.MouseUp += ButtonCtrl_MouseUp;
                descreteZone.Hide();
                Controls.Add(descreteZone);

                MouseEnter += ButtonCtrl_MouseEnter;
                MouseLeave += ButtonCtrl_MouseLeave;
                MouseUp += ButtonCtrl_MouseUp;
            }

            bool zoneState = false;
            void changeScene()
            {
                zoneState = !zoneState;
                if (zoneState) { maniZone.Hide(); descreteZone.Show(); }
                else { descreteZone.Hide(); maniZone.Show(); }

                this.Focus();
                Refresh();
            }
            private void ButtonCtrl_MouseUp(object sender, MouseEventArgs e)
            {
                if(e.Button == MouseButtons.Right) { changeScene(); }
            }

            private void ButtonCtrl_MouseLeave(object sender, EventArgs e)
            {
                BackColor = COLOR_SINGLE;
                //Refresh();
            }
            void ButtonCtrl_MouseEnter(object sender, EventArgs e)
            {
                BackColor = COLOR_ACTIVATED;
                //Refresh();
            }

            static readonly Font NAMEFONT = new Font("Consolas", 14);
            static readonly Brush NAMEBRUSH = Brushes.Black;
            public override void Refresh()
            {
                maniZone.RefreshRate();
                descreteZone.Refresh();
                OnPaint(null);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                bG.Graphics.Clear(BackColor);
                bG.Graphics.DrawString(M.Name, NAMEFONT, NAMEBRUSH, 0, 0);
                bG.Render();
            }
            protected override void OnPaintBackground(PaintEventArgs pevent) { }

            public static readonly Color COLOR_ACTIVATED = Color.Yellow, COLOR_SINGLE = Color.White;
            protected readonly Size BottomSize;

            class ManiZone : Control
            {
                readonly ButtonCtrl parent;
                readonly BufferedGraphics bG;
                public ManiZone(ButtonCtrl p)
                {
                    parent = p;
                    rate = parent.M.GetRate();

                    CheckForIllegalCrossThreadCalls = false;
                    BackColor = Color.Blue;
                    DoubleBuffered = false;
                    Size = parent.BottomSize;
                    Location = new Point(0, parent.Height - this.Height);

                    MouseDown += ManiZone_MouseDown;
                    MouseUp += ManiZone_MouseUp;
                    MouseMove += ManiZone_MouseMove;

                    bG = BufferedGraphicsManager.Current.Allocate(CreateGraphics(), ClientRectangle);
                    bG.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    refreshLoop();
                }

                double rate;
                public int DelayMS { get; set; } = 1000 / 60;
                void refreshLoop()
                {
                    new Thread(async).Start();
                    void async()
                    {
                        int prev = mx;

                        while (!Created) { Thread.Sleep(1); }
                        while (!IsDisposed)
                        {
                            Thread.Sleep(DelayMS);

                            if(prev != mx)
                            {
                                prev = mx;

                                var normx = Math.Max(0, Math.Min(Width, mx));
                                rate = normx / (double)Width;
                                parent.M.SetRate(rate);

                                Refresh();
                            }
                        }
                    }
                }

                int mx = 0;
                bool moving = false;
                private void ManiZone_MouseUp(object sender, MouseEventArgs e)
                {
                    if(e.Button == MouseButtons.Left)
                    {
                        mx = e.X;
                        moving = false;
                    }
                }
                private void ManiZone_MouseDown(object sender, MouseEventArgs e)
                {
                    if(e.Button == MouseButtons.Left)
                    {
                        mx = e.X;
                        moving = true;
                    }
                }
                private void ManiZone_MouseMove(object sender, MouseEventArgs e)
                {
                    if (moving) { mx = e.X; }
                }

                public override void Refresh()
                {
                    redraw(bG.Graphics);
                    bG.Render();
                }
                static readonly Brush POINTER_BRUSH = Brushes.Black;
                const int POINTER_HALFWIDTH = 3, POINTER_HALFDY = 4;
                static readonly Font VALUEFONT = new Font("Consolas", 12);
                static readonly Brush VALUEFONTBRUSH = Brushes.White;
                static readonly StringFormat
                    VALUEFORMAT_LEFT = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center },
                    VALUEFORMAT_RIGHT = new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                int rate_to_width() => (int)Math.Round(Width * Math.Max(0, Math.Min(1, rate)));
                void redraw(Graphics g)
                {
                    g.Clear(BackColor);
                    g.FillRectangle(POINTER_BRUSH, rate_to_width() - POINTER_HALFWIDTH, POINTER_HALFDY, POINTER_HALFWIDTH * 2, Height - POINTER_HALFDY * 2);

                    var fmt = rate > 0.5 ? VALUEFORMAT_LEFT : VALUEFORMAT_RIGHT;
                    g.DrawString(parent.M.GetCurrentString(), VALUEFONT, VALUEFONTBRUSH, ClientRectangle, fmt);
                }
                public void RefreshRate()
                {
                    rate = parent.M.GetRate();
                    Refresh();
                }

                protected override void OnPaint(PaintEventArgs e) => Refresh();
                protected override void OnPaintBackground(PaintEventArgs pevent) { }
            }
            class DescreteZone : Control
            {
                class SmallTBox : TextBox
                {
                    readonly DescreteZone parent;
                    readonly Func<string, bool> parser;
                    readonly Func<string> getvalue;
                    public SmallTBox(DescreteZone p, Func<string, bool> parsing, Func<string> get)
                    {
                        parent = p;
                        parser = parsing;
                        getvalue = get;

                        AutoSize = false;
                        TextAlign = HorizontalAlignment.Right;
                        BorderStyle = BorderStyle.FixedSingle;
                        Height = parent.Height - 2;

                        Font = INPUT_FONT;

                        KeyDown += CurrBox_KeyDown;
                        LostFocus += SmallTBox_LostFocus;
                        OnLostFocus(null);

                        MouseEnter += parent.parent.ButtonCtrl_MouseEnter;
                        MouseLeave += parent.parent.ButtonCtrl_MouseLeave;
                    }

                    private void SmallTBox_LostFocus(object sender, EventArgs e) => Refresh();
                    public override void Refresh()
                    {
                        BackColor = Color.White;
                        Text = getvalue();
                    }

                    static SmallTBox()
                    {
                        int font_height = 5;
                        var gfx = Graphics.FromImage(new Bitmap(100, HEIGHT));
                        while (gfx.MeasureString("dIDPJjy", new Font("Consolas", font_height)).Height < BOTTOM_HEIGHT) { font_height++; }
                        INPUT_FONT = new Font("Consolas", font_height - 3);
                    }
                    public static readonly Font INPUT_FONT;

                    private void CurrBox_KeyDown(object sender, KeyEventArgs e)
                    {
                        if(e.KeyCode == Keys.Enter)
                        {
                            if (parser(Text))
                            {
                                parent.Focus();
                            }
                            else
                            {
                                BackColor = Color.Red;
                            }
                        }
                        else
                        {
                            if(BackColor != Color.White) { BackColor = Color.White; }
                        }
                    }
                }

                readonly ButtonCtrl parent;
                readonly SmallTBox currBox, minBox, maxBox, deltaBox;
                public DescreteZone(ButtonCtrl p)
                {
                    parent = p;

                    CheckForIllegalCrossThreadCalls = false;
                    DoubleBuffered = false;
                    Size = parent.BottomSize;
                    Location = new Point(0, parent.Height - this.Height);

                    currBox = new SmallTBox(this, parent.M.SetCurrent, parent.M.GetCurrentString);
                    currBox.Width = Width / 3;
                    currBox.Left = 0;
                    this.Controls.Add(currBox);

                    minBox = new SmallTBox(this, parent.M.SetMin, parent.M.GetMinString);
                    minBox.Width = Width / 5;
                    minBox.Left = currBox.Right + 5;
                    this.Controls.Add(minBox);
                    maxBox = new SmallTBox(this, parent.M.SetMax, parent.M.GetMaxString);
                    maxBox.Width = Width / 5;
                    maxBox.Left = minBox.Right + 1;
                    this.Controls.Add(maxBox);

                    deltaBox = new SmallTBox(this, parent.M.SetDelta, parent.M.GetDeltaString);
                    deltaBox.Width = (Width - maxBox.Right) - 5;
                    deltaBox.Left = Width - deltaBox.Width;
                    this.Controls.Add(deltaBox);
                }
                protected override void OnGotFocus(EventArgs e) { }

                public override void Refresh()
                {
                    foreach(Control c in Controls) { c.Refresh(); }
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                    e.Graphics.Clear(parent.BackColor);
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                M.Dispose();
            }
        }
    }
}
