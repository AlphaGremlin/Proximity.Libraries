using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the Asynchronous Reader/Writer Lock
/// </summary>
[TestFixture]
public sealed class AsyncReadWriteLockTests
{
	[Test, CancelAfter(1000)]
	public async Task SingleRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = false;
		//****************************************

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Resource = true;
		}

		//****************************************

		Assert.That(Resource, Is.True, "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task SingleWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = false;
		//****************************************

		using (await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Resource = true;
		}

		//****************************************

		Assert.That(Resource, Is.True, "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadUpgrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Assert.IsTrue(await Instance.Upgrade(CancellationToken.None).WaitAsync(token), "Upgrade was not exclusive");

			Assert.IsTrue(Instance.IsWriter, "Lock is not a writer");
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadUpgradeExclusive(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Assert.IsTrue(await Instance.TryUpgrade(CancellationToken.None).WaitAsync(token), "Upgrade was not exclusive");

			Assert.IsTrue(Instance.IsWriter, "Lock is not a writer");
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConcurrentRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.LockRead(CancellationToken.None))
					{
						await Task.Yield();

						Interlocked.Increment(ref Resource);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000), Repeat(100), Pairwise]
	public async Task ConcurrentReadWrite([Values(true, false)] bool unfairRead, [Values(true, false)] bool unfairWrite, CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock(unfairRead, unfairWrite);
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					if (count % 2 == 0)
					{
						using (await Lock.LockWrite(CancellationToken.None))
						{
							await Task.Yield();

							Interlocked.Increment(ref Resource);
						}
					}
					else
					{
						using (await Lock.LockRead(CancellationToken.None))
						{
							await Task.Yield();

							Interlocked.Increment(ref Resource);
						}
					}
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConcurrentWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.LockWrite(CancellationToken.None))
					{
						await Task.Yield();

						Interlocked.Increment(ref Resource);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(2000), Repeat(4)]
	public async Task ShortReadLongWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = 0;
		var InWrite = false;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 8).Select(
				async count =>
				{
					await Task.Yield();

					if (count % 2 == 0)
					{
						using (await Lock.LockWrite(CancellationToken.None))
						{
							InWrite = true;

							Thread.Sleep(100);

							Interlocked.Increment(ref Resource);

							InWrite = false;
						}
					}
					else
					{
						using (await Lock.LockRead(CancellationToken.None))
						{
							if (InWrite)
								Assert.Fail("Read during write");

							Interlocked.Increment(ref Resource);
						}
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(8), "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(5000), Repeat(4)]
	public async Task ShortWriteLongRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Resource = 0;
		var InWrite = false;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 8).Select(
				async count =>
				{
					await Task.Yield();

					if (count % 2 == 0)
					{
						using (await Lock.LockWrite(CancellationToken.None))
						{
							InWrite = true;

							Thread.Sleep(50);

							Interlocked.Increment(ref Resource);

							InWrite = false;
						}
					}
					else
					{
						using (await Lock.LockRead(CancellationToken.None))
						{
							if (InWrite)
								Assert.Fail("Read during write");

							Thread.Sleep(100);

							Interlocked.Increment(ref Resource);
						}
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(8), "Block not entered");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Read, Write;
		//****************************************

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Write = Lock.LockWrite(CancellationToken.None);

			Read = Lock.LockRead(CancellationToken.None);

			Assert.IsFalse(Read.IsCompleted, "Read completed");
		}

		Assert.That(Read.IsCompleted, Is.False, "Read completed");

		(await Write.WaitAsync(token)).Dispose();

		//****************************************

		(await Read.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteReadUnfair(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock(true);
		ValueTask<AsyncReadWriteLock.Instance> Read, Write;
		//****************************************

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Write = Lock.LockWrite(CancellationToken.None);

			Read = Lock.LockRead(CancellationToken.None);

			Assert.IsTrue(Read.IsCompleted, "Read not completed");
		}

		(await Read.WaitAsync(token)).Dispose();

		//****************************************

		(await Write.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteUpgrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			var Write = Lock.LockWrite(CancellationToken.None);

			var Upgrade = Instance.Upgrade(CancellationToken.None);

			Assert.IsFalse(Upgrade.IsCompleted, "Upgrade completed");
			Assert.IsFalse(Instance.IsWriter, "Instance is writer");

			(await Write.WaitAsync(token)).Dispose();

			Assert.IsFalse(await Upgrade, "Upgrade completed exclusively");
			Assert.IsTrue(Instance.IsWriter, "Instance is reader");
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteUpgradeExclusive(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Write;
		//****************************************

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Write = Lock.LockWrite(CancellationToken.None);

			var Upgrade = Instance.TryUpgrade(CancellationToken.None);

			Assert.IsTrue(Upgrade.IsCompleted, "Upgrade not completed");
			Assert.IsFalse(await Upgrade.WaitAsync(token), "Upgrade completed exclusively");

			Assert.IsFalse(Instance.IsWriter, "Instance is writer");
		}

		(await Write.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteReadWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Read, Write;
		//****************************************

		using (await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Read = Lock.LockRead(CancellationToken.None);

			Write = Lock.LockWrite(CancellationToken.None);

			Assert.IsFalse(Write.IsCompleted, "Read completed");
		}

		// Waiting reads take priority over pending writes in fair mode
		(await Read.WaitAsync(token)).Dispose();

		(await Write.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteReadDowngrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Read;
		//****************************************

		using (var Instance = await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Read = Lock.LockRead(CancellationToken.None);

			Assert.IsFalse(Read.IsCompleted, "Read completed");

			Instance.Downgrade();

			(await Read.WaitAsync(token)).Dispose();
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteReadWriteUnfair(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock(true);
		ValueTask<AsyncReadWriteLock.Instance> Read, Write;
		//****************************************

		using (await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Read = Lock.LockRead(CancellationToken.None);

			Write = Lock.LockWrite(CancellationToken.None);

			Assert.IsFalse(Write.IsCompleted, "Read completed");
		}

		// Writes slip in front of reads in unfair mode
		(await Write.WaitAsync(token)).Dispose();

		(await Read.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteCancelRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Writer = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		var Reader = Lock.LockRead(CancelSource.Token);

		CancelSource.Cancel();

		Writer.Dispose();

		//****************************************

		Assert.That(Reader.IsCanceled, Is.True, "Did not cancel");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteCancelWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Writer1 = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		var Writer2 = Lock.LockWrite(CancelSource.Token);

		CancelSource.Cancel();

		Writer1.Dispose();

		//****************************************

		try
		{
			await Writer2.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteCancelReadRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Reader;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			CancelSource.Cancel();

			Reader = Lock.LockRead(CancelSource.Token);
		}

		//****************************************

		try
		{
			await Reader.WaitAsync(token);

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteCancelReadWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Writer = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		var Reader = Lock.LockRead(CancelSource.Token);

		CancelSource.Cancel();

		Writer.Dispose();

		(await Lock.LockWrite(CancellationToken.None).WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Reader.IsCanceled, Is.True, "Did not cancel");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteCancelReadMulti(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Writer = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		var Reader1 = Lock.LockRead(CancelSource.Token);
		var Reader2 = Lock.LockRead(CancellationToken.None);

		CancelSource.Cancel();

		Writer.Dispose();

		//****************************************

		Assert.That(Reader1.IsCanceled, Is.True, "Did not cancel");
		Assert.That(Reader2.IsCanceled, Is.False, "Cancelled");

		(await Reader2.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadCancelWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Reader = await Lock.LockRead(CancellationToken.None).WaitAsync(token);

		var Writer = Lock.LockWrite(CancelSource.Token);

		CancelSource.Cancel();

		Reader.Dispose();

		//****************************************

		await Task.Delay(100, token); // Release happens on another thread and there's nothing to wait on

		Assert.That(Writer.IsCanceled, Is.True, "Did not cancel");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteCancelReadRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Writer, Reader;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			CancelSource.Cancel();

			Writer = Lock.LockWrite(CancellationToken.None);

			Reader = Lock.LockRead(CancelSource.Token);
		}

		(await Writer.WaitAsync(token)).Dispose();

		//****************************************

		try
		{
			await Reader.WaitAsync(token);

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteCancelWriteRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Writer1;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			CancelSource.Cancel();

			Writer1 = Lock.LockWrite(CancellationToken.None);
		}

		var Writer2 = Lock.LockWrite(CancelSource.Token);

		(await Writer1).Dispose();

		//****************************************

		try
		{
			await Writer2.WaitAsync(token);

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadReadCancelUpgrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
			{
				var Upgrade = Instance.Upgrade(CancelSource.Token);

				CancelSource.Cancel();

				try
				{
					await Upgrade.WaitAsync(token);

					Assert.Fail("Did not cancel");
				}
				catch (OperationCanceledException)
				{
					Assert.That(token.IsCancellationRequested, Is.False);
				}
			}
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadReadCancelUpgradeRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
			{
				var Upgrade = Instance.Upgrade(CancelSource.Token);

				CancelSource.Cancel();

				try
				{
					await Upgrade.WaitAsync(token);

					Assert.Fail("Did not cancel");
				}
				catch (OperationCanceledException)
				{
					Assert.That(token.IsCancellationRequested, Is.False);
				}
			}

			// Ensure we can reach this point
			(await Lock.LockRead(CancellationToken.None).WaitAsync(token)).Dispose();
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteCancelUpgrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Write;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (var Instance = await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Write = Lock.LockWrite(CancellationToken.None);

			var Upgrade = Instance.Upgrade(CancelSource.Token);

			CancelSource.Cancel();

			// The Writer will have taken our place, so we need to release it
			(await Write.WaitAsync(token)).Dispose();

			try
			{
				await Upgrade.WaitAsync(token);

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}

			Assert.IsFalse(Instance.IsWriter, "Is Writer");
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadCancelWriteRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Reader1 = await Lock.LockRead(CancellationToken.None).WaitAsync(token);

		var Writer = Lock.LockWrite(CancelSource.Token);

		var Reader2 = Lock.LockRead(CancellationToken.None);

		CancelSource.Cancel();

		(await Reader2.WaitAsync(token)).Dispose();

		Reader1.Dispose();

		//****************************************

		await Task.Delay(100, token); // Release happens on another thread and there's nothing to wait on

		Assert.That(Writer.IsCanceled, Is.True, "Did not cancel");

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadCancelWriteMulti(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Reader = await Lock.LockRead(CancellationToken.None).WaitAsync(token);

		var Writer1 = Lock.LockWrite(CancelSource.Token);
		var Writer2 = Lock.LockWrite(CancellationToken.None);

		CancelSource.Cancel();

		Reader.Dispose();

		//****************************************

		Assert.That(Writer1.IsCanceled, Is.True, "Did not cancel");
		Assert.That(Writer2.IsCanceled, Is.False, "Cancelled");

		(await Writer2.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadCancelWriteMultiRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Reader = await Lock.LockRead(CancellationToken.None).WaitAsync(token);

		var Writer1 = Lock.LockWrite(CancelSource.Token);
		var Writer2 = Lock.LockWrite(CancellationToken.None);

		CancelSource.Cancel();

		Reader.Dispose();

		(await Writer2.WaitAsync(token)).Dispose();

		(await Lock.LockRead(CancellationToken.None).WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteTimeoutRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Reader;
		//****************************************

		using (await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Reader = Lock.LockRead(TimeSpan.FromMilliseconds(50), CancellationToken.None);

			await Task.Delay(100, token);
		}

		//****************************************

		try
		{
			await Reader.WaitAsync(token);

			Assert.Fail("Did not cancel");
		}
		catch (TimeoutException)
		{
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadTimeoutWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Writer;
		//****************************************

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Writer = Lock.LockWrite(TimeSpan.FromMilliseconds(50), CancellationToken.None);

			await Task.Delay(100, token);
		}

		//****************************************

		try
		{
			await Writer.WaitAsync(token);

			Assert.Fail("Did not cancel");
		}
		catch (TimeoutException)
		{
		}

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task NoTimeoutRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader = Lock.LockRead(TimeSpan.FromMilliseconds(50), CancellationToken.None);

		//****************************************

		Assert.That(Reader.IsCompleted, Is.True, "Is not completed");

		(await Reader.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task NoTimeoutWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(TimeSpan.FromMilliseconds(50), CancellationToken.None);

		//****************************************

		Assert.That(Writer.IsCompleted, Is.True, "Is not completed");

		(await Writer.WaitAsync(token)).Dispose();

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(2000)]
	public async Task StackBlow(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		var Depth = 0;
		Task ResultTask;
		Task[] Results;
		//****************************************

		using (await Lock.LockRead(CancellationToken.None).WaitAsync(token))
		{
			Results = Enumerable.Range(1, 40000).Select(
				async count =>
				{
					using (await Lock.LockWrite())
					{
						Depth++;
					}
				}).ToArray();

			ResultTask = Results[Results.Length - 1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
		}

		await ResultTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task Dispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		await Lock.DisposeAsync().WaitAsync(token);

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader = Lock.LockRead(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		(await Reader.WaitAsync(token)).Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadReadDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader1 = Lock.LockRead(CancellationToken.None);
		var Reader2 = Lock.LockRead(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		(await Reader1.WaitAsync(token)).Dispose();
		(await Reader2.WaitAsync(token)).Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadDisposeRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader = Lock.LockRead(CancellationToken.None);

		Lock.DisposeAsync().Ignore();

		try
		{
			(await Lock.LockRead(CancellationToken.None).WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Reader.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadDisposeContinueWithRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		var ReadTask = Lock.LockRead(CancellationToken.None).AsTask();

		var InnerReadTask = ReadTask.ContinueWith((task) => Lock.LockRead().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		Lock.DisposeAsync().Ignore();

		Writer.Dispose();

		//****************************************

		try
		{
			(await ReadTask.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		try
		{
			(await InnerReadTask.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");

		Assert.That(ReadTask.IsFaulted, Is.True, "Read did not fault");
		Assert.That(ReadTask.Exception!.InnerException, Is.InstanceOf<ObjectDisposedException>(), "Read is not disposed");
		Assert.That(InnerReadTask.IsFaulted, Is.True, "Inner Read did not fault");
		Assert.That(InnerReadTask.Exception!.InnerException, Is.InstanceOf<ObjectDisposedException>(), "Inner Read is not disposed");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadWriteDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader = Lock.LockRead(CancellationToken.None);

		var Writer = Lock.LockWrite(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			(await Writer.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Reader.WaitAsync(token)).Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		(await Writer.WaitAsync(token)).Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteDisposeWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(CancellationToken.None);

		Lock.DisposeAsync().Ignore();

		try
		{
			(await Lock.LockWrite(CancellationToken.None).WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Writer.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteDisposeRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			(await Lock.LockRead(CancellationToken.None).WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Writer.WaitAsync(token)).Dispose();

		await Dispose.WaitAsync(token);

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteDisposeContinueWithWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Reader = await Lock.LockRead(CancellationToken.None).WaitAsync(token);

		var WriteTask = Lock.LockWrite(CancellationToken.None).AsTask();

		var InnerWriteTask = WriteTask.ContinueWith((task) => Lock.LockWrite().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		Lock.DisposeAsync().Ignore();

		Reader.Dispose();

		//****************************************

		try
		{
			(await WriteTask.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		try
		{
			(await InnerWriteTask.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteReadDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(CancellationToken.None);

		var Reader = Lock.LockRead(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			(await Reader.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Writer.WaitAsync(token)).Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteReadDisposeInterleave(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer = Lock.LockWrite(CancellationToken.None);

		var Reader = Lock.LockRead(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		// Release the Writer
		(await Writer.WaitAsync(token)).Dispose();

		try
		{
			// Make sure the Reader didn't activate
			(await Reader.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteWriteDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Writer1 = Lock.LockWrite(CancellationToken.None);

		var Writer2 = Lock.LockWrite(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			(await Writer2.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		(await Writer1.WaitAsync(token)).Dispose();

		await Dispose.WaitAsync(token);

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteWriteDowngrade(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		ValueTask<AsyncReadWriteLock.Instance> Write;
		//****************************************

		using (var Instance = await Lock.LockWrite(CancellationToken.None).WaitAsync(token))
		{
			Write = Lock.LockWrite(CancellationToken.None);

			Assert.IsFalse(Write.IsCompleted, "Write completed");

			Instance.Downgrade();

			Assert.IsFalse(Write.IsCompleted, "Write completed");
		}

		(await Write.WaitAsync(token)).Dispose();

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeRead(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		Lock.DisposeAsync().Ignore();

		try
		{
			(await Lock.LockRead(CancellationToken.None).WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeWrite(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		Lock.DisposeAsync().Ignore();

		try
		{
			(await Lock.LockWrite(CancellationToken.None).WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task ReadDualRelease(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Instance = await Lock.LockRead(CancellationToken.None);

		Instance.Dispose();

		Assert.That(() => Instance.Dispose(), Throws.InvalidOperationException, "Allowed dual release");

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task WriteDualRelease(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncReadWriteLock();
		//****************************************

		var Instance = await Lock.LockWrite(CancellationToken.None).WaitAsync(token);

		Instance.Dispose();

		ClassicAssert.Throws<InvalidOperationException>(() => Instance.Dispose(), "Allowed dual release");

		//****************************************

		Assert.That(Lock.IsReading, Is.False, "Reader still registered");
		Assert.That(Lock.IsWriting, Is.False, "Writer still registered");
		Assert.That(Lock.WaitingReaders, Is.Zero, "Readers still waiting");
		Assert.That(Lock.WaitingWriters, Is.Zero, "Writers still waiting");
	}
}
