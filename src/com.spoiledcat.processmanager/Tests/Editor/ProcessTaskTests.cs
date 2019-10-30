namespace SpoiledCat.ProcessManager.Tests
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Base.Tests;
	using NUnit.Framework;
	using Threading;

	public partial class ProcessTaskTests : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator NestedProcessShouldChainCorrectly()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);

			var expected = new List<string> { "BeforeProcess", "ProcessOutput", "ProcessFinally", "AfterProcessFinally" };

			var results = new List<string>();

			var beforeProcess = new ActionTask(taskManager, _ => results.Add("BeforeProcess"));
			var processTask = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"--sleep 1000 -d ""ok""");
			var processOutputTask = new FuncTask<int>(taskManager, (b, i) => {
				results.Add("ProcessOutput");
				return 1234;
			});

			var innerChain = processTask.Then(processOutputTask).Finally((b, exception) => results.Add("ProcessFinally"));

			var outerChain = beforeProcess.Then(innerChain).Finally((b, exception) => results.Add("AfterProcessFinally"));

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(outerChain)) yield return frame;

			CollectionAssert.AreEqual(expected, results);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[CustomUnityTest]
		public IEnumerator MultipleFinallyOrder()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);


			var results = new List<string>();

			var beforeProcess = new ActionTask(taskManager, _ => results.Add("BeforeProcess"));
			var processTask = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, "-x");

			// this will never run because the process throws an exception
			var processOutputTask = new FuncTask<int>(taskManager, (b, i) => {
				results.Add("ProcessOutput");
				return 1234;
			});

			var innerChain = processTask.Then(processOutputTask).Finally((b, exception) => results.Add("ProcessFinally"));

			var outerChain = beforeProcess.Then(innerChain).Finally((b, exception) => results.Add("AfterProcessFinally"));

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(outerChain)) yield return frame;

			Assert.AreEqual(3, results.Count);
			Assert.AreEqual("BeforeProcess", results[0]);

			var expected = new List<string> { "ProcessFinally", "AfterProcessFinally" };
			results.Skip(1).MatchesUnsorted(expected);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[CustomUnityTest]
		public IEnumerator ProcessOnStartOnEndTaskOrder()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);

			var values = new List<string>();
			string process1Value = null;
			string process2Value = null;

			var process1Task = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"--sleep 100 -d process1")
			                   .Configure(processManager, withInput: true)
			                   .Then((b, s) => {
				                   process1Value = s;
				                   values.Add(s);
			                   });

			var process2Task = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"---sleep 100 -d process2")
			                   .Configure(processManager, withInput: true)
			                   .Then((b, s) => {
				                   process2Value = s;
				                   values.Add(s);
			                   });

			var combinedTask = process1Task.Then(process2Task);

			combinedTask.OnStart += task => { values.Add("OnStart"); };

			combinedTask.OnEnd += (task, success, ex) => { values.Add("OnEnd"); };

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(combinedTask)) yield return frame;

			Assert.AreEqual(process1Value, "process1");
			Assert.AreEqual(process2Value, "process2");
			Assert.True(values.SequenceEqual(new[] { "process1", "OnStart", "process2", "OnEnd" }));

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[CustomUnityTest]
		public IEnumerator ProcessReadsFromStandardInput()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);

			var input = new List<string> { "Hello", "World\u001A" };

			var expectedOutput = "Hello";

			var procTask = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"--sleep 100 -i")
				.Configure(processManager, withInput: true);

			procTask.OnStartProcess += proc => {
				foreach (var item in input)
				{
					proc.StandardInput.WriteLine(item);
				}
				proc.StandardInput.Close();
			};

			var chain = procTask.Finally((s, e, d) => d);

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(chain)) yield return frame;

			var output = chain.Result;

			Assert.AreEqual(expectedOutput, output);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[CustomUnityTest]
		public IEnumerator ProcessReturningErrorThrowsException()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);

			var success = false;
			Exception thrown = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"--sleep 100 -d ""one name""")
			           .Catch(ex => thrown = ex)
			           .Then((s, d) => output.Add(d))
			           .Then(new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"-e kaboom -r -1"))
			           .Catch(ex => thrown = ex)
			           .Then((s, d) => output.Add(d))
			           .Finally((s, e) => success = s);

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(thrown);
			Assert.AreEqual("kaboom", thrown.Message);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}
	}
}
