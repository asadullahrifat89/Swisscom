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
using System.Threading;
using Swisscom.AISCommon;
using Swisscom.AIS;
using Swisscom.AIS.Rest;
using Swisscom.AIS.Rest.PendingRequest;
using Swisscom.AIS.Rest.SignRequest;
using Swisscom.AIS.Rest.SignResponse;
using Swisscom.AIS.Rest;
using Swisscom.AIS.Utils;
using Common.Logging;
using iText.License;

namespace Swisscom.AIS
{
	public class AisClient : IAisClient
	{
		private static readonly string MissingMsisdnMessage = "<MSISDN> is missing";
		private readonly IRestClient restClient;
		private readonly AisClientConfiguration aisClientConfiguration;
		private static ILog logger = LogManager.GetLogger<AisClient>();

		public AisClient(IRestClient restClient, AisClientConfiguration aisClientConfiguration)
		{
			this.restClient = restClient;
			this.aisClientConfiguration = aisClientConfiguration;
			LoadLicense();
		}

		private void LoadLicense()
		{
			if (string.IsNullOrEmpty(aisClientConfiguration.LicenseFilePath))
			{
				return;
			}

			try
			{
				LicenseKey.LoadLicenseFile(aisClientConfiguration.LicenseFilePath);
				LicenseKey.ScheduledCheck(null);
				var licenseInfo = LicenseKey.GetLicenseeInfo();
				logger.Info($"Successfully load the {licenseInfo[8]} iText license granted for company {licenseInfo[2]}, with name {licenseInfo[0]}," +
							$" email {licenseInfo[1]}, having version {licenseInfo[6]} and producer line {licenseInfo[4]}. Is license expired: {licenseInfo[7]}.");
			}
			catch (Exception e)
			{
				logger.Error($"Failed to load the iText license: {e}");
			}
		}

		public SignatureResult SignWithStaticCertificate(List<PdfHandle> documentHandles, UserData userData)
		{
			return PerformSigning(new SignRequestDetails
			{
				AdditionalProfiles = new List<AdditionalProfile>(),
				DocumentHandles = documentHandles,
				SignatureMode = new SignatureMode(SignatureMode.Static),
				SignatureType = SignatureType.Cms,
				UserData = userData,
				WithStepUp = false,
				WithCertificateRequest = false
			});
		}

		public SignatureResult SignWithOnDemandCertificate(List<PdfHandle> documentHandles, UserData userData)
		{
			return PerformSigning(new SignRequestDetails
			{
				AdditionalProfiles = new List<AdditionalProfile> { AdditionalProfile.OnDemandCertificate },
				DocumentHandles = documentHandles,
				SignatureMode = new SignatureMode(SignatureMode.OnDemand),
				SignatureType = SignatureType.Cms,
				UserData = userData,
				WithStepUp = false,
				WithCertificateRequest = true
			});
		}

		public SignatureResult SignWithOnDemandCertificateAndStepUp(List<PdfHandle> documentHandles, UserData userData)
		{
			return PerformSigning(new SignRequestDetails
			{
				AdditionalProfiles = new List<AdditionalProfile> { AdditionalProfile.OnDemandCertificate, AdditionalProfile.Redirect, AdditionalProfile.Async },
				DocumentHandles = documentHandles,
				SignatureMode = new SignatureMode(SignatureMode.OnDemandStepUp),
				SignatureType = SignatureType.Cms,
				UserData = userData,
				WithStepUp = true,
				WithCertificateRequest = true
			});
		}

		public SignatureResult Timestamp(List<PdfHandle> documentHandles, UserData userData)
		{
			return PerformSigning(new SignRequestDetails
			{
				AdditionalProfiles = new List<AdditionalProfile> { AdditionalProfile.Timestamp },
				DocumentHandles = documentHandles,
				SignatureMode = new SignatureMode(SignatureMode.Timestamp),
				SignatureType = SignatureType.Timestamp,
				UserData = userData,
				WithStepUp = false,
				WithCertificateRequest = false
			});
		}

