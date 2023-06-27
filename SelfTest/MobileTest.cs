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

namespace SeleniumWebDriverTools.SelfTest
{

	[TestClass]
	[MobileEmulation]
	public class MobileTest : BaseUiTest.BaseUiTest
	{
		[TestMethod]
		public void BaseSelfTest()
		{
			GoToUrl("/");
			Assert.IsTrue(true);
		}
	}
}
