namespace SimpleChdDrive.Core.CHD.Utils;

internal class NodeT
{
    //internal node_t parent;       /* pointer to parent node */
    //internal uint count;          /* number of hits on this node */
    //internal uint weight;         /* assigned weight of this node */
    internal uint Bits;             /* bits used to encode the node */
    internal byte Numbits;          /* number of bits needed for this node */
}

internal class HuffmanDecoder
{
    /* internal state */
    private readonly uint _numcodes;                  /* number of total codes being processed */

    private readonly byte _maxbits;                   /* maximum bits per code */
    //uint prevdata;                /* value of the previous data (for delta-RLE encoding) */
    //int rleremaining;             /* number of RLE bytes remaining (for delta-RLE encoding) */
    private readonly ushort[] _lookup = null!;                /* pointer to the lookup table */

    private readonly NodeT[] _huffnode = null!;              /* array of nodes */

    private BitStream _bitbuf = null!;

    private static uint MAKE_LOOKUP(uint code, uint bits) { return (code << 5) | (bits & 0x1f); }


    /*-------------------------------------------------
    *  huffman_context_base - create an encoding/
    *  decoding context
    *-------------------------------------------------
    */
    public HuffmanDecoder(uint numcodes, byte maxbits, BitStream bitbuf, ushort[]? buffLookup = null)
    {
        /* limit to 24 bits */
        if (maxbits > 24)
            return;

        _numcodes = numcodes;
        _maxbits = maxbits;

        _lookup = buffLookup ?? new ushort[1 << maxbits];

        _huffnode = new NodeT[numcodes];
        //decoder.datahisto = null;
        //decoder.prevdata = 0;
        //decoder.rleremaining = 0;

        for (var i = 0; i < numcodes; i++)
        {
            _huffnode[i] = new NodeT();
        }

        _bitbuf = bitbuf;
    }

    public void AssignBitStream(BitStream bitbufReplace)
    {
        _bitbuf = bitbufReplace;
    }

    /*-------------------------------------------------
    *  decode_one - decode a single code from the
    *  huffman stream
    *-------------------------------------------------
    */
    public uint DecodeOne()
    {
        /* peek ahead to get maxbits worth of data */
        var bits = _bitbuf.Peek(_maxbits);

        /* look it up, then remove the actual number of bits for this code */
        uint lookup = _lookup[bits];
        _bitbuf.Remove((int)(lookup & 0x1f));

        /* return the value */
        return lookup >> 5;
    }

    /*-------------------------------------------------
    *  import_tree_rle - import an RLE-encoded
    *  huffman tree from a source data stream
    *-------------------------------------------------
    */
    public HuffmanError ImportTreeRle()
    {
        int curnode;

        var numbits = _maxbits switch
        {
            /* bits per entry depends on the maxbits */
            >= 16 => 5,
            >= 8 => 4,
            _ => 3
        };

        /* loop until we read all the nodes */
        for (curnode = 0; curnode < _numcodes;)
        {
            /* a non-one value is just raw */
            var nodebits = (int)_bitbuf.Read(numbits);
            if (nodebits != 1)
            {
                _huffnode[curnode++].Numbits = (byte)nodebits;
            }
            /* a one value is an escape code */
            else
            {
                /* a double 1 is just a single 1 */
                nodebits = (int)_bitbuf.Read(numbits);
                if (nodebits == 1)
                {
                    _huffnode[curnode++].Numbits = (byte)nodebits;
                }
                /* otherwise, we need one for value for the repeat count */
                else
                {
                    var repcount = (int)_bitbuf.Read(numbits) + 3;
                    if (repcount + curnode > _numcodes)
                        return HuffmanError.HufferrInvalidData;

                    while (repcount-- != 0)
                    {
                        _huffnode[curnode++].Numbits = (byte)nodebits;
                    }
                }
            }
        }

        /* make sure we ended up with the right number */
        if (curnode != _numcodes)
            return HuffmanError.HufferrInvalidData;

        /* assign canonical codes for all nodes based on their code lengths */
        var error = AssignCanonicalCodes();
        if (error != HuffmanError.HufferrNone)
            return error;

        /* build the lookup table */
        BuildLookupTable();

        /* determine final input length and report errors */
        return _bitbuf.Overflow() ? HuffmanError.HufferrInputBufferTooSmall : HuffmanError.HufferrNone;
    }


