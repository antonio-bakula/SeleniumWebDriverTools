using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using OpenQA.Selenium.Chromium;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SeleniumWebDriverTools.BaseUiTest
{

	public class MobileEmulationAttribute : Attribute { }

	[TestClass]
	public abstract class BaseUiTest
	{
		protected TestContext testContextInstance;
		protected IWebDriver driver;
		protected string baseURL;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return this.testContextInstance;
			}
			set
			{
				this.testContextInstance = value;
			}
		}


		[TestInitialize]
		public void InitializeBaseUiTest()
		{
			// Test Explorer -> Settings -> Configure Run Settings -> Select Solution Wide runsettings file - u fileu se definira varijabla "webAppUrl"
			/*
			<TestRunParameters>
			<Parameter name="webAppUrl" value="http://localhost" />
			<Parameter name="culture" value="hr-HR" />
			<Parameter name="currencySymbol" value="€" />
			<Parameter name="desktopWindow" value="{&quot;X&quot;:2050, &quot;Y&quot;:1460, &quot;Width&quot;:1440, &quot;Height&quot;:800}" />
			<Parameter name="mobileWindow" value="{&quot;X&quot;:2050, &quot;Y&quot;:1460, &quot;Width&quot;:500, &quot;Height&quot;:900}" />
			<Parameter name="mobileUserAgent" value="Mozilla/5.0 (Linux; Android 9;) AppleWebKit/537.36 (KHTML, like Gecko)  Chrome/88.0.4324.152 Mobile Safari/537.36" />
			</TestRunParameters>
			*/
			this.baseURL = (string)this.TestContext.Properties["webAppUrl"];
			if (string.IsNullOrEmpty(baseURL))
			{
				throw new Exception("Base URL not defined !");
			}

			string culture = (string)this.TestContext.Properties["culture"];
			if (!string.IsNullOrEmpty(culture))
			{
				var cultureInfo = new CultureInfo(culture);
				string currencySymbol = (string)this.TestContext.Properties["currencySymbol"];
				if (!string.IsNullOrEmpty(currencySymbol))
				{
					cultureInfo.NumberFormat.CurrencySymbol = "€";
				}

				CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
				CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
			}

			var windowSettings = GetUiTestSettings();
			var options = new ChromeOptions();
			options.AcceptInsecureCertificates = true;
			var mobileEmuAttribute = this.GetType().GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(MobileEmulationAttribute));
			if (mobileEmuAttribute != null)
			{
				SetMobileChromeSettings(windowSettings.Mobile, options);
			}
			else
			{
				SetDesktopChromeSettings(windowSettings.Desktop, options);
			}
			this.driver = new ChromeDriver(Directory.GetCurrentDirectory(), options);
		}

		private void SetMobileChromeSettings(Rectangle? window, ChromeOptions options)
		{
			var deviceSettings = new ChromiumMobileEmulationDeviceSettings();
			deviceSettings.EnableTouchEvents = true;
			deviceSettings.PixelRatio = 2;

			string mobileUserAgent = (string)this.TestContext.Properties["mobileUserAgent"];
			if (!string.IsNullOrEmpty(mobileUserAgent))
			{
				deviceSettings.UserAgent = mobileUserAgent;
			}
			options.EnableMobileEmulation(deviceSettings);
			options.AddArguments("--enable-touch-events");
			options.AddArgument("--touch_view");
			if (window.HasValue)
			{
				deviceSettings.Width = window.Value.Width;
				deviceSettings.Height = window.Value.Height;
				options.AddArgument($"--window-size={window.Value.Width},{window.Value.Height}");
				options.AddArgument($"--window-position={window.Value.X},{window.Value.Y}");
			}
		}

		private void SetDesktopChromeSettings(Rectangle? window, ChromeOptions options)
		{
			if (window.HasValue)
			{
				options.AddArgument($"--window-size={window.Value.Width},{window.Value.Height}");
				options.AddArgument($"--window-position={window.Value.X},{window.Value.Y}");
			}
		}

		[TestCleanup()]
		public void CleanUpBaseUiTest()
		{
			try
			{
				CheckForLoggedErrorsAndRecordScreenshotToTestContext();

				if (this.driver != null)
				{
					this.driver.Quit();
				}
			}
			catch (Exception)
			{
				KillChromeDriverProcess();
			}
		}

		private static void KillChromeDriverProcess()
		{
			foreach (var process in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
			{
				process.Kill();
			}
		}

		private (Rectangle? Desktop, Rectangle? Mobile) GetUiTestSettings()
		{
			(Rectangle? Desktop, Rectangle? Mobile) result = (null, null);

			string desktopJson = (string)this.TestContext.Properties["desktopWindow"];
			if (!string.IsNullOrEmpty(desktopJson))
			{
				result.Desktop = JsonSerializer.Deserialize<Rectangle>(desktopJson);
			}

			string mobileJson = (string)this.TestContext.Properties["mobileWindow"];
			if (!string.IsNullOrEmpty(mobileJson))
			{
				result.Mobile = JsonSerializer.Deserialize<Rectangle>(mobileJson);
			}

			return result;
		}

		protected void CheckForLoggedErrorsAndRecordScreenshotToTestContext()
		{
			if (TestContext.CurrentTestOutcome == UnitTestOutcome.Error || TestContext.CurrentTestOutcome == UnitTestOutcome.Failed)
			{
				TestContext.WriteLine("Taking screenshot of failed test");
				TakeScreenshot(new DirectoryInfo(TestContext.TestDeploymentDir), false);
				TakeScreenshot(new DirectoryInfo(TestContext.TestDeploymentDir), true);
			}
		}

		#region Methods covered with tests

		/// <summary>
		/// Is the JavaScript alert currently displayed on the screen
		/// </summary>
		/// <returns></returns>
		protected bool IsAlertPresent()
		{
			try
			{
				this.driver.SwitchTo().Alert();
				return true;
			}
			catch (NoAlertPresentException)
			{
				return false;
			}
		}

		/// <summary>
		/// Close the currently displayed JavaScript alert and return its text
		/// </summary>
		/// <returns></returns>
		protected string CloseAlertAndGetItsText()
		{
			IAlert alert = this.driver.SwitchTo().Alert();
			string alertText = alert.Text;
			alert.Accept();
			return alertText;
		}

		/// <summary>
		/// Checks if Checkbox element is checked
		/// </summary>
		/// <param name="checkbox"></param>
		/// <returns></returns>
		public bool CheckBoxIsChecked(By checkbox)
		{
			var element = GetElement(checkbox);
			string checkedAttribute = element.GetAttribute("checked");
			return !string.IsNullOrEmpty(checkedAttribute);
		}

		/// <summary>
		/// Toggles the Checkbox
		/// </summary>
		/// <param name="checkbox"></param>
		public void CheckBoxToggle(By checkbox)
		{
			SetText(checkbox, " ");
		}

		/// <summary>
		/// Unchecks checkbox
		/// </summary>
		/// <param name="checkbox"></param>
		public void CheckBoxUncheck(By checkbox)
		{
			if (CheckBoxIsChecked(checkbox))
			{
				SetText(checkbox, " ");
			}
		}

		/// <summary>
		/// Sets Checkbox
		/// </summary>
		/// <param name="checkbox"></param>
		public void CheckBoxCheck(By checkbox)
		{
			if (!CheckBoxIsChecked(checkbox))
			{
				SetText(checkbox, " ");
			}
		}

		/// <summary>
		/// Set the value of the HTML element from the parameter
		/// </summary>
		/// <param name="by"></param>
		/// <param name="value"></param>
		public void SetValue(By by, string value)
		{
			SetAttribute(by, "value", value);
		}

		/// <summary>
		/// Returns the value of the HTML element from the parameter
		/// </summary>
		/// <param name="by"></param>
		/// <returns></returns>
		public string GetValue(By by)
		{
			var element = this.driver.FindElement(by);
			return element.GetAttribute("value");
		}

		/// <summary>
		/// clicks select option
		/// </summary>
		/// <param name="select"></param>
		/// <param name="value"></param>
		public void SelectClickOptionByValue(By select, string value)
		{
			var selectElement = this.driver.FindElement(select);
			Assert.IsNotNull(selectElement, "Method SelectClickOptionByValue: Select was not found");
			var options = selectElement.FindElements(By.TagName("Option"));
			var myOption = options.FirstOrDefault(e => e.GetAttribute("value") == value);
			Assert.IsNotNull(myOption, $"Method SelectClickOptionByValue: No option with value: {value}");
			myOption.Click();
		}

		public IWebElement SelectGetSelectedOption(By select)
		{
			var selectElement = this.driver.FindElement(select);
			Assert.IsNotNull(selectElement, "Method SelectClickOptionByValue: Select was not found");
			var options = selectElement.FindElements(By.TagName("Option"));
			return options.FirstOrDefault(e => e.GetAttribute("selected") == "true");
		}

		public string SelectGetValue(By select)
		{
			var selectedOption = SelectGetSelectedOption(select);
			if (selectedOption != null)
			{
				return selectedOption.GetAttribute("value");
			}
			return null;
		}

		#endregion

		public void TakeScreenshot(DirectoryInfo saveToFolder, bool fullPage = false)
		{
			try
			{
				string fileName = "screenshot_" + (fullPage ? "_fullpage_" : "") + TestContext.TestName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
				var imageFile = new FileInfo(Path.Combine(saveToFolder.FullName, fileName));
				if (!imageFile.Directory.Exists)
				{
					imageFile.Directory.Create();
				}

				if (fullPage)
				{
					var screenShots = new List<Screenshot>();
					int originalScrollTop = GetScrollTop();
					int pageHeight = GetFullPageHeight();
					int screenHeight = GetClientScreenHeight();
					try
					{
						int currentScrollPos = 0;
						for (int i = 0; i < pageHeight / screenHeight + 1; i++)
						{
							SetScrollTop(currentScrollPos);
							int test = GetScrollTop();

							var sshot = ((ITakesScreenshot)driver).GetScreenshot();
							screenShots.Add(sshot);
							currentScrollPos += screenHeight;
						}
					}
					finally
					{
						SetScrollTop(originalScrollTop);
					}

					var bitmaps = screenShots.Select(s => Image.Load(new MemoryStream(s.AsByteArray))).ToList();
					if (bitmaps.Any())
					{
						// zadnji screenshot ima i dio predzadnjeg, računam koliko je to
						Int64 lastScreenShotSize = pageHeight - ((pageHeight / screenHeight) * screenHeight);
						float dpi = (float)bitmaps.First().Height / (float)screenHeight;
						int lastBitmapSize = (int)((float)lastScreenShotSize * dpi);

						int fullHeight = bitmaps.Sum(b => b.Height) - bitmaps.Last().Height + lastBitmapSize;
						var fullBitmap = new Image<Rgba32>(bitmaps.First().Width, fullHeight);
						int currentTop = 0;

						for (int i = 0; i < bitmaps.Count; i++)
						{
							var img = bitmaps[i];
							if (i == bitmaps.Count - 1)
							{
								currentTop = currentTop - (img.Height - lastBitmapSize);
							}
							fullBitmap.Mutate(x => x.DrawImage(img, new Point(0, currentTop), 1));
							currentTop += img.Height;
						}
						fullBitmap.SaveAsPng(imageFile.FullName);
					}
				}
				else
				{
					Screenshot img = ((ITakesScreenshot)driver).GetScreenshot();
					img.SaveAsFile(imageFile.FullName);
				}
				TestContext.WriteLine($"Screenshot image saved to {imageFile.FullName}.");
				TestContext.AddResultFile(imageFile.FullName);
			}
			catch (Exception e)
			{
				TestContext.WriteLine("Get screenshot failed, exception: " + e.Message);
			}
		}

		/// <summary>
		/// Does the element exist, this method returns true if the element is in the HTML, regardless of whether it is visible or not.
		/// </summary>
		/// <param name="by"></param>
		/// <returns></returns>
		protected bool IsElementPresent(By by)
		{
			try
			{
				this.driver.FindElement(by);
				return true;
			}
			catch (NoSuchElementException)
			{
				return false;
			}
		}

		/// <summary>
		/// Returns ScrollTop property of web page (document scrollTop)
		/// </summary>
		/// <returns></returns>
		protected int GetScrollTop()
		{
			return Convert.ToInt32(ExecuteReturnJS("return document.documentElement.scrollTop"));
		}

		/// <summary>
		/// Sets ScrollTop property of web page (document scrollTop)
		/// </summary>
		/// <param name="scrollTop"></param>
		/// <returns></returns>
		protected bool SetScrollTop(int scrollTop)
		{
			return ExecuteNonReturnJS($"document.documentElement.scrollTop={scrollTop}");
		}

		/// <summary>
		/// Gets a Height of a full page (scrolled to the bottom)
		/// </summary>
		/// <returns></returns>
		protected int GetFullPageHeight()
		{
			return Convert.ToInt32(ExecuteReturnJS("return document.documentElement.scrollHeight"));
		}

		/// <summary>
		/// Gets a Width of a full page (scrolled to the right)
		/// </summary>
		/// <returns></returns>
		protected int GetFullPageWidth()
		{
			return Convert.ToInt32(ExecuteReturnJS("return document.documentElement.scrollWidth"));
		}

		/// <summary>
		/// Gets a height of a client screen
		/// </summary>
		/// <returns></returns>
		protected int GetClientScreenHeight()
		{
			return Convert.ToInt32(ExecuteReturnJS("return document.documentElement.clientHeight"));
		}

		/// <summary>
		/// Gets a width of a client screen
		/// </summary>
		/// <returns></returns>
		protected int GetClientScreenWidth()
		{
			return Convert.ToInt32(ExecuteReturnJS("return document.documentElement.clientWidth"));
		}

		/// <summary>
		/// Čeka da završi jQuery Ajax request
		/// </summary>
		public void WaitForJQueryAjaxRequestToComplete()
		{
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(d => (bool)(d as IJavaScriptExecutor).ExecuteScript("return jQuery.active == 0"));
			Thread.Sleep(200);
		}

		/// <summary>
		/// Wait until the element is displayed (exists in the HTML and is visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="by"></param>
		/// <param name="secondsToWait"></param>
		public void WaitForElementDisplayed(By by, int secondsToWait = 5)
		{
			if (!IsElementDisplayedWithWait(by, secondsToWait))
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("WaitForElementDisplayed failed");
			}
		}

		/// <summary>
		/// Wait until the element is displayed (exists in the HTML and is visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="element"></param>
		/// <param name="secondsToWait"></param>
		public void WaitForElementDisplayed(IWebElement element, int secondsToWait = 5)
		{
			if (!IsElementDisplayedWithWait(element, secondsToWait))
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("WaitForElementDisplayed failed");
			}
		}

		/// <summary>
		/// Wait until the element is hidden (exists in the HTML and is NOT visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="by"></param>
		/// <param name="secondsToWait"></param>
		public void WaitForElementNotDisplayed(By by, int secondsToWait = 5)
		{
			if (!IsElementNotDisplayedWithWait(GetElement(by), secondsToWait))
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("WaitForElementNotDisplayed failed");
			}
		}

		/// <summary>
		/// Wait until the element is hidden (exists in the HTML and is NOT visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="element"></param>
		/// <param name="secondsToWait"></param>
		public void WaitForElementNotDisplayed(IWebElement element, int secondsToWait = 5)
		{
			if (!IsElementNotDisplayedWithWait(element, secondsToWait))
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("WaitForElementNotDisplayed failed");
			}
		}

		/// <summary>
		/// Is element displayed (exists in the HTML and is visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="by"></param>
		/// <param name="secondsToWait"></param>
		/// <returns></returns>
		public bool IsElementDisplayedWithWait(By by, int secondsToWait = 5)
		{
			bool result = false;
			for (int second = 0; ; second++)
			{
				if (second >= secondsToWait)
				{
					Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("IsElementDisplayed: timeout");
				}
				try
				{
					result = IsElementDisplayed(by);
					if (result)
					{
						break;
					}
				}
				catch (Exception)
				{
				}
				Thread.Sleep(1000);
			}
			return result;
		}

		/// <summary>
		/// Is element displayed (exists in the HTML and is visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="element"></param>
		/// <param name="secondsToWait"></param>
		/// <returns></returns>
		public bool IsElementDisplayedWithWait(IWebElement element, int secondsToWait = 5)
		{
			bool result;
			for (int second = 0; ; second++)
			{
				if (second >= secondsToWait)
				{
					Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("IsElementDisplayedWithWait: timeout");
				}
				try
				{
					result = element.Displayed;
					if (result)
					{
						break;
					}
				}
				catch (Exception)
				{
				}
				Thread.Sleep(1000);
			}
			return result;

		}

		/// <summary>
		/// Is element hidden (exists in the HTML and is NOT visible), and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="element"></param>
		/// <param name="secondsToWait"></param>
		/// <returns></returns>
		public bool IsElementNotDisplayedWithWait(IWebElement element, int secondsToWait = 5)
		{
			bool result;
			for (int second = 0; ; second++)
			{
				if (second >= secondsToWait)
				{
					Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("IsElementNotDisplayedWithWait: timeout");
				}
				try
				{
					result = !element.Displayed;
					if (result)
					{
						break;
					}
				}
				catch (Exception)
				{
				}
				Thread.Sleep(1000);
			}
			return result;

		}

		/// <summary>
		/// Checks if element is displayed (exists in the HTML and is visible)
		/// </summary>
		/// <param name="by"></param>
		/// <returns></returns>
		protected bool IsElementDisplayed(By by)
		{
			try
			{
				var element = this.driver.FindElement(by);
				return element.Displayed;
			}
			catch (NoSuchElementException)
			{
				return false;
			}
		}

		/// <summary>
		/// Checks is element present in HTML
		/// </summary>
		/// <param name="by"></param>
		/// <param name="secondsToWait"></param>
		/// <returns></returns>
		public bool IsElementPresentWithWait(By by, int secondsToWait = 5)
		{
			bool result = false;
			for (int second = 0; ; second++)
			{
				if (second >= secondsToWait)
				{
					Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("timeout");
				}
				try
				{
					result = IsElementPresent(by);
					if (result)
					{
						break;
					}
				}
				catch (Exception)
				{
				}
				Thread.Sleep(1000);
			}
			return result;
		}

		/// <summary>
		/// Waits for element to be present in HTML and clicks it, and also allows parameterizing the maximum waiting time
		/// </summary>
		/// <param name="by"></param>
		/// <param name="secondsToWait"></param>
		/// <returns></returns>
		public bool WaitElementAndClickIt(By by, int secondsToWait = 5)
		{
			if (IsElementPresentWithWait(by, secondsToWait))
			{
				ClickElement(by);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Selects Radio item in RadioGroup
		/// </summary>
		/// <param name="itemId"></param>
		public void SetRadioItem(By itemId)
		{
			SetText(itemId, " ");
		}

		/// <summary>
		/// Sets date in PickADate Date picker (https://amsul.ca/pickadate.js/)
		/// </summary>
		/// <param name="pickadate"></param>
		/// <param name="date"></param>
		public void SetPickADateControlValue(By pickadate, DateTime date)
		{
			string strdate = date.ToUniversalTime().ToString("o");
			string pickerId = "#" + GetElementId(pickadate);
			string script = "$(arguments[0]).pickadate('picker').set('select', new Date(arguments[1]))";
			ExecuteNonReturnJS(script, pickerId, strdate);
		}

		/// <summary>
		/// Postavlja datoteku na upload u Upload kontrolu
		/// </summary>
		/// <param name="upload"></param>
		/// <param name="fileFullPath"></param>
		public void UploadFile(By upload, string fileFullPath)
		{
			var uploadElement = driver.FindElement(upload);
			uploadElement.SendKeys(fileFullPath);
		}

		/// <summary>
		/// Ide na relativni URL u sklopu testiranog site-a
		/// </summary>
		/// <param name="url"></param>
		public void GoToUrl(string url)
		{
			if (!this.baseURL.EndsWith("/") && !url.StartsWith("/"))
			{
				url = "/" + url;
			}
			this.driver.Navigate().GoToUrl(this.baseURL + url);
		}

		/// <summary>
		/// Ponovo učitava aktivnu stranicu
		/// </summary>
		public void RefreshCurrentPage()
		{
			this.driver.Navigate().Refresh();
		}

		/// <summary>
		/// Vraća title trenutno prikazane stranice
		/// </summary>
		/// <param name="input"></param>
		public string GetPageTitle()
		{
			return this.driver.Title;
		}

		/// <summary>
		/// Da li je html element iz parametra u vidljivom dijelu web stranice
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public bool IsElementInView(IWebElement element)
		{
			string js =
			@"
var rect = arguments[0].getBoundingClientRect();
return (rect.top >= 0 && rect.left >= 0 && rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) && rect.right <= (window.innerWidth || document.documentElement.clientWidth));
";
			return ExecuteTypedReturnJS<bool>(js, element);
		}

		/// <summary>
		/// Scroll-a web stranicu da element iz parametra dođe u vidljivi dio
		/// </summary>
		/// <param name="element"></param>
		public void ScrollIntoView(IWebElement element)
		{
			ExecuteNonReturnJS("arguments[0].scrollIntoView({behavior: 'auto', block: 'center', inline: 'center'})", element);
			for (int i = 0; i < 20; i++)
			{
				if (IsElementInView(element))
				{
					break;
				}
				Thread.Sleep(10);
			}
		}

		/// <summary>
		/// Scroll-a web stranicu da element iz parametra dođe u vidljivi dio
		/// </summary>
		/// <param name="by"></param>
		public void ScrollIntoView(By by)
		{
			var element = this.driver.FindElement(by);
			ScrollIntoView(element);
		}

		/// <summary>
		/// Postavlja tekst html elementu iz parametra
		/// </summary>
		/// <param name="element"></param>
		/// <param name="text"></param>
		public void SetText(By element, string text)
		{
			this.driver.FindElement(element).SendKeys(text);
		}

		/// <summary>
		/// Lijevi click miša na element iz parametra
		/// </summary>
		/// <param name="by"></param>
		public void ClickElement(By by)
		{
			var element = this.driver.FindElement(by);
			ScrollIntoView(element);
			for (int i = 0; i < 10; i++)
			{
				// neke stranice imaju neki svoj scroll kod load-a i taj makne naš element koji želimo kliknuti
				if (IsElementInView(element))
				{
					break;
				}
				else
				{
					ScrollIntoView(element);
					Thread.Sleep(20);
				}
			}
			ClickElementInternal(element, 1);
		}

		/// <summary>
		/// Lijevi click miša na element iz parametra
		/// </summary>
		/// <param name="element"></param>
		public void ClickElement(IWebElement element)
		{
			ScrollIntoView(element);
			ClickElementInternal(element, 1);
		}

		public IWebElement FindElementContainsAttrValue(By by, string attributeName, string attributeValue)
		{
			var elements = this.driver.FindElements(by);
			foreach (var el in elements)
			{
				var val = el.GetAttribute(attributeName);
				if (val != null && val.Contains(attributeValue))
				{
					return el;
				}
			}
			return null;
		}

		private void ClickElementInternal(IWebElement element, int tryCount)
		{
			// probam kliknuti više puta, nekada element nije spreman za click, pa da ne moram svuda stavljati pauze ...
			bool handledExceptionOccured = false;
			try
			{
				element.Click();
			}
			catch (Exception e)
			{
				if (e.GetType() == typeof(ElementClickInterceptedException))
				{
					handledExceptionOccured = true;
				}
				else
				{
					throw;
				}
			}

			if (handledExceptionOccured && tryCount < 10)
			{
				Thread.Sleep(50);
				ClickElementInternal(element, tryCount + 1);
			}
		}

		/// <summary>
		/// Izvršava Javascript u kontekstu testirane stranice, ne vraća rezultat. Parametri koji se šalju javascriptu se unutar skripte (parametar js) 
		/// postavljaju kao: arguments[0], arguments[1] ... arguments[x] ovisno o broju parametara.
		/// </summary>
		/// <param name="js"></param>
		/// <param name="jsparams"></param>
		/// <returns></returns>
		public bool ExecuteNonReturnJS(string js, params object[] jsparams)
		{
			bool execSuccess;
			try
			{
				((IJavaScriptExecutor)this.driver).ExecuteScript(js, jsparams);
				execSuccess = true;
			}
			catch (Exception)
			{
				execSuccess = false;
			}


			return execSuccess;
		}

		/// <summary>
		/// Izvršava Javascript u kontekstu testirane stranice, i vraća typed rezultat. Parametri koji se šalju javascriptu se unutar skripte (parametar js) 
		/// postavljaju kao: arguments[0], arguments[1] ... arguments[x] ovisno o broju parametara.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="jsFunction"></param>
		/// <param name="jsparams"></param>
		/// <returns></returns>
		public T ExecuteTypedReturnJS<T>(string jsFunction, params object[] jsparams)
		{
			object result = ExecuteReturnJS(jsFunction, jsparams);
			return (T)result;
		}

		/// <summary>
		/// Izvršava Javascript u kontekstu testirane stranice, i vraća object rezultat. Parametri koji se šalju javascriptu se unutar skripte (parametar js) 
		/// postavljaju kao: arguments[0], arguments[1] ... arguments[x] ovisno o broju parametara.
		/// </summary>
		/// <param name="jsFunction"></param>
		/// <param name="jsparams"></param>
		/// <returns></returns>
		public object ExecuteReturnJS(string jsFunction, params object[] jsparams)
		{
			IJavaScriptExecutor js = (IJavaScriptExecutor)this.driver;
			object _result = null;
			try
			{
				_result = js.ExecuteScript(jsFunction, jsparams);
			}
			catch (Exception)
			{
				throw;
			}

			return _result;
		}

		public void TriggerChangeEvent(string elementId)
		{
			ExecuteNonReturnJS($"document.getElementById(arguments[0]).dispatchEvent(new Event('change'))", elementId);
		}

		public void TriggerChangeEvent(By by)
		{
			var element = this.driver.FindElement(by);
			TriggerChangeEvent(element);
		}

		public void TriggerChangeEvent(IWebElement element)
		{
			TriggerChangeEvent(element.GetAttribute("id"));
		}

		/// <summary>
		/// Postavlja vrijednost atributa html elementu iz parametra
		/// </summary>
		/// <param name="by"></param>
		/// <param name="attributeName"></param>
		/// <param name="attributeValue"></param>
		public void SetAttribute(By by, string attributeName, string attributeValue)
		{
			var element = this.driver.FindElement(by);
			string script = @"
el = document.getElementById(arguments[0]); 
el.setAttribute(arguments[1], arguments[2]);
";
			ExecuteNonReturnJS(script, element.GetAttribute("id"), attributeName, attributeValue);
		}

		/// <summary>
		/// Briše atribut html elementu iz parametra
		/// </summary>
		/// <param name="by"></param>
		/// <param name="attributeName"></param>
		public void RemoveAttribute(By by, string attributeName)
		{
			var element = this.driver.FindElement(by);
			string script = @"
el = document.getElementById(arguments[0]); 
el.removeAttribute(arguments[1]);
";
			ExecuteNonReturnJS(script, element.GetAttribute("id"), attributeName);
		}

		/// <summary>
		/// Traži child html element koji sadrži tekst iz parametra
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public IWebElement FindChildElementByText(IWebElement parent, string text)
		{
			return parent.FindElement(By.XPath($".//*[text()='{text}']"));
		}

		/// <summary>
		/// Vraća html element iz By parametra kao IWebElement rezultat
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public IWebElement GetElement(By element)
		{
			return driver.FindElement(element);
		}

		/// <summary>
		/// Vraća id elementa
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public string GetElementId(By element)
		{
			return this.driver.FindElement(element).GetAttribute("id");
		}

		public IWebElement GetColorBoxElementByClassName(string className)
		{
			var elements = this.driver.FindElements(By.ClassName(className));
			foreach (var element in elements)
			{
				if (element.Displayed)
				{
					return element;
				}
			}
			return null;
		}

		public void ClickColorBoxElementByClassName(string className)
		{
			var element = GetColorBoxElementByClassName(className);
			if (element != null)
			{
				ClickElement(element);
			}
		}
	}

	public static class IWebElementExtender
	{
		public static IWebElement GetParent(this IWebElement element)
		{
			return element.FindElement(By.XPath(".."));
		}

		public static ReadOnlyCollection<IWebElement> GetChildren(this IWebElement element)
		{
			return element.FindElements(By.XPath("child::*"));
		}

		/// <summary>
		/// Traži child html element koji sadrži tekst iz parametra
		/// </summary>
		/// <param name="element"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public static IWebElement FindElementByText(this IWebElement element, string text)
		{
			return element.FindElement(By.XPath($".//*[text()='{text}']"));
		}

		public static string Class(this IWebElement element)
		{
			return element.GetAttribute("class");
		}

		public static bool HasClass(this IWebElement element, string className)
		{
			var classList = element.GetAttribute("class").Trim().Split(' ');
			return classList.Contains(className);
		}
	}

	public class UiTestSettings
	{
		public Rectangle Desktop { get; set; }
		public Rectangle Mobile { get; set; }
	}

}