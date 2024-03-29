// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides a multi-producer, multi-consumer thread-safe bounded segment.  When the queue is full,
	/// enqueues fail and return false.  When the queue is empty, dequeues fail and return null.
	/// These segments are linked together to form the unbounded <see cref="ConcurrentQueue{T}"/>.
	/// </summary>
	[DebuggerDisplay("Capacity = {Capacity}")]
	internal sealed class WaiterQueueSegment<T> where T : class
	{
		// Segment design is inspired by the algorithm outlined at:
		// http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue

		/// <summary>The array of items in this queue.  Each slot contains the item in that slot and its "sequence number".</summary>
		internal readonly Slot[] Slots;
		/// <summary>Mask for quickly accessing a position within the queue's array.</summary>
		private readonly int _SlotsMask;
		/// <summary>The head and tail positions, with padding to help avoid false sharing contention.</summary>
		/// <remarks>Dequeuing happens from the head, enqueuing happens at the tail.</remarks>
		internal PaddedHeadAndTail HeadAndTail; // mutable struct: do not make this readonly

		/// <summary>Indicates whether the segment has been marked such that no additional items may be enqueued.</summary>
		internal bool FrozenForEnqueues;
		/// <summary>The segment following this one in the queue, or null if this segment is the last in the queue.</summary>
		internal WaiterQueueSegment<T>? NextSegment;
		internal int ErasedCount;

		//****************************************

		/// <summary>Creates the segment.</summary>
		/// <param name="boundedLength">
		/// The maximum number of elements the segment can contain.  Must be a power of 2.
		/// </param>
		internal WaiterQueueSegment(int boundedLength)
		{
			// Validate the length
			Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
			Debug.Assert((boundedLength & (boundedLength - 1)) == 0, $"Must be a power of 2, got {boundedLength}");

			// Initialize the slots and the mask.  The mask is used as a way of quickly doing "% _slots.Length",
			// instead letting us do "& _slotsMask".
			Slots = new Slot[boundedLength];
			_SlotsMask = boundedLength - 1;

			// Initialize the sequence number for each slot.  The sequence number provides a ticket that
			// allows dequeuers to know whether they can dequeue and enqueuers to know whether they can
			// enqueue.  An enqueuer at position N can enqueue when the sequence number is N, and a dequeuer
			// for position N can dequeue when the sequence number is N + 1.  When an enqueuer is done writing
			// at position N, it sets the sequence number to N + 1 so that a dequeuer will be able to dequeue,
			// and when a dequeuer is done dequeueing at position N, it sets the sequence number to N + _slots.Length,
			// so that when an enqueuer loops around the slots, it'll find that the sequence number at
			// position N is N.  This also means that when an enqueuer finds that at position N the sequence
			// number is < N, there is still a value in that slot, i.e. the segment is full, and when a
			// dequeuer finds that the value in a slot is < N + 1, there is nothing currently available to
			// dequeue. (It is possible for multiple enqueuers to enqueue concurrently, writing into
			// subsequent slots, and to have the first enqueuer take longer, so that the slots for 1, 2, 3, etc.
			// may have values, but the 0th slot may still be being filled... in that case, TryDequeue will
			// return false.)
			for (var i = 0; i < Slots.Length; i++)
			{
				Slots[i].SequenceNumber = i;
			}
		}

		//****************************************

		/// <summary>
		/// Ensures that the segment will not accept any subsequent enqueues that aren't already underway.
		/// </summary>
		/// <remarks>
		/// When we mark a segment as being frozen for additional enqueues,
		/// we set the <see cref="FrozenForEnqueues"/> bool, but that's mostly
		/// as a small helper to avoid marking it twice.  The real marking comes
		/// by modifying the Tail for the segment, increasing it by this
		/// <see cref="FreezeOffset"/>.  This effectively knocks it off the
		/// sequence expected by future enqueuers, such that any additional enqueuer
		/// will be unable to enqueue due to it not lining up with the expected
		/// sequence numbers.  This value is chosen specially so that Tail will grow
		/// to a value that maps to the same slot but that won't be confused with
		/// any other enqueue/dequeue sequence number.
		/// </remarks>
		internal void EnsureFrozenForEnqueues() // must only be called while queue's segment lock is held
		{
			if (!FrozenForEnqueues) // flag used to ensure we don't increase the Tail more than once if frozen more than once
			{
				FrozenForEnqueues = true;
				Interlocked.Add(ref HeadAndTail.Tail, FreezeOffset);
			}
		}

		/// <summary>Tries to dequeue an element from the queue.</summary>
		/// <param name="item">Receives the dequeued element, which may be null if it was removed</param>
		internal bool TryDequeue(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
# endif
			out T item)
		{
			Slot[] slots = Slots;

			// Loop in case of contention...
			SpinWait spinner = default;
			while (true)
			{
				// Get the head at which to try to dequeue.
				var currentHead = Volatile.Read(ref HeadAndTail.Head);
				var slotsIndex = currentHead & _SlotsMask;

				// Read the sequence number for the head position.
				var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

				// We can dequeue from this slot if it's been filled by an enqueuer, which
				// would have left the sequence number at pos+1.
				var diff = sequenceNumber - (currentHead + 1);
				if (diff == 0)
				{
					// We may be racing with other dequeuers.  Try to reserve the slot by incrementing
					// the head.  Once we've done that, no one else will be able to read from this slot,
					// and no enqueuer will be able to read from this slot until we've written the new
					// sequence number. WARNING: The next few lines are not reliable on a runtime that
					// supports thread aborts. If a thread abort were to sneak in after the CompareExchange
					// but before the Volatile.Write, enqueuers trying to enqueue into this slot would
					// spin indefinitely.  If this implementation is ever used on such a platform, this
					// if block should be wrapped in a finally / prepared region.
					if (Interlocked.CompareExchange(ref HeadAndTail.Head, currentHead + 1, currentHead) == currentHead)
					{
						// Successfully reserved the slot.  Note that after the above CompareExchange, other threads
						// trying to dequeue from this slot will end up spinning until we do the subsequent Write.
						item = Interlocked.Exchange(ref slots[slotsIndex].Item, null)!;

						if (item == null)
							Interlocked.Decrement(ref ErasedCount);

						Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);

						if (item == null)
							continue; // Dequeued an empty item, so loop and try again

						return true;
					}
				}
				else if (diff < 0)
				{
					// The sequence number was less than what we needed, which means this slot doesn't
					// yet contain a value we can dequeue, i.e. the segment is empty.  Technically it's
					// possible that multiple enqueuers could have written concurrently, with those
					// getting later slots actually finishing first, so there could be elements after
					// this one that are available, but we need to dequeue in order.  So before declaring
					// failure and that the segment is empty, we check the tail to see if we're actually
					// empty or if we're just waiting for items in flight or after this one to become available.
					var frozen = FrozenForEnqueues;
					var currentTail = Volatile.Read(ref HeadAndTail.Tail);
					if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
					{
						item = default!;
						return false;
					}

					// It's possible it could have become frozen after we checked _frozenForEnqueues
					// and before reading the tail.  That's ok: in that rare race condition, we just
					// loop around again.
				}

				// Lost a race. Spin a bit, then try again.
				spinner.SpinOnce();
			}
		}

		/// <summary>Tries to peek at an non-erased element from the queue, removing erased items as necessary.</summary>
		public bool TryPeek(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T result)
		{
			Slot[] slots = Slots;

			// Loop in case of contention...
			SpinWait spinner = default;
			while (true)
			{
				// Get the head at which to try to peek.
				var currentHead = Volatile.Read(ref HeadAndTail.Head);
				var slotsIndex = currentHead & _SlotsMask;

				// Read the sequence number for the head position.
				var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

				// We can peek from this slot if it's been filled by an enqueuer, which
				// would have left the sequence number at pos+1.
				var diff = sequenceNumber - (currentHead + 1);
				if (diff == 0)
				{
					var Result = slots[slotsIndex].Item;

					if (Result != null)
					{
						result = Result;

						return true; // Head item exists
					}

					// We may be racing with other dequeuers.  Try to reserve the slot by incrementing
					// the head.  Once we've done that, no one else will be able to read from this slot,
					// and no enqueuer will be able to read from this slot until we've written the new
					// sequence number. WARNING: The next few lines are not reliable on a runtime that
					// supports thread aborts. If a thread abort were to sneak in after the CompareExchange
					// but before the Volatile.Write, enqueuers trying to enqueue into this slot would
					// spin indefinitely.  If this implementation is ever used on such a platform, this
					// if block should be wrapped in a finally / prepared region.
					if (Interlocked.CompareExchange(ref HeadAndTail.Head, currentHead + 1, currentHead) == currentHead)
					{
						Interlocked.Decrement(ref ErasedCount);
						// Successfully reserved the slot.  Note that after the above CompareExchange, other threads
						// trying to dequeue from this slot will end up spinning until we do the subsequent Write.
						Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);

						continue; // Removed the erased item, loop back and try again
					}
				}
				else if (diff < 0)
				{
					// The sequence number was less than what we needed, which means this slot doesn't
					// yet contain a value we can peek, i.e. the segment is empty.  Technically it's
					// possible that multiple enqueuers could have written concurrently, with those
					// getting later slots actually finishing first, so there could be elements after
					// this one that are available, but we need to peek in order.  So before declaring
					// failure and that the segment is empty, we check the tail to see if we're actually
					// empty or if we're just waiting for items in flight or after this one to become available.
					var frozen = FrozenForEnqueues;
					var currentTail = Volatile.Read(ref HeadAndTail.Tail);
					if (currentTail - currentHead <= 0 || (frozen && (currentTail - FreezeOffset - currentHead <= 0)))
					{
						result = default!;
						return false;
					}

					// It's possible it could have become frozen after we checked _frozenForEnqueues
					// and before reading the tail.  That's ok: in that rare race condition, we just
					// loop around again.
				}

				// Lost a race. Spin a bit, then try again.
				spinner.SpinOnce();
			}
		}

		/// <summary>
		/// Attempts to enqueue the item.  If successful, the item will be stored
		/// in the queue and true will be returned; otherwise, the item won't be stored, and false
		/// will be returned.
		/// </summary>
		internal bool TryEnqueue(T item)
		{
			Slot[] slots = Slots;

			// Loop in case of contention...
			SpinWait spinner = default;
			while (true)
			{
				// Get the tail at which to try to return.
				var currentTail = Volatile.Read(ref HeadAndTail.Tail);
				var slotsIndex = currentTail & _SlotsMask;

				// Read the sequence number for the tail position.
				var sequenceNumber = Volatile.Read(ref slots[slotsIndex].SequenceNumber);

				// The slot is empty and ready for us to enqueue into it if its sequence
				// number matches the slot.
				var diff = sequenceNumber - currentTail;
				if (diff == 0)
				{
					// We may be racing with other enqueuers.  Try to reserve the slot by incrementing
					// the tail.  Once we've done that, no one else will be able to write to this slot,
					// and no dequeuer will be able to read from this slot until we've written the new
					// sequence number. WARNING: The next few lines are not reliable on a runtime that
					// supports thread aborts. If a thread abort were to sneak in after the CompareExchange
					// but before the Volatile.Write, other threads will spin trying to access this slot.
					// If this implementation is ever used on such a platform, this if block should be
					// wrapped in a finally / prepared region.
					if (Interlocked.CompareExchange(ref HeadAndTail.Tail, currentTail + 1, currentTail) == currentTail)
					{
						// Successfully reserved the slot.  Note that after the above CompareExchange, other threads
						// trying to return will end up spinning until we do the subsequent Write.
						slots[slotsIndex].Item = item;
						Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentTail + 1);
						return true;
					}
				}
				else if (diff < 0)
				{
					// The sequence number was less than what we needed, which means this slot still
					// contains a value, i.e. the segment is full.  Technically it's possible that multiple
					// dequeuers could have read concurrently, with those getting later slots actually
					// finishing first, so there could be spaces after this one that are available, but
					// we need to enqueue in order.
					return false;
				}

				// Lost a race. Spin a bit, then try again.
				spinner.SpinOnce();
			}
		}

		/// <summary>Computes the number of items in a segment based on a fixed head and tail in that segment.</summary>
		internal int GetCount(int head, int tail)
		{
			if (head != tail && head != tail - FreezeOffset)
			{
				head &= _SlotsMask;
				tail &= _SlotsMask;

				return head < tail ? tail - head : Slots.Length - head + tail;
			}

			return 0;
		}

		/// <summary>Tries to erase an element from the queue.</summary>
		/// <param name="item">The element to erase</param>
		internal bool TryErase(T item)
		{
			Slot[] slots = Slots;

			// What is the current head? We scan backwards, until we find it or we hit the tail
			var currentHead = Volatile.Read(ref HeadAndTail.Head);
			// Since the item we want to erase should already be inside, if we hit the tail that means
			// it's already been erased or dequeued
			var currentTail = Volatile.Read(ref HeadAndTail.Tail);

			while (true)
			{
				// Get the head at which to try to start examining.
				var slotsIndex = currentHead & _SlotsMask;

				// If we've reached the tail, we've run through the whole segment and not found the item
				// because it was never there, or because someone dequeued it first
				if (currentHead >= currentTail)
					return false;

				// If this item is our target, replace it with null
				if (Interlocked.CompareExchange(ref slots[slotsIndex].Item, null, item) == item)
				{
					Interlocked.Increment(ref ErasedCount);

					if (Volatile.Read(ref HeadAndTail.Head) == currentHead)
					{
						// We're (still) the head of the segment. Do a fake dequeue
						if (Interlocked.CompareExchange(ref HeadAndTail.Head, currentHead + 1, currentHead) == currentHead)
						{
							// Successfully reserved the slot.  Note that after the above CompareExchange, other threads
							// trying to dequeue from this slot will end up spinning until we do the subsequent Write.
							Interlocked.Decrement(ref ErasedCount);
							Volatile.Write(ref slots[slotsIndex].SequenceNumber, currentHead + slots.Length);
						}
						// If it fails, someone else did the dequeue, so we just continue
					}
					// If we're not the head, this segment may end up filling with erased items we can't free the slots for.
					// This makes the worst case scenario one entire segment per live item

					return true;
				}

				currentHead++;
			}
		}

		//****************************************

		/// <summary>Gets the number of elements this segment can store.</summary>
		internal int Capacity => Slots.Length;

		/// <summary>Gets the "freeze offset" for this segment.</summary>
		internal int FreezeOffset => Slots.Length * 2;

		//****************************************

		/// <summary>Represents a slot in the queue.</summary>
		[StructLayout(LayoutKind.Auto)]
		[DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
		internal struct Slot
		{
			/// <summary>The item.</summary>
			public T? Item;
			/// <summary>The sequence number for this slot, used to synchronize between enqueuers and dequeuers.</summary>
			public int SequenceNumber;
		}
	}

	/// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
	[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
	[StructLayout(LayoutKind.Explicit, Size = 3 * 64)] // padding before/between/after fields
	internal struct PaddedHeadAndTail
	{
		[FieldOffset(1 * 64)] public int Head;
		[FieldOffset(2 * 64)] public int Tail;
	}
}
