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

		[TestMethod]
		public void TestCheckBoxMethods()
		{
			GoToUrl("/ui-test-page");
			
			// test is checked
			Assert.IsTrue(CheckBoxIsChecked(By.Id("cb1")));
			Assert.IsFalse(CheckBoxIsChecked(By.Id("cb2")));

			// test toggle
			CheckBoxToggle(By.Id("cb1"));
			Assert.IsFalse(CheckBoxIsChecked(By.Id("cb1")));
			CheckBoxToggle(By.Id("cb1"));
			Assert.IsTrue(CheckBoxIsChecked(By.Id("cb1")));

			// test uncheck
			CheckBoxUncheck(By.Id("cb1"));
			Assert.IsFalse(CheckBoxIsChecked(By.Id("cb1")));
			CheckBoxUncheck(By.Id("cb2"));
			Assert.IsFalse(CheckBoxIsChecked(By.Id("cb2")));

			// test check
			CheckBoxCheck(By.Id("cb1"));
			Assert.IsTrue(CheckBoxIsChecked(By.Id("cb1")));
			CheckBoxCheck(By.Id("cb2"));
			Assert.IsTrue(CheckBoxIsChecked(By.Id("cb2")));

		}

		[TestMethod]
		public void TestGetAndSetValue()
		{
			GoToUrl("/ui-test-page");

			Assert.AreEqual<string>("Input value", GetValue(By.Id("input1")));
			SetValue(By.Id("input1"), "My new value");
			Assert.AreEqual<string>("My new value", GetValue(By.Id("input1")));
		}

	}
}
