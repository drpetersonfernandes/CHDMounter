namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class AudioSamples
{
    public const uint UINT32_MAX = 0xffffffff;

    public static unsafe void Interlace(int* res, int* src1, int* src2, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = *src1++;
            *res++ = *src2++;
        }
    }

    public static unsafe void Deinterlace(int* dst1, int* dst2, int* src, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *dst1++ = *src++;
            *dst2++ = *src++;
        }
    }

    public static unsafe bool MemCmp(int* res, int* smp, int n)
    {
        for (var i = n; i > 0; i--)
            if (*res++ != *smp++)
                return true;

        return false;
    }

    public static unsafe void MemCpy(uint* res, uint* smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = *smp++;
        }
    }

    public static unsafe void MemCpy(int* res, int* smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = *smp++;
        }
    }

    public static unsafe void MemCpy(long* res, long* smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = *smp++;
        }
    }

    public static unsafe void MemCpy(short* res, short* smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = *smp++;
        }
    }

    public static unsafe void MemCpy(byte* res, byte* smp, int n)
    {
        if ((((IntPtr)smp).ToInt64() & 7) == (((IntPtr)res).ToInt64() & 7) && n > 32)
        {
            var delta = (int)((8 - (((IntPtr)smp).ToInt64() & 7)) & 7);
            for (var i = delta; i > 0; i--)
            {
                *res++ = *smp++;
            }

            n -= delta;

            MemCpy((long*)res, (long*)smp, n >> 3);
            var n8 = (n >> 3) << 3;
            n -= n8;
            smp += n8;
            res += n8;
        }
        if ((((IntPtr)smp).ToInt64() & 3) == (((IntPtr)res).ToInt64() & 3) && n > 16)
        {
            var delta = (int)((4 - (((IntPtr)smp).ToInt64() & 3)) & 3);
            for (var i = delta; i > 0; i--)
            {
                *res++ = *smp++;
            }

            n -= delta;

            MemCpy((int*)res, (int*)smp, n >> 2);
            var n4 = (n >> 2) << 2;
            n -= n4;
            smp += n4;
            res += n4;
        }
        for (var i = n; i > 0; i--)
        {
            *res++ = *smp++;
        }
    }

    public static unsafe void MemSet(int* res, int smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = smp;
        }
    }

    public static unsafe void MemSet(long* res, long smp, int n)
    {
        for (var i = n; i > 0; i--)
        {
            *res++ = smp;
        }
    }

    public static unsafe void MemSet(byte* res, byte smp, int n)
    {
        if (IntPtr.Size == 8 && (((IntPtr)res).ToInt64() & 7) == 0 && smp == 0 && n > 8)
        {
            MemSet((long*)res, 0, n >> 3);
            var n8 = (n >> 3) << 3;
            n -= n8;
            res += n8;
        }
        if ((((IntPtr)res).ToInt64() & 3) == 0 && smp == 0 && n > 4)
        {
            MemSet((int*)res, 0, n >> 2);
            var n4 = (n >> 2) << 2;
            n -= n4;
            res += n4;
        }
        for (var i = n; i > 0; i--)
        {
            *res++ = smp;
        }
    }

    public static unsafe void MemSet(byte[] res, byte smp, int offs, int n)
    {
        fixed (byte* pres = &res[offs])
        {
            MemSet(pres, smp, n);
        }
    }

    public static unsafe void MemSet(int[] res, int smp, int offs, int n)
    {
        fixed (int* pres = &res[offs])
        {
            MemSet(pres, smp, n);
        }
    }

    public static unsafe void MemSet(long[] res, long smp, int offs, int n)
    {
        fixed (long* pres = &res[offs])
        {
            MemSet(pres, smp, n);
        }
    }
}