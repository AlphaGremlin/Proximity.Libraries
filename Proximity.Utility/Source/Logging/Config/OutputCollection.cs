/****************************************\
 OutputCollection.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
//****************************************

namespace Proximity.Utility.Logging.Config
{
	/// <summary>
	/// A list of Logging Outputs
	/// </summary>
	[ConfigurationCollection(typeof(OutputConfig), CollectionType=ConfigurationElementCollectionType.BasicMap, AddItemName="Output")]
	public sealed class OutputCollection : ConfigurationElementCollection, System.Collections.Generic.ICollection<OutputConfig>
	{
		/// <summary>
		/// Creates a new Logging Output Collection
		/// </summary>
		public OutputCollection()
		{
		}
		
		//****************************************

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new OutputConfig();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((OutputConfig)element).OutputType;
		}
		
		//****************************************
		
		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		public void Add(OutputConfig item)
		{
			this.BaseAdd(item);
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			this.BaseClear();
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(OutputConfig item)
		{
			return this.BaseIndexOf(item) != -1;
		}
		
		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		public bool Remove(OutputConfig item)
		{
			this.BaseRemove(item.OutputType);
			
			return this.BaseIsRemoved(item.OutputType);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public new IEnumerator<OutputConfig> GetEnumerator()
		{
			return new OutputEnum(this);
		}
		
		void ICollection<OutputConfig>.CopyTo(OutputConfig[] array, int arrayIndex)
		{
			base.CopyTo(array, arrayIndex);
		}
		
		bool ICollection<OutputConfig>.IsReadOnly
		{
			get { return base.IsReadOnly(); }
		}
		
		//****************************************
		
		private class OutputEnum : System.Collections.Generic.IEnumerator<OutputConfig>
		{
			private IEnumerator InnerEnumerator;
			
			public OutputEnum(OutputCollection outputSet)
			{
				InnerEnumerator = ((ICollection)outputSet).GetEnumerator();
			}
			
			void IDisposable.Dispose()
			{
				GC.SuppressFinalize(this);
			}
			
			public bool MoveNext()
			{
				return InnerEnumerator.MoveNext();
			}
			
			public void Reset()
			{
				InnerEnumerator.Reset();
			}
			
			object IEnumerator.Current
			{
				get { return InnerEnumerator.Current; }
			}
			
			public OutputConfig Current
			{
				get { return (OutputConfig)InnerEnumerator.Current; }
			}
		}
	}
}
