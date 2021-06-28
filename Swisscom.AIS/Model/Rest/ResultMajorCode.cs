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
using System.Linq;

namespace Swisscom.AIS.Rest
{
    public class ResultMajorCode
    {
        public static readonly ResultMajorCode SubsystemError = new ResultMajorCode("http://ais.swisscom.ch/1.0/resultmajor/SubsystemError",
            "Some subsystem of the server produced an error. Details are included in the minor status code.");

        public static readonly ResultMajorCode Pending = new ResultMajorCode("urn:oasis:names:tc:dss:1.0:profiles:asynchronousprocessing:resultmajor:Pending",
            "Asynchronous request was accepted. It is pending now.");

        public static readonly ResultMajorCode RequesterError = new ResultMajorCode("urn:oasis:names:tc:dss:1.0:resultmajor:RequesterError",
            "It is assumed that the caller has made a mistake. Details are included in the minor status code.");

        public static readonly ResultMajorCode ResponderError = new ResultMajorCode("urn:oasis:names:tc:dss:1.0:resultmajor:ResponderError",
            "The server could not process the request. Details are included in the minor status code.");

        public static readonly ResultMajorCode Success = new ResultMajorCode("urn:oasis:names:tc:dss:1.0:resultmajor:Success",
            "Request was successfully executed");

        public readonly string Uri;

        public readonly string Description;

        private static readonly List<ResultMajorCode> ResultMajorCodes = new List<ResultMajorCode>
        {
            SubsystemError,
            Pending,
            RequesterError,
            ResponderError,
            Success
        };

        public ResultMajorCode(string uri, string description)
        {
            Uri = uri;
            Description = description;
        }

        public static ResultMajorCode GetByUri(string uri)
        {
            return ResultMajorCodes.FirstOrDefault(rmc => rmc.Uri == uri);
        }

        private bool Equals(ResultMajorCode other)
        {
            return other?.Uri == Uri;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ResultMajorCode);
        }

        public override int GetHashCode()
        {
            return (Uri != null ? Uri.GetHashCode() : 0);
        }
    }
}