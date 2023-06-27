using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SeleniumWebDriverTools.BaseUiTest
{
	public class Utils
	{
		public static string GenerateRandomOIB()
		{
			string oib = "";
			int a = 10;
			for (int i = 0; i < 10; i++)
			{
				string digit = RandomNumberGenerator.GetInt32(0, 9).ToString();
				oib += digit;
				a = a + Convert.ToInt32(digit);
				a = a % 10;
				if (a == 0) a = 10;
				a *= 2;
				a = a % 11;
			}
			int check = 11 - a;
			if (check == 10) check = 0;
			return oib + check.ToString();
		}

		public static string SanatizeForUrl(string raw)
		{
			string result = raw?.Trim().ToLower() ?? "";
			while ((result.IndexOf(" ") > 0))
			{
				result = result.Replace(" ", "-");
			}

			while ((result.IndexOf("--") > 0))
			{
				result = result.Replace("--", "-");
			}

			result = result.Replace(" ", "-");

			result = Transliteration.ToLatin(result);

			string whiteList = "abcdefghijklmnopqrstuvwxyz-0123456789";
			string checkedResult = string.Empty;
			foreach (char urlChar in result.ToCharArray())
			{
				if (whiteList.Contains(urlChar.ToString()))
				{
					checkedResult += urlChar.ToString();
				}
			}

			if (checkedResult.StartsWith("-"))
			{
				checkedResult = checkedResult.Substring(1);
			}
			if (checkedResult.EndsWith("-"))
			{
				checkedResult = checkedResult.Substring(0, checkedResult.Length - 1);
			}
			return checkedResult;
		}

	}

	public static class Transliteration
	{
		public static string ToLatin(string foreign)
		{
			string result = foreign;

			var allForeign = "ç,Á,á,É,é,Í,í,Ó,ó,Ő,ő,Ú,ú,Ű,ű,Ö,ö,Ü,Ä,ü,Ą,Ć,Ę,Ł,Ń,Ó,Ś,Ź,Ż,ą,ć,ę,ł,ń,ó,ś,ź,ż,ć,č,š,đ,ž,Č,Ć,Š,Đ,Ž,Ä,Ü,Ö,ä,ü,ö,ß,ẞ".Split(',');
			var allLatin = "c,A,a,E,e,I,i,O,o,O,o,U,u,U,u,OE,oe,UE,AE,ue,A,C,E,L,N,O,S,Z,Z,a,c,e,l,n,o,s,z,z,c,c,s,dj,z,C,C,S,DJ,Z,AE,UE,OE,ae,ue,oe,ss,ss".Split(',');

			for (int xx = 0; xx < allForeign.Length; xx++)
			{
				result = result.Replace(allForeign[xx].Trim(), allLatin[xx].Trim());
			}
			return result;
		}
	}
}



