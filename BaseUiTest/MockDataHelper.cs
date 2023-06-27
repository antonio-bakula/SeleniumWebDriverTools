using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace SeleniumWebDriverTools.BaseUiTest
{
	public static class MockDataHelper
	{
		public static List<MockUser> CreateMockUsers(int count)
		{
			var users = GetMockData();
			var result = new List<MockUser>();
			for (int i = 0; i < count; i++)
			{
				string firstname = users.FirstNames[RandomNumberGenerator.GetInt32(users.FirstNames.Count - 1)];
				string lastname = users.LastNames[RandomNumberGenerator.GetInt32(users.LastNames.Count - 1)];
				var address = users.Addresses[RandomNumberGenerator.GetInt32(users.Addresses.Count - 1)];
				result.Add(new MockUser(firstname, lastname, address));
			}
			return result;
		}

		private static UsersMockData GetMockData()
		{
			var json = new MemoryStream(Resources.UserMockData);
			json.Position = 0;
			var ser = new DataContractJsonSerializer(typeof(UsersMockData));
			return ser.ReadObject(json) as UsersMockData;
		}
	}

	[Serializable]
	public class MockUser
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Street { get; set; }
		public string StreetNumber { get; set; }
		public string City { get; set; }
		public string PostalCode { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string EMail { get; set; }
		public string OIB { get; set; }
		public string Phone { get; set; }

		public MockUser(string firstName, string lastName, AddressMockData address)
		{
			this.FirstName = firstName;
			this.LastName = lastName;
			this.Street = address.Street;
			this.PostalCode = address.PostalCode;
			this.City = address.City;

			// street number
			this.StreetNumber = RandomNumberGenerator.GetInt32(1, 99).ToString();
			int lett = RandomNumberGenerator.GetInt32(80, 103);
			if (lett >= 97)
			{
				this.StreetNumber += ((char)lett).ToString();
			}

			// username
			this.UserName = Utils.SanatizeForUrl((this.FirstName?.FirstOrDefault().ToString() ?? "").ToLower() + (this.LastName ?? "").ToLower()).Replace("-", "");

			// password
			this.Password = "";
			for (int i = 0; i < 10; i++)
			{
				int random = RandomNumberGenerator.GetInt32(48, 90);
				this.Password += char.ConvertFromUtf32(random).ToString();
			}

			// e-mail
			var mailHosts = new List<string> { "gmail.com", "yahoo.com", "outlook.com", "aol.com", "mail.com", "inbox.com" };
			var randomHost = mailHosts[RandomNumberGenerator.GetInt32(0, mailHosts.Count - 1)];
			this.EMail = $"{Utils.SanatizeForUrl(this.FirstName)}.{Utils.SanatizeForUrl(this.LastName)}@{randomHost}";

			this.OIB = Utils.GenerateRandomOIB();
			this.Phone = "+38596" + RandomNumberGenerator.GetInt32(100, 999).ToString() + RandomNumberGenerator.GetInt32(100, 999).ToString();

		}
	}


	[DataContract]
	public class UsersMockData
	{
		[DataMember]
		public List<string> FirstNames { get; set; }
		[DataMember]
		public List<string> LastNames { get; set; }
		[DataMember]
		public List<AddressMockData> Addresses { get; set; }
	}

	[DataContract]
	public class AddressMockData
	{
		[DataMember]
		public string Street { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string City { get; set; }
	}
}
