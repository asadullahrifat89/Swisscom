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
namespace Swisscom.AIS.Rest
{
    public class DigestAlgorithm
    {

        public static readonly DigestAlgorithm SHA256 = new DigestAlgorithm("SHA-256", "http://www.w3.org/2001/04/xmlenc#sha256");

        public static readonly DigestAlgorithm SHA384 = new DigestAlgorithm("SHA-384", "http://www.w3.org/2001/04/xmldsig-more#sha384");

        public static readonly DigestAlgorithm SHA512 = new DigestAlgorithm("SHA-512", "http://www.w3.org/2001/04/xmlenc#sha512");

        /*
        * Name of the algorithm (to be used with security provider).
         */
        public readonly string Algorithm;

        /*
        * Uri of the algorithm (to be used in the AIS API).
        */
        public readonly string Uri;

        public DigestAlgorithm(string algorithm, string uri)
        {
            Algorithm = algorithm;
            Uri = uri;
        }
    }
}
