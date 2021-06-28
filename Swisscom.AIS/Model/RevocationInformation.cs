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
using System.Collections.Generic;
using System.Linq;

namespace Swisscom.AIS
{
    public class RevocationInformation
    {
        /**
         * For CMS signatures, if the attribute for revocation information is not provided (set as default),
         * the revocation information will match the defined SignatureStandard (CAdES or PAdES).
         * (In case the SignatureStandard is not set, the default is CAdES).
         *
         * For timestamps, the signature standard does not apply so the revocation information type must be
         * set explicitly.
         */
        public static readonly string Default = "";

        /**
         * Encode revocation information in CMS compliant to CAdES.
         * RI will be embedded as an unsigned attribute with OID 1.2.840.113549.1.9.16.2.24.
         */
        public static readonly string Cades = "CAdES";

        /**
         * PDF (formerly named PAdES): CMS Signatures: RI will be embedded in the signature as a signed attribute
         * with OID 1.2.840.113583.1.1.8. Trusted Timestamps: RI will be provided in the response as Base64 encoded
         * OCSP responses or CRLs within the <OptionalOutputs>-Element.
         */
        public static readonly string Pdf = "PDF";

        /**
         * Alias for PDF for backward compatibility. Since 1st of December 2020, the PADES signature standard has been replaced
         * with the PDF option, to better transmit the idea that the revocation information archival attribute is added to the
         * CMS signature that is returned to the client, as per the PDF reference. This revocation information value (PADES) is now
         * deprecated and should not be used. Use instead the PDF one, which has the same behaviour.
         *
         * @deprecated Please use the {@link #PDF} element.
         */
        public static readonly string Pades = "PAdES";

        /**
         * Add optional output with revocation information, to be used by clients to create PAdES-compliant signatures.
         * In order to get an LTV-enabled PDF signature, the client must process the optional output and fill the PDF's DSS (this AIS client
         * library already does this for your). This is in contrast with the PDF option (see above) that embeds the revocation information
         * as an archival attribute inside the CMS content, which might trip some strict checkers (e.g. ETSI Signature Conformance Checker).
         */
        public static readonly string Pades_baselines = "PAdES-baseline";

        /**
         * Both RI types (CAdES and PDF) will be provided (for backward compatibility).
         */
        public static readonly string Both = "BOTH";

        /**
         * Add optional output with revocation information.
         */
        public static readonly string Plain = "PLAIN";

        private static readonly List<string> RevocationInformations = new List<string>
        {
            Default,
            Cades,
            Pdf,
            Pades,
            Pades_baselines,
            Both,
            Plain
        };

        public readonly string Value;

        public RevocationInformation(string value)
        {
            if (!string.IsNullOrEmpty(value) && RevocationInformations.Any(ri => ri.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                Value = value;
            }
            else
            {
                throw new ArgumentException("Invalid revocation information value: " + value);
            }
        }

    }
}
