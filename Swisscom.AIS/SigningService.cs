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
using Swisscom.AIS;

namespace Swisscom.AIS
{
    public class SigningService
    {
        private AisClient client;

        public SigningService(AisClient aisClient)
        {
            client = aisClient;
        }

        public SignatureResult Sign(List<PdfHandle> documents, SignatureMode signatureMode, UserData userData)
        {
            switch (signatureMode.FriendlyName)
            {
                case SignatureMode.Static:
                    return client.SignWithStaticCertificate(documents, userData);
                case SignatureMode.OnDemand:
                    return client.SignWithOnDemandCertificate(documents, userData);
                case SignatureMode.OnDemandStepUp:
                    return client.SignWithOnDemandCertificateAndStepUp(documents, userData);
                case SignatureMode.Timestamp:
                    return client.Timestamp(documents, userData);
                default:
                    throw new ArgumentException($"Invalid signature mode. Can not sign the document(s) with the {signatureMode} signature. - {userData.TransactionId}");
            }
        }
    }
}
