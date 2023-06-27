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
		/// Da li je trenutno na ekranu prikazan javascript Alert
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
		/// Zatvori trenuno prikazani Javascript Alert i vrati njegov tekst
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
		/// Čeka da završi jQuery Ajax request
		/// </summary>
		public void WaitForJQueryAjaxRequestToComplete()
		{
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(d => (bool)(d as IJavaScriptExecutor).ExecuteScript("return jQuery.active == 0"));
			Thread.Sleep(200);
		}

		/// <summary>
		/// Čekaj dok se element prikaže (postoji u html-u i vidljiv je), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Čekaj dok se element sakrije (postoji u html-u ali NIJE vidljiv), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Čekaj dok se element sakrije (postoji u html-u ali NIJE vidljiv), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Da li je element sakrije (postoji u html-u ali NIJE vidljiv je), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Čekaj dok se element prikaže (postoji u html-u i vidljiv je), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Da li je element prikazan (postoji u html-u i vidljiv je), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Da li je element prikazan (postoji u html-u i vidljiv je)
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
		/// Da li element postoji u html-u
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
		/// Da li je element prikazan (postoji u html-u i vidljiv je), omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// Čeka element da bude prisutan u html-u i klikne na istoga, omogućava i parametriziranje maksimalnog vremena koje se čeka
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
		/// selektira opciju iz html elementa Select
		/// </summary>
		/// <param name="select"></param>
		/// <param name="value"></param>
		public void ClickSelectOptionByValue(By select, string value)
		{
			var selectElement = this.driver.FindElement(select);
			if (selectElement == null)
			{
				throw new Exception("Could not find select element !");
			}
			var options = selectElement.FindElements(By.TagName("Option"));
			var myOption = options.FirstOrDefault(e => e.GetAttribute("value") == value);
			if (myOption == null)
			{
				throw new Exception("Could not find select option with value: " + value);
			}
			myOption.Click();
		}

		/// <summary>
		/// Mijenja stanje checkbox-a
		/// </summary>
		/// <param name="checkbox"></param>
		public void ToggleCheckBox(By checkbox)
		{
			SetText(checkbox, " ");
		}

		/// <summary>
		/// Selektira Checkbox
		/// </summary>
		/// <param name="checkbox"></param>
		public void ResetCheckBox(By checkbox)
		{
			if (CheckBoxChecked(checkbox))
			{
				SetText(checkbox, " ");
			}
		}

		/// <summary>
		/// Selektira Checkbox
		/// </summary>
		/// <param name="checkbox"></param>
		public void SetCheckBox(By checkbox)
		{
			if (!CheckBoxChecked(checkbox))
			{
				SetText(checkbox, " ");
			}
		}

		/// <summary>
		/// Provjerava da li je checkbox checked
		/// </summary>
		/// <param name="listitem"></param>
		/// <returns></returns>
		public bool CheckBoxChecked(By checkbox)
		{
			var element = GetElement(checkbox);
			string elementClass = element.GetAttribute("class");
			var classes = elementClass.Split(' ').ToList();
			return classes.Contains("on");
		}

		/// <summary>
		/// Selektira Radio item unutar Radio grupe
		/// </summary>
		/// <param name="itemId"></param>
		public void SetRadioItem(By itemId)
		{
			SetText(itemId, " ");
		}

		/// <summary>
		/// Postavlja datum PickADate Date pickeru
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
		/// Postavlja vrijednost html elementu iz parametra
		/// </summary>
		/// <param name="by"></param>
		/// <param name="value"></param>
		public void SetValue(By by, string value)
		{
			SetAttribute(by, "value", value);
		}

		/// <summary>
		/// Vraća vrijednost html elementa iz parametra
		/// </summary>
		/// <param name="by"></param>
		/// <returns></returns>
		public string GetValue(By by)
		{
			var element = this.driver.FindElement(by);
			return element.GetAttribute("value");
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

		#region ASP.NET Web Forms helper metode
		private string formPrefix = "";

		/// <summary>
		/// Vraća vrijednost kontrole (po id-u) iz ASP.NET forme bez potrebe za ctlx_ prefiksom koji ovisi o parent kontrolama i podložan je promjenama
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public By FormControlById(string id)
		{
			// tražim prvu kontrolu moje forme
			if (string.IsNullOrEmpty(this.formPrefix))
			{
				for (int i = 1; i < 100; i++)
				{
					string ctlpfx = $"ctl{i}";
					var ctl = By.Id(ctlpfx + "_" + id);
					if (IsElementPresent(ctl))
					{
						this.formPrefix = ctlpfx;
						break;
					}
				}
			}

			// ako je još uvijek formPrefix prazan znači da nisam našao
			if (string.IsNullOrEmpty(this.formPrefix))
			{
				throw new Exception($"Form control {id} not found.");
			}
			return By.Id(this.formPrefix + "_" + id);
		}

		/// <summary>
		/// Vraća vrijednost kontrole (po name-u) iz ASP.NET forme bez potrebe za ctlx_ prefiksom koji ovisi o parent kontrolama i podložan je promjenama
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public By FormControlByName(string name)
		{
			// tražim prvu kontrolu moje forme
			if (string.IsNullOrEmpty(this.formPrefix))
			{
				for (int i = 1; i < 100; i++)
				{
					string ctlpfx = $"ctl{i}";
					var ctl = By.Name(ctlpfx + "$" + name);
					if (IsElementPresent(ctl))
					{
						this.formPrefix = ctlpfx;
						break;
					}
				}
			}

			// ako je još uvijek formPrefix prazan znači da nisam našao
			if (string.IsNullOrEmpty(this.formPrefix))
			{
				throw new Exception($"Form control {name} not found.");
			}
			return By.Name(this.formPrefix + "$" + name);
		}

		#endregion

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