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

namespace Swisscom.AIS.Rest
{
    public class RestClientConfiguration
    {
        public string RestServiceSignUrl { get; set; }

        public string RestServicePendingUrl { get; set; }

        public string ClientKeyFile { get; set; }

        public string ClientKeyPassword { get; set; }

        public string ClientCertificateFile { get; set; }

        public int MaxConnectionsPerServer { get; set; }

        public int RequestTimeoutInSec { get; set; }

        public bool SkipServerCertificateValidation { get; set; }

        public RestClientConfiguration(string filePath)
        {
            string config = File.ReadAllText(filePath);
            LoadConfiguration(JsonConvert.DeserializeObject<ConfigurationProperties>(config));
        }

        public RestClientConfiguration(ConfigurationProperties configurationProperties)
        {
           LoadConfiguration(configurationProperties);
        }

        private void LoadConfiguration(ConfigurationProperties configuration)
        {
            RestServiceSignUrl = ConfigParser.GetStringNotNull("ServerRestSignUrl", configuration.ServerRestSignUrl);
            RestServicePendingUrl = ConfigParser.GetStringNotNull("ServerRestPendingUrl", configuration.ServerRestPendingUrl);
            ClientKeyFile = ConfigParser.GetStringNotNull("ClientAuthKeyFile", configuration.ClientAuthKeyFile);
            ClientKeyPassword = configuration.ClientAuthKeyPassword;
            ClientCertificateFile = ConfigParser.GetStringNotNull("ClientCertFile", configuration.ClientCertFile);
            MaxConnectionsPerServer = ConfigParser.GetIntNotNull("ClientHttpMaxConnectionsPerServer", configuration.ClientHttpMaxConnectionsPerServer);
            RequestTimeoutInSec = ConfigParser.GetIntNotNull("ClientHttpRequestTimeoutInSeconds", configuration.ClientHttpRequestTimeoutInSeconds);
            SkipServerCertificateValidation = configuration.SkipServerCertificateValidation;

            ValidateFieldVales();
        }

        private void ValidateFieldVales()
        {
            Validator.AssertIntValueBetween(MaxConnectionsPerServer, 2, 100, "The MaxConnectionsPerRoute parameter of the REST client configuration must be between 2 and 100", null);
            Validator.AssertIntValueBetween(RequestTimeoutInSec, 2, 100, "The ResponseTimeoutInSec parameter of the REST client configuration must be between 2 and 100", null);
        }
    }

  
}
