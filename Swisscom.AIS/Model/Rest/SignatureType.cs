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
    public class SignatureType
    {
        public static readonly SignatureType Cms = new SignatureType("urn:ietf:rfc:3369", 30000);

        public static readonly SignatureType Timestamp = new SignatureType("urn:ietf:rfc:3161", 15000);

        /*
        * URI of the signature type.
        */
        public readonly string Uri;

        /*
        * The estimated final size of the signature in bytes.
        */
        public readonly int EstimatedSignatureSizeInBytes;

        public SignatureType(string uri, int estimatedSignatureSizeInBytes)
        {
            Uri = uri;
            EstimatedSignatureSizeInBytes = estimatedSignatureSizeInBytes;
        }
    }
}