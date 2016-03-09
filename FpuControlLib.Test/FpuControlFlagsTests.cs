using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FpuControlLib.Test
{
    [TestClass]
    public class FpuControlFlagsTests
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        [TestMethod]
        public void TestMethod1()
        {
            // this test will be succsess in x86, but will be failed in x64
            Trace.WriteLine(IntPtr.Size == 4 ? "x86" : "x64");
            if (IntPtr.Size == 8) return;

            double before = TestCalc();
            Assert.AreEqual(0.0, before);

            var oldState = FpuControlFlags.CurrentFlags;
            FpuControlFlags.ChangeFlag((uint)FpuControlFlags.PrecisionControl.Extended64Bits, FpuControlFlags.Mask.PrecisionControl);
            var newState = FpuControlFlags.CurrentFlags;
            Assert.AreEqual(FpuControlFlags.PrecisionControl.Extended64Bits, newState.PrecControl);

            double after = TestCalc();
            Assert.AreEqual(0.5, after);

            double afterSafe = TestCalcSafe();
            Assert.AreEqual(0.0, afterSafe);

            FpuControlFlags.ChangeFlag((uint)oldState.PrecControl, FpuControlFlags.Mask.PrecisionControl);

            double reset = TestCalc();
            Assert.AreEqual(0.0, reset);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public double TestCalc()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString("11111111111111111111111111111111111111111111111111110.0");
            //double b = DoubleConverter.FromFloatingPointBinaryString("00000000000000000000000000000000000000000000000000000.1");
            double b = 0.5;

            double result = a + b - a;
            return result;
        }

        // Here we add an explicit cast, which ensures that (a + b) is evaluated to 64-bits before continuing
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public double TestCalcSafe()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString("11111111111111111111111111111111111111111111111111110.0");
            double b = DoubleConverter.FromFloatingPointBinaryString("00000000000000000000000000000000000000000000000000000.1");

            double result = (double)(a + b) - a;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [TestMethod]
        public void TestPrecision64()
        {
            // this test will be succsess in x86, but will be failed in x64
            Trace.WriteLine(IntPtr.Size == 4 ? "x86" : "x64");
            if (IntPtr.Size == 8) return;

            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "0"); // 111....11110
            double b = DoubleConverter.FromFloatingPointBinaryString("0.1");                     // 000....00000.1
            double expected64 = a;

            FpuControlFlags.ChangeFlag((uint)FpuControlFlags.PrecisionControl.Extended64Bits, FpuControlFlags.Mask.PrecisionControl);
            double result64 = (a + b);
            Assert.AreEqual(expected64, result64);

            double result64_b = (a + b) - a;
            Assert.AreEqual(b, result64_b);

            double result64_0 = ((double) (a + b)) - a;
            Assert.AreNotEqual(b, result64_0);
            Assert.AreEqual(0.0, result64_0);
        }

        [TestMethod]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void TestPrecision53()
        {
            double a = DoubleConverter.FromFloatingPointBinaryString('1'.Repeat(52) + "0"); // 111....11110
            double b = DoubleConverter.FromFloatingPointBinaryString("0.1");                     // 000....00000.1
            double expected53 = a;

            FpuControlFlags.ChangeFlag((uint)FpuControlFlags.PrecisionControl.Double53Bits, FpuControlFlags.Mask.PrecisionControl);
            double result53 = a + b;
            Assert.AreEqual(expected53, result53);

            double result53_0 = (a + b) - a;
            Assert.AreEqual(0.0, result53_0);

            double result53_1 = ((double)(a + b)) - a;
            Assert.AreEqual(0.0, result53_1);

        }
    }
}
