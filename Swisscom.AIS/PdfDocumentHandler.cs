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
using System.Linq;
using Swisscom.AISCommon;
using Swisscom.AIS;
using Swisscom.AIS.Rest;
using Swisscom.AIS.Sign;
using Swisscom.AIS.Utils;
using Common.Logging;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using static System.String;

namespace Swisscom.AIS
{
    public class PdfDocumentHandler
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Base64HashToSign { get; set; }

        public DigestAlgorithm DigestAlgorithm { get; set; }

        private static ILog logger = LogManager.GetLogger<PdfDocumentHandler>();
        private const string Delimiter = ";";
        private byte[] contentIn;
        private Trace trace;
        private PdfReader pdfReader;
        private PdfWriter pdfWriter;
        private MemoryStream inMemoryStream;
        private PdfDocumentSigner pdfSigner;
        private PdfDocument pdfDocument;
        private string outputFile;
        private byte[] documentHash;

        public PdfDocumentHandler(string inputFile, string outputFile, Trace trace)
        {
            contentIn = File.ReadAllBytes(inputFile);
            this.outputFile = outputFile;
            this.trace = trace;
        }

        public void PrepareForSigning(DigestAlgorithm algorithm, SignatureType signatureType, UserData userData)
        {
            DigestAlgorithm = algorithm;
            Id = GenerateDocumentId();

            pdfDocument = new PdfDocument(new PdfReader(new MemoryStream(contentIn)));
            SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
            var hasSignature = signatureUtil.GetSignatureNames().Count > 0;

            pdfReader = new PdfReader(new MemoryStream(contentIn), new ReaderProperties());
            inMemoryStream = new MemoryStream();
            pdfWriter = new PdfWriter(inMemoryStream, new WriterProperties().AddXmpMetadata().SetPdfVersion(PdfVersion.PDF_1_0));
            StampingProperties stampingProperties = new StampingProperties();
            pdfSigner = new PdfDocumentSigner(pdfReader, pdfWriter, hasSignature ? stampingProperties.UseAppendMode() : stampingProperties);
            pdfSigner.GetSignatureAppearance().SetReason(GetOptionalAttribute(userData.SignatureReason))
                .SetLocation(GetOptionalAttribute(userData.SignatureLocation)).SetContact(GetOptionalAttribute(userData.SignatureContactInfo));
            if (!userData.SignatureName.IsEmpty())
            {
                pdfSigner.SetFieldName(userData.SignatureName);
            }
            var isTimestampSignature = signatureType.Uri == SignatureType.Timestamp.Uri;
            Dictionary<PdfName, PdfObject> signatureDictionary = new Dictionary<PdfName, PdfObject>();
            signatureDictionary[PdfName.Filter] = PdfName.Adobe_PPKLite;
            signatureDictionary[PdfName.SubFilter] = isTimestampSignature ? PdfName.ETSI_RFC3161 : PdfName.ETSI_CAdES_DETACHED;
            pdfSigner.SetSignDate(isTimestampSignature? DateTime.Now : DateTime.Now.AddMinutes(3));
            PdfHashSignatureContainer hashSignatureContainer = new PdfHashSignatureContainer(DigestAlgorithm.Algorithm, new PdfDictionary(signatureDictionary));
            var hash = pdfSigner.ComputeHash(hashSignatureContainer, signatureType.EstimatedSignatureSizeInBytes);
            Base64HashToSign = Convert.ToBase64String(hash, 0, hash.Length);
        }

