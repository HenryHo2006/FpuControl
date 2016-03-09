using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace FpuControlLib
{

    // x87 FPU is different with x64/SSE!!!
    // x87 FPU all operation(FADD,FSUB...) will opr with 80bit precision, the temp result immediately round to "setting" precision(float-24p/double-53p/extend-64p, as follwing defined)
    // x64/SSE all operation(ADDSD,MULSD...) will opr with 80bit precision, the temp result immediately round to "operand" precision(float-24p/double-53p), precision flag can not be changed(useless).
    // so .net default setting of x86 platform is double-53p precision, is keep equality with x64 platform

    // Intermediate Floating-Point Precision: https://randomascii.wordpress.com/2012/03/21/intermediate-floating-point-precision/
    // IA-32 reference: http://flint.cs.yale.edu/cs422/doc/24547012.pdf
    // Info for the C runtime call: http://msdn.microsoft.com/en-us/library/c9676k6h.aspx

    public struct FpuControlFlags
    {
        [Flags]
        public enum Mask : uint
        {
            DenormalControl = 0x03000000,
            InterruptExceptionMask = 0x0008001F,
            InfinityControl = 0x00040000,
            RoundingControl = 0x00000300,
            PrecisionControl = 0x00030000
        }

        // Intel(x86)-derived platforms support the DENORMAL input and output values in hardware.
        // The x86 behavior is to preserve DENORMAL values.
        // The ARM platform and the x64 platforms that have SSE2 support enable DENORMAL operands and results to be flushed, or forced to zero.
        //
        public enum DenormalControl : uint
        {
            Save = 0x00000000, // Denormal values preserved on ARM platforms and on x64 processors with SSE2 support. NOP on x86 platforms.
            Flush = 0x01000000 // Denormal values flushed to zero by hardware on ARM platforms 
                               // and x64 processors with SSE2 support. Ignored on other x86 platforms.
        }

        // the control bits actually turn exceptions OFF, not ON !
        // "For the _MCW_EM mask, clearing the mask sets the exception, which allows the hardware exception; setting the mask hides the exception."
        // managed application can not change it. https://support.microsoft.com/en-us/kb/326219
        [Flags]
        public enum ExceptionMask : uint
        {
            Invalid = 0x00000010,
            Denormal = 0x00080000,
            ZeroDivide = 0x00000008,
            Overflow = 0x00000004,
            Underflow = 0x00000002,
            Inexact = 0x00000001
        }

        // Wikipedia says a bit about this (http://en.wikipedia.org/wiki/IEEE_754-1985).
        // The Affine option has +infinity and -infinity, and is the only IEEE 754 option.
        // The Projective option has only -infinity after 80287.
        public enum InfinityControl : uint
        {
            Affine = 0x00040000,
            Projective = 0x00000000,
        }

        public enum RoundingControl : uint
        {
            Near = 0x00000000,
            Down = 0x00000100,
            Up = 0x00000200,
            ToZero = 0x00000300
        }

        // On the ARM and x64 architectures, changing the infinity mode or the floating-point precision is not supported.
        // If the precision control mask is used on the x64 platform, the function raises an assertion and the invalid parameter handler is invoked
        public enum PrecisionControl : uint
        {
            Single24Bits = 0x00020000,
            Double53Bits = 0x00010000,
            Extended64Bits = 0x00000000
        }

        //public const uint ErrorAmbiguous = 0x80000000;

        private uint _state;

        private FpuControlFlags(uint state)
        {
            _state = state;
        }

        public PrecisionControl PrecControl
        {
            get
            {
                return (PrecisionControl)(_state & (uint)Mask.PrecisionControl);
            }
        }

        public InfinityControl InfControl
        {
            get
            {
                return (InfinityControl)(_state & (uint)Mask.InfinityControl);
            }
        }

        public RoundingControl RndControl
        {
            get
            {
                return (RoundingControl)(_state & (uint)Mask.RoundingControl);
            }
        }

        public DenormalControl DenControl
        {
            get
            {
                return (DenormalControl)(_state & (uint)Mask.DenormalControl);
            }
        }

        public ExceptionMask ExcpMask
        {
            get
            {
                return (ExceptionMask)(_state & (uint)Mask.InterruptExceptionMask);
            }
        }

        // default flag, used to reset flags
        public static readonly FpuControlFlags Default =
            new FpuControlFlags((uint)RoundingControl.Near | (uint)PrecisionControl.Double53Bits |
                                (uint)ExceptionMask.Invalid | (uint)ExceptionMask.Denormal | (uint)ExceptionMask.ZeroDivide |
                                (uint)ExceptionMask.Overflow | (uint)ExceptionMask.Underflow | (uint)ExceptionMask.Inexact);

        public static FpuControlFlags CurrentFlags
        {
            get
            {
                uint state;
                int error = _controlfp_s(out state, 0, 0);
                if (error == 0)
                {
                    return new FpuControlFlags(state);
                }
                throw new Win32Exception(error);
            }
            set
            {
                uint state;
                int error = _controlfp_s(out state, value._state, 0xffffffff);
                if (error != 0)
                {
                    throw new Win32Exception(error);
                }
            }
        }

        public static void ChangeFlag(uint flag, Mask mask)
        {
            uint state;
            int error = _controlfp_s(out state, flag, (uint)mask);
            if (error != 0)
            {
                throw new Win32Exception(error);
            }
        }

        public override string ToString()
        {
            return string.Format("precision:{0} round:{1}", PrecControl.ToString(), RndControl.ToString());
        }

        // P/Invoke declare for the FPU control helper in the C runtime
        // errno_t __cdecl _controlfp_s(_Out_opt_ unsigned int *_CurrentState, _In_ unsigned int _NewValue, _In_ unsigned int _Mask);
        [SuppressUnmanagedCodeSecurity]
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
        static extern int _controlfp_s(out uint currentState, uint newValue, uint mask);
    }
}
