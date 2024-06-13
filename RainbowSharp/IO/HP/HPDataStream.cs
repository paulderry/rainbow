/*
Copyright 2024 Paul Derry

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp.IO.HP
{
    internal class HPDataStream : Stream
    {
        Stream stream;
        BinaryReader reader;
        HPDataType datatype;


        public HPDataStream(byte[] data)
        {
            stream = new MemoryStream(data);
            reader = new BinaryReader(stream);
            datatype = new HPDataType();
        }

        public HPDataStream(String path)
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            reader = new BinaryReader(stream);
            datatype = this.GetFileType();
        }


        public HPDataType DataType { get { return datatype; } }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        /// <summary>
        /// Reads 8 bytes from the internal MemoryStream as a double.
        /// </summary>
        /// <returns>the next 8 bytes as a double.</returns>
        public Double ReadDouble() { return reader.ReadDouble(); }

        /// <summary>
        /// Reads the next 4 bytes from the internal MemoryStream as a 32-bit int.
        /// </summary>
        /// <returns>The next 4 bytes as a 32-bit int.</returns>
        public UInt32 ReadUInt32() { return reader.ReadUInt32(); }

        /// <summary>
        /// Reads the next 4 bytes from the internal MemoryStream as a 16-bit int.
        /// </summary>
        /// <returns>The next 2 bytes as a 16-bit int.</returns>
        public UInt16 ReadUInt16() { return reader.ReadUInt16(); }

        /// <summary>
        /// Reads a 16-bit int to get the length of a string composed of 16-bit characters.
        /// </summary>
        /// <returns>An ASCII string derived from an array of 16-bit characters.</returns>
        public String ReadWideString()
        {
            int wide_length = ReadUInt16() * 2;
            return UnicodeEncoding.Unicode.GetString(reader.ReadBytes(wide_length));
        }

        public void Seek(int distance) { reader.BaseStream.Seek(distance, SeekOrigin.Current); }

        public void Reset() { reader.BaseStream.Seek(0, SeekOrigin.Begin); }

        /// <summary>
        /// Reads a string with the length defined by the first byte. This means a string can be up to 255 characters and the string is in ASCII.
        /// </summary>
        /// <returns>An ASCII string as long as the value in the first byte.</returns>
        public string ReadString()
        {
            byte len = reader.ReadByte();
            return ASCIIEncoding.ASCII.GetString(reader.ReadBytes(len));
        }

        public String ReadStringParameter(String key)
        {
            SeekToWideString(key);
            Seek(6);
            return ReadWideString();
        }

        public Double ReadDoubleParameter(String key)
        {
            SeekToWideString(key);
            Seek(6);
            return ReadDouble();
        }

        public void SeekToASCII(String str)
        {
            byte[] target_bytes = ASCIIEncoding.ASCII.GetBytes(str);
            int target_len = target_bytes.Length;
            bool found = false;
            while (reader.BaseStream.Position + target_len < reader.BaseStream.Length && found == false)
            {
                byte[] seq_bytes = reader.ReadBytes(target_len);
                reader.BaseStream.Seek(-target_len, SeekOrigin.Current);
                if (!seq_bytes.SequenceEqual(target_bytes))
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                else
                {
                    found = true;
                    reader.BaseStream.Seek(target_len, SeekOrigin.Current);
                }
            }
        }

        public bool FindASCIIKey(String key)
        {
            byte[] target_bytes = ASCIIEncoding.ASCII.GetBytes(key);
            long current_pos = reader.BaseStream.Position;
            int target_len = target_bytes.Length;
            bool found = false;
            while (reader.BaseStream.Position + target_len < reader.BaseStream.Length && found == false)
            {
                byte[] seq_bytes = reader.ReadBytes(target_len);
                reader.BaseStream.Seek(-target_len, SeekOrigin.Current);
                if (!seq_bytes.SequenceEqual(target_bytes))
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                else
                {
                    found = true;
                    reader.BaseStream.Seek(target_len, SeekOrigin.Current);
                }
            }
            reader.BaseStream.Seek(current_pos, SeekOrigin.Begin);
            return found;
        }


        public void SeekToWideString(String target)
        {
            int wideLength = target.Length * 2;
            byte[] target_seq = Encoding.Unicode.GetBytes(target);

            bool found = false;
            while (reader.BaseStream.Position + wideLength < reader.BaseStream.Length && found == false)
            {
                byte[] seq_bytes = reader.ReadBytes(wideLength);
                reader.BaseStream.Seek(-wideLength, SeekOrigin.Current);
                if (!seq_bytes.SequenceEqual(target_seq))
                    reader.BaseStream.Seek(1, SeekOrigin.Current);
                else
                {
                    found = true;
                    reader.BaseStream.Seek(wideLength, SeekOrigin.Current);
                }
            }
        }

        private bool IsLegacyStandard()
        {
            bool isLegacyStandard = true;

            if (!ReadString().Equals("32"))
                isLegacyStandard = false;
            Seek(1);
            if (!ReadString().Equals("REGISTER FILE") && isLegacyStandard == true)
                isLegacyStandard = false;
            Seek(6);
            if (!ReadString().Equals("A.00.01") && isLegacyStandard == true)
                isLegacyStandard = false;
            Seek(6);
            Reset();

            return isLegacyStandard;
        }

        private bool IsModernStandard()
        {
            bool isModernStandard = true;
            if (!ReadString().Equals("32"))
                isModernStandard = false;
            Seek(1);
            if (!ReadString().Equals("REGISTER FILE"))
                isModernStandard = false;
            Seek(6);
            if (!ReadString().Equals("notused"))
                isModernStandard = false;
            Seek(6);
            Reset();

            return isModernStandard;
        }

        private bool IsLegacyKinetic()
        {
            if (IsLegacyStandard() == true && FindASCIIKey("CycleTime") == true)
            {
                Console.WriteLine(FindASCIIKey("CycleTime"));
                return true;
            }
            else
                return false;
        }

        private bool IsModernKinetic()
        {
            if (IsModernStandard() == true && FindASCIIKey("CycleTime") == true)
                return true;
            else
                return false;
        }

        private HPDataType GetFileType()
        {
            Reset();

            if (IsLegacyKinetic())
                return HPDataType.LEGACY_KINETIC;
            else if (IsLegacyStandard())
                return HPDataType.LEGACY_STANDARD;
            else if (IsModernStandard())
                return HPDataType.MODERN_STANDARD;
            else if (IsModernKinetic())
                return HPDataType.MODERN_KINETIC;
            else
                return HPDataType.UNKNOWN;
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }
    }

    enum HPDataType
    {
        LEGACY_STANDARD,
        LEGACY_KINETIC,
        MODERN_STANDARD,
        MODERN_KINETIC,
        UNKNOWN
    }
}
