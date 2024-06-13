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
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp.IO.HP
{
    internal class HP8453Reader
    {
        private string path;
        protected MemoryStream _spectrumData;
        private BinaryReader _binaryReader;

        public HP8453Reader() { }

        public HP8453Reader(String path)
        {
            this.path = path;
            _spectrumData = new MemoryStream(File.ReadAllBytes(path));
            _binaryReader = new BinaryReader(_spectrumData);

            Console.WriteLine(GetFileType());
            _binaryReader.BaseStream.Position = 0;
        }

        public HPDataType GetFileType()
        {
            bool IsLegacyStandard() {
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
                _spectrumData.Seek(0, SeekOrigin.Begin);

                return isLegacyStandard;
            }

            bool IsModernStandard()
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
                _spectrumData.Seek(0, SeekOrigin.Begin);
                
                return isModernStandard;
            }
            bool isLegacyKinetic() 
            {
                if (IsLegacyStandard() == true && FindASCIIKey("CycleTime") == true)
                {
                    Console.WriteLine(FindASCIIKey("CycleTime"));
                    _spectrumData.Seek(0, SeekOrigin.Begin);
                    return true;
                }
                else
                    _spectrumData.Seek(0, SeekOrigin.Begin);
                    return false;
            }

            bool isModernKinetic() 
            { 
                if (IsModernStandard() == true && FindWideKey("CycleTime") == true)
                {
                     return true;
                } else
                {
                    _spectrumData.Seek(0, SeekOrigin.Begin);
                    return false;
                }
            } 
                    

            _spectrumData.Seek(0, SeekOrigin.Begin);

            if (isLegacyKinetic())
                return HPDataType.LEGACY_KINETIC;
            else if (IsLegacyStandard())
                return HPDataType.LEGACY_STANDARD;
            else if (isModernKinetic())
                return HPDataType.MODERN_KINETIC;
            else if (IsModernStandard())
                return HPDataType.MODERN_STANDARD;
            else
                return HPDataType.UNKNOWN;
        }   

        /// <summary>
        /// Reads 8 bytes from the internal MemoryStream as a double.
        /// </summary>
        /// <returns>the next 8 bytes as a double.</returns>
        public Double ReadDouble() { return _binaryReader.ReadDouble(); }

        /// <summary>
        /// Reads the next 4 bytes from the internal MemoryStream as a 32-bit int.
        /// </summary>
        /// <returns>The next 4 bytes as a 32-bit int.</returns>
        public UInt32 ReadUInt32() { return _binaryReader.ReadUInt32(); }

        /// <summary>
        /// Reads the next 4 bytes from the internal MemoryStream as a 16-bit int.
        /// </summary>
        /// <returns>The next 2 bytes as a 16-bit int.</returns>
        public UInt16 ReadUInt16() { return _binaryReader.ReadUInt16(); }

        /// <summary>
        /// Reads a 16-bit int to get the length of a string composed of 16-bit characters.
        /// </summary>
        /// <returns>An ASCII string derived from an array of 16-bit characters.</returns>
        public String ReadWideString()
        {
            int wide_length = ReadUInt16() * 2;
            return UnicodeEncoding.Unicode.GetString(_binaryReader.ReadBytes(wide_length));
        }

        public void Seek(int distance) { _binaryReader.BaseStream.Seek(distance, SeekOrigin.Current); }

        public void Reset() { _binaryReader.BaseStream.Seek(0, SeekOrigin.Begin); }

        /// <summary>
        /// Reads a string with the length defined by the first byte. This means a string can be up to 255 characters and the string is in ASCII.
        /// </summary>
        /// <returns>An ASCII string as long as the value in the first byte.</returns>
        public string ReadString()
        {
            byte len = _binaryReader.ReadByte();
            return ASCIIEncoding.ASCII.GetString(_binaryReader.ReadBytes(len));
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
        
        public bool FindASCIIKey(String key)
        {
            byte[] target_bytes = ASCIIEncoding.ASCII.GetBytes(key);
            long current_pos = _binaryReader.BaseStream.Position;
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
            _binaryReader.BaseStream.Seek(current_pos, SeekOrigin.Begin);
            return found;
        }


        public void SeekToWideString(String target)
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

        public bool FindWideKey(String keystring)
        {
            int wideLength = keystring.Length * 2;
            byte[] target_seq = Encoding.Unicode.GetBytes(keystring);

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
            return found;
        }
    }
}

namespace RainbowSharp.IO
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

