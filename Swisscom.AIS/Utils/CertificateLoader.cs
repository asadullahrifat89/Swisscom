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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using BouncyCastleX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Swisscom.AIS.Utils
{
    public class CertificateLoader
    {
        public X509Certificate2 LoadCertificate(CertificateConfiguration certificateConfiguration)
        {
            return BuildCertificate(certificateConfiguration);
        }

        private X509Certificate2 BuildCertificate(CertificateConfiguration certificateConfiguration)
        {
            var builder = new Pkcs12StoreBuilder();
            builder.SetUseDerEncoding(true);
            var store = builder.Build();

            var certEntry = new X509CertificateEntry(ReadCertificate(certificateConfiguration));
            store.SetCertificateEntry("", certEntry);
            store.SetKeyEntry("", new AsymmetricKeyEntry(ReadPrivateKey(certificateConfiguration)), new[] { certEntry });

            byte[] data;
            using (var ms = new MemoryStream())
            {
                store.Save(ms, Array.Empty<char>(), new SecureRandom());
                data = ms.ToArray();
            }

            return new X509Certificate2(data);
        }
        private BouncyCastleX509Certificate ReadCertificate(CertificateConfiguration certificateConfiguration)
        {
            var x509CertificateParser = new X509CertificateParser();
            return x509CertificateParser.ReadCertificate(File.ReadAllBytes(certificateConfiguration.CertificateFile));
        }

        private AsymmetricKeyParameter ReadPrivateKey(CertificateConfiguration certificateConfiguration)
        {
            var key = File.ReadAllText(certificateConfiguration.PrivateKeyFile);
            var trimmedKey = key.Substring(key.IndexOf("-----BEGIN", StringComparison.Ordinal));
            using (TextReader privateKeyTextReader = new StringReader(trimmedKey))
            {
                var reader = new PemReader(privateKeyTextReader, new PasswordFinder(certificateConfiguration.Password));
                var obj = reader.ReadObject();
                if (obj is AsymmetricKeyParameter)
                {
                    return obj as AsymmetricKeyParameter;
                }
                if (obj is AsymmetricCipherKeyPair)
                {
                    var pair = obj as AsymmetricCipherKeyPair;
                    return pair.Private;
                }
                return null;
            }
        }
    }
}
