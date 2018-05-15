/****************************************\
 RemoteTaskTests.cs
 Created: 2014-05-15
\****************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of RemoteTaskTests.
	/// </summary>
	[TestFixture]
	public class RemoteTaskTests
	{	//****************************************
		private AppDomain _OtherDomain;
		
		private RemoteTaskHost _RemoteHost;
		//****************************************
		
		[OneTimeSetUp()]
		public void Setup()
		{
			var CurrentSetup = AppDomain.CurrentDomain.SetupInformation;
			var MySetup = new AppDomainSetup();

			MySetup.ApplicationBase = Path.Combine(CurrentSetup.ApplicationBase, "Base");

			_OtherDomain = AppDomain.CreateDomain("Remote Task Tests", AppDomain.CurrentDomain.Evidence, MySetup);

			//_OtherDomain.UnhandledException += (sender, e) => { Trace.WriteLine(string.Format("Unhandled Remote Exception {0}", e.ExceptionObject.ToString())); throw new ApplicationException("Unhandled Remote Exception", (Exception)e.ExceptionObject); };
			
			_RemoteHost = (RemoteTaskHost)_OtherDomain.CreateInstanceAndUnwrap(typeof(RemoteTaskHost).Assembly.FullName, typeof(RemoteTaskHost).FullName);
			
			_RemoteHost.Prepare();
			
			LifetimeServices.LeaseManagerPollTime = new TimeSpan(0, 0, 1);
			LifetimeServices.RenewOnCallTime = new TimeSpan(0, 0, 1);
			LifetimeServices.SponsorshipTimeout = new TimeSpan(0, 0, 1);
			LifetimeServices.LeaseTime = new TimeSpan(0, 0, 2);
		}

		[OneTimeTearDown()]
		public void Teardown()
		{
			_RemoteHost = null;
			
			AppDomain.Unload(_OtherDomain);
		}
		
		[TearDown()]
		public void Cleanup()
		{
			GC.Collect();
		}
		
		//****************************************
		
		[Test, Timeout(1000)]
		public async Task FinishedTask()
		{
			await _RemoteHost.FinishedTask();
		}
		
		[Test, Timeout(1000)]
		public async Task FinishedTaskBool()
		{
			var Result = await _RemoteHost.FinishedBoolTask();
			
			Assert.IsTrue(Result);
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTask()
		{
			await _RemoteHost.DelayTask(50);
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTaskBool()
		{
			var Result = await _RemoteHost.DelayBoolTask(50);
			
			Assert.IsTrue(Result);
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTaskNoCancel()
		{
			using (var MySource = new CancellationTokenSource())
				using (var MyRemoteSource = RemoteCancellationToken.FromLocal(MySource))
			{
				await _RemoteHost.DelayTaskCancel(75, MyRemoteSource);
			}
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTaskCancel()
		{
			using (var MySource = new CancellationTokenSource(50))
			using (var MyRemoteSource = MySource.ToRemote())
			{
				var MyTask = _RemoteHost.DelayTaskCancel(100, MyRemoteSource);
				
				try
				{
					await MyTask;
					
					Assert.Fail("Did not cancel");
				}
				catch (OperationCanceledException)
				{
					Assert.Pass("Cancelled successfully");
				}
			}
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTaskNoCancelBool()
		{
			using (var MySource = new CancellationTokenSource())
			using (var MyRemoteSource = MySource.Token.ToRemote())
			{
				await _RemoteHost.DelayBoolTaskCancel(75, MyRemoteSource);
			}
		}
		
		[Test, Timeout(1000)]
		public async Task DelayTaskCancelBool()
		{
			using (var MySource = new CancellationTokenSource(50))
				using (var MyRemoteSource = new RemoteCancellationToken(MySource.Token))
			{
				var MyTask = _RemoteHost.DelayBoolTaskCancel(100, MyRemoteSource);
				
				try
				{
					await MyTask;
					
					Assert.Fail("Did not cancel");
				}
				catch (OperationCanceledException)
				{
					Assert.Pass("Cancelled successfully");
				}
			}
		}
		
		[Test, Timeout(1000)]
		public void ThrowTask()
		{
			Assert.ThrowsAsync<ApplicationException>(() => _RemoteHost.ThrowTask().ToTask());
		}

		[Test, Timeout(1000)]
		public async Task ThrowTaskExternal()
		{
			try
			{
				await GetExceptionThrower().RaiseException("Test Exception");

				Assert.Fail("Did not throw");
			}
			catch (ApplicationException e)
			{
				Assert.AreEqual("Test Exception", e.Message);
			}

			Assert.False(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName.StartsWith("Proximity.Utility.Tests.Help")), "Helper Assembly was loaded in our AppDomain");
		}

		[Test, Timeout(10000)]
		public async Task ThrowTaskExternalCustom()
		{
			try
			{
				await GetExceptionThrower().RaiseCustomException("Test Exception");

				Assert.Fail("Did not throw");
			}
			catch (Exception e)
			{
				Trace.WriteLine(e.ToString());
			}

			Assert.False(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName.StartsWith("Proximity.Utility.Tests.Help")), "Helper Assembly was loaded in our AppDomain. It should be in a subfolder and not with the test assembly");
		}
		
		[Test(), Timeout(15000)]
		public async Task LeaseTimeout()
		{
			var MyTask = (Task)_RemoteHost.DelayTask(10000);
			
			await Task.Delay(10000);
			
			await MyTask;
		}
		
		[Test(), Timeout(15000)]
		public async Task LeaseTimeoutBool()
		{
			var MyTask = (Task<bool>)_RemoteHost.DelayBoolTask(10000);
			
			await Task.Delay(10000);
			
			var MyResult = await MyTask;
			
			Assert.IsTrue(MyResult);
		}
		
		[Test(), Timeout(15000)]
		public async Task LeaseTimeoutNoCancel()
		{
			using (var MySource = new CancellationTokenSource(11000))
			using (var MyRemoteSource = new RemoteCancellationToken(MySource))
			{
				var MyTask = (Task)_RemoteHost.DelayTaskCancel(10000, MyRemoteSource);
				
				await Task.Delay(10000);
				
				await MyTask;
			}
		}
		
		[Test(), Timeout(15000)]
		public async Task LeaseTimeoutCancel()
		{
			using (var MySource = new CancellationTokenSource(9000))
			using (var MyRemoteSource = MySource.Token.ToRemote())
			{
				var MyTask = (Task)_RemoteHost.DelayTaskCancel(10000, MyRemoteSource);
				
				await Task.Delay(10000);
				
				try
				{
					await MyTask;
					
					Assert.Fail("Did not cancel");
				}
				catch (OperationCanceledException)
				{
					Assert.Pass("Cancelled successfully");
				}
			}
		}

		//****************************************

		private IExceptionThrower GetExceptionThrower()
		{
			return (IExceptionThrower)_OtherDomain.CreateInstanceAndUnwrap("Proximity.Utility.Tests.Help", "Proximity.Utility.Tests.Help.ExceptionThrower");
		}

		//****************************************
		
		private class RemoteTaskHost : MarshalByRefObject
		{
			public void Prepare()
			{
				LifetimeServices.LeaseManagerPollTime = new TimeSpan(0, 0, 1);
				LifetimeServices.RenewOnCallTime = new TimeSpan(0, 0, 1);
				LifetimeServices.SponsorshipTimeout = new TimeSpan(0, 0, 1);
				LifetimeServices.LeaseTime = new TimeSpan(0, 0, 2);
			}
			
			public override object InitializeLifetimeService()
			{
				var MyLease = (ILease)base.InitializeLifetimeService();
				
				if (MyLease.CurrentState == LeaseState.Initial)
				{
					MyLease.InitialLeaseTime = TimeSpan.FromMinutes(1);
					MyLease.SponsorshipTimeout = TimeSpan.FromMinutes(2);
					MyLease.RenewOnCallTime = TimeSpan.FromSeconds(2);
				}
				
				return MyLease;
			}
			
			//****************************************
			
			public RemoteTask FinishedTask()
			{
				return ((Task)Task.FromResult<bool>(true)).ToRemote();
			}
			
			public RemoteTask<bool> FinishedBoolTask()
			{
				return Task.FromResult<bool>(true).ToRemote();
			}
			
			public RemoteTask DelayTask(int timeout)
			{
				return Task.Delay(timeout).ToRemote();
			}
			
			public RemoteTask<bool> DelayBoolTask(int timeout)
			{
				return RemoteTask.Start(
					async () =>
					{
						await Task.Delay(timeout);
						
						return true;
					});
			}
			
			public RemoteTask DelayTaskCancel(int timeout, RemoteCancellationToken remoteToken)
			{
				return RemoteTask.Start(
					async (token) =>
					{
						await Task.Delay(timeout, token);
					}, remoteToken);
			}
			
			public RemoteTask<bool> DelayBoolTaskCancel(int timeout, RemoteCancellationToken remoteToken)
			{
				return RemoteTask.Start(
					async (token) =>
					{
						await Task.Delay(timeout);
						
						token.ThrowIfCancellationRequested();
						
						return true;
					}, remoteToken);
			}
			
			public RemoteTask ThrowTask()
			{
				return RemoteTask.Start(
					async () =>
					{
						await Task.Yield();
						
						throw new ApplicationException("Throw Task");
					});
			}
			
			public RemoteTask YieldTask()
			{
				return RemoteTask.Start(
					async () =>
					{
						await Task.Yield();
					});
			}
		}

		public interface IExceptionThrower
		{
			RemoteTask RaiseException(string message);

			RemoteTask RaiseCustomException(string message);
		}
	}
}
