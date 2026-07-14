namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class LpcSubframeInfo
{
    // public LpcContext[] lpc_ctx;
    public double[,] AutocorrSectionValues { get; } = new double[Lpc.MaxLpcSections, Lpc.MaxLpcOrder + 1];
    public int[] AutocorrSectionOrders { get; } = new int[Lpc.MaxLpcSections];
    //public int obits;

    public void Reset()
    {
        for (var sec = 0; sec < AutocorrSectionOrders.Length; sec++)
        {
            AutocorrSectionOrders[sec] = 0;
        }
    }
}

public unsafe struct LpcWindowSection
{
    public enum SectionType
    {
        Zero,
        One,
        OneLarge,
        Data,
        OneGlue,
        Glue
    }
    public int MStart { get; set; }
    public int MEnd { get; set; }
    public SectionType MType { get; set; }
    public int MId { get; set; }
    public LpcWindowSection(int end)
    {
        MId = -1;
        MStart = 0;
        MEnd = end;
        MType = SectionType.Data;
    }
    public void SetData(int start, int end)
    {
        MId = -1;
        MStart = start;
        MEnd = end;
        MType = SectionType.Data;
    }
    public void setOne(int start, int end)
    {
        MId = -1;
        MStart = start;
        MEnd = end;
        MType = SectionType.One;
    }
    public void SetGlue(int start)
    {
        MId = -1;
        MStart = start;
        MEnd = start;
        MType = SectionType.Glue;
    }
    public void SetZero(int start, int end)
    {
        MId = -1;
        MStart = start;
        MEnd = end;
        MType = SectionType.Zero;
    }

    public readonly void compute_autocorr( /*const*/ int* data, float* window, int minOrder, int order, int blocksize, double* autoc)
    {
        switch (MType)
        {
            case SectionType.OneLarge:
                Lpc.compute_autocorr_windowless_large(data + MStart, MEnd - MStart, minOrder, order, autoc);
                break;
            case SectionType.One:
                Lpc.compute_autocorr_windowless(data + MStart, MEnd - MStart, minOrder, order, autoc);
                break;
            case SectionType.Data:
                Lpc.compute_autocorr(data + MStart, window + MStart, MEnd - MStart, minOrder, order, autoc);
                break;
            case SectionType.Glue:
                Lpc.compute_autocorr_glue(data, window, MStart, MEnd, minOrder, order, autoc);
                break;
            case SectionType.OneGlue:
                Lpc.compute_autocorr_glue(data + MStart, minOrder, order, autoc);
                break;
        }
    }

