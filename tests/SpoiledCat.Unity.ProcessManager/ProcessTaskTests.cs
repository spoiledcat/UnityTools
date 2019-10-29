namespace SpoiledCat.ProcessManager.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Base.Tests;
	using NUnit.Framework;
	using Threading;

	[TestFixture]
	public class ProcessTaskTests : BaseTest
	{
		[Test]
		public async Task NestedProcessShouldChainCorrectly()
		{
			StartTest(out var watch, out var logger, out var taskManager, out var testPath, out var environment, out var processManager);

			var expected = new List<string> { "BeforeProcess", "ProcessOutput", "ProcessFinally" };

			var results = new List<string>();

			await new ActionTask(taskManager, _ => results.Add("BeforeProcess"))
			      .Then(
				      new FirstNonNullLineProcessTask(taskManager, processManager, TestApp, @"--sleep 1000 -d ""ok""")
					      .Then(new FuncTask<int>(taskManager, (b, i) => {
						      results.Add("ProcessOutput");
						      return 1234;
					      }))
					      .Finally((b, exception) => results.Add("ProcessFinally")))
			      .StartAsAsync();

			CollectionAssert.AreEqual(expected, results);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[Test]
		public async Task ProcessOnStartOnEndTaskOrder()
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

			await combinedTask.StartAsAsync();

			Assert.AreEqual(process1Value, "process1");
			Assert.AreEqual(process2Value, "process2");
			Assert.True(values.SequenceEqual(new[] { "process1", "OnStart", "process2", "OnEnd" }));

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[Test]
		public async Task ProcessReadsFromStandardInput()
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

			var output = await chain.StartAsAsync();

			Assert.AreEqual(expectedOutput, output);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}

		[Test]
		public async Task ProcessReturningErrorThrowsException()
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

			await task.StartAndSwallowException();

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(thrown);
			Assert.AreEqual("kaboom", thrown.Message);

			StopTest(watch, logger, taskManager, testPath, environment, processManager);
		}
	}
}
