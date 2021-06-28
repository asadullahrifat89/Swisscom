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
using Swisscom.AIS.Rest.PendingRequest;
using Swisscom.AIS.Rest.SignRequest;
using Swisscom.AIS.Rest.SignResponse;
using Swisscom.AIS.Utils;

namespace Swisscom.AIS.Rest
{
    public interface IRestClient
    {
        AISSignResponse RequestSignature(AISSignRequest request, Trace trace);

        AISSignResponse PollForSignatureStatus(AISPendingRequest request, Trace trace);
    }
}
