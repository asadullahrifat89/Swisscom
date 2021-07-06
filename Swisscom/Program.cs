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

				string workingDirectory = "d:/Work/Swisscom";
				string certFilePath = $"{workingDirectory}/my-ais.crt";
				string keyFilePath = $"{workingDirectory}/my-ais.key";

				string inputPdfFileLoc = $"{workingDirectory}/VerifyID4Signing-en.pdf";
				string outPutPdfFileLoc = $"{workingDirectory}/Signed-VerifyID4Signing-en.pdf";

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
					IServiceCollection serviceCollection = new ServiceCollection()
						.AddSingleton<ISwisscomAdapterService, SwisscomAdapterService>();

					serviceCollection.AddHttpService();

					serviceProvider = serviceCollection.BuildServiceProvider();

					bool concentUrlReceived = false;


					swisscomAdapterService = serviceProvider.GetService<ISwisscomAdapterService>();

					//do the actual work here
					bool success = swisscomAdapterService.SignAsync(
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

		public class VerifyResponse
		{
			public string evidenceId { get; set; }
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
					//string msisdn = "+8801841800532"; // me
					string msisdn = "+41792615748"; // julian
													//string msisdn = "+41432152563"; // rezwan

					string claimedIdentityName = "ais-90days-trial-OTP";
					string claimedIdentityKey = "OnDemand-Advanced4"; /*"static-saphir4-ch";*/
					httpService = serviceProvider.GetService<IHttpService>();

					VerifyResponse verifyResponse = new VerifyResponse();

					#region Verify Mobile Number and Get Evidence Id				

					var payload = new
					{
						claimedIdentity = claimedIdentityName,
						msisdn = msisdn,
						distinguishedName = "gn=Max,sn=Muster,cn =TEST Max Muster,c = CH",
						assuranceLevel = "4",
						jurisdiction = "zertes"
					};

					string url = "https://ras.scapp.swisscom.com/api/evidences/verify";
					Console.WriteLine($"Calling ... {url}");
					HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
					{
						Content = new StringContent(content: System.Text.Json.JsonSerializer.Serialize(payload), encoding: Encoding.UTF8, mediaType: "application/vnd.sc.ras.evidence.v1+json")
					};

					HttpResponseMessage response = await httpService.SendAsync(httpRequestMessage);

					string content = await response.Content.ReadAsStringAsync();

					if (response.IsSuccessStatusCode) // got serial number as evidenceId from swisscom
					{
						verifyResponse = System.Text.Json.JsonSerializer.Deserialize<VerifyResponse>(content);
					}
					else // failed so just use mock data
					{
						verifyResponse.evidenceId = "RAS5b45b027c6d9370008072c48"; // demo data
						Console.WriteLine(content);
					}
					#endregion

					#region Sign Request

					Console.WriteLine($"Calling ... https://ais.swisscom.com/AIS-Server/rs/v1.0/sign");

					ConfigurationProperties properties = new ConfigurationProperties
					{
						//ITextLicenseFilePath = "your-license-file",
						ClientPollRounds = "25",
						ClientPollIntervalInSeconds = "20",						
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
						ClaimedIdentityName = claimedIdentityName,
						ClaimedIdentityKey = claimedIdentityKey,

						DistinguishedName = $"cn=TEST Max Muster, givenname=Max, surname=Muster, c=CH, serialnumber={verifyResponse.evidenceId}",

						StepUpMsisdn = msisdn,
						StepUpLanguage = "en",
						StepUpMessage = "Please confirm the signing of the document",
						StepUpSerialNumber = verifyResponse.evidenceId,

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
						UserData userData = e.UserData;
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

					//SignatureResult signatureResult = aisClient.SignWithStaticCertificate(documents, userData); // works
					//SignatureResult signatureResult = aisClient.SignWithOnDemandCertificate(documents, userData); // doesn't work as it required MSISDN element to be present in UserData
					SignatureResult signatureResult = aisClient.SignWithOnDemandCertificateAndStepUp(documents, userData); // works but requires a valid mobile number registered with swisscom: https://www.mobileid.ch/en/login?origin=first-activation

					Console.WriteLine($"Finished signing the document(s) with the status: {signatureResult}");
					return signatureResult == SignatureResult.Success;

					#endregion
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
