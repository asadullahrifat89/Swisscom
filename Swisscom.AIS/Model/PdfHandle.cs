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
using Swisscom.AIS.Rest;
using Swisscom.AIS.Utils;

namespace Swisscom.AIS
{
    public class PdfHandle
    {
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public DigestAlgorithm DigestAlgorithm { get; set; }

        public PdfHandle()
        {
            DigestAlgorithm = DigestAlgorithm.SHA512;
        }

        public void ValidateYourself(Trace trace)
        {
            Validator.AssertValueNotEmpty(InputFileName, "The inputFromFile cannot be null or empty", trace);
            Validator.AssertValueNotEmpty(OutputFileName, "The outputToFile cannot be null or empty", trace);
            Validator.AssertValueNotNull(DigestAlgorithm, "The digest algorithm for a PDF handle cannot be NULL", trace);
        }
    }
}
