namespace SimpleChdDrive.Core.CHD.Utils;

public static class BigEndian
{  // Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.

    extension(BinaryReader binRdr)
    {
        public ushort ReadUInt16Be()
        {
            return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(ushort)).Reverse(), 0);
        }

        public short ReadInt16Be()
        {
            return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(short)).Reverse(), 0);
        }

        public uint ReadUInt32Be()
        {
            return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(uint)).Reverse(), 0);
        }

        public ulong ReadUInt48Be()
        {
            return ((ulong)binRdr.ReadByte() << 40) | ((ulong)binRdr.ReadByte() << 32) | ((ulong)binRdr.ReadByte() << 24) | ((ulong)binRdr.ReadByte() << 16) | ((ulong)binRdr.ReadByte() << 8) | binRdr.ReadByte();
        }

        public ulong ReadUInt64Be()
        {
            return BitConverter.ToUInt64(binRdr.ReadBytesRequired(sizeof(ulong)).Reverse(), 0);
        }

        public int ReadInt32Be()
        {
            return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(int)).Reverse(), 0);
        }

        public byte[] ReadBytesRequired(int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException($"{byteCount} bytes required from stream, but only {result.Length} returned.");

            return result;
        }
    }


    extension(byte[] arr)
    {
        public ushort ReadUInt16Be(int offset)
        {
            return (ushort)((arr[offset + 0] << 8) | arr[offset + 1]);
        }

        public uint ReadUInt24Be(int offset)
        {
            return ((uint)arr[offset + 0] << 16) | ((uint)arr[offset + 1] << 8) | arr[offset + 2];
        }

        public uint ReadUInt32Be(int offset)
        {
            return ((uint)arr[offset + 0] << 24) | ((uint)arr[offset + 1] << 16) | ((uint)arr[offset + 2] << 8) | arr[offset + 3];
        }

        public ulong ReadUInt48Be(int offset)
        {
            return ((ulong)arr[offset + 0] << 40) | ((ulong)arr[offset + 1] << 32) |
                   ((ulong)arr[offset + 2] << 24) | ((ulong)arr[offset + 3] << 16) | ((ulong)arr[offset + 4] << 8) | arr[offset + 5];
        }

        public void PutUInt16Be(int offset, uint value)
        {
            arr[offset++] = (byte)((value >> 8) & 0xFF);
            arr[offset] = (byte)(value & 0xFF);
        }

        public void PutUInt24Be(int offset, uint value)
        {
            arr[offset++] = (byte)((value >> 16) & 0xFF);
            arr[offset++] = (byte)((value >> 8) & 0xFF);
            arr[offset] = (byte)(value & 0xFF);
        }

        public void PutUInt48Be(int offset, ulong value)
        {
            arr[offset++] = (byte)((value >> 40) & 0xFF);
            arr[offset++] = (byte)((value >> 32) & 0xFF);
            arr[offset++] = (byte)((value >> 24) & 0xFF);
            arr[offset++] = (byte)((value >> 16) & 0xFF);
            arr[offset++] = (byte)((value >> 8) & 0xFF);
            arr[offset] = (byte)(value & 0xFF);
        }

        public byte[] Reverse()
        {
            Array.Reverse((Array)arr);
            return arr;
        }
    }
}
