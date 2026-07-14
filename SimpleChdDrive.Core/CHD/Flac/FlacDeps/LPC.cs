namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class Lpc
{
    public const int MaxLpcOrder = 32;
    public const int MaxLpcWindows = 16;
    public const int MaxLpcPrecisions = 4;
    public const int MaxLpcSections = 128;

    public static unsafe void window_welch(float* window, int l)
    {
        var nMax = l - 1;
        var n2 = nMax / 2.0;

        for (var n = 0; n <= nMax; n++)
        {
            var k = (n - n2) / n2;
            k = 1.0 - k * k;
            window[n] = (float)k;
        }
    }

    public static unsafe void window_bartlett(float* window, int l)
    {
        var nMax = l - 1;
        var n2 = nMax / 2.0;
        for (var n = 0; n <= nMax; n++)
        {
            var k = (n - n2) / n2;
            k = 1.0 - k * k;
            window[n] = (float)(k * k);
        }
    }

    public static unsafe void window_rectangle(float* window, int l)
    {
        for (var n = 0; n < l; n++)
        {
            window[n] = 1.0F;
        }
    }

    public static unsafe void window_flattop(float* window, int l)
    {
        var nMax = l - 1;
        for (var n = 0; n < l; n++)
        {
            window[n] = (float)(1.0 - 1.93 * Math.Cos(2.0 * Math.PI * n / nMax) + 1.29 * Math.Cos(4.0 * Math.PI * n / nMax) - 0.388 * Math.Cos(6.0 * Math.PI * n / nMax) + 0.0322 * Math.Cos(8.0 * Math.PI * n / nMax));
        }
    }

    public static unsafe void window_tukey(float* window, int l, double p)
    {
        const int z = 0;
        var np = (int)(p / 2.0 * l) - z;
        if (np > 0)
        {
            for (var n = 0; n < np - 1; n++)
            {
                window[n + z] = window[l - n - 1 - z] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * (n + 1) / np));
            }

            for (var n = z + np - 1; n < l - z - np + 1; n++)
            {
                window[n] = 1.0F;
            }
        }
    }

    public static unsafe void window_punchout_tukey(float* window, int l, double p, double p1, double start, double end)
    {
        var startN = (int)(start * l);
        var endN = (int)(end * l);
        var np = (int)(p / 2.0 * l);
        var np1 = (int)(p1 / 2.0 * l);
        int i, n = 0;

        if (startN != 0)
        {
            for (i = 1; n < np; n++, i++)
            {
                window[n] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * i / np));
            }

            for (; n < startN - np1; n++)
            {
                window[n] = 1.0f;
            }

            for (i = np1; n < startN; n++, i--)
            {
                window[n] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * i / np1));
            }
        }
        for (; n < endN; n++)
        {
            window[n] = 0.0f;
        }

        if (endN != l)
        {
            for (i = 1; n < endN + np1; n++, i++)
            {
                window[n] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * i / np1));
            }

            for (; n < l - np; n++)
            {
                window[n] = 1.0f;
            }

            for (i = np; n < l; n++, i--)
            {
                window[n] = (float)(0.5 - 0.5 * Math.Cos(Math.PI * i / np));
            }
        }
    }

    public static unsafe void window_hann(float* window, int l)
    {
        var nMax = l - 1;
        for (var n = 0; n < l; n++)
        {
            window[n] = (float)(0.5 - 0.5 * Math.Cos(2.0 * Math.PI * n / nMax));
        }
    }

    private static short sign_only(int val)
    {
        return (short)((val >> 31) + ((val - 1) >> 31) + 1);
    }

#if XXX
        static public unsafe void
            compute_corr_int(/*const*/ short* data1, short* data2, int len, int min, int lag, int* autoc)
        {
            for (int i = min; i <= lag; ++i)
            {
                int temp = 0;
                int temp2 = 0;

                for (int j = 0; j <= lag - i; ++j)
                    temp += data1[j + i] * data2[j];

                for (int j = lag + 1 - i; j < len - i; j += 2)
                {
                    temp += data1[j + i] * data2[j];
                    temp2 += data1[j + i + 1] * data2[j + 1];
                }
                autoc[i] = temp + temp2;
            }
        }
