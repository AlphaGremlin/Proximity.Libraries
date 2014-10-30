/****************************************\
 AsyncCollectionTests.cs
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
		
		public class TextElementCollection : ConfigCollection<ConfigurationTextElement>
		{
			public TextElementCollection() : base()
			{
			}
		}
	}
}
