using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Configuration.Tests;

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

		Assert.That(MySection.Text.Content, Is.EqualTo("TestText"));
	}

	[Test]
	public void ReadCustomTextElement()
	{
		var RawXML = @"
<Test>
	<Text Value=""10"">TestText</Text>
</Test>";

		var MySection = CustomTextElementSection.FromString(RawXML);

		Assert.That(MySection.Text.Content, Is.EqualTo("TestText"));
		Assert.That(MySection.Text.Value, Is.EqualTo(10));
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

		Assert.That(MySection.Text.Select(element => element.Content), Is.EquivalentTo(new string[] { "One" }));
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

		Assert.That(MySection.Text.Select(element => element.Content), Is.EquivalentTo(new string[] { "One", "Two" }));
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

		Assert.That(MySection.Text.Select(element => element.Content), Is.EquivalentTo(new string[] { "One", "Two", "Three" }));
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

		Assert.That(MySection.Text.Select(element => element.Content), Is.EquivalentTo(new int[] { 1, 2, 3 }));
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

			Assert.That(MySection.Text.Content, Is.EqualTo("RoamingText"));
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

			Assert.That(NewConfig, Is.EqualTo(RawStringSection.Replace("{text}", "ModifiedText")));
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

			Assert.That(MySection.Text.Select(elem => elem.Content), Is.EquivalentTo(new string[] { "ExeText", "RoamingText" }));
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

			Assert.That(NewConfig, Is.EqualTo(ExpectedConfig));
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

		Assert.That(MySection.Property, Is.Not.Null);

		var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

		Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedTestElement)));
		Assert.That(((SubTypedTestElement)RealElement).Value, Is.EqualTo("1"));
	}

	[Test]
	public void ReadTypedProperty2()
	{
		var RawXML = @"
	<Test>
		<Property Type=""Proximity.Configuration.Tests.ConfigurationTests+SubTypedTestElement2, Proximity.Configuration.Tests"" Value=""1""><Child Value=""2"" /></Property>
	</Test>";

		var MySection = TypedPropertyTestSection.FromString(RawXML);

		Assert.That(MySection.Property, Is.Not.Null);

		var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

		Assert.That(RealElement, Is.InstanceOf<SubTypedTestElement2>());

		var SubTypedElement2 = (SubTypedTestElement2)RealElement;
		Assert.That(SubTypedElement2.Value, Is.EqualTo(1));
		Assert.That(SubTypedElement2.Child, Is.Not.Null);
		Assert.That(SubTypedElement2.Child.Value, Is.EqualTo("2"));
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

		Assert.That(MySection.Property, Is.Not.Null);

		var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

		Assert.That(RealElement, Is.InstanceOf<SubTypedTestElement2>());

		var SubTypedElement2 = (SubTypedTestElement2)RealElement;
		Assert.That(SubTypedElement2.Value, Is.EqualTo(1));
		Assert.That(SubTypedElement2.Child, Is.Not.Null);
		Assert.That(SubTypedElement2.Child.Value, Is.EqualTo("2"));
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

			Assert.That(MySection.Property, Is.Not.Null);

			var RealElement = MySection.Property.Content.Populate<TypedTestElement>(Type.GetType(MySection.Property.Content.Type));

			Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedTestElement)));
			Assert.That(((SubTypedTestElement)RealElement).Value, Is.EqualTo("1"));
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

		Assert.That(MySection.Collection, Is.Not.Null);
		Assert.That(MySection.Collection.Count, Is.EqualTo(1));

		var FirstItem = MySection.Collection.First();

		var RealElement = FirstItem.Populate<TypedTestElement>(Type.GetType(FirstItem.Type));

		Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedTestElement)));
		Assert.That(((SubTypedTestElement)RealElement).Value, Is.EqualTo("1"));
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

			Assert.That(MySection.Collection, Is.Not.Null);
			Assert.That(MySection.Collection.Count, Is.EqualTo(4));


			var FirstItem = MySection.Collection.First();

			var RealElement = FirstItem.Populate<TypedTestElement>(Type.GetType(FirstItem.Type));

			Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedTestElement)));
			Assert.That(((SubTypedTestElement)RealElement).Value, Is.EqualTo("1"));


			var SecondItem = MySection.Collection.Skip(1).First();

			var RealElement2 = SecondItem.Populate<TypedTestElement>(Type.GetType(SecondItem.Type));

			Assert.That(RealElement2, Is.InstanceOf<SubTypedTestElement2>());

			var SubTypedElement2 = (SubTypedTestElement2)RealElement2;

			Assert.That(SubTypedElement2.Value, Is.EqualTo(2));
			Assert.That(SubTypedElement2.Child, Is.Not.Null);
			Assert.That(SubTypedElement2.Child.Value, Is.EqualTo("3"));


			var ThirdItem = MySection.Collection.Skip(2).First();

			var RealElement3 = ThirdItem.Populate<TypedTestElement>(Type.GetType(ThirdItem.Type));

			Assert.That(RealElement3, Is.InstanceOf(typeof(SubTypedTestElement3)));

			var SubTypedElement3 = (SubTypedTestElement3)RealElement3;

			Assert.That(SubTypedElement3.Child, Is.Not.Null);
			Assert.That(SubTypedElement3.Child.Content, Is.EqualTo("3"));


			var FourthItem = MySection.Collection.Skip(3).First();

			var RealElement4 = FourthItem.Populate<TypedTestElement>(Type.GetType(FourthItem.Type));

			Assert.That(RealElement4, Is.InstanceOf(typeof(SubTypedTestElement4)));

			var SubTypedElement4 = (SubTypedTestElement4)RealElement4;

			Assert.That(SubTypedElement4.Children, Is.Not.Null);
			Assert.That(SubTypedElement4.Children.Count, Is.EqualTo(2));
			Assert.That(SubTypedElement4.Children.Select(child => child.Content), Is.EqualTo(new[] { "4", "5" }).AsCollection);
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

		Assert.That(MySection.Collection, Is.Not.Null);
		Assert.That(MySection.Collection.Count, Is.EqualTo(1));

		var FirstItem = MySection.Collection.First();

		Assert.That(FirstItem.Value, Is.EqualTo("1"));

		var RealElement = FirstItem.Populate<TypedWithPropertyTestElement>(Type.GetType(FirstItem.Type));

		Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedWithPropertyTestElement)));
		Assert.That(RealElement.Value, Is.EqualTo("1"));
		Assert.That(((SubTypedWithPropertyTestElement)RealElement).SubValue, Is.EqualTo("2"));
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

			Assert.That(MySection.Collection, Is.Not.Null);
			Assert.That(MySection.Collection.Count, Is.EqualTo(1));


			var FirstItem = MySection.Collection.First();

			Assert.That(FirstItem.Value, Is.EqualTo("1"));

			var RealElement = FirstItem.Populate<TypedWithPropertyTestElement>(Type.GetType(FirstItem.Type));

			Assert.That(RealElement, Is.InstanceOf(typeof(SubTypedWithPropertyTestElement)));
			Assert.That(RealElement.Value, Is.EqualTo("1"));
			Assert.That(((SubTypedWithPropertyTestElement)RealElement).SubValue, Is.EqualTo("2"));
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
