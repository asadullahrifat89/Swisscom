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
    public class AdditionalProfile
    {
        /*
        * The signature may be processed in asynchronous mode. The response may be a Pending result that
        * contains the ResponseID. The client must be able to poll the result with a PendingRequest.
        */
        public static readonly AdditionalProfile Async = new AdditionalProfile("urn:oasis:names:tc:dss:1.0:profiles:asynchronousprocessing");

        /*
        * Required if a single signature request contains multiple document digests.
        * Since you cannot rely on the order of the <DocumentHash> elements in the response, it’s mandatory
        * for a request to define a unique ID attribute for each <DocumentHash>.
        */
        public static readonly AdditionalProfile Batch = new AdditionalProfile("http://ais.swisscom.ch/1.0/profiles/batchprocessing");

        /*
        * Used to indicate the use of an On Demand certificate instead of a Static certificate. The
        * signature request shall contain a subject Distinguished Name and optionally a Step-Up Authentication.
        */
        public static readonly AdditionalProfile OnDemandCertificate = new AdditionalProfile("http://ais.swisscom.ch/1.0/profiles/ondemandcertificate");

        /*
        * Required for step-up authentication. AIS receives an asynchronous call request. After receiving
        * a “pending” response including a Consent URL, the client needs to redirect the user to this
        * URL and to start polling the result with a PendingRequest.
        */
        public static readonly AdditionalProfile Redirect = new AdditionalProfile("http://ais.swisscom.ch/1.1/profiles/redirect");

        /*
        * The signature response will only contain a timestamp of the document hash.
        */
        public static readonly AdditionalProfile Timestamp = new AdditionalProfile("urn:oasis:names:tc:dss:1.0:profiles:timestamping");

        /*
        * Used to indicate the use of a static plain PKCS#1 signature.
        */
        public static readonly AdditionalProfile PlainSignature = new AdditionalProfile("http://ais.swisscom.ch/1.1/profiles/plainsignature");


        public readonly string Uri;

        public AdditionalProfile(string uri)
        {
            Uri = uri;
        }
    }
}
