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
    public static class Validator
    {
        public static void AssertValueNotNull(Object value, String errorMessage, Trace trace)
        {
            if (value == null)
            {
                if (trace == null)
                {
                    throw new ArgumentNullException(errorMessage);
                }

                throw new ArgumentNullException(errorMessage + " - " + trace.Id);

            }
        }

        public static void AssertIntValueBetween(int value, int min, int max, string errorMessage, Trace trace)
        {
            if (value < min || value > max)
            {
                if (trace == null)
                {
                    throw new ArgumentOutOfRangeException(errorMessage);
                }

                throw new ArgumentOutOfRangeException(errorMessage + " - " + trace.Id);

            }
        }

        public static void AssertValueNotEmpty(string value, string errorMessage, Trace trace)
        {
            if (value == null || value.Trim().Length == 0)
            {
                if (trace == null)
                {
                    throw new ArgumentException(errorMessage);
                }
                else
                {
                    throw new ArgumentException(errorMessage + " - " + trace.Id);
                }
            }
        }
    }
}
