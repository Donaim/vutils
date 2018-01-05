using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Diagnostics;
using System.IO;

using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

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
        public class OutBox : RichTextBox
        {
            readonly TestingWindow window;
            public const int XOFFSET = 5;
            public OutBox(TestingWindow w)
            {
                window = w;

                BorderStyle = BorderStyle.None;
                BackColor = Color.Black;
                ForeColor = Color.Red;
                Size = new Size(window.ClientRectangle.Width - XOFFSET, window.ClientRectangle.Height - 3/* - window.InB.Height*/);
                Location = new Point(XOFFSET, 0);
                Font = FONT;
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
                ReadOnly = true;

                KeyDown += OutBox_KeyDown;
                KeyPress += OutBox_KeyPress;
                KeyUp += OutBox_KeyUp;

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
            string getBuffer() => Text.Substring(lastLen).Replace("\n", "").Replace("\r", "");

            [DllImport("user32.dll", EntryPoint = "LockWindowUpdate", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern IntPtr LockWindow(IntPtr Handle);

            private void OutBox_KeyDown(object sender, KeyEventArgs e)
            {
                switch (e.KeyCode)
                {
                    case Keys.V:
                        if (e.Control && SelectionStart >= lastLen)
                        {
                            //Select(SelectionStart - 1, SelectionLength + 1);
                            SelectedText = Clipboard.GetText();
                            //lastLen = lastLen - (lastLen - TextLength);
                        }
                        break;
                    case Keys.C:
                        if (e.Control && !string.IsNullOrEmpty(SelectedText))
                        {
                            Clipboard.SetText(SelectedText);
                        }
                        break;
                    case Keys.Back:
                        if(SelectionLength == 0) { break; }
                        skip = true;
                        goto case Keys.Delete;
                    case Keys.Delete:
                        removeSelectedText();
                        break;
                    case Keys.X:
                        if (e.Control) { removeSelectedText(); }
                        break;
                }
            }
            void removeSelectedText()
            {
                if (SelectionLength > 0 && SelectionStart >= lastLen)
                {
                    LockWindow(this.Handle);

                    SelectedText = "?";
                    var selStart = SelectionStart - 1;
                    Text = Text.Remove(SelectionStart - 1, 1);
                    SelectionStart = selStart;

                    LockWindow(IntPtr.Zero);
                }
            }

            bool skip = false;
            protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
            {
                if (e.Control) { skip = true; }
                base.OnPreviewKeyDown(e);
            }
            private void OutBox_KeyPress(object sender, KeyPressEventArgs e)
            {
                if (skip) { skip = false; return; }

                if (e.KeyChar != '\b')
                {
                    if(SelectionStart == TextLength)
                    {
                        AppendText(e.KeyChar.ToString());
                    }
                    else if(SelectionStart >= lastLen)
                    {
                        LockWindow(this.Handle);

                        var startSel = SelectionStart;
                        SelectedText = e.KeyChar.ToString();
                        SelectionStart = startSel + 1;

                        LockWindow(IntPtr.Zero);
                    }
                }
                else
                {
                    if(TextLength > lastLen)
                    {
                        if(SelectionStart == TextLength)
                        {
                            LockWindow(this.Handle);

                            Text = Text.Remove(TextLength - 1);
                            SelectionStart = TextLength;

                            LockWindow(IntPtr.Zero);
                        }
                        else if(SelectionStart > lastLen)
                        {
                            LockWindow(this.Handle);

                            var startSel = SelectionStart;
                            Text = Text.Remove(SelectionStart - 1, 1);
                            SelectionStart = startSel - 1;

                            LockWindow(IntPtr.Zero);
                        }
                    }
                }
            }

            int lastLen = 3;
            private void OutBox_KeyUp(object sender, KeyEventArgs e)
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        queue.Enqueue(getBuffer());
                        resetInput();
                        break;
                }
            }
            public void EndRead(bool _resetInput)
            {
                forceEndTime = Environment.TickCount;
                if (_resetInput) { resetInput(); }
            }
            void resetInput()
            {
                lastLen = Text.Length;
            }

            public void AppendExtern(object o)
            {
                AppendText(o.ToString());
                lastLen = TextLength;
            }
            public static readonly Font FONT = new Font("Consolas", 12);
        }
    }
}
