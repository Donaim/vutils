using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using OxyPlot;
using OxyPlot.WindowsForms;
using OxyPlot.Series;

using System.Drawing;
using System.Windows.Forms;
using OxyPlot.Axes;

namespace vutils.Plotting
{
    public class FuncPlotC
    {
        public Func<double, double> F;
        public double X0, X1;
        public readonly DataPointSeries S;
        public FuncPlotC(Func<double, double> f, double x0, double x1)
        {
            this.F = f;
            this.X0 = x0;
            this.X1 = x1;

            S = new FunctionSeries();
        }
        public void Refresh(Func<double, double> func, double x0, double x1)
        {
            this.F = func;
            this.X0 = x0;
            this.X1 = x1;

            S.Points.AddRange(new FunctionSeries(F, x0, x1, DX).Points);
            plot.Refresh();
        }
        public void Refresh(Func<double, double> func)
        {
            this.F = func;

            for (int i = 0; i < S.Points.Count; i++)
            {
                var x = S.Points[i].X;
                S.Points[i] = new DataPoint(x, F(x));
            }
            plot.Refresh();
        }
        public IPlot P => plot;
        Plot plot;
        public Series CreateSeries()
        {
            if (DX == double.MaxValue) { DX = (X1 - X0) / 1000; }

            S.Points.Clear();
            S.Points.AddRange(new FunctionSeries(F, X0, X1, DX).Points);

            S.Title = Title;

            return S;
        }
        public void ShowAsync()
        {
            CreateSeries();

            plot = new Plot(new Series[] { S })
            {
                Width = WinWidth,
                Height = WinHeight
            };

            FastPlot.ShowAsync(plot);
        }

        public double DX { get; set; } = double.MaxValue;

        public int WinWidth { get; set; } = 600;
        public int WinHeight { get; set; } = 400;
        public string Title { get; set; } = null;
    }
    public static class FastPlot
    {
        public static IPlot ShowFuncGraph(Func<double, double> f, double x0, double x1, out Action<Func<double, double>> refreshFunc)
        {
            double dx = (x1 - x0) / 1000;
            return ShowFuncGraph(f: f, x0: x0, x1: x1, dx: dx, refreshFunc: out refreshFunc);
        }
        public static IPlot ShowFuncGraph(Func<double, double> f, double x0, double x1, double dx, out Action<Func<double, double>> refreshFunc)
        {
            var s = new FunctionSeries(f, x0, x1, dx);
            return ShowFuncGraph(s: s, refreshFunc: out refreshFunc);
        }
        public static IPlot ShowFuncGraph(DataPointSeries s, out Action<Func<double, double>> refreshFunc)
        {
            var plot = new Plot(new Series[] { s });
            refreshFunc = refresh;

            return ShowAsync(plot);

            void refresh(Func<double, double> g)
            {
                for (int i = 0; i < s.Points.Count; i++)
                {
                    var x = s.Points[i].X;
                    s.Points[i] = new DataPoint(x, g(x));
                }
                plot.Refresh();
            }
        }

        public static IPlot ShowConnectedDots(params double[] values)
        {
            return ShowFuncGraph((i) => values[(int)i], 0, values.Length - 1, 1, out _);
        }

        public static Plot GetColumnGraph(params double[] values)
        {
            var cs = new ColumnSeries();
            for (int i = 0; i < values.Length; i++) { cs.Items.Add(new ColumnItem(values[i])); }

            return new Plot(new Series[] { cs });
        }
        public static Plot GetColumnGraph(double[] values, string[] names)
        {
            var plot = GetColumnGraph(values);

            plot.Model.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                ItemsSource = names,
            });

            return plot;
        }
        public static IPlot ShowColumnGraph(params double[] values) => ShowAsync(GetColumnGraph(values));
        
        public static Plot GetColumnGraph(double[][] values)
        {
            var csList = new List<ColumnSeries>();
            for (int cat = 0; cat < values.Length; cat++)
            {
                var cs = new ColumnSeries();
                for (int i = 0; i < values[cat].Length; i++)
                {
                    cs.Items.Add(new ColumnItem(values[cat][i], cat));
                }

                csList.Add(cs);
            }

            return new Plot(csList);
        }
        public static IPlot ShowColumnGraph(double[][] values) => ShowAsync(GetColumnGraph(values));

        public static IPlot ShowAsync(Plot p)
        {
            new Thread(async).Start();
            return p;

            void async()
            {
                Application.Run(p);
            }
        }
    }
    
    public interface IPlot
    {
        PlotModel Model { get; }
        void Refresh(Series newSeries, int seriesIndex);
        void Refresh(bool resetAxes);
        void Refresh();
    }
    public class Plot : Form, IPlot
    {
        public Plot(IEnumerable<Series> fs)
        {
            CheckForIllegalCrossThreadCalls = false;
            Size = new Size(600, 400);

            this.View = new OxyPlot.WindowsForms.PlotView();
            InitializeComponent();
            initmodel(out pm, View, fs);

            //view.Model.Series[0] = new FunctionSeries((x) => 1, 0, 10, 0.1);
        }

        public override void Refresh() => Refresh(false);
        public void Refresh(bool resetAxes)
        {
            View.Model.InvalidatePlot(true);
            if (resetAxes) { View.Model.ResetAllAxes(); }
        }
        public void Refresh(Series newSeries, int seriesIndex = 0)
        {
            View.Model.Series[seriesIndex] = newSeries;
            //view.Model.Series.Add(newSeries);

            View.Model.InvalidatePlot(true);
        }

        readonly PlotModel pm;
        public PlotModel Model => pm;
        static void initmodel(out PlotModel pm, PlotView view, IEnumerable<Series> fs)
        {
            pm = new PlotModel
            {
                Title = null,
                Subtitle = null,
                PlotType = PlotType.XY,
                Background = OxyColors.White,
            };

            foreach (var f in fs) { pm.Series.Add(f); }

            //pm.Axes.Add(new CategoryAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    Key = "CakeAxis",
            //    ItemsSource = new[]
            //    {
            //        "Apple cake",
            //        "Baumkuchen",
            //        "Bundt Cake",
            //        "Chocolate cake",
            //        "Carrot cake"
            //    }
            //});

            //pm.Series.Add(new FunctionSeries(Math.Sin, -10, 10, 0.1, "sin(x)"));
            //pm.Series.Add(new FunctionSeries(Math.Cos, -10, 10, 0.1, "cos(x)"));
            //pm.Series.Add(new FunctionSeries(t => 5 * Math.Cos(t), t => 5 * Math.Sin(t), 0, 2 * Math.PI, 0.1, "cos(t),sin(t)"));

            view.Model = pm;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.View);
            this.Name = "Form1";
            this.Text = "OxyPlot";
            this.ResumeLayout(false);
            // 
            // plot1
            // 
            this.View.Dock = System.Windows.Forms.DockStyle.Fill;
            this.View.Location = new System.Drawing.Point(0, 0);
            this.View.Margin = new System.Windows.Forms.Padding(0);
            this.View.Name = "plot1";
            this.View.Size = this.ClientSize;
            this.View.TabIndex = 0;

        }

        public readonly PlotView View;
    }
}
