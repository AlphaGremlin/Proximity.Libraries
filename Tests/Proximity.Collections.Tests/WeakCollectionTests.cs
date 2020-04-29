using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests
{
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
		{
			AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not added");
			
			MyCollection.Remove(MyObject);
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void RemoveMulti()
		{
			AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);
			
			GC.Collect();
			
			Assert.AreEqual(2, MyCollection.Count(), "Items were not added");
			
			MyCollection.Remove(MyObject1);
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject1);
			GC.KeepAlive(MyObject2);
		}
		
		[Test()]
		public void WeakAdd()
		{
			AllocateRange<object>(out var MyCollection, 1);
			
			GC.Collect();
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not expired");
		}
		
		[Test()]
		public void WeakAddRange()
		{
			AllocateRange<object>(out var MyCollection, 2);
			
			GC.Collect();
			
			Assert.AreEqual(0, MyCollection.Count(), "Items were not removed");
		}
		
		[Test()]
		public void WeakAddSingle()
		{
			AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void Clear()
		{
			AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);
			
			GC.Collect();
			
			Assert.AreEqual(1, MyCollection.Count(), "Item was not added");
			
			MyCollection.Clear();
			
			Assert.AreEqual(0, MyCollection.Count(), "Item was not removed");
			
			GC.KeepAlive(MyObject);
		}
		
		[Test()]
		public void Enum()
		{	//****************************************
			bool Found1 = false, Found2 = false;
			//****************************************
			
			AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);
			
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
			bool Found1 = false, Found2 = false;
			//****************************************
			
			AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);
			
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
			bool Found = false;
			//****************************************
			
			AllocateWithSingle<object>(out var MyCollection, out var MyObject, 1);
			
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
			var Found = false;
			//****************************************
			
			AllocateWithSingle<object>(out var MyCollection, out var MyObject, 1);
			
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
			var MyCollection = new WeakCollection<object>(System.Runtime.InteropServices.GCHandleType.Normal)
			{
				StrongRef
			};

			CollectionAssert.Contains(MyCollection, StrongRef);

#pragma warning disable IDE0059 // Unnecessary assignment of a value
			StrongRef = null;
			MyCollection = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

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
			
			for (var Index = 0; Index < count; Index++)
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
			
			for (var Index = 0; Index < count; Index++)
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
			
			for (var Index = 0; Index < count; Index++)
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
