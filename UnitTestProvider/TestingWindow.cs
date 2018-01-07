using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Diagnostics;
using System.IO;

using System.Windows.Forms;
using System.Drawing;
// using System.Runtime.InteropServices;

// using vconsole;

namespace vutils.Testing
{
    public interface ITestingWindow
    {
        void WriteLine();
        void WriteLine(object o);
        void Write(object o);
        string ReadLine();
        void EndRead(bool resetInput = true);

        void Start();
        void Close();

        void RememberState();
        void RestoreState();
    }
    public class TestingWindow : Form, ITestingWindow
    {
        public TestingWindow() : this(true) { }
        public TestingWindow(bool terminateOnExit)
        {
            CheckForIllegalCrossThreadCalls = false;
            Application.EnableVisualStyles();

            var resources = new System.ComponentModel.ComponentResourceManager(typeof(TestingWindow));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            Size = new Size(900, 500);

            BackColor = Color.Black;
            Text = nameof(TestingWindow);
            
            OutB = new OutBox(this);

            if (terminateOnExit)
            {
                FormClosed += (s, e) => Environment.Exit(2);
            }
        }
        public void Start()
        {
            var th = new Thread(async);
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            void async()
            {
                Application.Run(this);
            }
        }

        public void WriteLine() => WriteLine("");
        public void WriteLine(object o) => Write(o.ToString() + '\n');
        public void Write(object o) => OutB.AppendExtern(o);

        public string ReadLine() => OutB.ReadLine();
        public void EndRead(bool resetInput = true) => OutB.EndRead(resetInput);

        string lastRtf = "";
        //public void RememberState() => lastIndex = OutB.TextLength;
        public void RememberState()
        {
            lastRtf = OutB.Rtf;
        }
        public void RestoreState()
        {
            OutB.Rtf = lastRtf;
        }

        public readonly OutBox OutB;
        public class OutBox : vtextbox
        {
            readonly TestingWindow window;
            public OutBox(TestingWindow w)
            {
                window = w;

                // BorderStyle = BorderStyle.None;
                BackColor = Color.Black;
                ForeColor = Color.Red;
                Size = new Size(window.ClientRectangle.Width - 0, window.ClientRectangle.Height/* - window.InB.Height*/);
                Location = new Point(0, 0);
                Font = FONT;
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
                // ReadOnly = true;

                window.Controls.Add(this);
            }
            
            readonly Queue<string> queue = new Queue<string>();
            int forceEndTime = -1;
            public string ReadLine()
            {
                int myTime = Environment.TickCount;

                while (queue.Count == 0)
                {
                    Thread.Sleep(1);
                    if (myTime < forceEndTime) { return getBuffer(); }
                }

                return queue.Dequeue();
            }

            public string Rtf { get => Text; set => Text = value; }

            string getBuffer() => lh.Current.Text.Trim('\n', '\r').Substring(lastindex);

            protected override void BeforeKeyDown(PreviewKeyDownEventArgs e, ref bool skipPress, ref bool skipNavigate, ref bool skipKeyDown) {
               
                if(lh.Current != lh.list.Last() || lh.Current.CursorIndex < lastindex) 
                {
                    skipNavigate = false;
                    skipPress = true;
                    skipKeyDown = true;
                }
                else
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Back:
                            if(lh.Current.CursorIndex <= lastindex) {
                                skipNavigate = true;
                                skipPress = true;
                                skipKeyDown = true;
                            } 
                            break;
                        case Keys.Enter:
                            queue.Enqueue(getBuffer());
                            resetInput();
                            break;
                    }
                }
            }
            public void EndRead(bool _resetInput)
            {
                forceEndTime = Environment.TickCount;
                if (_resetInput) { resetInput(); }
            }
            void resetInput()
            {
                AppendText("\n");
                // lastLen = Text.Length;
            }

            int lastindex = 0;
            public void AppendExtern(object o)
            {
                AppendText(o.ToString());
                int lastindex = lh.list.Last().Length;
            }
            public static readonly Font FONT = new Font("Consolas", 12);
        }
    }
}
