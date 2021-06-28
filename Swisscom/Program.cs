﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swisscom.AIS;
using Swisscom.AIS.Rest;

namespace Swisscom
{
	public class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Testing Swisscom Sign!");

			var workingDirectory = "d:/Work/Swisscom";
			var clientCertFile = $"{workingDirectory}/my-ais.crt";
			var clientAuthKeyFile = $"{workingDirectory}/my-ais.key";

			var inputPdfFileLoc = $"{workingDirectory}/VerifyID4Signing-en.pdf";
			var outPutPdfFileLoc = $"{workingDirectory}/Signed-VerifyID4Signing-en.pdf";

			//setup our DI
			var serviceProvider = new ServiceCollection()
				.AddSingleton<ISwisscomAdapterService, SwisscomAdapterService>()
				.BuildServiceProvider();

			//do the actual work here
			var service = serviceProvider.GetService<ISwisscomAdapterService>();
			var success = service.Sign(
				crtFileLoc: clientCertFile,
				keyFileloc: clientAuthKeyFile,
				inputPdfFileLoc: inputPdfFileLoc,
				outputPdfFileName: outPutPdfFileLoc,
				consentUrl: (url) =>
				{
					Console.WriteLine(url);
				});

			Console.WriteLine($"Testing finished with Success={success}!");
			Console.ReadLine();
		}

		public interface ISwisscomAdapterService
		{
			public bool Sign(
					string crtFileLoc,
					string keyFileloc,
					string inputPdfFileLoc,
					string outputPdfFileName,
					Action<string> consentUrl);
		}

		public class SwisscomAdapterService : ISwisscomAdapterService
		{
			public bool Sign(
				string crtFileLoc,
				string keyFileloc,
				string inputPdfFileLoc,
				string outputPdfFileName,
				Action<string> consentUrl)
			{
				try
				{
					ConfigurationProperties properties = new ConfigurationProperties
					{
						ClientPollRounds = "10",
						ClientPollIntervalInSeconds = "10",
						//ITextLicenseFilePath = "your-license-file",
						ServerRestSignUrl = "https://ais.swisscom.com/AIS-Server/rs/v1.0/sign",
						ServerRestPendingUrl = "https://ais.swisscom.com/AIS-Server/rs/v1.0/pending",
						ClientAuthKeyFile = keyFileloc,
						ClientAuthKeyPassword = "1234",
						ClientCertFile = crtFileLoc,
						SkipServerCertificateValidation = true,
						ClientHttpMaxConnectionsPerServer = "10",
						ClientHttpRequestTimeoutInSeconds = "10",
					};


					RestClientConfiguration restClientConfiguration = new RestClientConfiguration(properties);
					IRestClient restClient = new RestClient(restClientConfiguration);
					AisClientConfiguration aisClientConfiguration = new AisClientConfiguration(properties);

					IAisClient aisClient = new AisClient(restClient, aisClientConfiguration);
					UserData userData = new UserData
					{
						TransactionId = Guid.NewGuid().ToString(),
						ClaimedIdentityName = "ais-90days-trial",
						ClaimedIdentityKey = "OnDemand-Advanced4",
						DistinguishedName = "CN=arc.selise.biz,OU=ARC,O=Secure Link Services AG,L=Zurich,ST=Zurich,C=CH",
						//StepUpLanguage = "en",
						//StepUpMessage = "Please confirm the signing of the document",
						//StepUpMsisdn = "40740634075123",
						//SignatureReason = "For testing purposes",
						//SignatureLocation = "Dhaka, BD",
						//SignatureContactInfo = "asadullah.rifat@selise.ch",
						SignatureStandard = new SignatureStandard("PAdES-baseline"),
						RevocationInformation = new RevocationInformation("PAdES-baseline"),
						ConsentUrlCallback = new ConsentUrlCallback(),
						SignatureName = "SELISE ARC"
					};
					userData.ConsentUrlCallback.OnConsentUrlReceived += (sender, e) =>
					{
						consentUrl?.Invoke(e.Url);
					};

					List<PdfHandle> documents = new List<PdfHandle>
					{
						new PdfHandle
						{
							InputFileName = inputPdfFileLoc,
							OutputFileName = outputPdfFileName,
							DigestAlgorithm = DigestAlgorithm.SHA256
						}
					};

					SignatureResult signatureResult = aisClient.SignWithOnDemandCertificate(documents, userData);
					Console.WriteLine($"Finished signing the document(s) with the status: {signatureResult}");

					return signatureResult == SignatureResult.Success;
				}
				catch (Exception e)
				{
					Console.WriteLine($"Exception while signing the documents: {e}");
					return false;
				}
			}
		}
	}
}