#endif

    /**
     * Calculates autocorrelation data from audio samples
     * A window function is applied before calculation.
     */
    public static unsafe void
        compute_autocorr( /*const*/ int* data, float* window, int len, int min, int lag, double* autoc)
    {
#if FPAC
            short* data1 = stackalloc short[len + 1];
            short* data2 = stackalloc short[len + 1];
            int* c1 = stackalloc int[Lpc.MaxLpcOrder + 1];
            int* c2 = stackalloc int[Lpc.MaxLpcOrder + 1];
            int* c3 = stackalloc int[Lpc.MaxLpcOrder + 1];
            int* c4 = stackalloc int[Lpc.MaxLpcOrder + 1];

            for (int i = 0; i < len; i++)
            {
                int val = (int)(data[i] * window[i]);
                data1[i] = (short)(sign_only(val) * (Math.Abs(val) >> 9));
                data2[i] = (short)(sign_only(val) * (Math.Abs(val) & 0x1ff));
            }
            data1[len] = 0;
            data2[len] = 0;

            compute_corr_int(data1, data1, len, min, lag, c1);
            compute_corr_int(data1, data2, len, min, lag, c2);
            compute_corr_int(data2, data1, len, min, lag, c3);
            compute_corr_int(data2, data2, len, min, lag, c4);

            for (int coeff = min; coeff <= lag; coeff++)
                autoc[coeff] = (c1[coeff] * (double)(1 << 18) + (c2[coeff] + c3[coeff]) * (double)(1 << 9) + c4[coeff]);
#else
#if XXX
            if (min == 0 && lag >= 4)
            {
                int* pdata = data;
                float* pwindow = window;

                double temp0 = 1.0;
                double temp1 = 1.0;
                double temp2 = 1.0;
                double temp3 = 1.0;
                double temp4 = 1.0;

                double c0 = *(pdata++) * *(pwindow++);
                float c1 = *(pdata++) * *(pwindow++);
                float c2 = *(pdata++) * *(pwindow++);
                float c3 = *(pdata++) * *(pwindow++);
                float c4 = *(pdata++) * *(pwindow++);

                int* finish = data + len;

                while (pdata <= finish)
                {
                    temp0 += c0 * c0;
                    temp1 += c0 * c1;
                    temp2 += c0 * c2;
                    temp3 += c0 * c3;
                    temp4 += c0 * c4;

                    c0 = c1;
                    c1 = c2;
                    c2 = c3;
                    c3 = c4;
                    c4 = *(pdata++) * *(pwindow++);
                }

                temp0 += c0 * c0;
                temp1 += c0 * c1;
                temp2 += c0 * c2;
                temp3 += c0 * c3;
                c0 = c1;
                c1 = c2;
                c2 = c3;
                temp0 += c0 * c0;
                temp1 += c0 * c1;
                temp2 += c0 * c2;
                c0 = c1;
                c1 = c2;
                temp0 += c0 * c0;
                temp1 += c0 * c1;
                c0 = c1;
                temp0 += c0 * c0;

                autoc[0] += temp0;
                autoc[1] += temp1;
                autoc[2] += temp2;
                autoc[3] += temp3;
                autoc[4] += temp4;
                min = 5;

                if (lag < min) return;
            }
#endif
        var data1 = stackalloc double[len];
        int i;

        for (i = 0; i < len; i++)
        {
            data1[i] = data[i] * window[i];
        }

        for (i = min; i <= lag; ++i)
        {
            double temp = 0;
            double temp2 = 0;
            var pdata = data1;
            var finish = data1 + len - 1 - i;

            while (pdata < finish)
            {
                temp += pdata[i] * *pdata++;
                temp2 += pdata[i] * *pdata++;
            }
            if (pdata <= finish)
            {
                temp += pdata[i] * *pdata++;
            }

            autoc[i] += temp + temp2;
        }
#endif
    }

    public static unsafe void
        compute_autocorr_windowless( /*const*/ int* data, int len, int min, int lag, double* autoc)
    {
        // if databits*2 + log2(len) <= 64
#if !XXX
#if XXX
            if (min == 0 && lag >= 4)
            {
                long temp0 = 0;
                long temp1 = 0;
                long temp2 = 0;
                long temp3 = 0;
                long temp4 = 0;
                int* pdata = data;
                int* finish = data + len - 4;
                while (pdata < finish)
                {
                    long c0 = *(pdata++);
                    temp0 += c0 * c0;
                    temp1 += c0 * pdata[0];
                    temp2 += c0 * pdata[1];
                    temp3 += c0 * pdata[2];
                    temp4 += c0 * pdata[3];
                }
                {
                    long c0 = *(pdata++);
                    temp0 += c0 * c0;
                    temp1 += c0 * pdata[0];
                    temp2 += c0 * pdata[1];
                    temp3 += c0 * pdata[2];
                }
                {
                    long c0 = *(pdata++);
                    temp0 += c0 * c0;
                    temp1 += c0 * pdata[0];
                    temp2 += c0 * pdata[1];
                }
                {
                    long c0 = *(pdata++);
                    temp0 += c0 * c0;
                    temp1 += c0 * pdata[0];
                }
                {
                    long c0 = *(pdata++);
                    temp0 += c0 * c0;
                }
                autoc[0] += temp0;
                autoc[1] += temp1;
                autoc[2] += temp2;
                autoc[3] += temp3;
                autoc[4] += temp4;
                min = 5;

                if (lag < min) return;
            }
#endif
        for (var i = min; i <= lag; ++i)
        {
            long temp = 0;
            long temp2 = 0;
            var pdata = data;
            var finish = data + len - i - 1;
            while (pdata < finish)
            {
                temp += (long)pdata[i] * *pdata++;
                temp2 += (long)pdata[i] * *pdata++;
            }
            if (pdata <= finish)
            {
                temp += (long)pdata[i] * *pdata++;
            }

            autoc[i] += temp + temp2;
        }
#else
            for (int i = min; i <= lag; ++i)
            {
                double temp = 0;
                double temp2 = 0;
                int* pdata = data;
                int* finish = data + len - i - 1;

                while (pdata < finish)
                {
                    temp += (double)pdata[i] * (double)(*pdata++);
                    temp2 += (double)pdata[i] * (double)(*pdata++);
                }
                if (pdata <= finish)
                    temp += (double)pdata[i] * (double)(*pdata++);
                autoc[i] += temp + temp2;
            }
#endif
    }

    public static unsafe void
        compute_autocorr_windowless_large( /*const*/ int* data, int len, int min, int lag, double* autoc)
    {
        for (var i = min; i <= lag; ++i)
        {
            double temp = 0;
            double temp2 = 0;
            var pdata = data;
            var finish = data + len - i - 1;
            while (pdata < finish)
            {
                temp += (long)pdata[i] * *pdata++;
                temp2 += (long)pdata[i] * *pdata++;
            }
            if (pdata <= finish)
            {
                temp += (long)pdata[i] * *pdata++;
            }

            autoc[i] += temp + temp2;
        }
    }

    public static unsafe void
        compute_autocorr_glue( /*const*/ int* data, float* window, int offs, int offs1, int min, int lag, double* autoc)
    {
        var data1 = stackalloc double[lag + lag];
        for (var i = -lag; i < lag; i++)
        {
            data1[i + lag] = offs + i >= 0 && offs + i < offs1 ? data[offs + i] * window[offs + i] : 0;
        }

        for (var i = min; i <= lag; ++i)
        {
            double temp = 0;
            var pdata = data1 + lag - i;
            var finish = data1 + lag;
            while (pdata < finish)
            {
                temp += pdata[i] * *pdata++;
            }

            autoc[i] += temp;
        }
    }

    public static unsafe void
        compute_autocorr_glue( /*const*/ int* data, int min, int lag, double* autoc)
    {
        for (var i = min; i <= lag; ++i)
        {
            long temp = 0;
            var pdata = data - i;
            var finish = data;
            while (pdata < finish)
            {
                temp += (long)pdata[i] * *pdata++;
            }

            autoc[i] += temp;
        }
    }

    /**
     * Levinson-Durbin recursion.
     * Produces LPC coefficients from autocorrelation data.
     */
    public static unsafe void
        compute_lpc_coefs(uint maxOrder, double* reff, float* lpc /*[][MaxLpcOrder]*/)
    {
        var lpcTmp = stackalloc double[MaxLpcOrder];

        if (maxOrder > MaxLpcOrder)
            throw new InvalidOperationException("weird");

        for (var i = 0; i < maxOrder; i++)
        {
            lpcTmp[i] = 0;
        }

        for (var i = 0; i < maxOrder; i++)
        {
            var r = reff[i];
            var i2 = i >> 1;
            lpcTmp[i] = r;
            for (var j = 0; j < i2; j++)
            {
                var tmp = lpcTmp[j];
                lpcTmp[j] += r * lpcTmp[i - 1 - j];
                lpcTmp[i - 1 - j] += r * tmp;
            }

            if (0 != (i & 1))
            {
                lpcTmp[i2] += lpcTmp[i2] * r;
            }

            for (var j = 0; j <= i; j++)
            {
                lpc[i * MaxLpcOrder + j] = (float)-lpcTmp[j];
            }
        }
    }

    public static unsafe void
        compute_schur_reflection( /*const*/ double* autoc, uint maxOrder,
            double* reff /*[][MaxLpcOrder]*/, double* err)
    {
        var gen0 = stackalloc double[MaxLpcOrder];
        var gen1 = stackalloc double[MaxLpcOrder];

        // Schur recursion
        for (uint i = 0; i < maxOrder; i++)
        {
            gen0[i] = gen1[i] = autoc[i + 1];
        }

        var error = autoc[0];
        reff[0] = -gen1[0] / error;
        error += gen1[0] * reff[0];
        err[0] = error;
        for (uint i = 1; i < maxOrder; i++)
        {
            for (uint j = 0; j < maxOrder - i; j++)
            {
                gen1[j] = gen1[j + 1] + reff[i - 1] * gen0[j];
                gen0[j] = gen1[j + 1] * reff[i - 1] + gen0[j];
            }
            reff[i] = -gen1[0] / error;
            error += gen1[0] * reff[i];
            err[i] = error;
        }
    }

    /**
     * Quantize LPC coefficients
     */
    public static unsafe void
        quantize_lpc_coefs(float* lpcIn, int order, uint precision, int* lpcOut,
            out int shift, int maxShift, int zeroShift)
    {
        // define maximum levels
        var qmax = (1 << ((int)precision - 1)) - 1;

        // find maximum coefficient value
        var cmax = 0.0F;
        for (int i = 0; i < order; i++)
        {
            float d = Math.Abs(lpcIn[i]);
            if (d > cmax)
            {
                cmax = d;
            }
        }
        // if maximum value quantizes to zero, return all zeros
        if (cmax * (1 << maxShift) < 1.0)
        {
            shift = zeroShift;
            for (int i = 0; i < order; i++)
            {
                lpcOut[i] = 0;
            }

            return;
        }

        // calculate level shift which scales max coeff to available bits
        var sh = maxShift;
        while (cmax * (1 << sh) > qmax && sh > 0)
        {
            sh--;
        }

        // since negative shift values are unsupported in decoder, scale down
        // coefficients instead
        if (sh == 0 && cmax > qmax)
        {
            var scale = qmax / cmax;
            for (int i = 0; i < order; i++)
            {
                lpcIn[i] *= scale;
            }
        }

        // output quantized coefficients and level shift
        float error = 0;
        for (int i = 0; i < order; i++)
        {
            error += lpcIn[i] * (1 << sh);
            int q = (int)(error + 0.5);
            if (q < -(qmax + 1))
            {
                q = -(qmax + 1);
            }

            if (q > qmax)
            {
                q = qmax;
            }

            error -= q;
            lpcOut[i] = q;
        }
        shift = sh;
    }

    private static unsafe ulong
        encode_residual_partition(int* s, int* r, int* segEnd, int* coefs, int shift, int order)
    {
        var sum = 0ul;
        var c0 = coefs[0];
        var c1 = coefs[1];
        switch (order)
        {
            case 1:
                while (s < segEnd)
                {
                    var pred = c0 * *s++;
                    //*(r++) = *s - (pred >> shift);
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                }
                break;
            case 2:
                while (s < segEnd)
                {
                    var pred = c1 * *s++;
                    pred += c0 * *s++;
                    var d = *r++ = *s-- - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                }
                break;
            case 3:
                while (s < segEnd)
                {
                    var pred = coefs[2] * *s++ +
                               c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 2;
                }
                break;
            case 4:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 3;
                }
                break;
            case 5:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 4;
                }
                break;
            case 6:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 5;
                }
                break;
            case 7:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 6;
                }
                break;
            case 8:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 7;
                }
                break;
            case 9:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 8;
                }
                break;
            case 10:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 9;
                }
                break;
            case 11:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 10;
                }
                break;
            case 12:
                while (s < segEnd)
                {
                    var c = coefs + order - 1;
                    var pred =
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 11;
                }
                break;
            default:
                while (s < segEnd)
                {
                    var pred = 0;
                    var c = coefs + order - 1;
                    var c11 = coefs + 11;
                    while (c > c11)
                    {
                        pred += *c-- * *s++;
                    }

                    pred +=
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        *c-- * *s++ + *c-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    var d = *r++ = *s - (pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= order - 1;
                }
                break;
        }
        return sum;
    }

    public static unsafe void
        encode_residual(int* res, int* smp, int n, int order,
            int* coefs, int shift, ulong* sums, int pmax)
    {
        for (var i = 0; i < order; i++)
        {
            res[i] = smp[i];
        }

        var s = smp;
        var sEnd = smp + n - order;
        var segEnd = s + (n >> pmax) - order;
        var r = res + order;
        while (s < sEnd)
        {
            *sums++ = encode_residual_partition(s, r, segEnd, coefs, shift, order);
            r += segEnd - s;
            s = segEnd;
            segEnd += n >> pmax;
        }
    }

    private static unsafe ulong
        encode_residual_long_partition(int* s, int* r, int* segEnd, int* coefs, int shift, int order)
    {
        var sum = 0ul;
        var c0 = coefs[0];
        var c1 = coefs[1];
        switch (order)
        {
            case 1:
                while (s < segEnd)
                {
                    var pred = c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                }
                break;
            case 2:
                while (s < segEnd)
                {
                    var pred = c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s-- - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                }
                break;
            case 3:
                while (s < segEnd)
                {
                    var pred = coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 2;
                }
                break;
            case 4:
                while (s < segEnd)
                {
                    var pred = coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 3;
                }
                break;
            case 5:
                while (s < segEnd)
                {
                    var pred = coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 4;
                }
                break;
            case 6:
                while (s < segEnd)
                {
                    var pred = coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 5;
                }
                break;
            case 7:
                while (s < segEnd)
                {
                    var pred = coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 6;
                }
                break;
            case 8:
                while (s < segEnd)
                {
                    var pred = coefs[7] * (long)*s++;
                    pred += coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= 7;
                }
                break;
            default:
                while (s < segEnd)
                {
                    long pred = 0;
                    var co = coefs + order - 1;
                    var c7 = coefs + 7;
                    while (co > c7)
                    {
                        pred += *co-- * (long)*s++;
                    }

                    pred += coefs[7] * (long)*s++;
                    pred += coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    var d = *r++ = *s - (int)(pred >> shift);
                    sum += (uint)((d << 1) ^ (d >> 31));
                    s -= order - 1;
                }
                break;
        }
        return sum;
    }

    public static unsafe void
        encode_residual_long(int* res, int* smp, int n, int order,
            int* coefs, int shift, ulong* sums, int pmax)
    {
        for (var i = 0; i < order; i++)
        {
            res[i] = smp[i];
        }

        var s = smp;
        var sEnd = smp + n - order;
        var segEnd = s + (n >> pmax) - order;
        var r = res + order;
        while (s < sEnd)
        {
            *sums++ = encode_residual_long_partition(s, r, segEnd, coefs, shift, order);
            r += segEnd - s;
            s = segEnd;
            segEnd += n >> pmax;
        }
    }

    public static unsafe void
        decode_residual(int* res, int* smp, int n, int order,
            int* coefs, int shift)
    {
        for (var i = 0; i < order; i++)
        {
            smp[i] = res[i];
        }

        var s = smp;
        var r = res + order;
        var c0 = coefs[0];
        var c1 = coefs[1];
        switch (order)
        {
            case 1:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = c0 * *s++;
                    *s = *r++ + (pred >> shift);
                }
                break;
            case 2:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = c1 * *s++ + c0 * *s++;
                    *s-- = *r++ + (pred >> shift);
                }
                break;
            case 3:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 2;
                }
                break;
            case 4:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 3;
                }
                break;
            case 5:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 4;
                }
                break;
            case 6:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 5;
                }
                break;
            case 7:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 6;
                }
                break;
            case 8:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 7;
                }
                break;
            case 9:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 8;
                }
                break;
            case 10:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 9;
                }
                break;
            case 11:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 10;
                }
                break;
            case 12:
                for (var i = n - order; i > 0; i--)
                {
                    var co = coefs + order - 1;
                    var pred =
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        *co-- * *s++ + *co-- * *s++ +
                        c1 * *s++ + c0 * *s++;
                    *s = *r++ + (pred >> shift);
                    s -= 11;
                }
                break;
            default:
                for (var i = order; i < n; i++)
                {
                    s = smp + i - order;
                    var pred = 0;
                    var co = coefs + order - 1;
                    var c7 = coefs + 7;
                    while (co > c7)
                    {
                        pred += *co-- * *s++;
                    }

                    pred += coefs[7] * *s++;
                    pred += coefs[6] * *s++;
                    pred += coefs[5] * *s++;
                    pred += coefs[4] * *s++;
                    pred += coefs[3] * *s++;
                    pred += coefs[2] * *s++;
                    pred += c1 * *s++;
                    pred += c0 * *s++;
                    *s = *r++ + (pred >> shift);
                }
                break;
        }
    }
    public static unsafe void
        decode_residual_long(int* res, int* smp, int n, int order,
            int* coefs, int shift)
    {
        for (var i = 0; i < order; i++)
        {
            smp[i] = res[i];
        }

        var s = smp;
        var r = res + order;
        var c0 = coefs[0];
        var c1 = coefs[1];
        switch (order)
        {
            case 1:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                }
                break;
            case 2:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s-- = *r++ + (int)(pred >> shift);
                }
                break;
            case 3:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 2;
                }
                break;
            case 4:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 3;
                }
                break;
            case 5:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 4;
                }
                break;
            case 6:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 5;
                }
                break;
            case 7:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 6;
                }
                break;
            case 8:
                for (var i = n - order; i > 0; i--)
                {
                    var pred = coefs[7] * (long)*s++;
                    pred += coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                    s -= 7;
                }
                break;
            default:
                for (var i = order; i < n; i++)
                {
                    s = smp + i - order;
                    long pred = 0;
                    var co = coefs + order - 1;
                    var c7 = coefs + 7;
                    while (co > c7)
                    {
                        pred += *co-- * (long)*s++;
                    }

                    pred += coefs[7] * (long)*s++;
                    pred += coefs[6] * (long)*s++;
                    pred += coefs[5] * (long)*s++;
                    pred += coefs[4] * (long)*s++;
                    pred += coefs[3] * (long)*s++;
                    pred += coefs[2] * (long)*s++;
                    pred += c1 * (long)*s++;
                    pred += c0 * (long)*s++;
                    *s = *r++ + (int)(pred >> shift);
                }
                break;
        }
    }
}