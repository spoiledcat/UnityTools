using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ThreadingTests
{
	using System.Collections;
	using NUnit.Framework.Internal;
	using SpoiledCat.Extensions;
	using SpoiledCat.NiceIO;
	using SpoiledCat.Threading;
	using UnityEngine.TestTools;

	class BaseTest
	{
		protected const int Timeout = 30000;
		protected const int RandomSeed = 120938;

		protected ActionTask GetTask(ITaskManager taskManager, TaskAffinity affinity, int id, Action<int> body)
		{
			return new ActionTask(taskManager, _ => body(id)) { Affinity = affinity };
		}
	}

	[TestFixture]
	class SchedulerTests : BaseTest
	{
		/// <summary>
		/// This exemplifies that running a bunch of tasks that don't depend on anything on the concurrent (default) scheduler
		/// run in any order
		/// </summary>
		[UnityTest]
		public IEnumerator ConcurrentSchedulerDoesNotGuaranteeOrdering()
		{
			var taskManager = new TaskManager().Initialize();

			var runningOrder = new List<int>();
			var rand = new Random(RandomSeed);
			var tasks = new List<ActionTask>();
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.Concurrent, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			tasks.ForEach(x => taskManager.Schedule(x));
			yield return null;
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;

			Assert.AreEqual(10, runningOrder.Count);
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that depend on other things on the concurrent (default) scheduler
		/// run in dependency order. Each group of tasks depends on a task on the previous group, so the first group
		/// runs first, then the second group of tasks, then the third. Run order within each group is not guaranteed
		/// </summary>
		[UnityTest]
		public IEnumerator ConcurrentSchedulerWithDependencyOrdering()
		{
			var taskManager = new TaskManager().Initialize();
			var count = 3;
			var runningOrder = new List<int>();
			var rand = new Random(RandomSeed);
			var startTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				startTasks.Add(GetTask(taskManager, TaskAffinity.Concurrent, i + 1, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			var midTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = startTasks[i];
				midTasks.Add(previousTask.Then(GetTask(taskManager, TaskAffinity.Concurrent, i + 11, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				})));
				;
			}

			var endTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = midTasks[i];
				endTasks.Add(previousTask.Then(GetTask(taskManager, TaskAffinity.Concurrent, i + 21, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				})));
			}

			endTasks.ForEach(x => x.Start());
			yield return null;
			while (!endTasks.All(x => x.Task.IsCompleted)) yield return null;

			Assert.True(runningOrder.IndexOf(21) > runningOrder.IndexOf(11));
			Assert.True(runningOrder.IndexOf(11) > runningOrder.IndexOf(1));
			Assert.True(runningOrder.IndexOf(22) > runningOrder.IndexOf(12));
			Assert.True(runningOrder.IndexOf(12) > runningOrder.IndexOf(2));
			Assert.True(runningOrder.IndexOf(23) > runningOrder.IndexOf(13));
			Assert.True(runningOrder.IndexOf(13) > runningOrder.IndexOf(3));
		}

		[UnityTest]
		public IEnumerator ExclusiveSchedulerGuaranteesOrdering()
		{
			var taskManager = new TaskManager().Initialize();
			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = new Random(RandomSeed);
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.Exclusive, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			tasks.ForEach(x => taskManager.Schedule(x));
			yield return null;
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;

			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
		}

		[UnityTest]
		public IEnumerator UISchedulerGuaranteesOrdering()
		{
			var taskManager = new TaskManager().Initialize();
			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = new Random(RandomSeed);
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.UI, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			tasks.ForEach(x => taskManager.Schedule(x));
			yield return null;
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;

			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
		}

		[UnityTest]
		public IEnumerator NonUITasksAlwaysRunOnDifferentThreadFromUITasks()
		{
			var taskManager = new TaskManager().Initialize();
			var output = new Dictionary<int, int>();
			var tasks = new List<ITask>();

			var uiThread = Thread.CurrentThread.ManagedThreadId;
			Assert.AreEqual(1, uiThread);

			for (int i = 1; i < 100; i++)
			{
				tasks.Add(GetTask(taskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
					lock (output) output.Add(id, Thread.CurrentThread.ManagedThreadId);
				}).Start());
			}

			tasks.ForEach(x => x.Start());
			yield return null;
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;

			CollectionAssert.DoesNotContain(output.Values, uiThread);
		}


		[UnityTest]
		public IEnumerator ChainingOnDifferentSchedulers()
		{
			var taskManager = new TaskManager().Initialize();
			var output = new Dictionary<int, KeyValuePair<int, int>>();
			var tasks = new List<ITask>();
            var doneEvt = new ManualResetEventSlim(false);
            var taskCount = 100;

			var uiThread = Thread.CurrentThread.ManagedThreadId;
			Assert.AreEqual(1, uiThread);

			for (int i = 1; i <= taskCount; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.UI, i, id => {
					lock (output) output.Add(id, KeyValuePair.Create(Thread.CurrentThread.ManagedThreadId, -1));
				}).Then(GetTask(taskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
					lock (output) output[id] = KeyValuePair.Create(output[id].Key, Thread.CurrentThread.ManagedThreadId);
				})));
			}

            tasks.ForEach(x => x.Start());
            yield return null;
            while (!tasks.All(x => x.Task.IsCompleted)) yield return null;

			Assert.AreEqual(taskCount, output.Count);

			foreach (var t in output)
			{
				Assert.AreEqual(uiThread, t.Value.Key,
					$"Task {t.Key} pass 1 should have been on ui thread {uiThread} but ran instead on {t.Value.Key}");
				Assert.AreNotEqual(t.Value.Key, t.Value.Value, $"Task {t.Key} pass 2 should not have been on ui thread {uiThread}");
			}
		}
	}

	class Chains : BaseTest
	{
		[UnityTest]
		public IEnumerator ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			string thrown = "";
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(_ => throw new Exception("an exception")).Catch(ex => thrown = ex.Message)
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
					   .Finally((s, e) => {
						   success = s;
						   finallyException = e;
					   }, TaskAffinity.Concurrent);

			task.Start();

			yield return null;
			while (!task.IsCompleted)
			{
				yield return null;
			}

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);
            Assert.AreEqual("an exception", thrown);
		}

		[UnityTest]
		public IEnumerator FinallyReportsException()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(_ => throw new Exception("an exception"))
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
					   .Finally((s, e) => {
						   success = s;
						   finallyException = e;
					   }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual("an exception", finallyException.Message);
		}

		[UnityTest]
		public IEnumerator CatchAlwaysRunsBeforeFinally()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI, Name = "Task 1" }
					   .Then((s, d) => output.Add(d)).Then(_ => throw new Exception("an exception"))
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive, Name = "Task 2" }).Then(
						   new FuncTask<string, string>(taskManager, (s, d) => {
							   output.Add(d);
							   return "done";
						   }) { Name = "Task 3" }).Catch(ex => {
							   lock (runOrder)
							   {
								   exception = ex;
								   runOrder.Add("catch");
							   }
						   }).Finally((s, e, d) => {
							   lock (runOrder)
							   {
								   success = s;
								   finallyException = e;
								   runOrder.Add("finally");
							   }
							   return d;
						   }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual("an exception", exception.Message);
			Assert.AreEqual("an exception", finallyException.Message);
			CollectionAssert.AreEqual(new List<string> { "catch", "finally" }, runOrder);
		}

		[UnityTest]
		public IEnumerator YouCanUseCatchAtTheEndOfAChain()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			Exception exception = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(_ => { throw new Exception("an exception"); })
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
					   .Finally((_, __) => { }, TaskAffinity.Concurrent).Catch(ex => { exception = ex; });

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);
		}

		[UnityTest]
		public IEnumerator FinallyCanReturnData()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
					   .Then((s, d) => {
						   output.Add(d);
						   return "done";
					   }).Catch(ex => {
						   lock (runOrder)
						   {
							   exception = ex;
							   runOrder.Add("catch");
						   }
					   }).Finally((s, e, d) => {
						   lock (runOrder)
						   {
							   success = s;
							   output.Add(d);
							   finallyException = e;
							   runOrder.Add("finally");
						   }
						   return d;
					   }, TaskAffinity.Concurrent).ThenInUI((s, d) => {
						   lock (runOrder)
						   {
							   runOrder.Add("boo");
						   }
						   return d;
					   });

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;
			var ret = task.Result;

			Assert.AreEqual("done", ret);
			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally", "boo" }, runOrder);
		}

		[UnityTest]
		public IEnumerator FinallyCanAlsoNotReturnData()
		{
			var taskManager = new TaskManager().Initialize();
			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
					   .Then((s, d) => {
						   output.Add(d);
						   return "done";
					   }).Finally((s, e, d) => {
						   lock (runOrder)
						   {
							   success = s;
							   output.Add(d);
							   finallyException = e;
							   runOrder.Add("finally");
						   }
					   }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally" }, runOrder);
		}
	}

	class Exceptions : BaseTest
	{
		[UnityTest]
		public IEnumerator StartAndEndAreAlwaysRaised()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			ITask task = new ActionTask(taskManager, _ => { throw new Exception(); });
			task.OnStart += _ => runOrder.Add("start");
			task.OnEnd += (_, __, ___) => runOrder.Add("end");
			// we want to run a Finally on a new task (and not in-thread) so that the StartAndSwallowException handler runs after this
			// one, proving that the exception is propagated after everything is done
			task = task.Finally((_, __) => { }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			CollectionAssert.AreEqual(new string[] { "start", "end" }, runOrder);
		}

		[UnityTest]
		public IEnumerator ExceptionPropagatesOutIfNoFinally()
		{
			var taskManager = new TaskManager().Initialize();
			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException()).Catch(_ => { });

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

            Assert.IsFalse(task.Successful);
            Assert.IsTrue(task.Exception is InvalidOperationException);
		}

		[UnityTest]
		public IEnumerator AllFinallyHandlersAreCalledOnException()
		{
			var taskManager = new TaskManager().Initialize();

			var task = new FuncTask<string>(taskManager, () => throw new InvalidOperationException());
			bool exceptionThrown1, exceptionThrown2;
			exceptionThrown1 = exceptionThrown2 = false;

			var wait1 = task.Finally(success => exceptionThrown1 = !success);
			var wait2 = task.Finally((success, _) => exceptionThrown2 = !success);

			task.Start();
			yield return null;
			while (!wait1.IsCompleted && !wait2.IsCompleted) yield return null;

			Assert.IsTrue(exceptionThrown1);
			Assert.IsTrue(exceptionThrown2);
		}

		[UnityTest]
		public IEnumerator MultipleCatchStatementsCanHappen()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException()).Catch(e => {
				runOrder.Add("1");
				exceptions.Add(e);
			}).Then(_ => throw new InvalidCastException()).Catch(e => {
				runOrder.Add("2");
				exceptions.Add(e);
			}).Then(_ => throw new ArgumentNullException()).Catch(e => {
				runOrder.Add("3");
				exceptions.Add(e);
			}).Finally((b, e) => { }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			CollectionAssert.AreEqual(
				new[] {
					typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name
				}, exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);
		}

		[UnityTest]
		public IEnumerator ContinueAfterException()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException()).Catch(e => {
				runOrder.Add("1");
				exceptions.Add(e);
				return true;
			}).Then(_ => throw new InvalidCastException()).Catch(e => {
				runOrder.Add("2");
				exceptions.Add(e);
				return true;
			}).Then(_ => throw new ArgumentNullException()).Catch(e => {
				runOrder.Add("3");
				exceptions.Add(e);
				return true;
			}).Finally((s, e) => { }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			CollectionAssert.AreEqual(
				new[] { typeof(InvalidOperationException).Name, typeof(InvalidCastException).Name, typeof(ArgumentNullException).Name },
				exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);
		}
	}

	[TestFixture]
	class TaskToActionTask
	{
		[UnityTest]
		public IEnumerator CanWrapATask()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var tplTask = new Task(() => runOrder.Add("ran"));
			var task = new TPLTask(taskManager, tplTask) { Affinity = TaskAffinity.Exclusive };

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			CollectionAssert.AreEqual(new[] { "ran" }, runOrder);
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

		[UnityTest]
		public IEnumerator Inlining()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var task = new ActionTask(taskManager, _ => runOrder.Add($"started"))
					  .Then(Task.FromResult(1), TaskAffinity.Exclusive).Then((_, n) => n + 1).Then((_, n) => runOrder.Add(n.ToString()))
					  .Then(Task.FromResult(20f), TaskAffinity.Exclusive).Then((_, n) => n + 1).Then((_, n) => runOrder.Add(n.ToString()))
					  .Finally((s, _) => runOrder.Add("done"), TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			CollectionAssert.AreEqual(new string[] { "started", "2", "21", "done" }, runOrder);
		}
	}

	class Dependencies : BaseTest
	{
		class TestActionTask : ActionTask
		{
			public TestActionTask(ITaskManager taskManager, Action<bool> action) : base(taskManager, action)
			{ }

			public TaskBase Test_GetFirstStartableTask()
			{
				return base.GetTopMostStartableTask();
			}
		}

		[UnityTest]
		public IEnumerator GetTopOfChain_ReturnsTopMostInCreatedState()
		{
			var taskManager = new TaskManager().Initialize();
			var task = new ActionTask(taskManager, () => { });

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
            Assert.AreSame(task2, top);
		}

		[Test]
		public void GetTopOfChain_ReturnsTopTaskWhenNotStarted()
		{
			var taskManager = new TaskManager().Initialize();
			var task1 = new TPLTask(taskManager, Task.FromResult(true));
			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task1.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
			Assert.AreSame(task1, top);
		}

		[UnityTest]
		public IEnumerator GetFirstStartableTask_ReturnsNullWhenStarted()
		{
			var taskManager = new TaskManager().Initialize();
			var task = new ActionTask(taskManager, () => { });

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();
			Assert.AreSame(null, top);
		}

		[Test]
		public void GetFirstStartableTask_ReturnsTopTaskWhenNotStarted()
		{
			var taskManager = new TaskManager().Initialize();
			var task1 = new ActionTask(taskManager, () => { });
			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task1.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();
			Assert.AreSame(task1, top);
		}

		[UnityTest]
		public IEnumerator MergingTwoChainsWorks()
		{
			var taskManager = new TaskManager().Initialize();
			var callOrder = new List<string>();
			var dependsOrder = new List<ITask>();

			var innerChainTask1 = new TPLTask(taskManager, Task.FromResult(LogAndReturnResult(callOrder, "chain2 completed1", true)));
			var innerChainTask2 = innerChainTask1.Then(_ => {
				callOrder.Add("chain2 FuncTask<string>");
				return "1";
			});

			var innerChainTask3 = innerChainTask2.Finally((s, e, d) => {
				callOrder.Add("chain2 Finally");
				return d;
			}, TaskAffinity.Concurrent);


			var outerChainTask1 = new FuncTask<int>(taskManager, _ => {
				callOrder.Add("chain1 FuncTask<int>");
				return 1;
			});
			var outerChainTask2 = outerChainTask1.Then(innerChainTask3);

			var outerChainTask3 = outerChainTask2.Finally((s, e) => { callOrder.Add("chain1 Finally"); }, TaskAffinity.Concurrent);

			outerChainTask3.Start();
			yield return null;
			while (!outerChainTask3.IsCompleted) yield return null;

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

	class TaskQueueTests : BaseTest
	{
		[Test]
		public void ConvertsTaskResultsCorrectly()
		{
			var taskManager = new TaskManager().Initialize();
			var vals = new string[] { "2.1", Math.PI.ToString(), "1" };
			var expected = new double[] { 2.1, Math.PI, 1.0 };
			var queue = new TaskQueue<string, double>(taskManager, task => Double.Parse(task.Result));
			vals.All(s => {
				queue.Queue(new TPLTask<string>(taskManager, Task.FromResult(s)));
				return true;
			});
			var ret = queue.RunSynchronously();
			Assert.AreEqual(expected.Join(","), ret.Join(","));
		}

		[Test]
		public void ThrowsIfCannotConvert()
		{
			var taskManager = new TaskManager().Initialize();
			Assert.Throws<ArgumentNullException>(() => new TaskQueue<string, double>(taskManager));
			// NPath has an implicit operator to string, but we cannot verify this without using
			// reflection, so a converter is required
			Assert.Throws<ArgumentNullException>(() => new TaskQueue<NPath, string>(taskManager));
		}

		[Test]
		public void DoesNotThrowIfItCanConvert()
		{
			var taskManager = new TaskManager().Initialize();
			Assert.DoesNotThrow(() => new TaskQueue<DownloadTask, ITask>(taskManager));
		}

		[Test]
		public void FailingTasksThrowCorrectlyEvenIfFinallyIsPresent()
		{
			var taskManager = new TaskManager().Initialize();
			var queue = new TaskQueue(taskManager);
			var task = new ActionTask(taskManager, () => throw new InvalidOperationException()).Finally((s, e) => { });
			queue.Queue(task);
			Assert.Throws<InvalidOperationException>(() => queue.RunSynchronously());
		}

		[UnityTest]
		public IEnumerator DoubleSchedulingStartsOnlyOnce()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var queue = new TaskQueue(taskManager);
			var task1 = new FuncTask<string>(taskManager, () => {
				runOrder.Add("1");
				return "2";
			});
			task1.OnStart += _ => runOrder.Add("start 1");
			task1.OnEnd += (a, b, c, d) => runOrder.Add("end 1");
			var task2 = new FuncTask<string, string>(taskManager, (_, str) => {
				runOrder.Add(str);
				return "3";
			});
			task2.OnStart += _ => runOrder.Add("start 2");
			task2.OnEnd += (a, b, c, d) => runOrder.Add("end 2");
			var task3 = new FuncTask<string, string>(taskManager, (_, str) => {
				runOrder.Add(str);
				return "4";
			});
			task3.OnStart += _ => runOrder.Add("start 3");
			task3.OnEnd += (a, b, c, d) => runOrder.Add("end 3");

			queue.Queue(task1.Then(task2).Then(task3));

			queue.Start();
			yield return null;
			while (!queue.IsCompleted) yield return null;

			var expected = new string[] { "start 1", "1", "end 1", "start 2", "2", "end 2", "start 3", "3", "end 3", };
			Assert.AreEqual(expected.Join(","), runOrder.Join(","));
		}
	}

	class DependencyTests : BaseTest
	{
		[UnityTest]
		public IEnumerator RunningDifferentTasksDependingOnPreviousResult()
		{
			var taskManager = new TaskManager().Initialize();
			var callOrder = new List<string>();

			var taskEnd = new ActionTask(taskManager, () => callOrder.Add("chain completed")) { Name = "Chain Completed" };
			var final = taskEnd.Finally((_, __) => { }, TaskAffinity.Exclusive);

			var taskStart = new FuncTask<bool>(taskManager, _ => {
				callOrder.Add("chain start");
				return false;
			}) { Name = "Chain Start" }.Then(new ActionTask<bool>(taskManager, (_, __) => {
				callOrder.Add("failing");
				throw new InvalidOperationException();
			}) { Name = "Failing" });

			taskStart.Then(new ActionTask(taskManager, () => { callOrder.Add("on failure"); }) { Name = "On Failure" },
				runOptions: TaskRunOptions.OnFailure).Then(taskEnd, taskIsTopOfChain: true);

			taskStart.Then(new ActionTask(taskManager, () => { callOrder.Add("on success"); }) { Name = "On Success" },
				runOptions: TaskRunOptions.OnSuccess).Then(taskEnd, taskIsTopOfChain: true);

			final.Start();
			yield return null;
			while (!final.IsCompleted) yield return null;


			Assert.AreEqual(new string[] { "chain start", "failing", "on failure", "chain completed" }.Join(","), callOrder.Join(","));
		}

		[UnityTest]
		public IEnumerator TaskOnFailureGetsCalledWhenExceptionHappensUpTheChain()
		{
			var taskManager = new TaskManager().Initialize();
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();
			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException())
					   .Then(_ => runOrder.Add("1"))
					   .Catch(ex => exceptions.Add(ex))
					   .Then(() => runOrder.Add("OnFailure"),
						   runOptions: TaskRunOptions.OnFailure)
					   .Finally((s, e) => { }, TaskAffinity.Concurrent);

			task.Start();
			yield return null;
			while (!task.IsCompleted) yield return null;

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

	static class AsyncExtensions
	{
		public static Task<T> StartAndSwallowException<T>(this ITask<T> task)
		{
			var tcs = new TaskCompletionSource<T>();
			task.Then((success, result) => { tcs.SetResult(result); }, TaskAffinity.Concurrent);
			task.Start();
			return tcs.Task;
		}

		public static Task StartAndSwallowException(this ITask task)
		{
			var tcs = new TaskCompletionSource<bool>();
			task.Then(s => { tcs.SetResult(s); });
			task.Start();
			return tcs.Task;
		}
	}
}
