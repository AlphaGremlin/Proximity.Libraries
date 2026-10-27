using NUnit.Framework;
//****************************************

namespace Proximity.Collections.Tests;

/// <summary>
/// Tests the functionality of the Linq Extensions class
/// </summary>
[TestFixture()]
public class LinqExtensionTests
{
	[Test]
	public void FullOuterJoinMatch()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void FullOuterJoinDuplicateLeft()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void FullOuterJoinDuplicateRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void FullOuterJoinDuplicateBoth()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void FullOuterJoinDuplicateAll()
	{
		var Left = new string[] { "1", "1", "1", "1", "1" };
		var Right = new string[] { "1", "1", "1", "1", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(Enumerable.Repeat("11", 5 * 5)), "No match");
	}

	[Test]
	public void FullOuterJoinLeft()
	{
		var Left = new string[] { "1", "2", "3" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "_4" }), "No match");
	}

	[Test]
	public void FullOuterJoinLeftDefault()
	{
		var Left = new string[] { "1", "2", "3" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", null).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "_4" }), "No match");
	}

	[Test]
	public void FullOuterJoinRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "4_" }), "No match");
	}

	[Test]
	public void FullOuterJoinRightDefault()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "3", "2", "1" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, null, "_").ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "4_" }), "No match");
	}

	[Test]
	public void FullOuterJoinBoth()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "1_", "22", "33", "44", "_5" }), "No match");
	}

	[Test]
	public void FullOuterJoinBothDefault()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.FullOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", "_").ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "1_", "22", "33", "44", "_5" }), "No match");
	}

	[Test]
	public void LeftOuterJoinMatch()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void LeftOuterJoinDuplicateLeft()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void LeftOuterJoinDuplicateRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void LeftOuterJoinDuplicateBoth()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void LeftOuterJoinDuplicateAll()
	{
		var Left = new string[] { "1", "1", "1", "1", "1" };
		var Right = new string[] { "1", "1", "1", "1", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(Enumerable.Repeat("11", 5 * 5)), "No match");
	}

	[Test]
	public void LeftOuterJoinLeft()
	{
		var Left = new string[] { "1", "2", "3" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33" }), "No match");
	}

	[Test]
	public void LeftOuterJoinRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "4_" }), "No match");
	}

	[Test]
	public void LeftOuterJoinRightDefault()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "3", "2", "1" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", null).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "4_" }), "No match");
	}

	[Test]
	public void LeftOuterJoinBoth()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "1_", "22", "33", "44" }), "No match");
	}

	[Test]
	public void LeftOuterJoinBothDefault()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.LeftOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", null).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "1_", "22", "33", "44" }), "No match");
	}

	[Test]
	public void RightOuterJoinMatch()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void RightOuterJoinDuplicateLeft()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44" }), "No match");
	}

	[Test]
	public void RightOuterJoinDuplicateRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void RightOuterJoinDuplicateBoth()
	{
		var Left = new string[] { "1", "1", "2", "3", "4" };
		var Right = new string[] { "4", "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "11", "22", "33", "44", "44" }), "No match");
	}

	[Test]
	public void RightOuterJoinDuplicateAll()
	{
		var Left = new string[] { "1", "1", "1", "1", "1" };
		var Right = new string[] { "1", "1", "1", "1", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(Enumerable.Repeat("11", 5 * 5)), "No match");
	}

	[Test]
	public void RightOuterJoinLeft()
	{
		var Left = new string[] { "1", "2", "3" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "_4" }), "No match");
	}

	[Test]
	public void RightOuterJoinLeftDefault()
	{
		var Left = new string[] { "1", "2", "3" };
		var Right = new string[] { "4", "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", null).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33", "_4" }), "No match");
	}

	[Test]
	public void RightOuterJoinRight()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "3", "2", "1" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "11", "22", "33" }), "No match");
	}

	[Test]
	public void RightOuterJoinBoth()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResult).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "22", "33", "44", "_5" }), "No match");
	}

	[Test]
	public void RightOuterJoinBothDefault()
	{
		var Left = new string[] { "1", "2", "3", "4" };
		var Right = new string[] { "5", "4", "3", "2" };

		var Results = Left.RightOuterJoin(Right, ToKey, ToKey, ToResultUnsafe, "_", null).ToArray();

		Assert.That(Results, Is.EquivalentTo(new string[] { "22", "33", "44", "_5" }), "No match");
	}

	//****************************************

	private string ToKey(string? item)
	{
		return item ?? "";
	}

	private string ToResult(string left, string right)
	{
		return (left ?? "_") + (right ?? "_");
	}

	private string ToResultUnsafe(string? left, string? right)
	{
		return string.Concat(left, right);
	}
}
