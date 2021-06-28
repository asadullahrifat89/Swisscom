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
    public class ResultMessageCode
    {
        public static readonly ResultMessageCode InvalidPassword = new ResultMessageCode("urn:swisscom:names:sas:1.0:status:InvalidPassword",
            "User entered an invalid password, as part of the Step Up phase");

        public static readonly ResultMessageCode InvalidOtp = new ResultMessageCode("urn:swisscom:names:sas:1.0:status:InvalidOtp",
            "User entered an invalid OTP, as part of the Step Up phase");

        public readonly string Uri;

        public readonly string Description;

        private static readonly List<ResultMessageCode> ResultMessageCodes = new List<ResultMessageCode>
        {
            InvalidPassword,
            InvalidOtp
        };
        public ResultMessageCode(string uri, string description)
        {
            Uri = uri;
            Description = description;
        }

        public static ResultMessageCode GetByUri(string uri)
        {
            return ResultMessageCodes.FirstOrDefault(rmc => rmc.Uri == uri);
        }

        private bool Equals(ResultMessageCode other)
        {
            return other?.Uri == Uri;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ResultMessageCode);
        }

        public override int GetHashCode()
        {
            return (Uri != null ? Uri.GetHashCode() : 0);
        }
    }
}