    public static void Detect(int windowcount, float* windowSegment, int stride, int sz, int bps, LpcWindowSection* sections)
    {
        var sectionId = 0;
        var boundaries = new List<int>();
        var types = new SectionType[windowcount, Lpc.MaxLpcSections * 2];
        var alias = new int[windowcount, Lpc.MaxLpcSections * 2];
        var aliasSet = new int[windowcount, Lpc.MaxLpcSections * 2];
        for (var x = 0; x < sz; x++)
        {
            for (var i = 0; i < windowcount; i++)
            {
                var a = alias[i, boundaries.Count];
                var w = windowSegment[i * stride + x];
                var wa = windowSegment[a * stride + x];
                if (wa != w)
                {
                    for (var i1 = i; i1 < windowcount; i1++)
                        if (alias[i1, boundaries.Count] == a
                            && w == windowSegment[i1 * stride + x])
                        {
                            alias[i1, boundaries.Count] = i;
                        }
                }
                if (boundaries.Count >= Lpc.MaxLpcSections * 2) throw new InvalidOperationException();

                types[i, boundaries.Count] =
                    boundaries.Count >= Lpc.MaxLpcSections * 2 - 2 ? SectionType.Data : w == 0.0 ? SectionType.Zero : w != 1.0 ? SectionType.Data : bps * 2 + BitReader.Log2I(sz) >= 61 ? SectionType.OneLarge :
                                    SectionType.One;
            }
            var isBoundary = false;
            for (var i = 0; i < windowcount; i++)
            {
                isBoundary |= boundaries.Count == 0 ||
                              types[i, boundaries.Count - 1] != types[i, boundaries.Count];
            }
            if (isBoundary)
            {
                for (var i = 0; i < windowcount; i++)
                for (var i1 = 0; i1 < windowcount; i1++)
                    if (i != i1 && alias[i, boundaries.Count] == alias[i1, boundaries.Count])
                    {
                        aliasSet[i, boundaries.Count] |= 1 << i1;
                    }

                boundaries.Add(x);
            }
        }
        boundaries.Add(sz);
        var secs = new int[windowcount];
        // Reconstruct segments list.
        for (var j = 0; j < boundaries.Count - 1; j++)
        {
            for (var i = 0; i < windowcount; i++)
            {
                var windowSections = sections + i * Lpc.MaxLpcSections;
                // leave room for glue
                if (secs[i] >= Lpc.MaxLpcSections - 1)
                {
                    throw new InvalidOperationException();
                    //window_sections[secs[i] - 1].m_type = LpcWindowSection.SectionType.Data;
                    //window_sections[secs[i] - 1].m_end = boundaries[j + 1];
                    //continue;
                }
                windowSections[secs[i]].SetData(boundaries[j], boundaries[j + 1]);
                windowSections[secs[i]++].MType = types[i, j];
            }
            for (var i = 0; i < windowcount; i++)
            {
                var windowSections = sections + i * Lpc.MaxLpcSections;
                var sec = secs[i] - 1;
                if (sec > 0
                    && j > 0 && (aliasSet[i, j] == aliasSet[i, j - 1] || windowSections[sec].MType == SectionType.Zero)
                    && windowSections[sec].MStart == boundaries[j]
                    && windowSections[sec].MEnd == boundaries[j + 1]
                    && windowSections[sec - 1].MEnd == boundaries[j]
                    && windowSections[sec - 1].MType == windowSections[sec].MType)
                {
                    windowSections[sec - 1].MEnd = windowSections[sec].MEnd;
                    secs[i]--;
                    continue;
                }
                if (sectionId >= Lpc.MaxLpcSections) throw new InvalidOperationException();

                if (aliasSet[i, j] != 0
                    && types[i, j] != SectionType.Zero)
                {
                    for (var i1 = i; i1 < windowcount; i1++)
                        if (alias[i1, j] == i && secs[i1] > 0)
                        {
                            sections[i1 * Lpc.MaxLpcSections + secs[i1] - 1].MId = sectionId;
                        }

                    sectionId++;
                }

                switch (sec)
                {
                    // TODO: section_id for glue? nontrivial, must be sure next sections are the same size
                    case > 0
                        when (windowSections[sec].MType == SectionType.One || windowSections[sec].MType == SectionType.OneLarge)
                             && windowSections[sec].MEnd - windowSections[sec].MStart >= Lpc.MaxLpcOrder
                             && (windowSections[sec - 1].MType == SectionType.One || windowSections[sec - 1].MType == SectionType.OneLarge)
                             && windowSections[sec - 1].MEnd - windowSections[sec - 1].MStart >= Lpc.MaxLpcOrder:
                        windowSections[sec + 1] = windowSections[sec];
                        windowSections[sec].MEnd = windowSections[sec].MStart;
                        windowSections[sec].MType = SectionType.OneGlue;
                        windowSections[sec].MId = -1;
                        secs[i]++;
                        continue;
                    case > 0
                        when windowSections[sec].MType != SectionType.Zero
                             && windowSections[sec - 1].MType != SectionType.Zero:
                        windowSections[sec + 1] = windowSections[sec];
                        windowSections[sec].MEnd = windowSections[sec].MStart;
                        windowSections[sec].MType = SectionType.Glue;
                        windowSections[sec].MId = -1;
                        secs[i]++;
                        break;
                }
            }
        }
        for (var i = 0; i < windowcount; i++)
        {
            for (var s = 0; s < secs[i]; s++)
            {
                var windowSections = sections + i * Lpc.MaxLpcSections;
                if (windowSections[s].MType == SectionType.Glue
                    || windowSections[s].MType == SectionType.OneGlue)
                {
                    windowSections[s].MEnd = windowSections[s + 1].MEnd;
                }
            }
            while (secs[i] < Lpc.MaxLpcSections)
            {
                var windowSections = sections + i * Lpc.MaxLpcSections;
                windowSections[secs[i]++].SetZero(sz, sz);
            }
        }
    }
}

/// <summary>
/// Context for LPC coefficients calculation and order estimation
/// </summary>
public unsafe class LpcContext
{
    /// <summary>
    /// Reset to initial (blank) state
    /// </summary>
    public void Reset()
    {
        _autocorrOrder = 0;
        for (var iPrecision = 0; iPrecision < Lpc.MaxLpcPrecisions; iPrecision++)
        {
            DoneLpcs[iPrecision] = 0;
        }
    }

