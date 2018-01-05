using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Testing
{
    [TestClass]
    public class TestingModuleTest : vutils.Testing.IAsyncTesting
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod] 
        static void LUL(string alo, string hello = "hi", int n = 2)
        {

        }

        public void Command(string s, Thread workingthread)
        {
            if(s == "stop") { workingthread.Abort(); }
        }
    }
}
