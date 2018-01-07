using System;
using System.Threading;
// using Microsoft.VisualStudio.TestTools.UnitTesting;

using vutils.Testing;

namespace Testing
{
    // [TestClass]
    public class TestingModuleTest : vutils.Testing.IAsyncTesting
    {
        [TestingObjectAttribute]
        public void TestingObjectAttribute1()
        {
        }

        [TestingObjectAttribute] 
        static void LUL(string alo, string hello = "hi", int n = 2)
        {

        }

        public void Command(string s, Thread workingthread)
        {
            if(s == "stop") { workingthread.Abort(); }
        }
    }
}
