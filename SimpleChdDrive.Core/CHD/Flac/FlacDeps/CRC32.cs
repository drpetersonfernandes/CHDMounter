namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public static class Crc32
{
    public static readonly uint[] Table;

    public static uint ComputeChecksum(uint crc, byte val)
    {
        return (crc >> 8) ^ Table[(crc & 0xff) ^ val];
    }

    public static unsafe uint ComputeChecksum(uint crc, byte* bytes, int count)
    {
        fixed (uint* t = Table)
        {
            for (var i = 0; i < count; i++)
            {
                crc = (crc >> 8) ^ t[(crc ^ bytes[i]) & 0xff];
            }
        }

        return crc;
    }

    public static unsafe uint ComputeChecksum(uint crc, byte[] bytes, int pos, int count)
    {
        fixed (byte* pbytes = &bytes[pos])
        {
            return ComputeChecksum(crc, pbytes, count);
        }
    }

    public static uint ComputeChecksum(uint crc, uint s)
    {
        return ComputeChecksum(ComputeChecksum(ComputeChecksum(ComputeChecksum(
            crc, (byte)s), (byte)(s >> 8)), (byte)(s >> 16)), (byte)(s >> 24));
    }

    public static unsafe uint ComputeChecksum(uint crc, int* samples, int count)
    {
        for (var i = 0; i < count; i++)
        {
            int s1 = samples[2 * i], s2 = samples[2 * i + 1];
            crc = ComputeChecksum(ComputeChecksum(ComputeChecksum(ComputeChecksum(
                crc, (byte)s1), (byte)(s1 >> 8)), (byte)s2), (byte)(s2 >> 8));
        }
        return crc;
    }

    internal static uint Reflect(uint val, int ch)
    {
        uint value = 0;
        // Swap bit 0 for bit 7
        // bit 1 for bit 6, etc.
        for (var i = 1; i < ch + 1; i++)
        {
            if (0 != (val & 1))
            {
                value |= 1U << (ch - i);
            }

            val >>= 1;
        }
        return value;
    }

    private const uint UPolynomial = 0x04c11db7;
    private const uint UReversePolynomial = 0xedb88320;
    private const uint UReversePolynomial2 = 0xdb710641;

    private static readonly uint[,] CombineTable;
    private static readonly uint[,] SubstractTable;

#if need_invert_binary_matrix
        static unsafe void invert_binary_matrix(uint* mat, uint* inv, int rows)
        {
            int cols, i, j;
            uint tmp;

            cols = rows;

            for (i = 0; i < rows; i++) inv[i] = (1U << i);

            /* First -- convert into upper triangular */

            for (i = 0; i < cols; i++)
            {

                /* Swap rows if we ave a zero i,i element.  If we can't swap, then the
                   matrix was not invertible */

                if ((mat[i] & (1 << i)) == 0)
                {
                    for (j = i + 1; j < rows && (mat[j] & (1 << i)) == 0; j++) ;
                    if (j == rows)
                        throw new Exception("Matrix not invertible");
                    tmp = mat[i]; mat[i] = mat[j]; mat[j] = tmp;
                    tmp = inv[i]; inv[i] = inv[j]; inv[j] = tmp;
                }

                /* Now for each j>i, add A_ji*Ai to Aj */
                for (j = i + 1; j != rows; j++)
                {
                    if ((mat[j] & (1 << i)) != 0)
                    {
                        mat[j] ^= mat[i];
                        inv[j] ^= inv[i];
                    }
                }
            }

            /* Now the matrix is upper triangular.  Start at the top and multiply down */

            for (i = rows - 1; i >= 0; i--)
            {
                for (j = 0; j < i; j++)
                {
                    if ((mat[j] & (1 << i)) != 0)
                    {
                        /*        mat[j] ^= mat[i]; */
                        inv[j] ^= inv[i];
                    }
                }
            }
        }
#endif

    static unsafe Crc32()
    {
        Table = new uint[256];
        for (uint i = 0; i < Table.Length; i++)
        {
            Table[i] = Reflect(i, 8) << 24;
            for (var j = 0; j < 8; j++)
            {
                Table[i] = (Table[i] << 1) ^ ((Table[i] & (1U << 31)) == 0 ? 0 : UPolynomial);
            }

            Table[i] = Reflect(Table[i], 32);
        }
        CombineTable = new uint[Gf2Dim, Gf2Dim];
        SubstractTable = new uint[Gf2Dim, Gf2Dim];
        CombineTable[0, 0] = UReversePolynomial;
        SubstractTable[0, 31] = UReversePolynomial2;
        for (var n = 1; n < Gf2Dim; n++)
        {
            CombineTable[0, n] = 1U << (n - 1);
            SubstractTable[0, n - 1] = 1U << n;
        }
        fixed (uint* ct = &CombineTable[0, 0], st = &SubstractTable[0, 0])
        {
            //for (int i = 0; i < GF2_DIM; i++)
            //  st[32 + i] = ct[i];
            //invert_binary_matrix(st + 32, st, GF2_DIM);

            for (var i = 1; i < Gf2Dim; i++)
            {
                gf2_matrix_square(ct + i * 32, ct + (i - 1) * 32);
                gf2_matrix_square(st + i * 32, st + (i - 1) * 32);
            }
        }
    }

    private const int Gf2Dim = 32;
    //const int GF2_DIM2 = 67;

    private static unsafe uint gf2_matrix_times(uint* umat, uint uvec)
    {
        var vec = (int)uvec;
        var mat = (int*)umat;
        return (uint)(
            (*mat++ & ((vec << 31) >> 31)) ^
            (*mat++ & ((vec << 30) >> 31)) ^
            (*mat++ & ((vec << 29) >> 31)) ^
            (*mat++ & ((vec << 28) >> 31)) ^
            (*mat++ & ((vec << 27) >> 31)) ^
            (*mat++ & ((vec << 26) >> 31)) ^
            (*mat++ & ((vec << 25) >> 31)) ^
            (*mat++ & ((vec << 24) >> 31)) ^
            (*mat++ & ((vec << 23) >> 31)) ^
            (*mat++ & ((vec << 22) >> 31)) ^
            (*mat++ & ((vec << 21) >> 31)) ^
            (*mat++ & ((vec << 20) >> 31)) ^
            (*mat++ & ((vec << 19) >> 31)) ^
            (*mat++ & ((vec << 18) >> 31)) ^
            (*mat++ & ((vec << 17) >> 31)) ^
            (*mat++ & ((vec << 16) >> 31)) ^
            (*mat++ & ((vec << 15) >> 31)) ^
            (*mat++ & ((vec << 14) >> 31)) ^
            (*mat++ & ((vec << 13) >> 31)) ^
            (*mat++ & ((vec << 12) >> 31)) ^
            (*mat++ & ((vec << 11) >> 31)) ^
            (*mat++ & ((vec << 10) >> 31)) ^
            (*mat++ & ((vec << 09) >> 31)) ^
            (*mat++ & ((vec << 08) >> 31)) ^
            (*mat++ & ((vec << 07) >> 31)) ^
            (*mat++ & ((vec << 06) >> 31)) ^
            (*mat++ & ((vec << 05) >> 31)) ^
            (*mat++ & ((vec << 04) >> 31)) ^
            (*mat++ & ((vec << 03) >> 31)) ^
            (*mat++ & ((vec << 02) >> 31)) ^
            (*mat++ & ((vec << 01) >> 31)) ^
            (*mat & (vec >> 31)));
    }

    /* ========================================================================= */
    private static unsafe void gf2_matrix_square(uint* square, uint* mat)
    {
        for (var n = 0; n < Gf2Dim; n++)
        {
            square[n] = gf2_matrix_times(mat, mat[n]);
        }
    }

    public static unsafe uint Combine(uint crc1, uint crc2, long len2)
    {
        /* degenerate case */
        if (len2 == 0)
            return crc1;
        if (crc1 == 0)
            return crc2;

        if (len2 < 0)
            throw new ArgumentException("crc.Combine length cannot be negative", nameof(len2));

        fixed (uint* ct = &CombineTable[0, 0])
        {
            var n = 3;
            do
            {
                /* apply zeros operator for this bit of len2 */
                if ((len2 & 1) != 0)
                {
                    crc1 = gf2_matrix_times(ct + 32 * n, crc1);
                }

                len2 >>= 1;
                n = (n + 1) & (Gf2Dim - 1);
                /* if no more bits set, then done */
            } while (len2 != 0);
        }

        /* return combined crc */
        crc1 ^= crc2;
        return crc1;
    }

    public static unsafe uint Subtract(uint crc1, uint crc2, long len2)
    {
        switch (len2)
        {
            /* degenerate case */
            case 0:
                return crc1;
            case < 0:
                throw new ArgumentException("crc.Combine length cannot be negative", nameof(len2));
        }

        crc1 ^= crc2;

        fixed (uint* st = &SubstractTable[0, 0])
        {
            var n = 3;
            do
            {
                /* apply zeros operator for this bit of len2 */
                if ((len2 & 1) != 0)
                {
                    crc1 = gf2_matrix_times(st + 32 * n, crc1);
                }

                len2 >>= 1;
                n = (n + 1) & (Gf2Dim - 1);
                /* if no more bits set, then done */
            } while (len2 != 0);
        }

        /* return combined crc */
        return crc1;
    }
}