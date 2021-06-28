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
using System.Collections.Generic;
using System.Linq;
using Swisscom.AIS.Rest.PendingRequest;
using Swisscom.AIS.Rest.SignRequest;
using Swisscom.AIS.Utils;
using ClaimedIdentity = Swisscom.AIS.Rest.SignRequest.ClaimedIdentity;
using OptionalInputs = Swisscom.AIS.Rest.SignRequest.OptionalInputs;

namespace Swisscom.AIS.Rest
{
    public class RequestBuilder
    {
        private static readonly string SwisscomBasicProfile = "http://ais.swisscom.ch/1.1";

        public AISSignRequest BuildAisSignRequest(SignRequestDetails requestDetails)
        {
            // Input documents --------------------------------------------------------------------------------
            List<DocumentHash> documentHashes = requestDetails.Documents.Select(d => new DocumentHash
            {
                Id = d.Id, 
                DsigDigestMethod = new DsigDigestMethod {Algorithm = d.DigestAlgorithm.Uri},
                DsigDigestValue = d.Base64HashToSign
            }).ToList();
            InputDocuments inputDocuments = new InputDocuments{DocumentHash = documentHashes};

            // Optional inputs --------------------------------------------------------------------------------
            OptionalInputs optionalInputs = new OptionalInputs
            {
                SignatureType = requestDetails.SignatureType.Uri,
                AdditionalProfile = requestDetails.AdditionalProfiles.Select(ap => ap.Uri).ToList()
            };

            if (requestDetails.SignatureMode.FriendlyName != SignatureMode.Timestamp &&
                requestDetails.UserData.SignatureStandard.Value != SignatureStandard.Default)
            {
                optionalInputs.ScSignatureStandard = requestDetails.UserData.SignatureStandard.Value;
            }

            optionalInputs.AddTimestamp = requestDetails.UserData.IsAddTimestamp()
                ? new AddTimestamp {Type = SignatureType.Timestamp.Uri}
                : null;
            
            ClaimedIdentity claimedIdentity = new ClaimedIdentity { Name = requestDetails.UserData.ClaimedIdentityName };
            if (requestDetails.SignatureMode.FriendlyName != SignatureMode.Timestamp && !requestDetails.UserData.ClaimedIdentityKey.IsEmpty())
            {
                claimedIdentity.Name += ":" + requestDetails.UserData.ClaimedIdentityKey;
            }
            optionalInputs.ClaimedIdentity = claimedIdentity;

            ScCertificateRequest certificateRequest = null;
            if (requestDetails.WithCertificateRequest)
            {
                certificateRequest = new ScCertificateRequest {ScDistinguishedName = requestDetails.UserData.DistinguishedName};
            }

            if (requestDetails.WithStepUp)
            {
                if (certificateRequest == null)
                {
                    certificateRequest = new ScCertificateRequest();
                }
                certificateRequest.ScStepUpAuthorisation = new ScStepUpAuthorisation{ScPhone = 
                    new ScPhone
                    {
                        ScLanguage = requestDetails.UserData.StepUpLanguage,
                        ScMSISDN = requestDetails.UserData.StepUpMsisdn,
                        ScMessage = requestDetails.UserData.StepUpMessage,
                        ScSerialNumber = requestDetails.UserData.StepUpSerialNumber
                    }};
            }

            optionalInputs.ScCertificateRequest = certificateRequest;

            ScAddRevocationInformation addRevocationInformation = new ScAddRevocationInformation();
            if (requestDetails.UserData.RevocationInformation.Value == RevocationInformation.Default)
            {
                if (requestDetails.SignatureMode.FriendlyName == SignatureMode.Timestamp)
                {
                    addRevocationInformation.Type = RevocationInformation.Both;
                }
                else
                {
                    addRevocationInformation.Type = null;
                }
            }
            else
            {
                addRevocationInformation.Type = requestDetails.UserData.RevocationInformation.Value;
            }

            optionalInputs.ScAddRevocationInformation = addRevocationInformation;

            return new AISSignRequest{ SignRequest = new SignRequest.SignRequest
            {
                InputDocuments = inputDocuments,
                OptionalInputs = optionalInputs,
                RequestId = GenerateRequestId(),
                Profile = SwisscomBasicProfile
            }
            };
        }

        public static AISPendingRequest BuildAisPendingRequest(string asyncResponseId, UserData userData)
        {
            return new AISPendingRequest
            {
                AsyncPendingRequest = new AsyncPendingRequest
                {
                    OptionalInputs = new PendingRequest.OptionalInputs
                    {
                        ClaimedIdentity = new PendingRequest.ClaimedIdentity
                        {
                            Name = userData.ClaimedIdentityName
                        },
                        AsyncResponseId = asyncResponseId
                    },
                    Profile = SwisscomBasicProfile
                }
            };
        }

        private static string GenerateRequestId()
        {
            return "ID-" + Guid.NewGuid();
        }

    }
}