		private SignatureResult PerformSigning(SignRequestDetails details)
		{
			Trace trace = new Trace(details.UserData.TransactionId);
			details.UserData.ValidatePropertiesForSignature(details.SignatureMode, trace);
			details.DocumentHandles.ForEach(dh => dh.ValidateYourself(trace));

			List<PdfDocumentHandler> documentsToSign = details.DocumentHandles.Select(dh => PrepareDocumentForSigning(dh, details.SignatureMode, details.SignatureType, details.UserData, trace)).ToList();
			try
			{
				List<AdditionalProfile> additionalProfiles = PrepareAdditionalProfiles(documentsToSign, details.AdditionalProfiles);

				AISSignRequest signRequest = new RequestBuilder().BuildAisSignRequest(new SignRequestDetails
				{
					AdditionalProfiles = additionalProfiles,
					Documents = documentsToSign,
					SignatureMode = details.SignatureMode,
					SignatureType = details.SignatureType,
					UserData = details.UserData,
					WithStepUp = details.WithStepUp,
					WithCertificateRequest = details.WithCertificateRequest
				});
				AISSignResponse signResponse = restClient.RequestSignature(signRequest, trace);
				if (details.WithStepUp && !ResponseUtils.IsResponseAsyncPending(signResponse))
				{
					return ExtractSignatureResultFromResponse(signResponse, trace);
				}
				if (details.WithStepUp)
				{
					signResponse = PollUntilSignatureIsComplete(signResponse, details.UserData, trace);
				}
				if (!ResponseUtils.IsResponseMajorSuccess(signResponse))
				{
					return ExtractSignatureResultFromResponse(signResponse, trace);
				}
				FinishDocumentSigning(documentsToSign, signResponse, details.SignatureMode, details.SignatureType.EstimatedSignatureSizeInBytes, trace);
				return SignatureResult.Success;
			}
			catch (Exception e)
			{
				throw new AisClientException($"Failed to communicate with the AIS service and obtain the signature(s) - {trace.Id}", e);
			}
			finally
			{
				documentsToSign.ForEach(d => d.Close());
			}
		}

		private AISSignResponse PollUntilSignatureIsComplete(AISSignResponse signResponse, UserData userData, Trace trace)
		{
			AISSignResponse localResponse = signResponse;
			try
			{
				if (ProcessConsentUrl(localResponse, userData, trace))
				{
					Thread.Sleep(aisClientConfiguration.SignaturePollingIntervalInSeconds * 1000);
				}

				for (int round = 0; round < aisClientConfiguration.SignaturePollingRounds; round++)
				{
					logger.Debug($"Polling for signature status, round {round + 1}/{aisClientConfiguration.SignaturePollingRounds} - {trace.Id}");
					AISPendingRequest pendingRequest = RequestBuilder.BuildAisPendingRequest(ResponseUtils.GetAsyncResponseId(localResponse), userData);
					localResponse = restClient.PollForSignatureStatus(pendingRequest, trace);
					ProcessConsentUrl(localResponse, userData, trace);
					if (ResponseUtils.IsResponseAsyncPending(localResponse))
					{
						Thread.Sleep(aisClientConfiguration.SignaturePollingIntervalInSeconds * 1000);
					}
					else
					{
						break;
					}
				}
			}
			catch (Exception e)
			{
				throw new AisClientException($"Failed to poll AIS for the status of the signature(s) - {trace.Id}. Exception: {e}");
			}
			return localResponse;
		}

		private bool ProcessConsentUrl(AISSignResponse response, UserData userData, Trace trace)
		{
			if (ResponseUtils.ResponseHasStepUpConsentUrl(response))
			{
				userData.ConsentUrlCallback.RaiseConsentUrlReceivedEvent(ResponseUtils.GetStepUpConsentUrl(response), userData, trace);
				return true;
			}
			return false;
		}

		private SignatureResult ExtractSignatureResultFromResponse(AISSignResponse response, Trace trace)
		{
			if (response?.SignResponse?.Result?.ResultMajor == null)
			{
				throw new AisClientException($"Incomplete response received from the AIS service: {response} - {trace.Id}");
			}

			ResultMajorCode resultMajorCode = ResultMajorCode.GetByUri(response.SignResponse.Result.ResultMajor);
			ResultMinorCode resultMinorCode = ResultMinorCode.GetByUri(response.SignResponse.Result.ResultMinor);
			if (resultMajorCode == null)
			{
				throw new AisClientException($"Failure response received from the AIS service: {ResponseUtils.GetResponseResultSummary(response)} - {trace.Id}");
			}

			if (resultMajorCode.Equals(ResultMajorCode.Success))
			{
				return SignatureResult.Success;
			}
			if (resultMajorCode.Equals(ResultMajorCode.Pending))
			{
				return SignatureResult.UserTimeout;
			}
			if (resultMajorCode.Equals(ResultMajorCode.RequesterError) || resultMajorCode.Equals(ResultMajorCode.SubsystemError))
			{
				SignatureResult? result = ExtractSignatureResultFromMinorCode(resultMinorCode, response.SignResponse.Result);
				if (result != null)
				{
					return result.Value;
				}
			}
			var responseSummary = ResponseUtils.GetResponseResultSummary(response);
			throw new AisClientException($"Failure response received from the AIS service: {responseSummary} - {trace.Id}");
		}

