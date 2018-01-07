using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

using vutils;
using vutils.Testing;
using vutils.Plotting;
using vutils.Statistics;
using OxyPlot.Series;

namespace Testing
{
    // [TestClass]
    public class SliderTest
    {
        [TestingObjectAttribute]
        static void basicSliderTest()
        {
            var slider = VSlider.RunAsync();
            Thread.Sleep(1000);
            var asl= slider.AddWatch(act: PrintValue, current: 2, lower: 0, upper: 100, delta: 1);
            asl.Name = "a value";

            Thread.Sleep(1000);
            slider.AddWatch(PrintValue, new AlphabetOverloader('C'), new AlphabetOverloader('A'), new AlphabetOverloader('F'), new AlphabetOverloader('A'), AlphabetOverloader.TryParse);

            void PrintValue<T>(T d)
            {
                Console.WriteLine($"Value: {d}");
            }
        }

        [TestingObjectAttribute]
        static void sliderWithGraphTest()
        {
            double a = 1, b = 0;
            double func(double x) => Math.Sin(x * a + b);

            var plot = FastPlot.ShowFuncGraph(func, 0, 10, out var refrFunc);

            var slider = VSlider.RunAsync();
            slider.AddWatch<double>(act: a_set, current: 1.0, lower: -10.0, upper: 10.0, delta: 0.1, parser: double.TryParse);
            slider.AddWatch<double>(act: b_set, current: 0.0, lower: -10.0, upper: 10.0, delta: 0.1, parser: double.TryParse);
            slider.AddWatch<double>(act: b_set, current: 0.0, lower: -10.0, upper: 10.0, delta: 2.1, parser: double.TryParse);

            void a_set(double to) { a = to; refrFunc(func); }
            void b_set(double to) { b = to; refrFunc(func); }
        }

        class AlphabetOverloader
        {
            static readonly List<string> alphabet = new List<string>() { "A", "B", "C", "D", "E", "F" };
            string curr;
            public AlphabetOverloader(char c)
            {
                curr = c.ToString();
            }
            public override string ToString() => curr;

            public static AlphabetOverloader operator +(AlphabetOverloader a, object b)
            {
                var bb = (AlphabetOverloader)b;
                return a + bb.curr;
            }
            public static AlphabetOverloader operator +(AlphabetOverloader a, double b)
            {
                int diff = alphabet.IndexOf(a.curr) + (int)b;
                return new AlphabetOverloader(alphabet[diff][0]);
            }
            public static AlphabetOverloader operator *(AlphabetOverloader a, double b)
            {
                //var aa = (AlphabetOverloader)a;
                var ai = alphabet.IndexOf(a.curr);
                var ind = (int)Math.Round(ai * b);
                return new AlphabetOverloader(alphabet[ind][0]);
            }
            //public static AlphabetOverloader operator -(AlphabetOverloader a, object b)
            //{
            //    var bb = (AlphabetOverloader)b;
            //    int diff = alphabet.IndexOf(a.curr) - alphabet.IndexOf(bb.curr);
            //    return new AlphabetOverloader(alphabet[diff][0]);
            //}
            public static double operator -(AlphabetOverloader a, object b)
            {
                var bb = (AlphabetOverloader)b;
                int diff = alphabet.IndexOf(a.curr) - alphabet.IndexOf(bb.curr);
                return (double)diff;
                //return new AlphabetOverloader(alphabet[diff][0]);
            }

            public static double operator /(AlphabetOverloader a, object b)
            {
                var bb = (double)b;
                //double div = alphabet.IndexOf(a.curr) / (double)alphabet.IndexOf(bb.curr);
                double div = alphabet.IndexOf(a.curr) / bb;
                return div;
            }

            public static bool TryParse(string s, out AlphabetOverloader re)
            {
                re = new AlphabetOverloader(s[0]);
                return true;
            }
        }
    }
}
