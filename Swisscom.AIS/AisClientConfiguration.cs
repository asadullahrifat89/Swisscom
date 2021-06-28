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
using System.IO;
using Swisscom.AIS;
using Swisscom.AIS.Utils;
using Newtonsoft.Json;

namespace Swisscom.AIS
{
    public class AisClientConfiguration
    {
        public int SignaturePollingIntervalInSeconds { get; set; }

        public int SignaturePollingRounds { get; set; }

        public string LicenseFilePath { get; set; }


        public AisClientConfiguration(ConfigurationProperties properties)
        {
            LoadConfiguration(properties);
        }
        public AisClientConfiguration(string filePath)
        {
            string config = File.ReadAllText(filePath);
            LoadConfiguration(JsonConvert.DeserializeObject<ConfigurationProperties>(config));
        }

        private void LoadConfiguration(ConfigurationProperties configuration)
        {

            SignaturePollingIntervalInSeconds = ConfigParser.GetIntNotNull("ClientPollIntervalInSeconds", configuration.ClientPollIntervalInSeconds);
            SignaturePollingRounds = ConfigParser.GetIntNotNull("ClientPollRounds", configuration.ClientPollRounds);
            LicenseFilePath = configuration.ITextLicenseFilePath;
            ValidateFieldVales();
        }

        private void ValidateFieldVales()
        {
            Validator.AssertIntValueBetween(SignaturePollingIntervalInSeconds, 1, 300, "The SignaturePollingIntervalInSeconds parameter of the AIS client " +
                                                                                       "configuration must be between 1 and 300 seconds", null);
            Validator.AssertIntValueBetween(SignaturePollingRounds, 1, 100, "The SignaturePollingRounds parameter of the AIS client "
                                                                            + "configuration must be between 1 and 100 rounds", null);

        }
    }
}
