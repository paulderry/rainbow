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

namespace RainbowSharp.IO.CSV
{
    internal class CSVWriter
    {

        public List<Spectrum> spectra;

        public CSVWriter(List<Spectrum> spectralData, String path) {
            spectra = spectralData;
        }

        public string StandardToCSV()
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

        public string KineticToCSV()
        {
            Spectrum s = spectra[0];
            StringBuilder sb = new StringBuilder();
            sb.Append("Time (s) / Wavelength (nm),");
            foreach (SpectralPoint sp in s.SpectralData)
            {
                sb.Append((sp.Energy + ","));
            }
            sb.AppendLine();
            foreach (Spectrum spectrum in spectra)
            {
                double time = spectrum.DoubleParameters.GetValueOrDefault("RelTime");
                sb.Append($"{time},");
                foreach (SpectralPoint sp in spectrum)
                {
                    sb.Append(sp.Signal + ",");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
