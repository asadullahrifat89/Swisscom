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
using Swisscom.AIS.Utils;
using Common.Logging;

namespace Swisscom.AIS
{
    public class ConsentUrlCallback
    {
        public event EventHandler<OnConsentUrlReceivedArgs> OnConsentUrlReceived;
        private static ILog logger = LogManager.GetLogger<ConsentUrlCallback>();

        public void RaiseConsentUrlReceivedEvent(string url, UserData userData, Trace trace)
        {
            var handler = OnConsentUrlReceived;
            if (handler != null)
            {
                handler(this, new OnConsentUrlReceivedArgs
                {
                    Url = url,
                    UserData = userData
                });
            }
            else
            {
                logger.Warn("Consent URL was received from AIS, but no consent URL callback was configured (in UserData). " +
                            $"This transaction will probably fail - {trace.Id}");
            }
        }
    }

    public class OnConsentUrlReceivedArgs
    {
        public string Url { get; set; }
        public UserData UserData { get; set; }
    }
}
