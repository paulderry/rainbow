﻿/*
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
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp.IO.HP
{
        internal class LegacyStandardParser : I8453Parser
        {
            private UInt16 spectraCount;
            List<Spectrum> spectra;
            private HP8453Reader reader;
            internal List<Spectrum> SpectralData { get => spectra; set => spectra = value; }

            public LegacyStandardParser(HP8453Reader reader)
            {

                this.reader = reader;
                spectra = ParsedSpectra;
            }

            public List<Spectrum> ParsedSpectra
            {
                get
                {

                    if (!reader.ReadString().Equals("32"))
                        throw new Exception("Invalid Magic \"32\"");
                    reader.Seek(1);
                    if (!reader.ReadString().Equals("REGISTER FILE"))
                        throw new Exception("Invalid @ REGISTER FILE");
                    reader.Seek(6);
                    if (!reader.ReadString().Equals("A.00.01"))
                        throw new Exception("Invalid @ Version");
                    reader.Seek(6);

                    spectraCount = reader.ReadUInt16();
                    Console.WriteLine($"There are {spectraCount} spectra in this file.");

                    List<Spectrum> spectra = new List<Spectrum>();

                    for (int i = 0; i < spectraCount; i++)
                    {
                        Dictionary<String, double> doubleParameters = new Dictionary<String, double>();
                        Dictionary<String, String> stringParameters = new Dictionary<String, String>();
                        reader.SeekToASCII("Absorbance (AU)");
                        reader.Seek(1);
                        List<double> energy = new List<double>();
                        List<double> signal = new List<double>();

                        for (int j = 0; j < 910; j++)
                        {
                            energy.Add(j + 190);
                            signal.Add(reader.ReadDouble());
                        }
                        reader.SeekToASCII("SampleName");
                        reader.Seek(14);
                        String sampleName = reader.ReadStringParameter("SampleName");
                        stringParameters.Add("SampleName", sampleName);
                        spectra.Add(new Spectrum(energy, signal, stringParameters, new Dictionary<String, Double>()));
                    }
                    return spectra;
                }
            }

            public String CSV
            {
                get
                {
                    Spectrum s = spectra[0];
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Sample Name / Wavelength (nm),");
                    foreach (SpectralPoint sp in s.SpectralData)
                    {
                        sb.Append((sp.Energy + ","));
                    }
                    sb.AppendLine();
                    foreach (Spectrum spectrum in spectra)
                    {
                        string sampleName = spectrum.StringParameters.GetValueOrDefault("SampleName","No Name");
                        sb.Append($"{sampleName},");
                        foreach (SpectralPoint sp in spectrum)
                        {
                            sb.Append(sp.Signal + ",");
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                }

            }

            public bool IsValid(bool reset)
            {
                bool isLegacyStandard = true;

                if (!reader.ReadString().Equals("32"))
                    isLegacyStandard = false;
                reader.Seek(1);
                if (!reader.ReadString().Equals("REGISTER FILE") && isLegacyStandard == true)
                    isLegacyStandard = false;
                reader.Seek(6);
                if (!reader.ReadString().Equals("A.00.01") && isLegacyStandard == true)
                    isLegacyStandard = false;
                reader.Seek(6);
                if (reset == true)
                    reader.Reset();

                return isLegacyStandard;
            }
        }
}
