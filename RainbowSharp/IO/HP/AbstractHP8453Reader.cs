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
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp.IO.HP
{
    internal abstract class AbstractHP8453Reader
    {

        protected MemoryStream _spectrumData;
        private BinaryReader _binaryReader;
        
        public AbstractHP8453Reader(String path)
        {
            _spectrumData = new MemoryStream(File.ReadAllBytes(path));
            _binaryReader = new BinaryReader(_spectrumData);
        }

        protected abstract List<Spectrum> Parse();
        public abstract String ToCSV();



        protected Double ReadDouble()
        {
            return _binaryReader.ReadDouble();
        }

        protected UInt32 ReadUInt32()
        {
            return _binaryReader.ReadUInt32();
        }

        protected UInt16 ReadUInt16() {

            return _binaryReader.ReadUInt16();
        }

        protected String ReadWideString()
        {
            int wide_length = ReadUInt16() * 2;
            return UnicodeEncoding.Unicode.GetString(_binaryReader.ReadBytes(wide_length));
        }

        protected void Seek(int distance)
        {
            _binaryReader.BaseStream.Seek(distance, SeekOrigin.Current);
        }

        protected abstract bool ValidatePreamble();

        protected string ReadString()
        {
            byte len = _binaryReader.ReadByte();
            return ASCIIEncoding.ASCII.GetString(_binaryReader.ReadBytes(len));
        }

        protected String ReadStringParameter(String key)
        {
            SeekToWideString(key);
            Seek(6);
            return ReadWideString();
        }

        protected Double ReadDoubleParameter(String key)
        {
            SeekToWideString(key);
            Seek(6);
            return ReadDouble();
        }

        protected void SeekToASCII(String str)
        {
            byte[] target_bytes = ASCIIEncoding.ASCII.GetBytes(str);
            int target_len = target_bytes.Length;
            bool found = false;
            while (_binaryReader.BaseStream.Position + target_len < _binaryReader.BaseStream.Length && found == false)
            {
                byte[] seq_bytes = _binaryReader.ReadBytes(target_len);
                _binaryReader.BaseStream.Seek(-target_len, SeekOrigin.Current);
                if (!seq_bytes.SequenceEqual(target_bytes))
                    _binaryReader.BaseStream.Seek(1, SeekOrigin.Current);
                else
                {
                    found = true;
                    _binaryReader.BaseStream.Seek(target_len, SeekOrigin.Current);
                }
            }
        }
        protected void SeekToWideString(String target)
        {
            int wideLength = target.Length * 2;
            byte[] target_seq = Encoding.Unicode.GetBytes(target);

            bool found = false;
            while (_binaryReader.BaseStream.Position + wideLength < _binaryReader.BaseStream.Length && found == false)
            {
                byte[] seq_bytes = _binaryReader.ReadBytes(wideLength);
                _binaryReader.BaseStream.Seek(-wideLength, SeekOrigin.Current);
                if (!seq_bytes.SequenceEqual(target_seq))
                    _binaryReader.BaseStream.Seek(1, SeekOrigin.Current);
                else
                {
                    found = true;
                    _binaryReader.BaseStream.Seek(wideLength, SeekOrigin.Current);
                }
            }
        }

    }
}

namespace RainbowSharp
{
    enum HPDataType
    {
        LEGACY_STANDARD,
        LEGACY_KINETIC,
        MODERN_STANDARD,
        MODERN_KINETIC,
        UNKNOWN
    }
}