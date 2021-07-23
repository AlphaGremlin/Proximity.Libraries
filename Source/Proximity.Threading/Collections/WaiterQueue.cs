using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides a lightweight threadsafe queue that allows erasing items, specifically for wait queues
	/// </summary>
	/// <remarks>Heavily modified from <see cref="ConcurrentQueue{T}"/></remarks>
	internal sealed class WaiterQueue<T> where T : class
	{	//****************************************
		private const int InitialSegmentLength = 32;
		private const int MaxSegmentLength = 1024 * 1024;
		//****************************************
		private readonly object _CrossSegmentLock;

		private volatile WaiterQueueSegment<T> _Tail, _Head;
		//****************************************

		/// <summary>
		/// Initializes a new instance of the <see cref="WaiterQueue{T}"/> class.
		/// </summary>
		public WaiterQueue()
		{
			_CrossSegmentLock = new object();
			_Tail = _Head = new WaiterQueueSegment<T>(InitialSegmentLength);
		}

		//****************************************

		/// <summary>Adds an object to the end of the <see cref="WaiterQueue{T}"/>.</summary>
		/// <param name="item">The object to add to the end of the <see cref="WaiterQueue{T}"/>. The value cannot be null</param>
		/// <returns>True if we're the first item in the queue, otherwise False</returns>
		public void Enqueue(T item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			for (; ;)
			{
				var Tail = _Tail;

				// Try to append to the existing tail.
				if (Tail.TryEnqueue(item))
					return;

				// If we were unsuccessful, take the lock so that we can compare and manipulate
				// the tail. Assuming another enqueuer hasn't already added a new segment,
				// do so, then loop around to try enqueueing again.
				lock (_CrossSegmentLock)
				{
					if (Tail == _Tail)
					{
						// Make sure no one else can enqueue to this segment.
						Tail.EnsureFrozenForEnqueues();

						// We determine the new segment's length based on the old length.
						// In general, we double the size of the segment, to make it less likely
						// that we'll need to grow again.  However, if the tail segment has a majority
						// of erased items, we leave the size as-is.
						var nextSize = (Tail.ErasedCount > Tail.Capacity / 2) ? Tail.Capacity : Math.Min(Tail.Capacity * 2, MaxSegmentLength);
						var newTail = new WaiterQueueSegment<T>(nextSize);

						// Hook up the new tail.
						Tail.NextSegment = newTail;
						_Tail = newTail;
					}
				}
			}
		}

		/// <summary>
		/// Attempts to remove and return the object at the beginning of the <see
		/// cref="ConcurrentQueue{T}"/>.
		/// </summary>
		/// <param name="result">
		/// When this method returns, if the operation was successful, <paramref name="result"/> contains the
		/// object removed. If no object was available to be removed, the value is unspecified.
		/// </param>
		/// <returns>
		/// true if an element was removed and returned from the beginning of the
		/// <see cref="ConcurrentQueue{T}"/> successfully; otherwise, false.
		/// </returns>
		public bool TryDequeue(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out T result)
		{
			// Dequeue items until we find a non-null one or run out
			for (; ;)
			{
				// Get the current head
				var head = _Head;

				// Try to take.  If we're successful, we're done.
				if (head.TryDequeue(out result!))
					return true;

				// Check to see whether this segment is the last. If it is, we can consider
				// this to be a moment-in-time empty condition (even though between the TryDequeue
				// check and this check, another item could have arrived).
				if (head.NextSegment == null)
					return false;

				// At this point we know that head.Next != null, which means
				// this segment has been frozen for additional enqueues. But between
				// the time that we ran TryDequeue and checked for a next segment,
				// another item could have been added.  Try to dequeue one more time
				// to confirm that the segment is indeed empty.
				Debug.Assert(head.FrozenForEnqueues);
				if (head.TryDequeue(out result!))
				{
					if (result != null)
						return true;

					continue;
				}

				// This segment is frozen (nothing more can be added) and empty (nothing is in it).
				// Update head to point to the next segment in the list, assuming no one's beat us to it.
				lock (_CrossSegmentLock)
				{
					if (head == _Head)
						_Head = head.NextSegment;
				}
			}
		}

		/// <summary>
		/// Erases a particular item from the <see cref="WaiterQueue{T}"/>
		/// </summary>
		/// <param name="item">The item to erase</param>
		/// <returns>True if the item was erased, otherwise False</returns>
		/// <remarks>Uses reference-equality</remarks>
		public bool Erase(T item)
		{
			// Get the current head
			var Segment = _Head;

			// Scan through until we hit the tail
			for (; ; )
			{
				// Try to take.  If we're successful, we're done.
				if (Segment.TryErase(item))
					return true;

				// Check to see whether this segment is the last. If it is, we can consider
				// this to be a moment-in-time empty condition (even though between the TryDequeue
				// check and this check, another item could have arrived).
				if (Segment.NextSegment == null)
					return false;

				Segment = Segment.NextSegment;
			}
		}

		/// <summary>Attempts to retrieve the value for the first element in the queue.</summary>
		/// <param name="result">The value of the first element, if found.</param>
		/// <returns>true if an element was found; otherwise, false.</returns>
		public bool TryPeek(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out T result)
		{
			// Starting with the head segment, look through all of the segments
			// for the first one we can find that's not empty.
			WaiterQueueSegment<T> s = _Head;
			while (true)
			{
				// Grab the next segment from this one, before we peek.
				// This is to be able to see whether the value has changed
				// during the peek operation.
				WaiterQueueSegment<T>? next = Volatile.Read(ref s.NextSegment);

				// Peek at the segment.  If we find an element, we're done.
				if (s.TryPeek(out result!))
					return true;

				// The current segment was empty at the moment we checked.

				if (next != null)
				{
					// If prior to the peek there was already a next segment, then
					// during the peek no additional items could have been enqueued
					// to it and we can just move on to check the next segment.
					Debug.Assert(next == s.NextSegment);
					s = next;
				}
				else if (Volatile.Read(ref s.NextSegment) == null)
				{
					// The next segment is null.  Nothing more to peek at.
					break;
				}

				// The next segment was null before we peeked but non-null after.
				// That means either when we peeked the first segment had
				// already been frozen but the new segment not yet added,
				// or that the first segment was empty and between the time
				// that we peeked and then checked _nextSegment, so many items
				// were enqueued that we filled the first segment and went
				// into the next.  Since we need to peek in order, we simply
				// loop around again to peek on the same segment.  The next
				// time around on this segment we'll then either successfully
				// peek or we'll find that next was non-null before peeking,
				// and we'll traverse to that segment.
			}

			result = default!;
			return false;
		}

		//****************************************

		/// <summary>
		/// Gets a value that indicates whether the <see cref="WaiterQueue{T}"/> is empty.
		/// </summary>
		/// <value>true if the <see cref="WaiterQueue{T}"/> is empty; otherwise, false.</value>
		public bool IsEmpty
		{
			get
			{
				// Starting with the head segment, look through all of the segments
				// for the first one we can find that's not empty.
				var Current = _Head;

				for (; ; )
				{
					// Grab the next segment from this one, before we peek.
					// This is to be able to see whether the value has changed
					// during the peek operation.
					var next = Volatile.Read(ref Current.NextSegment);

					// Peek at the segment.  If we find an element, we're done.
					if (Current.TryPeek(out _))
						return false;

					// The current segment was empty at the moment we checked.

					if (next != null)
					{
						// If prior to the peek there was already a next segment, then
						// during the peek no additional items could have been enqueued
						// to it and we can just move on to check the next segment.
						Debug.Assert(next == Current.NextSegment);
						Current = next;
					}
					else if (Volatile.Read(ref Current.NextSegment) == null)
					{
						// The next segment is null.  Nothing more to peek at.
						return true;
					}

					// The next segment was null before we peeked but non-null after.
					// That means either when we peeked the first segment had
					// already been frozen but the new segment not yet added,
					// or that the first segment was empty and between the time
					// that we peeked and then checked _nextSegment, so many items
					// were enqueued that we filled the first segment and went
					// into the next.  Since we need to peek in order, we simply
					// loop around again to peek on the same segment.  The next
					// time around on this segment we'll then either successfully
					// peek or we'll find that next was non-null before peeking,
					// and we'll traverse to that segment.
				}
			}
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="WaiterQueue{T}"/>.
		/// </summary>
		/// <value>The number of elements contained in the <see cref="WaiterQueue{T}"/>, including items that have been erased.</value>
		/// <remarks>
		/// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
		/// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
		/// property and comparing it to 0.
		/// </remarks>
		public int Count
		{
			get
			{
				SpinWait spinner = default;
				while (true)
				{
					// Capture the head and tail, as well as the head's head and tail.
					var head = _Head;
					var tail = _Tail;
					var headHead = Volatile.Read(ref head.HeadAndTail.Head);
					var headTail = Volatile.Read(ref head.HeadAndTail.Tail);

					if (head == tail)
					{
						// There was a single segment in the queue.  If the captured segments still
						// match, then we can trust the values to compute the segment's count. (It's
						// theoretically possible the values could have looped around and still exactly match,
						// but that would required at least ~4 billion elements to have been enqueued and
						// dequeued between the reads.)
						if (head == _Head &&
								tail == _Tail &&
								headHead == Volatile.Read(ref head.HeadAndTail.Head) &&
								headTail == Volatile.Read(ref head.HeadAndTail.Tail))
						{
							return head.GetCount(headHead, headTail);
						}
					}
					else if (head.NextSegment == tail)
					{
						// There were two segments in the queue.  Get the positions from the tail, and as above,
						// if the captured values match the previous reads, return the sum of the counts from both segments.
						var tailHead = Volatile.Read(ref tail.HeadAndTail.Head);
						var tailTail = Volatile.Read(ref tail.HeadAndTail.Tail);
						if (head == _Head &&
								tail == _Tail &&
								headHead == Volatile.Read(ref head.HeadAndTail.Head) &&
								headTail == Volatile.Read(ref head.HeadAndTail.Tail) &&
								tailHead == Volatile.Read(ref tail.HeadAndTail.Head) &&
								tailTail == Volatile.Read(ref tail.HeadAndTail.Tail))
						{
							return head.GetCount(headHead, headTail) + tail.GetCount(tailHead, tailTail);
						}
					}
					else
					{
						// There were more than two segments in the queue.  Fall back to taking the cross-segment lock,
						// which will ensure that the head and tail segments we read are stable (since the lock is needed to change them);
						// for the two-segment case above, we can simply rely on subsequent comparisons, but for the two+ case, we need
						// to be able to trust the internal segments between the head and tail.
						lock (_CrossSegmentLock)
						{
							// Now that we hold the lock, re-read the previously captured head and tail segments and head positions.
							// If either has changed, start over.
							if (head == _Head && tail == _Tail)
							{
								// Get the positions from the tail, and as above, if the captured values match the previous reads,
								// we can use the values to compute the count of the head and tail segments.
								var tailHead = Volatile.Read(ref tail.HeadAndTail.Head);
								var tailTail = Volatile.Read(ref tail.HeadAndTail.Tail);
								if (headHead == Volatile.Read(ref head.HeadAndTail.Head) &&
										headTail == Volatile.Read(ref head.HeadAndTail.Tail) &&
										tailHead == Volatile.Read(ref tail.HeadAndTail.Head) &&
										tailTail == Volatile.Read(ref tail.HeadAndTail.Tail))
								{
									// We got stable values for the head and tail segments, so we can just compute the sizes
									// based on those and add them. Note that this and the below additions to count may overflow: previous
									// implementations allowed that, so we don't check, either, and it is theoretically possible for the
									// queue to store more than int.MaxValue items.
									var count = head.GetCount(headHead, headTail) + tail.GetCount(tailHead, tailTail);

									// Now add the counts for each internal segment. Since there were segments before these,
									// for counting purposes we consider them to start at the 0th element, and since there is at
									// least one segment after each, each was frozen, so we can count until each's frozen tail.
									// With the cross-segment lock held, we're guaranteed that all of these internal segments are
									// consistent, as the head and tail segment can't be changed while we're holding the lock, and
									// dequeueing and enqueueing can only be done from the head and tail segments, which these aren't.
									for (var s = head.NextSegment!; s != tail; s = s.NextSegment!)
									{
										Debug.Assert(s.FrozenForEnqueues, "Internal segment must be frozen as there's a following segment.");
										count += s.HeadAndTail.Tail - s.FreezeOffset;
									}

									return count;
								}
							}
						}
					}

					// We raced with enqueues/dequeues and captured an inconsistent picture of the queue.
					// Spin and try again.
					spinner.SpinOnce();
				}
			}
		}

		public int Capacity
		{
			get
			{
				var Segment = _Head;

				var Total = Segment.Capacity;

				for (Segment = Segment.NextSegment; Segment != null; Segment = Segment.NextSegment)
				{
					Total += Segment.Capacity;
				}

				return Total;
			}
		}
	}
}