        public void CreateSignedPdf(byte[] externalsignature, int estimatedSize, List<string> encodedCrlEntries, List<string> encodedOcspEntries)
        {
            if (pdfSigner.GetCertificationLevel() == PdfSigner.CERTIFIED_NO_CHANGES_ALLOWED)
            {
                throw new AisClientException($"Could not apply signature because source file contains a certification " +
                                    $"that does not allow any changes to the document with id {Id}");
            }
            logger.Debug($"Signature size [estimated: {estimatedSize}" + Delimiter + $"actual: {externalsignature.Length}" + Delimiter + 
                         $"remaining: {estimatedSize-externalsignature.Length}" + $"] - {trace.Id}");
            if (estimatedSize < externalsignature.Length)
            {
                throw new AisClientException($"Not enough space for signature in the document with id {trace.Id}. The estimated size needs to be " +
                                    $" increased with {externalsignature.Length - estimatedSize} bytes.");
            }

            try
            {
                pdfSigner.SignWithAuthorizedSignature(new PdfSignatureContainer(externalsignature), estimatedSize);
                if (encodedOcspEntries != null || encodedCrlEntries != null)
                {
                    ExtendDocumentWithCrlOcspMetadata(new MemoryStream(inMemoryStream.ToArray()), encodedCrlEntries,
                        encodedOcspEntries);
                }
                else
                {
                    logger.Info($"No CRL and OCSP entries were received to be embedded into the PDF - {trace.Id}");
                    File.WriteAllBytes(outputFile, inMemoryStream.ToArray());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                Close();
            }
        }

        private void ExtendDocumentWithCrlOcspMetadata(MemoryStream documentStream, List<string> encodedCrlEntries, List<string> encodedOcspEntries)
        {
            List<byte[]> crl = encodedCrlEntries != null ? encodedCrlEntries.Select(MapEncodedCrl).ToList() : new List<byte[]>();
            List<byte[]> ocsp = encodedOcspEntries != null ? encodedOcspEntries.Select(MapEncodedOcsp).ToList() : new List<byte[]>();

            try
            {
                PdfReader reader = new PdfReader(documentStream);
                PdfWriter writer = new PdfWriter(File.OpenWrite(outputFile));
                PdfDocument document = new PdfDocument(reader, writer, new StampingProperties().PreserveEncryption().UseAppendMode());
                LtvVerification validation = new LtvVerification(document);
                IList<string> signatureNames = new SignatureUtil(document).GetSignatureNames();
                string signatureName = signatureNames[signatureNames.Count - 1];
                bool isSignatureVerificationAdded = validation.AddVerification(signatureName, ocsp, crl, null);
                validation.Merge();
                document.Close();
                LogSignatureVerificationInfo(isSignatureVerificationAdded);
            }
            catch (Exception e)
            {
                throw new AisClientException($"Failed to embed the signature(s) in the document(s) and close the streams - {trace.Id}");
            }
        }

        private void LogSignatureVerificationInfo(bool isSignatureVerificationAdded)
        {
            if (isSignatureVerificationAdded)
            {
                logger.Info($"Merged LTV validation information to the output stream - {trace.Id}");
            }
            else
            {
                logger.Warn($"Failed to merge LTV validation information to the output stream - {trace.Id}");
            }
        }

        private string GetOptionalAttribute(string attribute)
        {
            return attribute.IsEmpty() ? Empty : attribute;
        }
        private string GenerateDocumentId()
        {
            return "DOC-" + Guid.NewGuid();
        }

        private byte[] MapEncodedCrl(String encodedCrl)
        {
            try
            {
                MemoryStream inputStream = new MemoryStream(Convert.FromBase64String(encodedCrl));
                X509Crl x509Crl = new X509CrlParser().ReadCrl(inputStream);
                LogCrlInfo(x509Crl);
                return x509Crl.GetEncoded();
            }
            catch (Exception e)
            {
                throw new AisClientException($"Failed to map the received encoded CRL entry - {trace.Id}", e);
            }
        }

        private void LogCrlInfo(X509Crl x509Crl)
        {
            int revokedCertificatesNo = x509Crl?.GetRevokedCertificates()?.Count ?? 0;
            logger.Debug("Embedding CRL response... ["
                         + "IssuerDN: " + x509Crl.IssuerDN + Delimiter
                         + "This update: " + x509Crl.ThisUpdate + Delimiter
                         + "Next update: " + x509Crl.NextUpdate + Delimiter
                         + "No. of revoked certificates: " + revokedCertificatesNo
                         + "] - " + trace.Id);
        }

        private byte[] MapEncodedOcsp(String encodedOcsp)
        {
            try
            {
                MemoryStream inputStream = new MemoryStream(Convert.FromBase64String(encodedOcsp));
                OcspResp ocspResp = new OcspResp(inputStream);
                BasicOcspResp basicOcspResp = (BasicOcspResp)ocspResp.GetResponseObject();
                LogOcspInfo(ocspResp, basicOcspResp);
                return basicOcspResp.GetEncoded();
            }
            catch (Exception e)
            {
                throw new AisClientException($"Failed to map the received encoded OCSP entry - {trace.Id}", e);
            }
        }

        private void LogOcspInfo(OcspResp ocspResp, BasicOcspResp basicResp)
        {
            SingleResp response = basicResp.Responses[0];
            var serialNumber = response.GetCertID().SerialNumber;
            var firstCertificate = basicResp.GetCerts()[0];
            logger.Debug("Embedding OCSP response... [Status: " + (ocspResp.Status == 0 ? "OK" : "NOK") + Delimiter
                         + "Produced at: " + basicResp.ProducedAt + Delimiter
                         + "This update: " + response.ThisUpdate + Delimiter
                         + "Next update: " + response.NextUpdate + Delimiter
                         + "X509 cert issuer: " + firstCertificate.IssuerDN + Delimiter
                         + "X509 cert subject: " + firstCertificate.SubjectDN + Delimiter
                         + "Certificate ID: " + serialNumber + "(" + serialNumber.ToString(16).ToUpper() + ")"
                         + "] - " + trace.Id);
        }

        public void Close()
        {
            CloseResource(pdfReader);
            CloseResource(pdfWriter);
            CloseResource(pdfDocument);
            CloseResource(inMemoryStream);
        }
        private void CloseResource(IDisposable resource)
        {
            try
            {
                resource?.Dispose();
            }
            catch (Exception e)
            {
               logger.Debug($"Failed to close the resource - {trace.Id}. Reason: {e.Message}");
            }
        }
    }
}
