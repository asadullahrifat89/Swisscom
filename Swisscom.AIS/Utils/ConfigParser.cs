/*
 * Copyright 2021 Swisscom Trust Services (Schweiz) AG
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

namespace Swisscom.AIS.Utils
{
    public static class ConfigParser
    {
        public static string GetStringNotNull(string propertyName, string nonNullableString)
        {
            if (string.IsNullOrEmpty(nonNullableString))
            {
                throw new ArgumentNullException($"Invalid configuration. {propertyName} is missing or empty");
            }

            return nonNullableString;
        }

        public static int GetIntNotNull(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException($"Invalid configuration. {propertyName} is missing or empty");
            }

            return int.Parse(value);
        }
    }
}
