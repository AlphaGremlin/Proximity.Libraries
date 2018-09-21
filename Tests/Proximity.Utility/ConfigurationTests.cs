/****************************************\
 ConfigurationTests.cs
 Created: 2014-07-09
\****************************************/
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Proximity.Utility.Configuration;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of ConfigurationTests.
	/// </summary>
	[TestFixture]
	public class ConfigurationTests
	{
		private const string RawStringSection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Utility.Tests.ConfigurationTests+StringElementSection, Proximity.Utility.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Text>{text}</Text>
    </Test>
</configuration>";

		private const string RawStringCollectionSection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Utility.Tests.ConfigurationTests+StringCollectionSection, Proximity.Utility.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Text>{text}
        </Text>
    </Test>
</configuration>";
		
		private const string RawStringCollectionRoaming = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <Test>
        <Text>{text}
        </Text>
    </Test>
</configuration>";

		private const string RawTypedPropertySection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Utility.Tests.ConfigurationTests+TypedPropertyTestSection, Proximity.Utility.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Property Type=""Proximity.Utility.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Utility.Tests"" Value=""1"" />
    </Test>
</configuration>";
		
		//****************************************
		
		[Test]
		public void ReadStringElement()
		{
			var RawXML = @"
<Test>
	<Text>TestText</Text>
</Test>";
			
			var MySection = StringElementSection.FromString(RawXML);
			
			Assert.AreEqual("TestText", MySection.Text.Content);
		}
		
		[Test]
		public void ReadStringCollectionOne()
		{
			var RawXML = @"
<Test>
	<Text>
		<add>One</add>
	</Text>
</Test>";
			
			var MySection = StringCollectionSection.FromString(RawXML);
			
			CollectionAssert.AreEquivalent(new string[] { "One" }, MySection.Text.Select(element => element.Content));
		}
		
		[Test]
		public void ReadStringCollectionTwo()
		{
			var RawXML = @"
<Test>
	<Text>
		<add>One</add>
		<add>Two</add>
	</Text>
</Test>";
			
			var MySection = StringCollectionSection.FromString(RawXML);
			
			CollectionAssert.AreEquivalent(new string[] { "One", "Two" }, MySection.Text.Select(element => element.Content));
		}
		
		[Test]
		public void ReadStringCollectionThree()
		{
			var RawXML = @"
<Test>
	<Text>
		<add>One</add>
		<add>Two</add>
		<add>Three</add>
	</Text>
</Test>";
			
			var MySection = StringCollectionSection.FromString(RawXML);
			
			CollectionAssert.AreEquivalent(new string[] { "One", "Two", "Three"}, MySection.Text.Select(element => element.Content));
		}

		[Test]
		public void ReadCustomCollectionThree()
		{
			var RawXML = @"
<Test>
	<Text>
		<add>1</add>
		<add>2</add>
		<add>3</add>
	</Text>
</Test>";
			
			var MySection = CustomCollectionSection.FromString(RawXML);
			
			CollectionAssert.AreEquivalent(new int[] { 1, 2, 3 }, MySection.Text.Select(element => element.Content));
		}

		[Test]
		public void ReadStringOverride()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();
			var Temp2 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;
				MyMap.RoamingUserConfigFilename = Temp2;

				File.WriteAllText(Temp1, RawStringSection.Replace("{text}", "ExeText"));
				File.WriteAllText(Temp2, RawStringSection.Replace("{text}", "RoamingText"));

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.PerUserRoaming);

				var MySection = (StringElementSection)MyConfig.GetSection("Test");

				Assert.AreEqual("RoamingText", MySection.Text.Content);
			}
			finally
			{
				File.Delete(Temp1);
				File.Delete(Temp2);
			}
		}

		[Test]
		public void SaveStringOverride()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();
			var Temp2 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;
				MyMap.RoamingUserConfigFilename = Temp2;

				File.WriteAllText(Temp1, RawStringSection.Replace("{text}", "ExeText"));
				//File.WriteAllText(Temp2, RawStringSection.Replace("{text}", "RoamingText"));

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.None);

				var MySection = (StringElementSection)MyConfig.GetSection("Test");
				
				MySection.Text.Content = "ModifiedText";
				
				MySection.CurrentConfiguration.SaveAs(Temp2);
				
				var NewConfig = File.ReadAllText(Temp2);

				Assert.AreEqual(RawStringSection.Replace("{text}", "ModifiedText"), NewConfig);
			}
			finally
			{
				File.Delete(Temp1);
				File.Delete(Temp2);
			}
		}

		[Test]
		public void ReadStringCollectionOverride()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();
			var Temp2 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;
				MyMap.RoamingUserConfigFilename = Temp2;

				File.WriteAllText(Temp1, RawStringCollectionSection.Replace("{text}", "\r\n            <add>ExeText</add>"));
				File.WriteAllText(Temp2, RawStringCollectionSection.Replace("{text}", "\r\n            <add>RoamingText</add>"));

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.PerUserRoaming);

				var MySection = (StringCollectionSection)MyConfig.GetSection("Test");

				CollectionAssert.AreEquivalent(new string[] { "ExeText", "RoamingText" }, MySection.Text.Select(elem => elem.Content));
			}
			finally
			{
				File.Delete(Temp1);
				File.Delete(Temp2);
			}
		}

		[Test]
		public void SaveStringCollection()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();
			var Temp2 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;
				MyMap.RoamingUserConfigFilename = Temp2;

				File.WriteAllText(Temp1, RawStringCollectionSection.Replace("{text}", "\r\n            <add>ExeText</add>"));
				File.Delete(Temp2);

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.PerUserRoaming);

				var MySection = (StringCollectionSection)MyConfig.GetSection("Test");
				
				MySection.Text.Add(new ConfigurationTextElement() { Content = "RoamingText" });
				
				MyConfig.Save();
				
				var NewConfig = File.ReadAllText(Temp2);

				var ExpectedConfig = RawStringCollectionRoaming
					.Replace("{text}", "\r\n            <add>RoamingText</add>");
				
				Assert.AreEqual(ExpectedConfig, NewConfig);
			}
			finally
			{
				File.Delete(Temp1);
				File.Delete(Temp2);
			}
		}

		[Test]
		public void ReadTypedProperty()
		{
			var RawXML = @"
	<Test>
		<Property Type=""Proximity.Utility.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Utility.Tests"" Value=""1"" />
	</Test>";

			var MySection = TypedPropertyTestSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Property);
			Assert.IsInstanceOf(typeof(SubTypedTestElement), MySection.Property.Content);
			Assert.AreEqual("1", ((SubTypedTestElement)MySection.Property.Content).Value);
		}

		[Test]
		public void ReadTypedPropertySection()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;

				File.WriteAllText(Temp1, RawTypedPropertySection);

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.None);

				var MySection = (TypedPropertyTestSection)MyConfig.GetSection("Test");

				Assert.IsNotNull(MySection.Property);
				Assert.IsInstanceOf(typeof(SubTypedTestElement), MySection.Property.Content);
				Assert.AreEqual("1", ((SubTypedTestElement)MySection.Property.Content).Value);
			}
			finally
			{
				File.Delete(Temp1);
			}
		}

		//****************************************
		
		public class StringElementSection : ConfigurationSection
		{
			public static StringElementSection FromString(string rawXml)
			{
				using (var MyStream = new StringReader(rawXml))
				using (var MyReader = XmlReader.Create(MyStream))
				{
					MyReader.Read();
					
					var MySection = new StringElementSection();
					
					MySection.DeserializeSection(MyReader);
					
					return MySection;
				}
			}
			
			[ConfigurationProperty("Text", IsRequired = true)]
			public ConfigurationTextElement Text
			{
				get { return (ConfigurationTextElement)base["Text"]; }
				set { base["Text"] = value; }
			}
		}
		
		public class StringCollectionSection : ConfigurationSection
		{
			public static StringCollectionSection FromString(string rawXml)
			{
				using (var MyStream = new StringReader(rawXml))
				using (var MyReader = XmlReader.Create(MyStream))
				{
					MyReader.Read();
					
					var MySection = new StringCollectionSection();
					
					MySection.DeserializeSection(MyReader);
					
					return MySection;
				}
			}
			
			[ConfigurationProperty("Text", IsDefaultCollection = false, IsRequired = true)]
			public TextElementCollection Text
			{
				get { return (TextElementCollection)this["Text"] ?? new TextElementCollection(); }
			}
		}
		
		public class CustomCollectionSection : ConfigurationSection
		{
			public static CustomCollectionSection FromString(string rawXml)
			{
				using (var MyStream = new StringReader(rawXml))
				using (var MyReader = XmlReader.Create(MyStream))
				{
					MyReader.Read();
					
					var MySection = new CustomCollectionSection();
					
					MySection.DeserializeSection(MyReader);
					
					return MySection;
				}
			}
			
			[ConfigurationProperty("Text", IsDefaultCollection = false, IsRequired = true)]
			public CustomTextElementCollection Text
			{
				get { return (CustomTextElementCollection)this["Text"] ?? new CustomTextElementCollection(); }
			}
		}
		
		public class TextElementCollection : ConfigCollection<ConfigurationTextElement>
		{
			public TextElementCollection() : base()
			{
			}
		}
		
		public class CustomTextElementCollection : ConfigCollection<CustomTextElement>
		{
			public CustomTextElementCollection() : base()
			{
			}
		}
		
		public class CustomTextElement : ConfigurationTextElement<int>
		{
		}

		public class TypedTestElement : TypedElement
		{
		}

		public class SubTypedTestElement : TypedTestElement
		{
			[ConfigurationProperty("Value", IsRequired = false)]
			public string Value
			{
				get { return (string)base["Value"]; }
				set { base["Value"] = value; }
			}
		}

		public class TypedPropertyTestSection : ConfigurationSection
		{
			public static TypedPropertyTestSection FromString(string rawXml)
			{
				using (var MyStream = new StringReader(rawXml))
				using (var MyReader = XmlReader.Create(MyStream))
				{
					MyReader.Read();

					var MySection = new TypedPropertyTestSection();

					MySection.DeserializeSection(MyReader);

					return MySection;
				}
			}
			
			[ConfigurationProperty("Property", IsRequired = false)]
			public TypedElementProperty<TypedTestElement> Property
			{
				get { return (TypedElementProperty<TypedTestElement>)base["Property"]; }
				set { base["Property"] = value; }
			}
		}
	}
}
