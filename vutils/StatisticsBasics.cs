using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace vutils.Statistics
{
    public static class RNG
    {
        static class Primitive
        {
            public const int iterations = 55, onesize = 222, randsize = iterations * onesize;

            public static readonly int[] INTBUFFER = new int[randsize];
            public static readonly bool[] BOOLBUFFER = new bool[randsize];
            public static readonly double[] DOUBLEBUFFER = new double[randsize];
            public static int randIter;
            static Primitive()
            {
                int tickPre = System.Diagnostics.Process.GetCurrentProcess().Id;
                int n = 0;
                while (true)
                {
                    int tt = Environment.TickCount + (tickPre++);
                    int ticks = tt % 117 + tt;
                    Random random = new Random(ticks);
                    for (int j = 0; j < onesize; j++, n++)
                    {
                        if (n >= randsize) { return; }

                        var r = random.Next();
                        INTBUFFER[n] = r;
                        BOOLBUFFER[n] = r % 2 == 0;
                        DOUBLEBUFFER[n] = random.NextDouble();
                    }
                }
            }
        }

        public static int Next() => Primitive.INTBUFFER[Primitive.randIter++ % Primitive.randsize];
        public static double NextDouble() => Primitive.DOUBLEBUFFER[Primitive.randIter++ % Primitive.randsize];

        public static int[] Dist(int size)
        {
            var re = new int[size];
            for (int i = 0; i < size; i++) { re[i] = Next(); }
            return re;
        }
        public static double[] DoubleDist(int size)
        {
            var re = new double[size];
            for (int i = 0; i < size; i++) { re[i] = NextDouble(); }
            return re;
        }
    }
    public static class Histogram
    {
        public static void GetLimits(double[] array, out double lower, out double upper)
        {
            lower = double.MaxValue; upper = double.MinValue;
            for(int i = 0; i < array.Length; i++)
            {
                if (array[i] < lower) { lower = array[i]; }
                if (array[i] > upper) { upper = array[i]; }
            }
        }
        public static double[] GetBounds(double[] array, double lower, double upper, int categories = 5)
        {
            double[] bounds = new double[categories];

            for (double i = 0, diff = (upper - lower) / categories; i < categories; i++)
            {
                bounds[(int)i] = lower + diff * (i + 1);
            }

            return bounds;
        }
        public static double[] GetBounds(double[] array, int categories = 5)
        {
            GetLimits(array, out var lower, out var upper);
            return GetBounds(array, lower, upper, categories);
        }

        public static double[] GetNormalized(double[] array, out double lower, out double upper, int categories = 5)
        {
            GetLimits(array, out lower, out upper);
            return GetNormalized(array, lower, upper, categories);
        }
        public static double[] GetNormalized(double[] array, double lower, double upper, int categories = 5)
        {
            var count = GetRaw(array, lower, upper, categories);

            double[] normalized = new double[categories];
            for (int i = 0; i < categories; i++)
            {
                normalized[i] = count[i] / (double)array.Length;
            }

            return normalized;
        }
        public static int[] GetRaw(double[] array, out double lower, out double upper, int categories = 5)
        {
            GetLimits(array, out lower, out upper);
            return GetRaw(array, lower, upper, categories);
        }
        public static int[] GetRaw(double[] array, double lower, double upper, int categories = 5)
        {
            double[] bounds = GetBounds(array, lower, upper, categories);

            int[] count = new int[categories];
            foreach (var o in array)
            {
                for (int j = 0; j < categories; j++)
                {
                    if (o <= bounds[j]) { count[j]++; break; }
                }
            }

            return count;
        }

        public static void PrintHist(double[] array, int categories = 5, double lower = double.MaxValue, double upper = double.MinValue)
        {
            var normalized = GetNormalized(array, out lower, out upper, categories);

            var normText = new string[categories];
            for (int i = 0; i < categories; i++)
            {
                normText[i] = (normalized[i].ToString("N2").PadRight(20, '0')).Remove(4);
            }

            Console.WriteLine($"[{string.Join(" | ", normText)}] ({lower.ToString("N2")},{upper.ToString("N2")}) ({(upper - lower).ToString("N2")})");
        }
        public static Plotting.IPlot ShowHist(double[] array, int categories = 5, string name = null)
        {
            var hist = GetNormalized(array, out var lower, out var upper, categories);
            var bounds = GetBounds(array, categories).Select(o => o.ToString("N2")).ToArray();

            var plot = Plotting.FastPlot.GetColumnGraph(hist, bounds);
            if(name != null) { plot.Text = name; }
            return Plotting.FastPlot.ShowAsync(plot);
        }
    }

    public static class MovingAverage
    {
        public static double[] Get1D(double[] array, int K)
        {
            var re = new List<double>(array.Length - K + 2);

            int z = 0;
            double mean = 0.0;
            for(int i = 0; i < array.Length; i++)
            {
                mean = (mean * z + array[i]) / (z + 1);
                z++;

                if(z >= K)
                {
                    re.Add(mean);
                    z = 0;
                    mean = 0.0;
                }
            }

            return re.ToArray();
        }

        public static Plotting.IPlot ShowMovingAverage(double[] array, int K)
        {
            var g1d = Get1D(array, K);
            return Plotting.FastPlot.ShowConnectedDots(g1d);
        }
        public static Plotting.IPlot ShowMovingAverage(DPoint[] array, int K)
        {
            //var g2d = Get2D(array, K);


            throw new NotImplementedException();
        }
    }
}
