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
using iText.Kernel.Pdf;
using iText.Signatures;

namespace Swisscom.AIS.Sign
{
    public class PdfHashSignatureContainer : IExternalSignatureContainer
    {
        private readonly string hashAlgorithm;
        private PdfDictionary signatureDictionary;

        public PdfHashSignatureContainer(string hashAlgorithm, PdfDictionary signatureDictionary)
        {
            this.hashAlgorithm = hashAlgorithm;
            this.signatureDictionary = signatureDictionary;
        }

        public byte[] Sign(Stream data)
        {
            return DigestAlgorithms.Digest(data, hashAlgorithm);
        }

        public void ModifySigningDictionary(PdfDictionary signDic)
        {
            signDic.PutAll(signatureDictionary);
        }
    }
}
