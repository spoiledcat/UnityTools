using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SpoiledCat.Threading.Tests
{
	using Logging;

	using System.ComponentModel;
	using Base.Tests;
	using NiceIO;
	using Extensions;

	[TestFixture]
	class SchedulerTests : BaseTest
	{
		private ActionTask GetTask(TaskAffinity affinity, int id, Action<int> body)
		{
			return new ActionTask(TaskManager, _ => body(id)) { Affinity = affinity };
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that don't depend on anything on the concurrent (default) scheduler
		/// run in any order
		/// </summary>
		[Test]
		public void ConcurrentSchedulerDoesNotGuaranteeOrdering()
		{
			var runningOrder = new List<int>();
			var rand = TestContext.CurrentContext.Random;
			var tasks = new List<ActionTask>();
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(TaskAffinity.Concurrent, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				}));
			}

			foreach (var task in tasks)
				TaskManager.Schedule(task);

			Task.WaitAll(tasks.Select(x => x.Task).ToArray());
			//Console.WriteLine(String.Join(",", runningOrder.Select(x => x.ToString()).ToArray()));
			Assert.AreEqual(10, runningOrder.Count);
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that depend on other things on the concurrent (default) scheduler
		/// run in dependency order. Each group of tasks depends on a task on the previous group, so the first group
		/// runs first, then the second group of tasks, then the third. Run order within each group is not guaranteed
		/// </summary>
		[Test]
		public void ConcurrentSchedulerWithDependencyOrdering()
		{
			var count = 3;
			var runningOrder = new List<int>();
			var rand = TestContext.CurrentContext.Random;
			var startTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				startTasks.Add(GetTask(TaskAffinity.Concurrent, i + 1, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				}));
			}

			var midTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = startTasks[i];
				midTasks.Add(previousTask.Then(GetTask(TaskAffinity.Concurrent, i + 11, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				})));
				;
			}

			var endTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = midTasks[i];
				endTasks.Add(previousTask.Then(GetTask(TaskAffinity.Concurrent, i + 21, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				})));
			}

			foreach (var t in endTasks)
				t.Start();
			Task.WaitAll(endTasks.Select(x => x.Task).ToArray());

			Assert.True(runningOrder.IndexOf(21) > runningOrder.IndexOf(11));
			Assert.True(runningOrder.IndexOf(11) > runningOrder.IndexOf(1));
			Assert.True(runningOrder.IndexOf(22) > runningOrder.IndexOf(12));
			Assert.True(runningOrder.IndexOf(12) > runningOrder.IndexOf(2));
			Assert.True(runningOrder.IndexOf(23) > runningOrder.IndexOf(13));
			Assert.True(runningOrder.IndexOf(13) > runningOrder.IndexOf(3));
		}

		[Test]
		public void ExclusiveSchedulerGuaranteesOrdering()
		{
			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = TestContext.CurrentContext.Random;
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(TaskAffinity.Exclusive, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				}));
			}

			foreach (var task in tasks)
				TaskManager.Schedule(task);
			Task.WaitAll(tasks.Select(x => x.Task).ToArray());
			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
		}

		[Test]
		public void UISchedulerGuaranteesOrdering()
		{
			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = TestContext.CurrentContext.Random;
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(TaskAffinity.UI, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock(runningOrder) runningOrder.Add(id);
				}));
			}

			foreach (var task in tasks)
				TaskManager.Schedule(task);
			Task.WaitAll(tasks.Select(x => x.Task).ToArray());
			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
		}

		[Test]
		public async Task NonUITasksAlwaysRunOnDifferentThreadFromUITasks()
		{
			var output = new Dictionary<int, int>();
			var tasks = new List<ITask>();

			var uiThread = 0;
			await new ActionTask(TaskManager, _ => uiThread = Thread.CurrentThread.ManagedThreadId) { Affinity = TaskAffinity.UI }.StartAsAsync();

			for (int i = 1; i < 100; i++)
			{
				tasks.Add(GetTask(i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
					lock(output) output.Add(id, Thread.CurrentThread.ManagedThreadId);
				}).Start());
			}

			Task.WaitAll(tasks.Select(x => x.Task).ToArray());
			CollectionAssert.DoesNotContain(output.Values, uiThread);
		}


		[Test]
		public async Task ChainingOnDifferentSchedulers()
		{
			var output = new Dictionary<int, KeyValuePair<int, int>>();
			var tasks = new List<ITask>();

			var uiThread = 0;
			await new ActionTask(TaskManager, _ => uiThread = Thread.CurrentThread.ManagedThreadId) { Affinity = TaskAffinity.UI }.StartAsAsync();

			for (int i = 1; i < 100; i++)
			{
				tasks.Add(GetTask(TaskAffinity.UI, i, id => {
					lock(output) output.Add(id, KeyValuePair.Create(Thread.CurrentThread.ManagedThreadId, -1));
				}).Then(GetTask(i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
					lock(output) output[id] = KeyValuePair.Create(output[id].Key, Thread.CurrentThread.ManagedThreadId);
				})).Start());
			}

			Task.WaitAll(tasks.Select(x => x.Task).ToArray());
			//Console.WriteLine(String.Join(",", output.Select(x => x.Key.ToString()).ToArray()));
			foreach (var t in output)
			{
				Assert.AreEqual(uiThread, t.Value.Key,
					$"Task {t.Key} pass 1 should have been on ui thread {uiThread} but ran instead on {t.Value.Key}");
				Assert.AreNotEqual(t.Value.Key, t.Value.Value, $"Task {t.Key} pass 2 should not have been on ui thread {uiThread}");
			}
		}
	}

	[TestFixture]
	class Chains : BaseTest
	{
		[Test]
		public async Task ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch()
		{
			var success = false;
			string thrown = "";
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI }
			           .Then((s, d) => output.Add(d)).Then(_ => { throw new Exception("an exception"); }).Catch(ex => thrown = ex.Message)
			           .Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
			           .Finally((s, e) => {
				           success = s;
				           finallyException = e;
			           }, TaskAffinity.Concurrent);

			await task.StartAndSwallowException();

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);
		}

		[Test]
		public async Task FinallyReportsException()
		{
			var success = false;
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI }
			           .Then((s, d) => output.Add(d)).Then(_ => { throw new Exception("an exception"); })
			           .Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
			           .Finally((s, e) => {
				           success = s;
				           finallyException = e;
			           }, TaskAffinity.Concurrent);

			await task.StartAndSwallowException();

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual("an exception", finallyException.Message);
		}

		[Test]
		public async Task CatchAlwaysRunsBeforeFinally()
		{
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI, Name = "Task 1" }
			           .Then((s, d) => output.Add(d)).Then(_ => { throw new Exception("an exception"); })
			           .Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive, Name = "Task 2" }).Then(
				           new FuncTask<string, string>(TaskManager, (s, d) => {
					           output.Add(d);
					           return "done";
				           }) { Name = "Task 3" }).Catch(ex => {
				           lock(runOrder)
				           {
					           exception = ex;
					           runOrder.Add("catch");
				           }
			           }).Finally((s, e, d) => {
				           lock(runOrder)
				           {
					           success = s;
					           finallyException = e;
					           runOrder.Add("finally");
				           }
				           return d;
			           }, TaskAffinity.Concurrent);

			await task.StartAndSwallowException();

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual("an exception", exception.Message);
			Assert.AreEqual("an exception", finallyException.Message);
			CollectionAssert.AreEqual(new List<string> { "catch", "finally" }, runOrder);
		}

		[Test]
		public async Task YouCanUseCatchAtTheEndOfAChain()
		{
			var success = false;
			Exception exception = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI }
			           .Then((s, d) => output.Add(d)).Then(_ => { throw new Exception("an exception"); })
			           .Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
			           .Finally((_, __) => {}, TaskAffinity.Concurrent).Catch(ex => { exception = ex; });

			await task.Start().Task;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);
		}

		[Test]
		public async Task FinallyCanReturnData()
		{
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI }
			           .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
			           .Then((s, d) => {
				           output.Add(d);
				           return "done";
			           }).Catch(ex => {
				           lock(runOrder)
				           {
					           exception = ex;
					           runOrder.Add("catch");
				           }
			           }).Finally((s, e, d) => {
				           lock(runOrder)
				           {
					           success = s;
					           output.Add(d);
					           finallyException = e;
					           runOrder.Add("finally");
				           }
				           return d;
			           }, TaskAffinity.Concurrent).ThenInUI((s, d) => {
				           lock(runOrder)
				           {
					           runOrder.Add("boo");
				           }
				           return d;
			           });

			var ret = await task.StartAsAsync();
			Assert.AreEqual("done", ret);
			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally", "boo" }, runOrder);
		}

		[Test]
		public async Task FinallyCanAlsoNotReturnData()
		{
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(TaskManager, _ => "one name") { Affinity = TaskAffinity.UI }
			           .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(TaskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
			           .Then((s, d) => {
				           output.Add(d);
				           return "done";
			           }).Finally((s, e, d) => {
				           lock(runOrder)
				           {
					           success = s;
					           output.Add(d);
					           finallyException = e;
					           runOrder.Add("finally");
				           }
			           }, TaskAffinity.Concurrent);

			await task.StartAsAsync();

			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally" }, runOrder);
		}
	}

	[TestFixture]
	class Exceptions : BaseTest
	{
		[Test]
		public async Task StartAndEndAreAlwaysRaised()
		{
			var runOrder = new List<string>();
			ITask task = new ActionTask(TaskManager, _ => { throw new Exception(); });
			task.OnStart += _ => runOrder.Add("start");
			task.OnEnd += (_, __, ___) => runOrder.Add("end");
			// we want to run a Finally on a new task (and not in-thread) so that the StartAndSwallowException handler runs after this
			// one, proving that the exception is propagated after everything is done
			task = task.Finally((_, __) => {}, TaskAffinity.Concurrent);

			await task.StartAndSwallowException();
			CollectionAssert.AreEqual(new string[] { "start", "end" }, runOrder);
		}

		[Test]
		public async Task ExceptionPropagatesOutIfNoFinally()
		{
			var task = new ActionTask(TaskManager, _ => { throw new InvalidOperationException(); }).Catch(_ => {});
			Func<Task> act = async () => await task.StartAsAsync();
			await act.Should().ThrowAsync<InvalidOperationException>();
		}

		[Test]
		public async Task AllFinallyHandlersAreCalledOnException()
		{
			Stopwatch watch;
			ILogging logger;
			StartTest(out watch, out logger);

			var task = new FuncTask<string>(TaskManager, () => { throw new InvalidOperationException(); });
			bool exceptionThrown1, exceptionThrown2;
			exceptionThrown1 = exceptionThrown2 = false;

			task.Finally(success => exceptionThrown1 = !success);
			task.Finally((success, _) => exceptionThrown2 = !success);

			StartTrackTime(watch);
			var waitTask = task.Start().Task;
			var ret = await Task.WhenAny(waitTask, Task.Delay(Timeout));
			StopTrackTimeAndLog(watch, logger);
			Assert.AreEqual(ret, waitTask);

			exceptionThrown1.Should().BeTrue();
			exceptionThrown2.Should().BeTrue();
		}

		[Test]
		public async Task StartAsyncWorks()
		{
			Stopwatch watch;
			ILogging logger;
			StartTest(out watch, out logger);

			var task = new FuncTask<int>(TaskManager, _ => 1);

			StartTrackTime(watch);
			var waitTask = task.StartAsAsync();
			var retTask = await Task.WhenAny(waitTask, Task.Delay(Timeout));
			StopTrackTimeAndLog(watch, logger);
			Assert.AreEqual(retTask, waitTask);
			var ret = await waitTask;

			Assert.AreEqual(1, ret);
		}

		[Test]
		public async Task MultipleCatchStatementsCanHappen()
		{
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(TaskManager, _ => { throw new InvalidOperationException(); }).Catch(e => {
				runOrder.Add("1");
				exceptions.Add(e);
			}).Then(_ => { throw new InvalidCastException(); }).Catch(e => {
				runOrder.Add("2");
				exceptions.Add(e);
			}).Then(_ => { throw new ArgumentNullException(); }).Catch(e => {
				runOrder.Add("3");
				exceptions.Add(e);
			}).Finally((b, e) => {}, TaskAffinity.Concurrent);
			await task.StartAndSwallowException();
			CollectionAssert.AreEqual(
				new string[] {
					typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name
				}, exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);
		}

		[Test]
		public async Task ContinueAfterException()
		{
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(TaskManager, _ => { throw new InvalidOperationException(); }).Catch(e => {
				runOrder.Add("1");
				exceptions.Add(e);
				return true;
			}).Then(_ => { throw new InvalidCastException(); }).Catch(e => {
				runOrder.Add("2");
				exceptions.Add(e);
				return true;
			}).Then(_ => { throw new ArgumentNullException(); }).Catch(e => {
				runOrder.Add("3");
				exceptions.Add(e);
				return true;
			}).Finally((s, e) => {}, TaskAffinity.Concurrent);
			await task.StartAndSwallowException();
			CollectionAssert.AreEqual(
				new string[] { typeof(InvalidOperationException).Name, typeof(InvalidCastException).Name, typeof(ArgumentNullException).Name },
				exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);
		}

		[Test]
		public async Task StartAwaitSafelyAwaits()
		{
			var task = new ActionTask(TaskManager, _ => { throw new InvalidOperationException(); }).Catch(_ => {});
			await task.StartAwait(_ => {});
		}
	}

	[TestFixture]
	class TaskToActionTask : BaseTest
	{
		[Test]
		public async Task CanWrapATask()
		{
			var runOrder = new List<string>();
			var task = new Task(() => runOrder.Add($"ran"));
			var act = new TPLTask(TaskManager, task) { Affinity = TaskAffinity.Exclusive };
			await act.Start().Task;
			CollectionAssert.AreEqual(new string[] { $"ran" }, runOrder);
		}

		private async Task<List<int>> GetData(List<int> v)
		{
			await Task.Delay(10);
			v.Add(1);
			return v;
		}

		private async Task<List<int>> GetData2(List<int> v)
		{
			await Task.Delay(10);
			v.Add(3);
			return v;
		}

		[Test]
		public async Task Inlining()
		{
			var runOrder = new List<string>();
			var act = new ActionTask(TaskManager, _ => runOrder.Add($"started"))
			          .Then(Task.FromResult(1), TaskAffinity.Exclusive).Then((_, n) => n + 1).Then((_, n) => runOrder.Add(n.ToString()))
			          .Then(Task.FromResult(20f), TaskAffinity.Exclusive).Then((_, n) => n + 1).Then((_, n) => runOrder.Add(n.ToString()))
			          .Finally((s, _) => runOrder.Add("done"), TaskAffinity.Concurrent);
			await act.StartAsAsync();
			CollectionAssert.AreEqual(new string[] { "started", "2", "21", "done" }, runOrder);
		}
	}

	[TestFixture]
	class Dependencies : BaseTest
	{
		class TestActionTask : ActionTask
		{
			public TestActionTask(ITaskManager taskManager, Action<bool> action) : base(taskManager, action)
			{}

			public TaskBase Test_GetFirstStartableTask()
			{
				return base.GetTopMostStartableTask();
			}
		}

		[Test]
		public async Task GetTopOfChain_ReturnsTopMostInCreatedState()
		{
			var task1 = new ActionTask(TaskManager, () => {});
			await task1.StartAwait();
			var task2 = new TestActionTask(TaskManager, _ => {});
			var task3 = new TestActionTask(TaskManager, _ => {});

			task1.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
			Assert.AreSame(task2, top);
		}

		[Test]
		public void GetTopOfChain_ReturnsTopTaskWhenNotStarted()
		{
			var task1 = new TPLTask(TaskManager, Task.FromResult(true));
			var task2 = new TestActionTask(TaskManager, _ => {});
			var task3 = new TestActionTask(TaskManager, _ => {});

			task1.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
			Assert.AreSame(task1, top);
		}

		public async Task GetFirstStartableTask_ReturnsNullWhenItsAlreadyStarted()
		{
			var task1 = new ActionTask(TaskManager, () => {});
			await task1.StartAwait();
			var task2 = new TestActionTask(TaskManager, _ => {});
			var task3 = new TestActionTask(TaskManager, _ => {});

			task1.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();
			Assert.AreSame(task2, top);
		}

		public void GetFirstStartableTask_ReturnsTopTaskWhenNotStarted()
		{
			var task1 = new ActionTask(TaskManager, () => {});
			var task2 = new TestActionTask(TaskManager, _ => {});
			var task3 = new TestActionTask(TaskManager, _ => {});

			task1.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();
			Assert.AreSame(task1, top);
		}

		[Test]
		public async Task MergingTwoChainsWorks()
		{
			var callOrder = new List<string>();
			var dependsOrder = new List<ITask>();

			var innerChainTask1 = new TPLTask(TaskManager, Task.FromResult(LogAndReturnResult(callOrder, "chain2 completed1", true)));
			var innerChainTask2 = innerChainTask1.Then(_ => {
				callOrder.Add("chain2 FuncTask<string>");
				return "1";
			});

			var innerChainTask3 = innerChainTask2.Finally((s, e, d) => {
				callOrder.Add("chain2 Finally");
				return d;
			}, TaskAffinity.Concurrent);


			var outerChainTask1 = new FuncTask<int>(TaskManager, _ => {
				callOrder.Add("chain1 FuncTask<int>");
				return 1;
			});
			var outerChainTask2 = outerChainTask1.Then(innerChainTask3);

			var outerChainTask3 = outerChainTask2.Finally((s, e) => { callOrder.Add("chain1 Finally"); }, TaskAffinity.Concurrent);

			await outerChainTask3.StartAwait();

			var dependsOn = outerChainTask3;
			while (dependsOn != null)
			{
				dependsOrder.Add(dependsOn);
				dependsOn = dependsOn.DependsOn;
			}

			Assert.AreEqual(innerChainTask3, outerChainTask2);
			CollectionAssert.AreEqual(new ITask[] { outerChainTask1, innerChainTask1, innerChainTask2, innerChainTask3, outerChainTask3 },
				dependsOrder.Reverse<ITask>().ToArray());

			CollectionAssert.AreEqual(
				new string[] { "chain2 completed1", "chain1 FuncTask<int>", "chain2 FuncTask<string>", "chain2 Finally", "chain1 Finally" },
				callOrder);
		}

		private T LogAndReturnResult<T>(List<string> callOrder, string msg, T result)
		{
			callOrder.Add(msg);
			return result;
		}
	}

	[TestFixture]
	class TaskQueueTests : BaseTest
	{
		[Test]
		public void ConvertsTaskResultsCorrectly()
		{
			var vals = new string[] { "2.1", Math.PI.ToString(), "1" };
			var expected = new double[] { 2.1, Math.PI, 1.0 };
			var queue = new TaskQueue<string, double>(TaskManager, task => Double.Parse(task.Result));
			vals.All(s => {
				queue.Queue(new TPLTask<string>(TaskManager, Task.FromResult(s)));
				return true;
			});
			var ret = queue.RunSynchronously();
			Assert.AreEqual(expected.Join(","), ret.Join(","));
		}

		[Test]
		public void ThrowsIfCannotConvert()
		{
			Assert.Throws<ArgumentNullException>(() => new TaskQueue<string, double>(TaskManager));
			// NPath has an implicit operator to string, but we cannot verify this without using
			// reflection, so a converter is required
			Assert.Throws<ArgumentNullException>(() => new TaskQueue<NPath, string>(TaskManager));
		}

		[Test]
		public void DoesNotThrowIfItCanConvert()
		{
			Assert.DoesNotThrow(() => new TaskQueue<DownloadTask, ITask>(TaskManager));
		}

		[Test]
		public void FailingTasksThrowCorrectlyEvenIfFinallyIsPresent()
		{
			var queue = new TaskQueue(TaskManager);
			var task = new ActionTask(TaskManager, () => throw new InvalidOperationException()).Finally((s, e) => {});
			queue.Queue(task);
			Assert.Throws<InvalidOperationException>(() => queue.RunSynchronously());
		}

		[Test]
		public async Task DoubleSchedulingStartsOnlyOnce()
		{
			var runOrder = new List<string>();
			var queue = new TaskQueue(TaskManager);
			var task1 = new FuncTask<string>(TaskManager, () => {
				runOrder.Add("1");
				return "2";
			});
			task1.OnStart += _ => runOrder.Add("start 1");
			task1.OnEnd += (a, b, c, d) => runOrder.Add("end 1");
			var task2 = new FuncTask<string, string>(TaskManager, (_, str) => {
				runOrder.Add(str);
				return "3";
			});
			task2.OnStart += _ => runOrder.Add("start 2");
			task2.OnEnd += (a, b, c, d) => runOrder.Add("end 2");
			var task3 = new FuncTask<string, string>(TaskManager, (_, str) => {
				runOrder.Add(str);
				return "4";
			});
			task3.OnStart += _ => runOrder.Add("start 3");
			task3.OnEnd += (a, b, c, d) => runOrder.Add("end 3");

			queue.Queue(task1.Then(task2).Then(task3));
			await queue.StartAwait();
			var expected = new string[] { "start 1", "1", "end 1", "start 2", "2", "end 2", "start 3", "3", "end 3", };
			Assert.AreEqual(expected.Join(","), runOrder.Join(","));
		}
	}

	[TestFixture]
	// for some reason these two are failing in appveyor, suspect nunit is doing something stupid
	[Category("DoNotRunOnAppVeyor")]
	class DependencyTests : BaseTest
	{
		[Test]
		public async Task RunningDifferentTasksDependingOnPreviousResult()
		{
			var callOrder = new List<string>();

			var taskEnd = new ActionTask(TaskManager, () => callOrder.Add("chain completed")) { Name = "Chain Completed" };
			var final = taskEnd.Finally((_, __) => {}, TaskAffinity.Exclusive);

			var taskStart = new FuncTask<bool>(TaskManager, _ => {
				callOrder.Add("chain start");
				return false;
			}) { Name = "Chain Start" }.Then(new ActionTask<bool>(TaskManager, (_, __) => {
				callOrder.Add("failing");
				throw new InvalidOperationException();
			}) { Name = "Failing" });

			taskStart.Then(new ActionTask(TaskManager, () => { callOrder.Add("on failure"); }) { Name = "On Failure" },
				runOptions: TaskRunOptions.OnFailure).Then(taskEnd, taskIsTopOfChain: true);

			taskStart.Then(new ActionTask(TaskManager, () => { callOrder.Add("on success"); }) { Name = "On Success" },
				runOptions: TaskRunOptions.OnSuccess).Then(taskEnd, taskIsTopOfChain: true);

			await final.StartAndSwallowException();


			Assert.AreEqual(new string[] { "chain start", "failing", "on failure", "chain completed" }.Join(","), callOrder.Join(","));
		}

		[Test]
		public async Task TaskOnFailureGetsCalledWhenExceptionHappensUpTheChain()
		{
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(TaskManager, _ => throw new InvalidOperationException())
			           .Then(_ => runOrder.Add("1"))
			           .Catch(ex => exceptions.Add(ex))
			           .Then(() => runOrder.Add("OnFailure"),
				           runOptions: TaskRunOptions.OnFailure)
			           .Finally((s, e) => {}, TaskAffinity.Concurrent);

			await task.StartAwait(_ => {});

			CollectionAssert.AreEqual(new string[] { typeof(InvalidOperationException).Name }, exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "OnFailure" }, runOrder);
		}

	}


	static class KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue>(key, value);
		}
	}

}
