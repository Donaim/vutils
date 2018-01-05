using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace vutils.Testing
{
    public static class TestingModule
    {
        public static void ChooseMethodsLoop(bool clearConsole = true)
        {
            var window = new TestingWindow();
            while (true)
            {
                ChooseMethods(window);
                if (clearConsole) { Console.Clear(); }
            }
        }
        public static void ChooseMethods() => ChooseMethods(new TestingWindow());
        public static void ChooseMethods(TestingWindow w) => ChooseMethods(GetTestMethods(w), w);
        public static void ChooseMethods(IEnumerable<TestMethodExt> mtds, TestingWindow w)
        {
            w.Start();
            var methods = mtds.ToArray();

            again:
            w.WriteLine();
            w.RememberState();
            w.WriteLine("Choose method:");
            for (int i = 0; i < methods.Length; i++)
            {
                w.WriteLine($"{i + 1}: {methods[i].Name}");
            }
            var answ = w.ReadLine();
            if(int.TryParse(answ, out var ch) && ch <= methods.Length && ch > 0)
            {
                methods[ch - 1].InvokeWithConsole();
            }
            else
            {
                w.RestoreState();
                w.WriteLine($"\"{answ}\" is really bad choice! Try again");
                goto again;
            }
        }
        public static IEnumerable<TestMethodExt> GetTestMethods(TestingWindow w) => GetTestMethods(Assembly.GetEntryAssembly(), w);
        public static IEnumerable<TestMethodExt> GetTestMethods(Assembly ass, TestingWindow w)
        {
            var types = getClasses(ass);
            return getMethods(types, w);
        }

        static IEnumerable<Type> getClasses(Assembly ass) => ass.GetTypes();//.Where(o => o.GetCustomAttribute<TestClassAttribute>() != null);
        static IEnumerable<TestMethodExt> getMethods(IEnumerable<Type> types, TestingWindow w)
        {
            foreach(var t in types)
            {
                foreach(var m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if(m.GetCustomAttribute<TestMethodAttribute>() != null 
                        || m.GetCustomAttribute<TestingObjectAttribute>() != null)
                    {
                        var parent = m.IsStatic ? null : t;
                        yield return new TestMethodExt(m, parent, w);
                    }
                }
            }
        }
    }
    public interface IAsyncTesting
    {
        void Command(string s, Thread workingthread);
    }
    public struct TestMethodExt
    {
        public readonly TestingWindow Window;
        public readonly string Name;
        public readonly MethodInfo Target;
        public readonly Type Parent;
        public readonly IReadOnlyList<ParameterInfo> Params;
        public TestMethodExt(MethodInfo m, Type parent, TestingWindow w)
        {
            Window = w;
            Target = m;
            Parent = parent;
            Name = m.Name;
            Params = Target.GetParameters();
        }

        public void InvokeWithConsole()
        {
            object parentInstance = null;
            if(Parent != null) // may be static in which case its ok for parent to be null
            {
                try { parentInstance = Activator.CreateInstance(Parent); } // creating with empty constructor
                catch (Exception ex) { throw new Exception("Cannot create parent instance!", ex); }
            }

            var obj = new object[Params.Count];
            //foreach(var p in Params)
            for(int i = 0; i < Params.Count; i++)
            {
                if(i < 0) { return; }
                var p = Params[i];
                obj[i] = parseParam(ref i, p, Window);
            }

            runall(Target, obj.ToArray(), parentInstance, Window);
        }
        static void runall(MethodInfo Target, object[] obj, object parentInstance, TestingWindow Window)
        {
            IAsyncTesting at = null;
            if (parentInstance != null && parentInstance is IAsyncTesting)
            {
                at = (IAsyncTesting)parentInstance;
                Window.WriteLine("Method supports async commands. You can write");
            }

            var th = new Thread(asyncRun);
            th.Start();
            var wth = new Thread(asyncWriting);
            wth.Start();

            if(th != null && th.IsAlive) { th.Join(); }
            try { wth.Abort(); } catch { }
            Window.EndRead();

            void asyncRun()
            {
                Window.WriteLine($"\nInvoking \"{Target.Name}\" with params: \n\t{string.Join("\n\t", obj)}\n\t");
                Target.Invoke(parentInstance, obj);
                Window.WriteLine($"\nEnd of \"{Target.Name}\" invoke.");
            }
            void asyncWriting()
            {
                while (th != null && th.IsAlive)
                {
                    try { parseComm(Window.ReadLine(), Window, th, at); }
                    catch (ThreadAbortException) { }
                    catch (Exception ex) { Window.WriteLine($"Error during execution command: {ex.Message}"); }
                }
            }
        }
        static void parseComm(string str, ITestingWindow w, Thread th, IAsyncTesting at)
        {
            switch (str)
            {
                case "$abort": th.Abort(); return;
                default:
                    if(at != null) { at.Command(str, th); }
                    else { w.WriteLine("This class does not support custom commands!"); }
                    return;
            }
        }

        static object parseParam(ref int i, ParameterInfo p, TestingWindow Window)
        {
            Window.WriteLine();
            again:
            Window.RememberState();
            parse2(p, out var res, out var o, Window);

            switch(o)
            {
                case ParseResult.DEFAULT: goto case ParseResult.OK;
                case ParseResult.OK: return res;
                case ParseResult.BAD:
                    //long diff = Window.CursorTop - cstart;
                    //for(int i = 0; i < diff; i++) { Window.Write("\b"); }
                    Window.RestoreState();
                    Window.WriteLine($"\"{res}\" has bad input format! Try again");
                    break;
                case ParseResult.CANCEL:
                    i -= 2;
                    Window.RestoreState();
                    return null;

                default: throw new NotImplementedException();
            }

            goto again;
        }
        enum ParseResult { OK, BAD, DEFAULT, CANCEL }
        static void parse2(ParameterInfo p, out object res, out ParseResult o, TestingWindow Window)
        {
            if (p.HasDefaultValue)
            {
                Window.WriteLine($"(default: {p.DefaultValue})");
            }
            Window.Write($"{p.Name}=");
            string answ = Window.ReadLine();
            res = answ;
            if (string.IsNullOrWhiteSpace(answ) && p.HasDefaultValue) { o = ParseResult.DEFAULT; res = p.DefaultValue; return; }
            else if(answ == "$back") { o = ParseResult.CANCEL; res = null; return; }

            o = ParseResult.OK;
            var type = p.ParameterType;
            if (type == typeof(string)) { res = answ; return; }
            else if(type == typeof(int))
            {
                if(int.TryParse(answ, out var ire)) { res = ire; return; }
            }
            else if(type == typeof(double))
            {
                if (double.TryParse(answ, out var dre)) { res = dre; return; }
            }
            else if(type == typeof(bool))
            {
                if(bool.TryParse(answ, out var bre)) { res = bre; return; }
            }
            else { throw new NotImplementedException(); }

            o = ParseResult.BAD;
        }
    }
}
