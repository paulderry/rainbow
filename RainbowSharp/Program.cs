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

using RainbowSharp.IO.HP;
using RainbowSharp.IO.CSV;

foreach (string path in args)
{
        HP8453Reader reader = new HP8453Reader(path);
        HPDataType dtype = reader.GetFileType();

    switch (dtype)
    {
        case HPDataType.LEGACY_KINETIC:
            Console.WriteLine("This file is most likely an 8453 Version A Kinetic Data File.");
            File.WriteAllText($"{path}.csv", new LegacyKineticsParser(reader).CSV);
            break;
        case HPDataType.LEGACY_STANDARD:
            Console.WriteLine("This file is most likely an 8453 Version A Standard Data File.");
            File.WriteAllText($"{path}.csv", new LegacyStandardParser(reader).CSV);
            break;
        case HPDataType.MODERN_STANDARD:
            Console.WriteLine("This file is most likely an 8453 Version B Standard Data File.");
            File.WriteAllText($"{path}.csv", new ModernStandardParser(reader).CSV);
            break;
        case HPDataType.MODERN_KINETIC:
            Console.WriteLine("This file is most likely an 8453 Version B Kinetic Data File.");
            File.WriteAllText($"{path}.csv", new ModernKineticsParser(reader).CSV);
            break;
    }
}

Console.ReadKey();