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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp.IO.HP
{
    internal class ModernStandardParser : I8453Parser
    {
        
        List<Spectrum> spectra = new List<Spectrum>();
        private HP8453Reader reader;
        internal List<Spectrum> SpectralData { get => spectra; set => spectra = value; }

        public ModernStandardParser(HP8453Reader reader) {
            this.reader = reader;
            spectra = ParsedSpectra;
        }

        public List<Spectrum> ParsedSpectra
        {
            get
            {
                int spectraCount;

                List<Spectrum> spectra = new List<Spectrum>();

                if (!reader.ReadString().Equals("32"))
                    throw new Exception("Magic \"32\" is not valid.");
                reader.Seek(1);
                if (!reader.ReadString().Equals("REGISTER FILE"))
                    throw new Exception("REGISTER FILE is not valid.");
                reader.Seek(6);
                if (!reader.ReadString().Equals("notused"))
                    throw new Exception("notused is not valid.");
                reader.Seek(6);

                spectraCount = reader.ReadUInt16();

                for (int s = 0; s < spectraCount; s++)
                {
                    Dictionary<String, String> stringParameters = new Dictionary<string, string>();
                    Dictionary<String, Double> doubleParameters = new Dictionary<string, double>();
                    List<double> energies = new List<double>();
                    List<double> signals = new List<double>();

                    reader.SeekToASCII("CHPUVObject");
                    stringParameters.Add("SampleName", reader.ReadStringParameter("SampleName"));
                    doubleParameters.Add("DataType", reader.ReadDoubleParameter("DataType"));
                    doubleParameters.Add("DerivOrder", reader.ReadDoubleParameter("DerivOrder"));
                    stringParameters.Add("InstrId", reader.ReadStringParameter("InstrId"));
                    doubleParameters.Add("InstrNbr", reader.ReadDoubleParameter("InstrNbr"));
                    doubleParameters.Add("IntegrTime", reader.ReadDoubleParameter("IntegrTime"));
                    doubleParameters.Add("OptBandwidth", reader.ReadDoubleParameter("OptBandwidth"));
                    stringParameters.Add("TimeOfDay", reader.ReadStringParameter("TimeOfDay"));
                    stringParameters.Add("Date", reader.ReadStringParameter("Date"));
                    doubleParameters.Add("TimeSince1970", reader.ReadDoubleParameter("TimeSince1970"));
                    doubleParameters.Add("RelTime", reader.ReadDoubleParameter("RelTime"));
                    stringParameters.Add("SolventName", reader.ReadStringParameter("SolventName"));
                    doubleParameters.Add("DilutionFactor", reader.ReadDoubleParameter("DilutionFactor"));
                    doubleParameters.Add("Pathlength", reader.ReadDoubleParameter("Pathlength"));
                    stringParameters.Add("PathlengthUnit", reader.ReadStringParameter("PathlengthUnit"));
                    stringParameters.Add("Operator", reader.ReadStringParameter("Operator"));
                    stringParameters.Add("FileName", reader.ReadStringParameter("FileName"));

                    reader.SeekToWideString("Absorbance (AU)");
                    reader.Seek(4);
                    uint wavelengths = reader.ReadUInt16();
                    reader.Seek(3);
                    for (int i = 0; i < wavelengths; i++)
                    {
                        energies.Add(i + 190);
                        signals.Add(reader.ReadDouble());
                    }
                    spectra.Add(new Spectrum(energies, signals, stringParameters, doubleParameters));
                }

                return spectra;
            }
        }

        public string CSV
        {
            get
            {
                Spectrum s = spectra[0]; //We need to make the first row with this.
                StringBuilder sb = new StringBuilder();
                sb.Append("Sample Name / Wavelength (nm),");
                foreach (SpectralPoint sp in s.SpectralData)
                {
                    sb.Append(sp.Energy + ",");
                }
                sb.AppendLine(); // Terminate the line.
                foreach (Spectrum spectrum in spectra)
                {

                    string sampleName = spectrum.StringParameters.GetValueOrDefault("SampleName");
                    sb.Append(sampleName + ",");
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
            throw new NotImplementedException();
        }
    }
}
