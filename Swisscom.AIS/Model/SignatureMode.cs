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
namespace Swisscom.AIS
{
    public class SignatureMode
    {
        public const string Timestamp = "Timestamp";

        public const string Static = "Static";

        public const string OnDemand = "On demand";

        public const string OnDemandStepUp = "On demand step up";

        public readonly string FriendlyName;

        public SignatureMode(string friendlyName)
        {
            FriendlyName = friendlyName;
        }
    }
}
