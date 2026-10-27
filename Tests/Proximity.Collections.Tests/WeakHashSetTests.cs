using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests;

[TestFixture()]
public class WeakHashSetTests
{
	[Test()]
	public void Add()
	{
		var MyCollection = new WeakHashSet<object>();
		var MyObject = new object();

		MyCollection.Add(MyObject);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not added");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void AddRange()
	{
		var MyCollection = new WeakHashSet<object>();
		var MyObject1 = new object();
		var MyObject2 = new object();

		MyCollection.UnionWith(new object[] { MyObject1, MyObject2 });

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(2), "Items were not added");

		GC.KeepAlive(MyObject1);
		GC.KeepAlive(MyObject2);
	}

	[Test()]
	public void Remove()
	{
		AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not added");

		MyCollection.Remove(MyObject);

		Assert.That(MyCollection.Count(), Is.Zero, "Item was not removed");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void RemoveMulti()
	{
		AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(2), "Items were not added");

		MyCollection.Remove(MyObject1);

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not removed");

		GC.KeepAlive(MyObject1);
		GC.KeepAlive(MyObject2);
	}

	[Test()]
	public void WeakAdd()
	{
		AllocateRange<object>(out var MyCollection, 1);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.Zero, "Item was not expired");
	}

	[Test()]
	public void WeakAddRange()
	{
		AllocateRange<object>(out var MyCollection, 2);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.Zero, "Items were not removed");
	}

	[Test()]
	public void WeakAddSingle()
	{
		AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not removed");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void Clear()
	{
		AllocateWithSingle<object>(out var MyCollection, out var MyObject, 0);

		GC.Collect();

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not added");

		MyCollection.Clear();

		Assert.That(MyCollection.Count(), Is.Zero, "Item was not removed");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void Enum()
	{ //****************************************
		bool Found1 = false, Found2 = false;
		//****************************************

		AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);

		GC.Collect();

		// Should find two objects
		foreach (var MyObject in MyCollection)
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

		Assert.That(MyCollection.Count(), Is.EqualTo(2), "Items were not added");
		Assert.That(Found1, Is.True, "Object 1 not found");
		Assert.That(Found2, Is.True, "Object 2 not found");

		GC.KeepAlive(MyObject1);
		GC.KeepAlive(MyObject2);
	}

	[Test()]
	public void EnumValues()
	{ //****************************************
		bool Found1 = false, Found2 = false;
		//****************************************

		AllocateWithDual<object>(out var MyCollection, out var MyObject1, out var MyObject2, 0);

		GC.Collect();

		// Should find two objects
		foreach (var MyObject in MyCollection.ToStrong())
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

		Assert.That(MyCollection.Count(), Is.EqualTo(2), "Items were not added");
		Assert.That(Found1, Is.True, "Object 1 not found");
		Assert.That(Found2, Is.True, "Object 2 not found");

		GC.KeepAlive(MyObject1);
		GC.KeepAlive(MyObject2);
	}

	[Test()]
	public void WeakEnum()
	{ //****************************************
		var Found = false;
		//****************************************

		AllocateWithSingle<object>(out var MyCollection, out var MyObject, 1);

		GC.Collect();

		// Should find only one object
		foreach (var MyInnerObject in MyCollection)
		{
			if (object.ReferenceEquals(MyObject, MyInnerObject))
			{
				Found = true;
				continue;
			}

			Assert.Fail($"Unknown Object: {MyInnerObject}");
		}

		Assert.That(Found, Is.True, "Object 1 not found");

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not removed");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void WeakEnumValues()
	{ //****************************************
		var Found = false;
		//****************************************

		AllocateWithSingle<object>(out var MyCollection, out var MyObject, 1);

		GC.Collect();

		// Should find only one object
		foreach (var MyInnerObject in MyCollection.ToStrong())
		{
			if (object.ReferenceEquals(MyObject, MyInnerObject))
			{
				Found = true;
				continue;
			}

			Assert.Fail($"Unknown Object: {MyInnerObject}");
		}

		Assert.That(Found, Is.True, "Object 1 not found");

		Assert.That(MyCollection.Count(), Is.EqualTo(1), "Item was not removed");

		GC.KeepAlive(MyObject);
	}

	[Test()]
	public void Cleanup()
	{
		var WeakCollection = new WeakHashSet<object>(EqualityComparer<object>.Default);

		static WeakReference AddTo(WeakHashSet<object> collection)
		{
			var StrongRef = new object();

			collection.Add(StrongRef);

			Assert.That(collection, Has.Member(StrongRef));

			return new WeakReference(StrongRef);
		}

		var WeakRef = AddTo(WeakCollection);

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
		GC.WaitForPendingFinalizers();

		Assert.That(WeakRef.Target, Is.Null);
	}

	//****************************************

	private void AllocateRange<TItem>(out WeakHashSet<TItem> collection, int count) where TItem : class, new()
	{ //****************************************
		var Items = new TItem[count];
		//****************************************

		for (var Index = 0; Index < count; Index++)
		{
			Items[Index] = new TItem();
		}

		collection = new WeakHashSet<TItem>();
		collection.UnionWith(Items);
	}

	private void AllocateWithSingle<TItem>(out WeakHashSet<TItem> collection, out TItem item, int count) where TItem : class, new()
	{ //****************************************
		var Items = new TItem[count];
		//****************************************

		for (var Index = 0; Index < count; Index++)
		{
			Items[Index] = new TItem();
		}

		collection = new WeakHashSet<TItem>();

		collection.UnionWith(Items);
		collection.Add(item = new TItem());
	}

	private void AllocateWithDual<TItem>(out WeakHashSet<TItem> collection, out TItem item1, out TItem item2, int count) where TItem : class, new()
	{ //****************************************
		var Items = new TItem[count];
		//****************************************

		for (var Index = 0; Index < count; Index++)
		{
			Items[Index] = new TItem();
		}

		collection = new WeakHashSet<TItem>();

		collection.UnionWith(Items);
		collection.Add(item1 = new TItem());
		collection.Add(item2 = new TItem());
	}
}
