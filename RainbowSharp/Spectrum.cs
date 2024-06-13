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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowSharp
{
    internal class Spectrum : IEnumerable<SpectralPoint>
    {
        private List<SpectralPoint> spectrum = new List<SpectralPoint>();
        private Dictionary<String, String> stringParameters;
        private Dictionary<String, Double> doubleParameters;

        public Dictionary<string, double> DoubleParameters { get => doubleParameters; set => doubleParameters = value; }
        public Dictionary<string, string> StringParameters { get => stringParameters; set => stringParameters = value; }
        internal List<SpectralPoint> SpectralData { get => spectrum; set => spectrum = value; }

        public Spectrum(List<Double> energies, List<double> signals, Dictionary<String, String> stringParams, Dictionary<String, double> doubleParams) 
        {
            SpectralData = createSpectrum(energies, signals);
            StringParameters = stringParams;
            DoubleParameters = doubleParams;
        }
        public Spectrum(List<double> energies, List<double> signals)
        {
            SpectralData = createSpectrum(energies, signals);
        }

        private List<SpectralPoint> createSpectrum(List<double> energies, List<double> signals)
        {
            List<SpectralPoint> spectrum;

            if (energies.Count == signals.Count)
            {
                var zipped = energies.Zip(signals, (e, s) => new { energy = e, signal = s });
                spectrum = new List<SpectralPoint>();
                foreach (var point in zipped)
                {
                    spectrum.Add(new SpectralPoint(point.signal, point.energy));
                }
            }
            else
            {
                throw new Exception("List lengths do not match.");
            }
            return spectrum;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)SpectralData).GetEnumerator();
        }

        IEnumerator<SpectralPoint> IEnumerable<SpectralPoint>.GetEnumerator()
        {
            return ((IEnumerable<SpectralPoint>)SpectralData).GetEnumerator();
        }
    }

    internal struct SpectralPoint
    {
        private double signal;
        private double energy;

        public SpectralPoint(double signal, double energy)
        {
            this.Signal = signal;
            this.Energy = energy;
        }

        public double Signal { get => signal; set => signal = value; }
        public double Energy { get => energy; set => energy = value; }
    }
}