		private SignatureResult? ExtractSignatureResultFromMinorCode(ResultMinorCode minorCode, Result responseResult)
		{
			if (minorCode == null)
			{
				return null;
			}

			if (minorCode.Equals(ResultMinorCode.SerialNumberMismatch))
			{
				return SignatureResult.SerialNumberMismatch;
			}
			if (minorCode.Equals(ResultMinorCode.StepupTimeout))
			{
				return SignatureResult.UserTimeout;
			}
			if (minorCode.Equals(ResultMinorCode.StepupCancel))
			{
				return SignatureResult.UserCancel;
			}
			if (minorCode.Equals(ResultMinorCode.InsufficientData) && responseResult.ResultMessage.S.Contains(MissingMsisdnMessage))
			{
				logger.Error("The required MSISDN parameter was missing in the request.This can happen sometimes in the context of the"
					+ " on-demand flow, depending on the user's server configuration. As an alternative, the on-demand with"
					+ " step-up flow can be used instead.");
				return SignatureResult.InsufficientDataWithAbsentMsisdn;
			}
			if (minorCode.Equals(ResultMinorCode.ServiceError) && responseResult.ResultMessage != null)
			{
				ResultMessageCode resultMessageCode = ResultMessageCode.GetByUri(responseResult.ResultMessage.S);
				if (resultMessageCode != null && (resultMessageCode.Equals(ResultMessageCode.InvalidPassword) ||
												  resultMessageCode.Equals(ResultMessageCode.InvalidOtp)))
				{
					return SignatureResult.UserAuthenticationFailed;
				}
			}
			return null;
		}


		private List<AdditionalProfile> PrepareAdditionalProfiles(List<PdfDocumentHandler> documentsToSign, List<AdditionalProfile> defaultProfiles)
		{
			List<AdditionalProfile> additionalProfiles = new List<AdditionalProfile>();
			if (documentsToSign.Count > 1)
			{
				additionalProfiles.Add(AdditionalProfile.Batch);
			}
			additionalProfiles.AddRange(defaultProfiles);
			return additionalProfiles;
		}

		private PdfDocumentHandler PrepareDocumentForSigning(PdfHandle documentHandle, SignatureMode signatureMode,
			SignatureType signatureType, UserData userData, Trace trace)
		{
			logger.Info($"Preparing {signatureMode.FriendlyName} signing for document: {documentHandle.InputFileName} - {trace.Id}");
			try
			{
				PdfDocumentHandler newDocument = new PdfDocumentHandler(documentHandle.InputFileName, documentHandle.OutputFileName, trace);
				newDocument.PrepareForSigning(documentHandle.DigestAlgorithm, signatureType, userData);
				return newDocument;
			}
			catch (Exception e)
			{
				throw new AisClientException($"Failed to prepare the document for {signatureMode.FriendlyName} signing - {trace.Id}", e);
			}
		}

		private void FinishDocumentSigning(List<PdfDocumentHandler> documents, AISSignResponse response,
			SignatureMode signatureMode, int signatureEstimatedSize, Trace trace)
		{
			List<string> encodedCrlEntries = ResponseUtils.ExtractScCRLs(response);
			List<string> encodedOcspEntries = ResponseUtils.ExtractScOCSPs(response);
			bool containsSingleDocument = documents.Count == 1;
			logger.Info($"Embedding the signature(s) into the document(s)... - {trace.Id}");
			documents.ForEach(doc =>
			{
				string encodedSignature = ExtractEncodedSignature(response, containsSingleDocument, signatureMode, doc);
				doc.CreateSignedPdf(Convert.FromBase64String(encodedSignature), signatureEstimatedSize, encodedCrlEntries, encodedOcspEntries);
			});
		}

		private string ExtractEncodedSignature(AISSignResponse response, bool containsSingleDocument, SignatureMode signatureMode, PdfDocumentHandler documentHandler)
		{
			if (containsSingleDocument)
			{
				SignatureObject signatureObject = response.SignResponse.SignatureObject;
				return signatureMode.FriendlyName == SignatureMode.Timestamp
					? signatureObject.Timestamp.RFC3161TimeStampToken
					: signatureObject.Base64Signature.S;
			}
			ScExtendedSignatureObject extendedSignatureObject = ResponseUtils.GetSignatureObjectByDocumentId(documentHandler.Id, response);
			return signatureMode.FriendlyName == SignatureMode.Timestamp
				? extendedSignatureObject.Timestamp.RFC3161TimeStampToken
				: extendedSignatureObject.Base64Signature.S;
		}
	}
}
