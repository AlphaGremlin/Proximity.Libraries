using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Collections.Tests
{
	internal readonly struct Collider : IEquatable<Collider>
	{
		public Collider(int value) => Value = value;

		//****************************************

		public override bool Equals(object obj) => obj is Collider Other && Equals(Other);

		public bool Equals(Collider other) => Value == other.Value;

		public override int GetHashCode() => Value > int.MaxValue / 2 ? 1 : 0;

		//****************************************

		public int Value { get; }

		//****************************************

		public static implicit operator Collider(int value) => new Collider(value);
	}
}
