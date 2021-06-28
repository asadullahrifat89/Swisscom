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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Swisscom.AIS.Rest.SignRequest
{
    public class OptionalInputs
    {
        public AddTimestamp AddTimestamp { get; set; }

        public List<string> AdditionalProfile { get; set; }

        public ClaimedIdentity ClaimedIdentity { get; set; }

        public string SignatureType { get; set; }

        [JsonProperty("sc.AddRevocationInformation")]
        public ScAddRevocationInformation ScAddRevocationInformation { get; set; }

        [JsonProperty("sc.SignatureStandard")]
        public string ScSignatureStandard { get; set; }

        [JsonProperty("sc.CertificateRequest")]
        public ScCertificateRequest ScCertificateRequest { get; set; }
    }
}
