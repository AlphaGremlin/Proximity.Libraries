/****************************************\
 WeakCollectionTests.cs
 Created: 2013-08-20
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of WeakCollectionTests.
	/// </summary>
	[TestFixture()]
	public class WeakCollectionTests
	{
		[Test()]
		public void Add()
		{
			var MyCollection = new WeakCollection<object>();
			var MyObject = new object();
			
			MyCollection.Add(MyObject);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not added");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void AddRange()
		{
			var MyCollection = new WeakCollection<object>();
			var MyObject1 = new object();
			var MyObject2 = new object();
			
			MyCollection.AddRange(new object[] { MyObject1, MyObject2 });
			
			GC.Collect();
			
			Assert.AreEqual(2, MyCollection.Count(), "Items were not added");
			
			GC.KeepAlive(MyObject1);
			GC.KeepAlive(MyObject2);
		}
		
		[Test()]
		public void Remove()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject;
			//****************************************
			
			AllocateWithSingle(out MyCollection, out MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not added");
			
			MyCollection.Remove(MyObject);
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void RemoveMulti()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject1, MyObject2;
			//****************************************
			
			AllocateWithDual(out MyCollection, out MyObject1, out MyObject2, 0);
			
			GC.Collect();
			
			Assert.AreEqual(2, MyCollection.Count(), "Items were not added");
			
			MyCollection.Remove(MyObject1);
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject1);
			GC.KeepAlive(MyObject2);
		}
		
		[Test()]
		public void WeakAdd()
		{	//****************************************
			WeakCollection<object> MyCollection;
			//****************************************
			
			AllocateRange(out MyCollection, 1);
			
			GC.Collect();
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not expired");
		}
		
		[Test()]
		public void WeakAddRange()
		{	//****************************************
			WeakCollection<object> MyCollection;
			//****************************************
			
			AllocateRange(out MyCollection, 2);
			
			GC.Collect();
			
			Assert.AreEqual(0, MyCollection.Count(), "Items were not removed");
		}
		
		[Test()]
		public void WeakAddSingle()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject;
			//****************************************
			
			AllocateWithSingle(out MyCollection, out MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void Clear()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject;
			//****************************************
			
			AllocateWithSingle(out MyCollection, out MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not added");
			
			MyCollection.Clear();
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void Enum()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject1, MyObject2;
			bool Found1 = false, Found2 = false;
			//****************************************
			
			AllocateWithDual(out MyCollection, out MyObject1, out MyObject2, 0);
			
			GC.Collect();
			
			// Should find two objects
			foreach(var MyObject in MyCollection)
			{
				if (object.ReferenceEquals(MyObject, MyObject1))
				{
					Found1 = true;
					continue;
				}
				
				if (object.ReferenceEquals(MyObject, MyObject2))
				{
					Found2 = true;
					continue;
				}
				
				Assert.Fail("Unknown Object");
			}
			
			Assert.AreEqual(2, MyCollection.Count(), "Items were not added");
			Assert.IsTrue(Found1, "Object 1 not found");
			Assert.IsTrue(Found2, "Object 2 not found");
			
			GC.KeepAlive(MyObject1);
			GC.KeepAlive(MyObject2);
		}
		
		[Test()]
		public void EnumValues()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject1, MyObject2;
			bool Found1 = false, Found2 = false;
			//****************************************
			
			AllocateWithDual(out MyCollection, out MyObject1, out MyObject2, 0);
			
			GC.Collect();
			
			// Should find two objects
			foreach(var MyObject in MyCollection.ToStrongList())
			{
				if (object.ReferenceEquals(MyObject, MyObject1))
				{
					Found1 = true;
					continue;
				}
				
				if (object.ReferenceEquals(MyObject, MyObject2))
				{
					Found2 = true;
					continue;
				}
				
				Assert.Fail("Unknown Object");
			}
			
			Assert.AreEqual(2, MyCollection.Count(), "Items were not added");
			Assert.IsTrue(Found1, "Object 1 not found");
			Assert.IsTrue(Found2, "Object 2 not found");
			
			GC.KeepAlive(MyObject1);
			GC.KeepAlive(MyObject2);
		}
		
		[Test()]
		public void WeakEnum()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject;
			bool Found = false;
			//****************************************
			
			AllocateWithSingle(out MyCollection, out MyObject, 1);
			
			GC.Collect();
			
			// Should find only one object
			foreach(var MyInnerObject in MyCollection)
			{
				if (object.ReferenceEquals(MyObject, MyInnerObject))
				{
					Found = true;
					continue;
				}
				
				Assert.Fail("Unknown Object: {0}", MyInnerObject);
			}
			
			Assert.IsTrue(Found, "Object 1 not found");
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void WeakEnumValues()
		{	//****************************************
			WeakCollection<object> MyCollection;
			object MyObject;
			bool Found = false;
			//****************************************
			
			AllocateWithSingle(out MyCollection, out MyObject, 1);
			
			GC.Collect();
			
			// Should find only one object
			foreach(var MyInnerObject in MyCollection.ToStrongList())
			{
				if (object.ReferenceEquals(MyObject, MyInnerObject))
				{
					Found = true;
					continue;
				}
				
				Assert.Fail("Unknown Object: {0}", MyInnerObject);
			}
			
			Assert.IsTrue(Found, "Object 1 not found");
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}

		[Test()]
		public void Cleanup()
		{
			var StrongRef = new object();
			var WeakRef = new WeakReference(StrongRef);
			var MyCollection = new WeakCollection<object>(System.Runtime.InteropServices.GCHandleType.Normal);

			MyCollection.Add(StrongRef);

			CollectionAssert.Contains(MyCollection, StrongRef);

			StrongRef = null;
			MyCollection = null;

			GC.Collect();
			GC.WaitForPendingFinalizers();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.IsNull(WeakRef.Target);
		}

		//****************************************
		
		private void AllocateRange<TItem>(out WeakCollection<TItem> collection, int count) where TItem : class, new()
		{	//****************************************
			var Items = new TItem[count];
			//****************************************
			
			for (int Index = 0; Index < count; Index++)
			{
				Items[Index] = new TItem();
			}
			
			collection = new WeakCollection<TItem>();
			collection.AddRange(Items);
		}
		
		private void AllocateWithSingle<TItem>(out WeakCollection<TItem> collection, out TItem item, int count) where TItem : class, new()
		{	//****************************************
			var Items = new TItem[count];
			//****************************************
			
			for (int Index = 0; Index < count; Index++)
			{
				Items[Index] = new TItem();
			}
			
			collection = new WeakCollection<TItem>();
			
			collection.AddRange(Items);
			collection.Add(item = new TItem());
		}
		
		private void AllocateWithDual<TItem>(out WeakCollection<TItem> collection, out TItem item1, out TItem item2, int count) where TItem : class, new()
		{	//****************************************
			var Items = new TItem[count];
			//****************************************
			
			for (int Index = 0; Index < count; Index++)
			{
				Items[Index] = new TItem();
			}
			
			collection = new WeakCollection<TItem>();
			
			collection.AddRange(Items);
			collection.Add(item1 = new TItem());
			collection.Add(item2 = new TItem());
		}
	}
}
