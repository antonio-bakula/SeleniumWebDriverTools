# SeleniumWebDriverTools

## Abstract Unit Test class that offers functionalities:

- captures screenshots of the full screen and the currently visible screen when a test fails, and attaches the files to the test so that they are visible in, for example, the DevOps test runner
- enables testing in mobile mode using the `[MobileEmulation]` attribute set on the test class
- methods that works with JavaScript alerts (`IsAlertPresent`, `CloseAlertAndGetItsText`)
- methods for testing checkboxes, i.e., HTML input type checkbox (`CheckBoxIsChecked`, `CheckBoxToggle`, `CheckBoxUncheck`, `CheckBoxCheck`)
- methods for testing HTML Select (`SelectClickOptionByValue`, `SelectGetSelectedOption`, `SelectGetValue`)
- methods for managing the scroll position of the browser (`GetScrollTop`, `SetScrollTop`)
- methods that return the screen size (`GetFullPageHeight`, `GetFullPageWidth`, `GetClientScreenHeight`, `GetClientScreenWidth`)
- method that waits for a jQuery Ajax request to complete (`WaitForJQueryAjaxRequestToComplete`)
- methods for executing JavaScript in the context of the tested page (`ExecuteNonReturnJS`, `ExecuteTypedReturnJS`, `ExecuteReturnJS`)
- methods that scroll the page to make an element visible (`ScrollIntoView`, `IsElementInView`)
- allows setting the browser window to specific coordinates, useful when you want it to appear on an additional monitor (Settings in *.runsettings file)
- many other minor functionalities, check the code :blush:

### Usage
To use it, you need to inherit from `BaseUiTest` abstract class.

Desktop test example:
```
[TestClass]
public class DesktopTest : BaseUiTest.BaseUiTest
{
  [TestMethod]
  public void BaseSelfTest()
  {
    GoToUrl("/");
    Assert.IsTrue(true);
  }
}
```

Mobile test example:

```
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
```

### Supported settings in *.runsettings file:

**webAppUrl**: test web page host, use different *.runsettings files to test various instances of one site (eg. test, staging, production)

**culture**: sets culture for executed test (sets `CultureInfo.DefaultThreadCurrentCulture` and `CultureInfo.DefaultThreadCurrentUICulture`)

**currencySymbol**: sets currency for executed test 

**desktopWindow**: sets browser position and size in desktop mode, JSON with parameters: X, Y, Width, Height

**mobileWindow**: sets browser position and size mobile mode, JSON with parameters: X, Y, Width, Height

**mobileUserAgent**: sets browser UserAgent in mobile mode
Example:
```
<TestRunParameters>
  <Parameter name="webAppUrl" value="https://www.antoniob.com" /> 
  <Parameter name="culture" value="hr-HR" />	 
  <Parameter name="currencySymbol" value="€" />
  <Parameter name="desktopWindow" value="{&quot;X&quot;:2050, &quot;Y&quot;:1460, &quot;Width&quot;:1440, &quot;Height&quot;:800}" /> 
  <Parameter name="mobileWindow" value="{&quot;X&quot;:2050, &quot;Y&quot;:1460, &quot;Width&quot;:500, &quot;Height&quot;:800}" /> 
  <Parameter name="mobileUserAgent" value="Mozilla/5.0 (Linux; Android 9;) AppleWebKit/537.36 (KHTML, like Gecko)  Chrome/88.0.4324.152 Mobile Safari/537.36" /> 
</TestRunParameters>
```

### Code in Solution
There are 2 projects in the solution:

- `SeleniumWebDriverTools.BaseUiTest` a test class that defines the functionalities
- `SeleniumWebDriverTools.SelfTest` an example of usage for desktop and mobile tests (sets Chrome in mobile mode), and test for funcionality in BaseUiTest

### Drawbacks and future improvements:

- the current documentation is only in the code and some is in Croatian, create documentation in a separate file and translate it to English
- works only for Google Chrome, it would be nice to support Firefox
