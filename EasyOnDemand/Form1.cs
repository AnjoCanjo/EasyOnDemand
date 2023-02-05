using FlareSolverrSharp;
using LibVLCSharp.Shared;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace EasyOnDemand
{
	public partial class Form1 : Form
	{
		const double WaitTime = 10;
		HttpClient client = new HttpClient();
		private readonly LibVLC _libVLC = new LibVLC();
		bool prestazioni = false;

		public Form1()
		{
			InitializeComponent();
		}
		
		private async void button1_Click(object sender, EventArgs e)
		{
			string urlBase = "https://streamingcommunity.cafe/";
			Clean();
			button1.Enabled = false;
			string searchTerm = textBox1.Text;
			// Sostituisci i caratteri non ammessi nella query con un valore adeguato
			searchTerm = WebUtility.UrlEncode(searchTerm);

			// Costruisci l'URL della query
			string query = string.Format(urlBase + "search?q={0}", searchTerm);
			// Parametri Driver Selenium per nascondere Chromne e terminale
			using IWebDriver driver = Silent();
			// Naviga all'URL
			
			driver.Navigate().GoToUrl(query);

			// Aspetta fino a quando il contenuto viene caricato
			var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(WaitTime));

			var searchNode = wait.Until(driver => driver.FindElement(By.XPath("//div[@id='search']")));
			ImageList copertine = new ImageList();
			copertine.ImageSize = new Size(250, 200);
			copertine.ColorDepth = ColorDepth.Depth32Bit;
			listView1.LargeImageList = copertine;
			int count = 0;
			Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\img");
			foreach (IWebElement linkNode in wait.Until(driver => searchNode.FindElements(By.TagName("a"))))
			{
				// Estrai il valore dell'attributo href
				string link = linkNode.GetAttribute("href");
				string patternGetTitleNumber = @"(\d+)";
				Regex regexGetTitleNumber = new Regex(patternGetTitleNumber);
				Match matchGetTitleNumber = regexGetTitleNumber.Match(link);
				if (matchGetTitleNumber.Success)
				{
					int titleNumber = int.Parse(matchGetTitleNumber.Value);
					if(!File.Exists(System.Environment.CurrentDirectory + "\\img\\" + titleNumber.ToString() + ".jpg"))
					{
						var imageNode = wait.Until(driver => linkNode.FindElement(By.XPath("img[@class='tile-image']")));
						var linkImage = imageNode.GetAttribute("src");
						await SaveFile(linkImage, System.Environment.CurrentDirectory + "\\img\\" + titleNumber.ToString() + ".jpg");
					}
					copertine.Images.Add("", new Bitmap(System.Environment.CurrentDirectory + "\\img\\" + titleNumber.ToString() + ".jpg"));
					ListViewItem item = new ListViewItem();
					item.ImageIndex = count++;
					item.Text = string.Format(urlBase + "watch/{0}", titleNumber); //Da mettere un titolo vero, magari cercando con imbd o tmdb
					listView1.Items.Add(item);
				}
			}
			button1.Enabled = true;
		}

		
		private async void button2_Click(object sender, EventArgs e)
		{
			string masterBase = "https://scws.work/master/";
			string videosBase = "https://scws.work/videos/";
			string info = string.Empty; //file(solo da leggere)
			string tokenM3U8 = string.Empty; //file(da scaricare)
			List<string> resolutions = new List<string>();
			List<string> videosM3U8 = new List<string>(); //file(da scaricare
			List<string> additionalSubPaths = new List<string>();
			button1.Enabled = false;
			button2.Enabled = false;
			listView1.Enabled = false;
			IWebDriver driver = SilentLog();
			driver.Navigate().GoToUrl(listView1.SelectedItems[0].Text);
			for (int notFound = 0; notFound < 5; notFound++)
			{
				var networkRequests = driver.Manage().Logs.GetLog("performance");
				string patternToken = @"(\d+)\?token=.*expires=(\d+)";
				string patternInfo = @"(\d+)";
				string patternResolution = @"\d{3,4}p";
				string patternAdditional = @"(\/.*\/)|(\/)";
				Regex regexToken = new Regex(patternToken);
				Regex regexInfo = new Regex(patternInfo);
				Regex regexResolution = new Regex(patternResolution);
				Regex regexAdditional = new Regex(patternAdditional);
				foreach (var entry in networkRequests)
				{
					if (entry.Message.Contains("?token="))
					{
						Match match = regexToken.Match(entry.Message);
						if (match.Success)
						{
							tokenM3U8 = masterBase + match.Value;
							Match searchInfo = regexInfo.Match(match.Value);
							if (searchInfo.Success)
							{
								info = videosBase + searchInfo.Groups[0].Value;
							}
							await SaveFile(tokenM3U8, System.Environment.CurrentDirectory + "\\token.m3u8");
							string tokenPath = System.Environment.CurrentDirectory + "\\token.m3u8";
							if (File.Exists(tokenPath))
							{
								string[] lines = File.ReadAllLines(tokenPath);
								for (int i = 0; i < lines.Length; i++)
								{
									if (!lines[i].StartsWith("#"))
									{
										Match searchResolution = regexResolution.Match(lines[i]);
										if (searchResolution.Success)
										{
											resolutions.Add(searchResolution.Value);
										}
										Match searchAdditional = regexAdditional.Match(lines[i]);
										if (searchAdditional.Success)
										{
											additionalSubPaths.Add(searchAdditional.Groups[0].Value);
										}
									}
								}
								File.Delete(System.Environment.CurrentDirectory + "\\token.m3u8");
							}
							else
							{
								Debug.WriteLine("File non trovato");
							}
						}
					}
				}
				if (tokenM3U8 == string.Empty)
				{
					notFound++;
					Debug.WriteLine("Link non trovato");
					System.Threading.Thread.Sleep(1000); // Attendere 1 secondo
				}
				else
				{
					resolutions = resolutions.Distinct().ToList();
					additionalSubPaths = additionalSubPaths.Distinct().ToList();
					if(additionalSubPaths.First() == "/") //Vecchio stile di directory
					{
						foreach (var i in resolutions)
						{
							int index = tokenM3U8.IndexOf("token=");
							string newString = "type=video&rendition={0}.m3u8&";
							newString = string.Format(newString, i);
							videosM3U8.Add(tokenM3U8.Insert(index, newString));
						}
					}
					else
					{
						foreach (var i in resolutions)
						{
							int index = tokenM3U8.IndexOf("token=");
							string newString = "type=video&rendition={0}&";
							newString = string.Format(newString, i);
							videosM3U8.Add(tokenM3U8.Insert(index, newString));
						}
					}
					

					Debug.WriteLine("Link Token: " + tokenM3U8);
					Debug.WriteLine("Link Info: " + info);
					foreach (var i in resolutions)
					{
						Debug.WriteLine("Risoluzione: " + i);
					}
					foreach (var i in videosM3U8)
					{
						Debug.WriteLine("Nome file video: " + i);
					}
					
					foreach (var i in additionalSubPaths)
					{
						Debug.WriteLine("Sotto-directory: " + i);
					}
					break;
				}
				if (notFound > 5)
				{
					Debug.WriteLine("Troppi tentativi senza corrispondenze");
					return;
				}
			}
			int indexChoise = 0;
			for(int i = 1; i < resolutions.Count(); i++)
			{
				if (prestazioni)
				{
					if (int.Parse(resolutions.ElementAt(i).Replace("p", "")) > int.Parse(resolutions.ElementAt(indexChoise).Replace("p", "")))
					{
						indexChoise = i;
					}
				}
				else
				{
					if (int.Parse(resolutions.ElementAt(i).Replace("p", "")) < int.Parse(resolutions.ElementAt(indexChoise).Replace("p", "")))
					{
						indexChoise = i;
					}
				}
			}
			Debug.WriteLine(resolutions.ElementAt(indexChoise));
			string key = "https://scws.work/storage/enc.key"; //file((non da scaricare)
			int numerOfServer = 0;
			int proxyIndex = 0;
			int storageNumber = 0;
			string host = string.Empty;
			string folderId = string.Empty;
			char type = (char)0;
			int typeNumber = 0;
			string jsonString = string.Empty;
			string filePath = System.Environment.CurrentDirectory + "\\video.m3u8";
			using HttpResponseMessage response = await client.GetAsync(info);
			using HttpContent content = response.Content;
			jsonString = await content.ReadAsStringAsync();

			dynamic jsonData = JsonConvert.DeserializeObject(jsonString)!;
			type = jsonData.cdn.type;
			typeNumber = jsonData.cdn.number;
			host = jsonData.host;
			storageNumber = jsonData.storage.number;
			folderId = jsonData.folder_id;
			var proxies = jsonData["cdn"]["proxies"];
			numerOfServer = proxies[proxies.Count - 1]["number"];
			proxyIndex = jsonData.proxy_index;
			await SaveFile(videosM3U8.ElementAt(indexChoise), filePath);
			Debug.WriteLine(videosM3U8.ElementAt(indexChoise));
			Debug.WriteLine(resolutions.ElementAt(indexChoise));

			if (File.Exists(filePath))
			{
				string[] lines = File.ReadAllLines(filePath);

				string incrementalNOS = (proxyIndex + 1).ToString();

				for (int i = 0; i < lines.Length; i++)
				{
					if (int.Parse(incrementalNOS) < 10)
					{
						incrementalNOS = (0).ToString() + int.Parse(incrementalNOS).ToString();
					}
					if (!lines[i].StartsWith("#"))
					{
						lines[i] = "https://sc-" + type + typeNumber + "-" + incrementalNOS + "." + host + "/hls/" + storageNumber + "/" + folderId + additionalSubPaths.ElementAt(indexChoise) + lines[i];
						incrementalNOS = (int.Parse(incrementalNOS) + 1).ToString();

						if (int.Parse(incrementalNOS) == (numerOfServer + 1))
						{
							incrementalNOS = "1";
						}
					}
					else
					{
						if (lines[i].Contains("URI=\"/storage/enc.key\""))
						{
							string patternKey = "URI=\"/storage/enc.key\"";
							string replacementKey = "URI=\"" + key + "\"";
							lines[i] = Regex.Replace(lines[i], patternKey, replacementKey);
						}
					}
				}

				File.WriteAllLines(filePath, lines);
			}
			else
			{
				Debug.WriteLine("File non trovato");
			}
			var media = new Media(_libVLC, filePath);
			var mp = new MediaPlayer(media);
			Form2 playVideo = new Form2(this, mp);
			this.Hide();
			playVideo.Show();
			button1.Enabled = true;
			if(listView1.SelectedItems.Count != 0) 
			{
				button2.Enabled = true;
			}
			listView1.Enabled = true;
		}

		private void listView1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView1.SelectedItems.Count != 0 && (radioButton1.Checked != false || radioButton2.Checked != false))
			{
				button2.Enabled = true;
				Debug.WriteLine(listView1.SelectedItems[0].Text);
			}
			else
			{
				button2.Enabled = false;
			}
		}

		private async Task SaveFile(string fileUrl, string pathToSave)
		{
			var httpClient = new HttpClient();
			var httpResult = await httpClient.GetAsync(fileUrl);
			using var resultStream = await httpResult.Content.ReadAsStreamAsync();
			using var fileStream = File.Create(pathToSave);
			resultStream.CopyTo(fileStream);
		}
		private void Clean()
		{
			listView1.Clear();
			button2.Enabled = false;
		}

		private ChromeDriver Silent()
		{
			new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
			var driverService = ChromeDriverService.CreateDefaultService();
			driverService.HideCommandPromptWindow = true;
			var options = new ChromeOptions();
			options.AddArgument("--headless");

			return new ChromeDriver(driverService, options);
		}
		private ChromeDriver SilentLog()
		{
			new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
			var driverService = ChromeDriverService.CreateDefaultService();
			driverService.HideCommandPromptWindow = true;
			var options = new ChromeOptions();
			options.AddArgument("--headless");
			options.SetLoggingPreference("performance", OpenQA.Selenium.LogLevel.All);
			return new ChromeDriver(driverService, options);
		}

		private void radioButton2_CheckedChanged(object sender, EventArgs e)
		{
			prestazioni = true;
			if (listView1.SelectedItems.Count != 0)
			{
				button2.Enabled = true;
				Debug.WriteLine(listView1.SelectedItems[0].Text);
			}
			else
			{
				button2.Enabled = false;
			}
		}

		private void radioButton1_CheckedChanged(object sender, EventArgs e)
		{
			prestazioni = false;
			if (listView1.SelectedItems.Count != 0)
			{
				button2.Enabled = true;
				Debug.WriteLine(listView1.SelectedItems[0].Text);
			}
			else
			{
				button2.Enabled = false;
			}
		}
	}


















































	//ClearanceHandlerSample.SampleGet("Sito oscurato").Wait();
	//ClearanceHandlerSample.SamplePostUrlEncoded("Sito oscurato").Wait();
	public static class ClearanceHandlerSample
	{

		public static string FlareSolverrUrl = "http://localhost:8191/";

		public static async Task SampleGet(string protectedUrl)
		{
			var handler = new ClearanceHandler(FlareSolverrUrl)
			{
				MaxTimeout = 60000
			};

			var client = new HttpClient(handler);
			var content = await client.GetStringAsync(protectedUrl);
			Console.WriteLine(content);
		}

		public static async Task SamplePostUrlEncoded(string protectedUrl)
		{
			var handler = new ClearanceHandler(FlareSolverrUrl)
			{
				MaxTimeout = 60000
			};

			var request = new HttpRequestMessage();
			request.Headers.ExpectContinue = false;
			request.RequestUri = new Uri(protectedUrl);
			var postData = new Dictionary<string, string> { { "story", "test" } };
			request.Content = FormUrlEncodedContentWithEncoding(postData, Encoding.UTF8);
			request.Method = HttpMethod.Post;

			var client = new HttpClient(handler);
			var content = await client.SendAsync(request);
			Console.WriteLine(content);
		}

		static ByteArrayContent FormUrlEncodedContentWithEncoding(IEnumerable<KeyValuePair<string, string>> nameValueCollection, Encoding encoding)
		{
			// utf-8 / default
			if (Encoding.UTF8.Equals(encoding) || encoding == null)
				return new FormUrlEncodedContent(nameValueCollection);

			// other encodings
			var builder = new StringBuilder();
			foreach (var pair in nameValueCollection)
			{
				if (builder.Length > 0)
					builder.Append('&');
				builder.Append(HttpUtility.UrlEncode(pair.Key, encoding));
				builder.Append('=');
				builder.Append(HttpUtility.UrlEncode(pair.Value, encoding));
			}
			// HttpRuleParser.DefaultHttpEncoding == "latin1"
			var data = Encoding.GetEncoding("latin1").GetBytes(builder.ToString());
			var content = new ByteArrayContent(data);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			return content;
		}

	}


}