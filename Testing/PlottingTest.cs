using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
// using Microsoft.VisualStudio.TestTools.UnitTesting;

using vutils.Plotting;
using vutils.Statistics;
using OxyPlot.Series;
// using UnitTestProvider;
using vutils.Testing;

namespace Testing
{
    // [TestClass]
    class PlottingTest
    {
        [TestingObjectAttribute]
        static void createSinGraph()
        {
            var s = new FunctionSeries((x) => Math.Sin(x), 0, 10, 0.1);
            var plot = FastPlot.ShowFuncGraph(s, out _);
            double a = 1;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!((Plot)plot).IsDisposed)
            {
                Thread.Sleep(200);
                
                for(int i = 0; i < s.Points.Count; i++)
                {
                    var p = s.Points[i];
                    s.Points[i] = new OxyPlot.DataPoint(p.X, Math.Sin(p.X + sw.ElapsedMilliseconds));
                }
                s.Points.Add(new OxyPlot.DataPoint(s.Points.Last().X + a, a));


                //plot.Refresh(s, 0);
                plot.Refresh();

                a += 0.1;
            }
        }

        [TestingObjectAttribute]
        static void printHistogram()
        {
            var randVector = RNG.DoubleDist(1000);
            Histogram.PrintHist(randVector);
        }
        [TestingObjectAttribute]
        static void histogramVSkmean()
        {
            int n = 1000;
            //var randVector = RNG.DoubleDist(n);
            Func<double, double> f = (x) => Math.Sin(x);
            var randVector = Enumerable.Range(0, n).Select(o => f(o / (n / (3 * Math.PI)))).ToArray();

            Histogram.ShowHist(randVector, 10);
            var kmeanPlot = (Plot)MovingAverage.ShowMovingAverage(randVector, n / 20);
            //FastPlot.ShowDynamicGraph(new FunctionSeries((x) => randVector[(int)(x)], 0.0, n-1, 1 - double.Epsilon));
            FastPlot.ShowConnectedDots(randVector);

            Histogram.PrintHist(randVector);
        }

        [TestingObjectAttribute]
        static void plotCreatorTest()
        {
            new FuncPlotC(Math.Sin, -2, 2) { WinWidth = 1000, Title = "Sin" }.ShowAsync();
        }
    }
}