    /*-------------------------------------------------
    *  import_tree_huffman - import a huffman-encoded
    *  huffman tree from a source data stream
    *-------------------------------------------------
    */
    public HuffmanError ImportTreeHuffman()
    {
        var last = 0;
        var count = 0;
        int index;
        uint curcode;
        byte rlefullbits = 0;

        /* start by parsing the lengths for the small tree */
        var smallhuff = new HuffmanDecoder(24, 6, _bitbuf);
        smallhuff._huffnode[0].Numbits = (byte)_bitbuf.Read(3);
        var start = (int)_bitbuf.Read(3) + 1;
        for (index = 1; index < 24; index++)
        {
            if (index < start || count == 7)

            {
                smallhuff._huffnode[index].Numbits = 0;
            }
            else
            {
                count = (int)_bitbuf.Read(3);
                smallhuff._huffnode[index].Numbits = (byte)(count == 7 ? 0 : count);
            }
        }

        /* then regenerate the tree */
        var error = smallhuff.AssignCanonicalCodes();
        if (error != HuffmanError.HufferrNone)
            return error;

        smallhuff.BuildLookupTable();

        /* determine the maximum length of an RLE count */
        var temp = _numcodes - 9;
        while (temp != 0)
        {
            temp >>= 1;
            rlefullbits++;
        }
        /* now process the rest of the data */
        for (curcode = 0; curcode < _numcodes;)
        {
            var value = (int)smallhuff.DecodeOne();
            if (value != 0)
            {
                _huffnode[curcode++].Numbits = (byte)(last = value - 1);
            }
            else
            {
                count = (int)_bitbuf.Read(3) + 2;
                if (count == 7 + 2)
                {
                    count += (int)_bitbuf.Read(rlefullbits);
                }

                for (; count != 0 && curcode < _numcodes; count--)
                {
                    _huffnode[curcode++].Numbits = (byte)last;
                }
            }
        }

        /* make sure we ended up with the right number */
        if (curcode != _numcodes)
            return HuffmanError.HufferrInvalidData;

        /* assign canonical codes for all nodes based on their code lengths */
        error = AssignCanonicalCodes();
        if (error != HuffmanError.HufferrNone)
            return error;

        /* build the lookup table */
        BuildLookupTable();

        /* determine final input length and report errors */
        return _bitbuf.Overflow() ? HuffmanError.HufferrInputBufferTooSmall : HuffmanError.HufferrNone;
    }


    /*-------------------------------------------------
    *  assign_canonical_codes - assign canonical codes
    *  to all the nodes based on the number of bits
    *  in each
    *-------------------------------------------------
    */
    private HuffmanError AssignCanonicalCodes()
    {
        uint curcode;
        int codelen;
        uint curstart = 0;
        /* build up a histogram of bit lengths */
        var bithisto = new uint[33];
        for (curcode = 0; curcode < _numcodes; curcode++)
        {
            var node = _huffnode[curcode];
            if (node.Numbits > _maxbits)
                return HuffmanError.HufferrInternalInconsistency;

            if (node.Numbits <= 32)
            {
                bithisto[node.Numbits]++;
            }
        }

        /* for each code length, determine the starting code number */
        for (codelen = 32; codelen > 0; codelen--)
        {
            var nextstart = (curstart + bithisto[codelen]) >> 1;
            if (codelen != 1 && nextstart * 2 != curstart + bithisto[codelen])
                return HuffmanError.HufferrInternalInconsistency;

            bithisto[codelen] = curstart;
            curstart = nextstart;
        }


        /* now assign canonical codes */
        for (curcode = 0; curcode < _numcodes; curcode++)
        {
            var node = _huffnode[curcode];
            if (node.Numbits > 0)
            {
                node.Bits = bithisto[node.Numbits]++;
            }
        }
        return HuffmanError.HufferrNone;
    }

    /*-------------------------------------------------
    *  build_lookup_table - build a lookup table for
    *  fast decoding
    *-------------------------------------------------
    */
    private void BuildLookupTable()
    {
        uint curcode;
        /* iterate over all codes */
        for (curcode = 0; curcode < _numcodes; curcode++)
        {
            /* process all nodes which have non-zero bits */
            var node = _huffnode[curcode];
            if (node.Numbits > 0)
            {
                /* set up the entry */
                var value = MAKE_LOOKUP(curcode, node.Numbits);
                /* fill all matching entries */
                var shift = _maxbits - node.Numbits;
                var dest = node.Bits << shift;
                var destend = ((node.Bits + 1) << shift) - 1;
                while (dest <= destend)
                {
                    _lookup[dest++] = (ushort)value;
                }
            }
        }
    }
}