    /// <summary>
    /// Calculate autocorrelation data and reflection coefficients.
    /// Can be used to incrementaly compute coefficients for higher orders,
    /// because it caches them.
    /// </summary>
    /// <param name="subframe">Subframe info</param>
    /// <param name="order">Maximum order</param>
    /// <param name="blocksize">Block size</param>
    /// <param name="samples">Samples pointer</param>
    /// <param name="window">Window function</param>
    /// <param name="sections">Window sections</param>
    public void GetReflection(LpcSubframeInfo subframe, int order, int blocksize, int* samples, float* window, LpcWindowSection* sections)
    {
        if (_autocorrOrder > order)
            return;

        fixed (double* reff = Reflection, autoc = AutocorrValues, err = PredictionError)
        {
            for (var i = _autocorrOrder; i <= order; i++)
            {
                autoc[i] = 0;
            }

            for (var section = 0; section < Lpc.MaxLpcSections; section++)
            {
                if (sections[section].MType == LpcWindowSection.SectionType.Zero)
                {
                    continue;
                }
                if (sections[section].MId >= 0)
                {
                    if (subframe.AutocorrSectionOrders[sections[section].MId] <= order)
                    {
                        fixed (double* autocsec = &subframe.AutocorrSectionValues[sections[section].MId, 0])
                        {
                            var minOrder = subframe.AutocorrSectionOrders[sections[section].MId];
                            for (var i = minOrder; i <= order; i++)
                            {
                                autocsec[i] = 0;
                            }

                            sections[section].compute_autocorr(samples, window, minOrder, order, blocksize, autocsec);
                        }
                        subframe.AutocorrSectionOrders[sections[section].MId] = order + 1;
                    }
                    for (var i = _autocorrOrder; i <= order; i++)
                    {
                        autoc[i] += subframe.AutocorrSectionValues[sections[section].MId, i];
                    }
                }
                else
                {
                    sections[section].compute_autocorr(samples, window, _autocorrOrder, order, blocksize, autoc);
                }
            }
            Lpc.compute_schur_reflection(autoc, (uint)order, reff, err);
            _autocorrOrder = order + 1;
        }
    }
#if XXX
        public void GetReflection1(int order, int* samples, int blocksize, float* window)
        {
            if (autocorr_order > order)
                return;
            fixed (double* reff = reflection_coeffs, autoc = autocorr_values, err = prediction_error)
            {
                Lpc.compute_autocorr(samples, blocksize, 0, order + 1, autoc, window);
                for (int i = 1; i <= order; i++)
                    autoc[i] = autoc[i + 1];
                Lpc.compute_schur_reflection(autoc, (uint)order, reff, err);
                autocorr_order = order + 1;
            }
        }

        public void ComputeReflection(int order, float* autocorr)
        {
            fixed (double* reff = reflection_coeffs, autoc = autocorr_values, err = prediction_error)
            {
                for (int i = 0; i <= order; i++)
                    autoc[i] = autocorr[i];
                Lpc.compute_schur_reflection(autoc, (uint)order, reff, err);
                autocorr_order = order + 1;
            }
        }

        public void ComputeReflection(int order, double* autocorr)
        {
            fixed (double* reff = reflection_coeffs, autoc = autocorr_values, err = prediction_error)
            {
                for (int i = 0; i <= order; i++)
                    autoc[i] = autocorr[i];
                Lpc.compute_schur_reflection(autoc, (uint)order, reff, err);
                autocorr_order = order + 1;
            }
        }
#endif
    public double Akaike(int blocksize, int order, double alpha, double beta)
    {
        //return (blocksize - order) * (Math.Log(prediction_error[order - 1]) - Math.Log(1.0)) + Math.Log(blocksize) * order * (alpha + beta * order);
        //return blocksize * (Math.Log(prediction_error[order - 1]) - Math.Log(autocorr_values[0]) / 2) + Math.Log(blocksize) * order * (alpha + beta * order);
        return blocksize * Math.Log(PredictionError[order - 1]) + Math.Log(blocksize) * order * (alpha + beta * order);
    }

    /// <summary>
    /// Sorts orders based on Akaike's criteria
    /// </summary>
    /// <param name="blocksize">Frame size</param>
    /// <param name="count">Number of orders to select</param>
    /// <param name="minOrder">Minimum LPC order</param>
    /// <param name="maxOrder">Maximum LPC order</param>
    /// <param name="alpha">Alpha coefficient for Akaike criterion</param>
    /// <param name="beta">Beta coefficient for Akaike criterion</param>
    public void SortOrdersAkaike(int blocksize, int count, int minOrder, int maxOrder, double alpha, double beta)
    {
        for (var i = minOrder; i <= maxOrder; i++)
        {
            BestOrders[i - minOrder] = i;
        }

        var lim = maxOrder - minOrder + 1;
        for (var i = 0; i < lim && i < count; i++)
        {
            for (var j = i + 1; j < lim; j++)
            {
                if (Akaike(blocksize, BestOrders[j], alpha, beta) < Akaike(blocksize, BestOrders[i], alpha, beta))
                {
                    (BestOrders[j], BestOrders[i]) = (BestOrders[i], BestOrders[j]);
                }
            }
        }
    }

    /// <summary>
    /// Produces LPC coefficients from autocorrelation data.
    /// </summary>
    /// <param name="lpcs">LPC coefficients buffer (for all orders)</param>
    public void ComputeLpc(float* lpcs)
    {
        fixed (double* reff = Reflection)
        {
            Lpc.compute_lpc_coefs((uint)_autocorrOrder - 1, reff, lpcs);
        }
    }

    public double[] AutocorrValues { get; } = new double[Lpc.MaxLpcOrder + 1];
    public double[] PredictionError { get; } = new double[Lpc.MaxLpcOrder];
    public int[] BestOrders { get; } = new int[Lpc.MaxLpcOrder];
    public int[] Coefs { get; set; } = new int[Lpc.MaxLpcOrder];
    private int _autocorrOrder;
    public int Shift { get; set; }

    public double[] Reflection { get; } = new double[Lpc.MaxLpcOrder];

    public uint[] DoneLpcs { get; } = new uint[Lpc.MaxLpcPrecisions];
}