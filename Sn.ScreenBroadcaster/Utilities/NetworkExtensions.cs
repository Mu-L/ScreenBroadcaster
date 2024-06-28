﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Sn.ScreenBroadcaster.Utilities
{
    public static class NetworkExtensions
    {
        public static async Task ReadBlockAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var received = 0;

            while (received < count)
            {
                received += await stream.ReadAsync(buffer, offset + received, count - received);
            }
        }

        public static void ReadBlock(this Stream stream, Span<byte> buffer)
        {
#if NET6_0_OR_GREATER
            var received = 0;

            while (received < buffer.Length)
            {
                var currentReceived = stream.Read(buffer.Slice(received, buffer.Length - received));
                if (currentReceived == 0)
                    throw new EndOfStreamException();

                received += currentReceived;
            }
#else
            throw new NotSupportedException();
#endif
        }

        public static unsafe TStruct ReadValue<TStruct>(this Stream stream)
            where TStruct : unmanaged
        {
#if NET6_0_OR_GREATER
            TStruct result;
            stream.ReadBlock(new Span<byte>(&result, sizeof(TStruct)));
            return result;
#else
            using var binaryReader = new BinaryReader(stream, Encoding.Default, true);
            var structBytes = binaryReader.ReadBytes(sizeof(TStruct));

            fixed (byte* structBytesPtr = structBytes)
            {
                TStruct resultValue = ((TStruct*)(void*)structBytesPtr)[0];
                return resultValue;
            }
#endif
        }

        public static unsafe void WriteValue<TStruct>(this Stream stream, in TStruct value)
            where TStruct : unmanaged
        {
            fixed (TStruct* valuePtr = &value)
            {
#if NET6_0_OR_GREATER
                stream.Write(new Span<byte>(valuePtr, sizeof(TStruct)));
#else
                byte* ptr = (byte*)(void*)valuePtr;
                byte[] buffer = new byte[sizeof(TStruct)];

                Marshal.Copy((nint)ptr, buffer, 0, buffer.Length);
                stream.Write(buffer, 0, buffer.Length);
#endif
            }
        }
    }
}
