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
//****************************************

namespace Proximity.Configuration.Tests
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
        <section name=""Test"" type=""Proximity.Configuration.Tests.ConfigurationTests+StringElementSection, Proximity.Configuration.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Text>{text}</Text>
    </Test>
</configuration>";

		private const string RawStringCollectionSection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Configuration.Tests.ConfigurationTests+StringCollectionSection, Proximity.Configuration.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
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
        <section name=""Test"" type=""Proximity.Configuration.Tests.ConfigurationTests+TypedPropertyTestSection, Proximity.Configuration.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Property Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Configuration.Tests"" Value=""1"" />
    </Test>
</configuration>";

		private const string RawTypedCollectionSection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Configuration.Tests.ConfigurationTests+TypedTestCollectionSection, Proximity.Configuration.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Collection>
            <add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Configuration.Tests"" Value=""1"" />
            <add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement2, Proximity.Configuration.Tests"" Value=""2"">
                <Child Value=""3"" />
            </add>
            <add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement3, Proximity.Configuration.Tests"">
                <Child>3</Child>
            </add>
            <add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement4, Proximity.Configuration.Tests"">
                <Children>
                    <add>4</add>
                    <add>5</add>
                </Children>
            </add>
        </Collection>
    </Test>
</configuration>";

		private const string RawTypedWithPropertyCollectionSection = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <section name=""Test"" type=""Proximity.Configuration.Tests.ConfigurationTests+TypedWithPropertyTestCollectionSection, Proximity.Configuration.Tests"" allowExeDefinition=""MachineToRoamingUser"" />
    </configSections>
    <Test>
        <Collection>
            <add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedWithPropertyTestElement, Proximity.Configuration.Tests"" Value=""1"" SubValue=""2"" />
        </Collection>
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
		public void ReadCustomTextElement()
		{
			var RawXML = @"
<Test>
	<Text Value=""10"">TestText</Text>
</Test>";

			var MySection = CustomTextElementSection.FromString(RawXML);

			Assert.AreEqual("TestText", MySection.Text.Content);
			Assert.AreEqual(10, MySection.Text.Value);
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
		<Property Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Configuration.Tests"" Value=""1"" />
	</Test>";

			var MySection = TypedPropertyTestSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Property);

			var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

			Assert.IsInstanceOf(typeof(SubTypedTestElement), RealElement);
			Assert.AreEqual("1", ((SubTypedTestElement)RealElement).Value);
		}

		[Test]
		public void ReadTypedProperty2()
		{
			var RawXML = @"
	<Test>
		<Property Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement2, Proximity.Configuration.Tests"" Value=""1""><Child Value=""2"" /></Property>
	</Test>";

			var MySection = TypedPropertyTestSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Property);

			var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

			Assert.IsInstanceOf(typeof(SubTypedTestElement2), RealElement);

			var SubTypedElement2 = RealElement as SubTypedTestElement2;
			Assert.AreEqual(1, SubTypedElement2.Value);
			Assert.IsNotNull(SubTypedElement2.Child);
			Assert.AreEqual("2", SubTypedElement2.Child.Value);
		}

		[Test]
		public void ReadTypedProperty2Whitespace()
		{
			var RawXML = @"
	<Test>
		<Property Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement2, Proximity.Configuration.Tests"" Value=""1"">
			<Child      Value=""2""        />      
		</Property>
	</Test>";

			var MySection = TypedPropertyTestSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Property);

			var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

			Assert.IsInstanceOf(typeof(SubTypedTestElement2), RealElement);

			var SubTypedElement2 = RealElement as SubTypedTestElement2;
			Assert.AreEqual(1, SubTypedElement2.Value);
			Assert.IsNotNull(SubTypedElement2.Child);
			Assert.AreEqual("2", SubTypedElement2.Child.Value);
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

				var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

				Assert.IsInstanceOf(typeof(SubTypedTestElement), RealElement);
				Assert.AreEqual("1", ((SubTypedTestElement)RealElement).Value);
			}
			finally
			{
				File.Delete(Temp1);
			}
		}

		[Test]
		public void ReadTypedCollection()
		{
			var RawXML = @"
	<Test>
		<Collection>
			<add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement, Proximity.Configuration.Tests"" Value=""1"" />
		</Collection>
	</Test>";

			var MySection = TypedTestCollectionSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Collection);
			Assert.AreEqual(1, MySection.Collection.Count);

			var FirstItem = MySection.Collection.First();

			var RealElement = FirstItem.Populate<TypedTestElement>(Type.GetType(FirstItem.Type));

			Assert.IsInstanceOf(typeof(SubTypedTestElement), RealElement);
			Assert.AreEqual("1", ((SubTypedTestElement)RealElement).Value);
		}

		[Test]
		public void ReadTypedCollectionSection()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;

				File.WriteAllText(Temp1, RawTypedCollectionSection);

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.None);

				var MySection = (TypedTestCollectionSection)MyConfig.GetSection("Test");

				Assert.IsNotNull(MySection.Collection);
				Assert.AreEqual(4, MySection.Collection.Count);


				var FirstItem = MySection.Collection.First();

				var RealElement = FirstItem.Populate<TypedTestElement>(Type.GetType(FirstItem.Type));

				Assert.IsInstanceOf(typeof(SubTypedTestElement), RealElement);
				Assert.AreEqual("1", ((SubTypedTestElement)RealElement).Value);


				var SecondItem = MySection.Collection.Skip(1).First();

				var RealElement2 = SecondItem.Populate<TypedTestElement>(Type.GetType(SecondItem.Type));

				Assert.IsInstanceOf(typeof(SubTypedTestElement2), RealElement2);

				var SubTypedElement2 = RealElement2 as SubTypedTestElement2;

				Assert.AreEqual(2, SubTypedElement2.Value);
				Assert.IsNotNull(SubTypedElement2.Child);
				Assert.AreEqual("3", SubTypedElement2.Child.Value);


				var ThirdItem = MySection.Collection.Skip(2).First();

				var RealElement3 = ThirdItem.Populate<TypedTestElement>(Type.GetType(ThirdItem.Type));

				Assert.IsInstanceOf(typeof(SubTypedTestElement3), RealElement3);

				var SubTypedElement3 = RealElement3 as SubTypedTestElement3;

				Assert.IsNotNull(SubTypedElement3.Child);
				Assert.AreEqual("3", SubTypedElement3.Child.Content);


				var FourthItem = MySection.Collection.Skip(3).First();

				var RealElement4 = FourthItem.Populate<TypedTestElement>(Type.GetType(FourthItem.Type));

				Assert.IsInstanceOf(typeof(SubTypedTestElement4), RealElement4);

				var SubTypedElement4 = RealElement4 as SubTypedTestElement4;

				Assert.IsNotNull(SubTypedElement4.Children);
				Assert.AreEqual(2, SubTypedElement4.Children.Count);
				CollectionAssert.AreEqual(new[] { "4", "5" }, SubTypedElement4.Children.Select(child => child.Content));
			}
			finally
			{
				File.Delete(Temp1);
			}
		}

		[Test]
		public void ReadTypedWithPropertyCollection()
		{
			var RawXML = @"
	<Test>
		<Collection>
			<add Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedWithPropertyTestElement, Proximity.Configuration.Tests"" Value=""1"" SubValue=""2"" />
		</Collection>
	</Test>";

			var MySection = TypedWithPropertyTestCollectionSection.FromString(RawXML);

			Assert.IsNotNull(MySection.Collection);
			Assert.AreEqual(1, MySection.Collection.Count);

			var FirstItem = MySection.Collection.First();

			Assert.AreEqual("1", FirstItem.Value);

			var RealElement = FirstItem.Populate<TypedWithPropertyTestElement>(Type.GetType(FirstItem.Type));

			Assert.IsInstanceOf(typeof(SubTypedWithPropertyTestElement), RealElement);
			Assert.AreEqual("1", RealElement.Value);
			Assert.AreEqual("2", ((SubTypedWithPropertyTestElement)RealElement).SubValue);
		}

		[Test]
		public void ReadTypedWithPropertyCollectionSection()
		{
			var MyMap = new ExeConfigurationFileMap();

			var Temp1 = Path.GetTempFileName();

			try
			{
				MyMap.ExeConfigFilename = Temp1;

				File.WriteAllText(Temp1, RawTypedWithPropertyCollectionSection);

				var MyConfig = ConfigurationManager.OpenMappedExeConfiguration(MyMap, ConfigurationUserLevel.None);

				var MySection = (TypedWithPropertyTestCollectionSection)MyConfig.GetSection("Test");

				Assert.IsNotNull(MySection.Collection);
				Assert.AreEqual(1, MySection.Collection.Count);


				var FirstItem = MySection.Collection.First();

				Assert.AreEqual("1", FirstItem.Value);

				var RealElement = FirstItem.Populate<TypedWithPropertyTestElement>(Type.GetType(FirstItem.Type));

				Assert.IsInstanceOf(typeof(SubTypedWithPropertyTestElement), RealElement);
				Assert.AreEqual("1", RealElement.Value);
				Assert.AreEqual("2", ((SubTypedWithPropertyTestElement)RealElement).SubValue);
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
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new StringElementSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}
			
			[ConfigurationProperty("Text", IsRequired = true)]
			public ConfigurationTextElement Text
			{
				get => (ConfigurationTextElement)base["Text"];
				set => base["Text"] = value;
			}
		}
		
		public class StringCollectionSection : ConfigurationSection
		{
			public static StringCollectionSection FromString(string rawXml)
			{
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new StringCollectionSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
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
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new CustomCollectionSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}
			
			[ConfigurationProperty("Text", IsDefaultCollection = false, IsRequired = true)]
			public CustomTextElementCollection Text
			{
				get { return (CustomTextElementCollection)this["Text"] ?? new CustomTextElementCollection(); }
			}
		}

		public class CustomTextElementSection : ConfigurationSection
		{
			public static CustomTextElementSection FromString(string rawXml)
			{
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new CustomTextElementSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}

			[ConfigurationProperty("Text", IsRequired = true)]
			public CustomTextElement Text
			{
				get => (CustomTextElement)base["Text"];
				set => base["Text"] = value;
			}
		}

		public class TextElementCollection : ConfigurationElementCollection<ConfigurationTextElement>
		{
			public TextElementCollection() : base()
			{
			}
		}
		
		public class CustomTextElementCollection : ConfigurationElementCollection<CustomIntTextElement>
		{
			public CustomTextElementCollection() : base()
			{
			}
		}

		public class CustomTextElement : ConfigurationTextElement
		{
			[ConfigurationProperty("Value", IsRequired = false)]
			public int Value
			{
				get => (int)base["Value"];
				set => base["Value"] = value;
			}
		}

		public class CustomIntTextElement : ConfigurationTextElement<int>
		{
		}

		public class TypedTestElement : TypedElement
		{
		}

		public class TypedWithPropertyTestElement : TypedElement
		{
			[ConfigurationProperty("Value", IsRequired = false)]
			public string Value
			{
				get => (string)base["Value"];
				set => base["Value"] = value;
			}
		}

		public class SubTypedWithPropertyTestElement : TypedWithPropertyTestElement
		{
			[ConfigurationProperty("SubValue", IsRequired = false)]
			public string SubValue
			{
				get => (string)base["SubValue"];
				set => base["SubValue"] = value;
			}
		}

		public class SubTypedTestElement : TypedTestElement
		{
			[ConfigurationProperty("Value", IsRequired = false)]
			public string Value
			{
				get => (string)base["Value"];
				set => base["Value"] = value;
			}
		}

		public class SubTypedTestElement2 : TypedTestElement
		{
			[ConfigurationProperty("Child", IsRequired = false)]
			public SubTypedTestElement Child
			{
				get => (SubTypedTestElement)base["Child"];
				set => base["Child"] = value;
			}

			[ConfigurationProperty("Value", IsRequired = false)]
			public int Value
			{
				get => (int)base["Value"];
				set => base["Value"] = value;
			}
		}

		public class SubTypedTestElement3 : TypedTestElement
		{
			[ConfigurationProperty("Child", IsRequired = false)]
			public ConfigurationTextElement Child
			{
				get => (ConfigurationTextElement)base["Child"];
				set => base["Child"] = value;
			}
		}

		public class SubTypedTestElement4 : TypedTestElement
		{
			[ConfigurationProperty("Children", IsRequired = false)]
			public TextElementCollection Children
			{
				get => (TextElementCollection)base["Children"];
				set => base["Children"] = value;
			}
		}

		public class TypedPropertyTestSection : ConfigurationSection
		{
			public static TypedPropertyTestSection FromString(string rawXml)
			{
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new TypedPropertyTestSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}
			
			[ConfigurationProperty("Property", IsRequired = false)]
			public TypedElementProperty<TypedTestElement> Property
			{
				get => (TypedElementProperty<TypedTestElement>)base["Property"];
				set => base["Property"] = value;
			}
		}

		public class TypedTestCollectionSection : ConfigurationSection
		{
			public static TypedTestCollectionSection FromString(string rawXml)
			{
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new TypedTestCollectionSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}

			[ConfigurationProperty("Collection", IsRequired = false, IsDefaultCollection = false)]
			public TypedTestCollection Collection
			{
				get => (TypedTestCollection)base["Collection"];
				set => base["Collection"] = value;
			}
		}

		public class TypedWithPropertyTestCollectionSection : ConfigurationSection
		{
			public static TypedWithPropertyTestCollectionSection FromString(string rawXml)
			{
				using var MyStream = new StringReader(rawXml);
				using var MyReader = XmlReader.Create(MyStream);

				MyReader.Read();

				var MySection = new TypedWithPropertyTestCollectionSection();

				MySection.DeserializeSection(MyReader);

				return MySection;
			}

			[ConfigurationProperty("Collection", IsRequired = false, IsDefaultCollection = false)]
			public TypedWithPropertyTestCollection Collection
			{
				get => (TypedWithPropertyTestCollection)base["Collection"];
				set => base["Collection"] = value;
			}
		}

		public class TypedTestCollection : TypedElementCollection<TypedTestElement>
		{
		}

		public class TypedWithPropertyTestCollection : TypedElementCollection<TypedWithPropertyTestElement>
		{
		}
	}
}
