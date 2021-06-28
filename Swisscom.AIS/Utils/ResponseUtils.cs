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
using Swisscom.AISCommon;
using Swisscom.AIS.Rest;
using Swisscom.AIS.Rest.SignResponse;

namespace Swisscom.AIS.Utils
{
    public class ResponseUtils
    {
        public static List<string> ExtractScCRLs(AISSignResponse response)
        {
            return response?.SignResponse?.OptionalOutputs?.ScRevocationInformation?.ScCRLs?.ScCRL;
        }

        public static List<string> ExtractScOCSPs(AISSignResponse response)
        {
            return response?.SignResponse?.OptionalOutputs?.ScRevocationInformation?.ScOCSPs?.ScOCSP;
        }

        public static bool IsResponseAsyncPending(AISSignResponse response)
        {
            return response?.SignResponse?.Result?.ResultMajor == ResultMajorCode.Pending.Uri;
        }

        public static bool IsResponseMajorSuccess(AISSignResponse response)
        {
            return response?.SignResponse?.Result?.ResultMajor == ResultMajorCode.Success.Uri;
        }

        public static string GetResponseResultSummary(AISSignResponse response)
        {
            Result result = response.SignResponse.Result;
            return "Major=[" + result.ResultMajor + "], "
                   + "Minor=[" + result.ResultMinor + "], "
                   + "Message=[" + result.ResultMessage.S + ']';
        }

        public static bool ResponseHasStepUpConsentUrl(AISSignResponse response)
        {
            return response?.SignResponse?.OptionalOutputs?.ScStepUpAuthorisationInfo?.ScResult.ScConsentURL != null;
        }

        public static string GetStepUpConsentUrl(AISSignResponse response)
        {
            return response.SignResponse.OptionalOutputs.ScStepUpAuthorisationInfo.ScResult.ScConsentURL;
        }

        public static string GetAsyncResponseId(AISSignResponse response)
        {
            return response.SignResponse.OptionalOutputs.AsyncResponseID;
        }

        public static ScExtendedSignatureObject GetSignatureObjectByDocumentId(string documentId, AISSignResponse signResponse)
        {
            ScSignatureObjects signatureObjects = signResponse.SignResponse.SignatureObject.Other.ScSignatureObjects;
            foreach (var seSignatureObject in signatureObjects.ScExtendedSignatureObject)
            {
                if (documentId == seSignatureObject.WhichDocument)
                {
                    return seSignatureObject;
                }
            }
            throw new AisClientException($"Invalid AIS response. Cannot find the extended signature object for document with ID=[{documentId}]");
        }
    }
}
