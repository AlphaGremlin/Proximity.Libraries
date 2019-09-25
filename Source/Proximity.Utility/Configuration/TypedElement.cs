using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// The base class of an element within a <see cref="TypedElementCollection&lt;TValue&gt;" /> or <see cref="TypedElementProperty&lt;TValue&gt;" />
	/// </summary>
	public abstract class TypedElement : ConfigurationElement
	{
		/// <summary>
		/// Creates a typed element
		/// </summary>
		protected TypedElement() : base()
		{
			base["Type"] = GetType().AssemblyQualifiedName;
		}

		//****************************************

		public TValue Populate<TValue>() where TValue : TypedElement, new() => Populate<TValue>(System.Type.GetType(Type));

		public TValue Populate<TValue>(Type targetType) where TValue : TypedElement, new()
		{
			if (RawElement == null)
				throw new InvalidOperationException("Element already populated");

			if (targetType != null)
			{
				var Attributes = targetType.GetCustomAttributes(typeof(TypedElementAttribute), true);

				if (Attributes.Length != 0)
				{
					TValue NewElement;

					var ConfigType = ((TypedElementAttribute)Attributes[0]).ConfigType;

					if (typeof(TValue).IsAssignableFrom(targetType))
					{
						NewElement = (TValue)Activator.CreateInstance(targetType);

						NewElement.InstanceType = ConfigType;
					}
					else
					{
						NewElement = (TValue)Activator.CreateInstance(ConfigType);

						NewElement.InstanceType = targetType;
					}

					Populate(NewElement);

					return NewElement;
				}
				else
				{
					var NewElement = (TValue)Activator.CreateInstance(targetType);

					Populate(NewElement);

					return NewElement;
				}
			}

			try
			{
				var NewElement = new TValue();

				// No TypedElement attribute, so InstanceType remains empty

				Populate(NewElement);

				return NewElement;
			}
			catch (MissingMethodException)
			{
				return null;
			}
		}

		public void Populate(TypedElement target)
		{
			target.Type = Type;

			using var Reader = RawElement.CreateReader();

			Reader.Read();

			target.Deserialise(Reader, false);
		}

		internal bool Serialise(XmlWriter writer, bool serializeCollectionKey)
		{
			if (RawElement != null)
			{
				RawElement.WriteTo(writer);

				return true;
			}

			return SerializeElement(writer, serializeCollectionKey);
		}
		
		internal void Deserialise(XmlReader reader, bool serializeCollectionKey)
		{
			DeserializeElement(reader, serializeCollectionKey);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the base Type of this Element
		/// </summary>
		[ConfigurationProperty("Type", IsRequired=true)]
		public string Type
		{
			get => (string)base["Type"];
			internal set => base["Type"] = value;
		}

		/// <summary>
		/// Gets the Type of the Instance this Typed Element refers to
		/// </summary>
		/// <remarks>Can be null if no TypedElementAttribute was found</remarks>
		public Type InstanceType { get; private set; }

		internal XElement RawElement { get; set; }
	}
}