using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Selise.Arc.Core;
using Swisscom.AIS;
using Swisscom.AIS.Rest;

namespace Swisscom
{
	public class Program
	{
		private static ISwisscomAdapterService swisscomAdapterService;
		private static IHttpService httpService;
		private static ServiceProvider serviceProvider;

		static void Main(string[] args)
		{
			try
			{
				Console.WriteLine("Testing Swisscom Sign!");

				var workingDirectory = "d:/Work/Swisscom";
				var certFilePath = $"{workingDirectory}/my-ais.crt";
				var keyFilePath = $"{workingDirectory}/my-ais.key";

				var inputPdfFileLoc = $"{workingDirectory}/VerifyID4Signing-en.pdf";
				var outPutPdfFileLoc = $"{workingDirectory}/Signed-VerifyID4Signing-en.pdf";

				FileInfo crtFile = new FileInfo(certFilePath);
				FileInfo keyFile = new FileInfo(keyFilePath);

				if (crtFile.Exists)
				{
					Console.WriteLine($"File found ={certFilePath}");
				}

				if (keyFile.Exists)
				{
					Console.WriteLine($"File found ={keyFilePath}");
				}

				if (crtFile.Exists && keyFile.Exists)
				{
					Console.WriteLine($"Required auth files found. Proceeding to test.");

					//setup our DI
					var serviceCollection = new ServiceCollection()
						.AddSingleton<ISwisscomAdapterService, SwisscomAdapterService>();

					serviceCollection.AddHttpService();

					serviceProvider = serviceCollection.BuildServiceProvider();

					var concentUrlReceived = false;


					swisscomAdapterService = serviceProvider.GetService<ISwisscomAdapterService>();

					//do the actual work here
					var success = swisscomAdapterService.SignAsync(
						crtFileLoc: certFilePath,
						keyFileloc: keyFilePath,
						inputPdfFileLoc: inputPdfFileLoc,
						outputPdfFileName: outPutPdfFileLoc,
						consentUrl: (url) =>
						{
							if (!concentUrlReceived)
							{
								concentUrlReceived = true;

								Console.WriteLine(url);

								//Process.Start(new ProcessStartInfo
								//{
								//	FileName = url,
								//	UseShellExecute = true
								//});
							}
						}).Result;

					Console.WriteLine($"Testing finished with Success={success}!");
				}
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public interface ISwisscomAdapterService
		{
			public Task<bool> SignAsync(
					string crtFileLoc,
					string keyFileloc,
					string inputPdfFileLoc,
					string outputPdfFileName,
					Action<string> consentUrl);
		}

		public class SwisscomAdapterService : ISwisscomAdapterService
		{
			public async Task<bool> SignAsync(
				string crtFileLoc,
				string keyFileloc,
				string inputPdfFileLoc,
				string outputPdfFileName,
				Action<string> consentUrl)
			{
				try
				{
					string stepUpSerialNumber = "RAS5b45b027c6d9370008072c48";
					//string msisdn = "+41792615748"; // julian vai
					string msisdn = "+41432152563"; // rezwan vai

					httpService = serviceProvider.GetService<IHttpService>();

					//TODO: get EvidenceId

					var payload = new
					{
						claimedIdentity = "ais-90days-trial",
						distinguishedName = "gn=heinrich,sn=mustermann,cn =TEST heini mustermann,c = CH",
						msisdn = msisdn,
						assuranceLevel = "3"
					};

					var url = "https://ras.scapp.swisscom.com/api/evidences/verify";
					Console.WriteLine($"Calling ... {url}");
					HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
					{
						Content = new StringContent(content: System.Text.Json.JsonSerializer.Serialize(payload), encoding: Encoding.UTF8, mediaType: "application/vnd.sc.ras.evidence.v1+json")
					};

					HttpResponseMessage response = await httpService.SendAsync(httpRequestMessage);

					var content = response.Content.ReadAsStringAsync();

					if (response.IsSuccessStatusCode)
					{
						//TODO: set stepUpSerialNumber
					}
					else
					{
						Console.WriteLine(content);
					}

					Console.WriteLine($"Calling ... https://ais.swisscom.com/AIS-Server/rs/v1.0/sign");

					ConfigurationProperties properties = new ConfigurationProperties
					{
						ClientPollRounds = "20",
						ClientPollIntervalInSeconds = "15",
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

						ClaimedIdentityKey = "OnDemand-Advanced",
						//ClaimedIdentityKey = "static-saphir4-ch",

						DistinguishedName = $"cn=TEST Max Muster, givenname=Max, surname=Muster, c=CH, serialnumber={stepUpSerialNumber}",

						StepUpMsisdn = msisdn,
						StepUpLanguage = "en",
						StepUpMessage = "Please confirm the signing of the document",
						StepUpSerialNumber = stepUpSerialNumber,

						SignatureReason = "For testing purposes",
						SignatureLocation = "Dhaka, BD",
						SignatureContactInfo = "rezwan.rafiq@selise.ch",

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

					SignatureResult signatureResult = aisClient.SignWithOnDemandCertificateAndStepUp(documents, userData);

					//SignatureResult signatureResult = aisClient.SignWithStaticCertificate(documents, userData);

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
