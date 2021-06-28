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
using System.IO;
using Swisscom.AISCommon;
using iText.Kernel.Pdf;
using iText.Signatures;

namespace Swisscom.AIS.Sign
{
	public class PdfDocumentSigner : PdfSigner
	{

		public PdfDocumentSigner(PdfReader reader, Stream outputStream, StampingProperties properties) : base(reader, outputStream, properties)
		{
		}

		public byte[] ComputeHash(IExternalSignatureContainer externalSignatureContainer, int estimatedSize)
		{
			PdfSignature signatureDictionary = new PdfSignature();
			PdfSignatureAppearance appearance = GetSignatureAppearance();
			signatureDictionary.SetReason(appearance.GetReason());
			signatureDictionary.SetLocation(appearance.GetLocation());
			signatureDictionary.SetSignatureCreator(appearance.GetSignatureCreator());
			signatureDictionary.SetContact(appearance.GetContact());
			signatureDictionary.SetDate(new PdfDate(GetSignDate()));
			externalSignatureContainer.ModifySigningDictionary(signatureDictionary.GetPdfObject());
			cryptoDictionary = signatureDictionary;
			Dictionary<PdfName, int?> exc = new Dictionary<PdfName, int?> { [PdfName.Contents] = estimatedSize * 2 + 2 };
			PreClose(exc);
			Stream dataRangeStream = GetRangeStream();
			return externalSignatureContainer.Sign(dataRangeStream);
		}

		public void SignWithAuthorizedSignature(IExternalSignatureContainer externalSignatureContainer, int estimatedSize)
		{
			Stream dataRangeStream = GetRangeStream();
			var authorizedSignature = externalSignatureContainer.Sign(dataRangeStream);
			if (estimatedSize < authorizedSignature.Length)
			{
				throw new AisClientException("Not enough space");
			}

			byte[] paddedSignature = new byte[estimatedSize];
			Array.Copy(authorizedSignature, 0, paddedSignature, 0, authorizedSignature.Length);
			PdfDictionary dict2 = new PdfDictionary();
			dict2.Put(PdfName.Contents, new PdfString(paddedSignature).SetHexWriting(true));
			Close(dict2);
			closed = true;
		}
	}
}
