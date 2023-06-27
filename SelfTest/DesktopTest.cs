using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using SeleniumWebDriverTools.BaseUiTest;
using System.IO;

namespace SeleniumWebDriverTools.SelfTest
{

	[TestClass]
	public class DesktopTest : BaseUiTest.BaseUiTest
	{
		[TestMethod]
		public void BaseSelfTest()
		{
			GoToUrl("/contact");
			Assert.IsTrue(true);
		}

		[TestMethod]
		public void TestJavascriptAlertHandling()
		{
			GoToUrl("/");
			string alertText = "RED Alert !";
			ExecuteNonReturnJS($"alert('{alertText}')");
			bool alertPresent = IsAlertPresent();
			Assert.IsTrue(alertPresent);
			string actualAlertText = CloseAlertAndGetItsText();
			Assert.AreEqual<string>(alertText, actualAlertText);
			alertPresent = IsAlertPresent();
			Assert.IsFalse(alertPresent);
		}

	}
}
