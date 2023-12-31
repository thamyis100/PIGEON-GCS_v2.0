﻿/*
The MIT License (MIT)

Copyright (c) 2013, David Suarez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.IO;
using System.Text;

namespace MavLinkNet
{
    public abstract class MavLinkPacketBase
    {
        public bool IsValid = false;

        public WireProtocolVersion WireProtocolVersion;
        public byte PayLoadLength;
        public byte PacketSequenceNumber;
        public byte SystemId;
        public byte ComponentId;
        public uint MessageId;
        public byte[] Payload;
        public byte Checksum1;
        public byte Checksum2;

        public UasMessage Message;
        
        public abstract int GetPacketSize();
        
        public abstract void Serialize(BinaryWriter w);

        public static BinaryReader GetBinaryReader(Stream s)
        {
            return new BinaryReader(s, Encoding.ASCII);
        }

        // __ CRC _____________________________________________________________


        // CRC code adapted from Mavlink C# generator (https://github.com/mavlink/mavlink)

        public const UInt16 X25CrcSeed = 0xffff;

        public static UInt16 X25CrcAccumulate(byte b, UInt16 crc)
        {
            unchecked
            {
                byte ch = (byte)(b ^ (byte)(crc & 0x00ff));
                ch = (byte)(ch ^ (ch << 4));
                return (UInt16)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
            }
        }        
    }
}