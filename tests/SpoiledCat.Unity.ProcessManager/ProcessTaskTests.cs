using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SpoiledCat.ProcessManager.Tests
{
	using Base.Tests;
	using Threading;

	[TestFixture]
	public class ProcessTaskTests : BaseTest
	{
		[Test]
		public async Task ProcessReadsFromStandardInput()
		{
			var input = new List<string> { "Hello", "World\u001A" };

			var expectedOutput = "Hello";

			var procTask = new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"--sleep 100 -i")
				.Configure(ProcessManager, withInput: true);

			procTask.OnStartProcess += proc => {
				foreach (var item in input)
				{
					proc.StandardInput.WriteLine(item);
				}
				proc.StandardInput.Close();
			};

			var chain = procTask.Finally((s, e, d) => d, TaskAffinity.Concurrent);

			var output = await chain.StartAsAsync();

			Assert.AreEqual(expectedOutput, output);
		}

		[Test]
		public async Task ProcessOnStartOnEndTaskOrder()
		{
			var values = new List<string>();
			string process1Value = null;
			string process2Value = null;

			var process1Task = new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"--sleep 100 -d process1")
			                   .Configure(ProcessManager, withInput: true)
			                   .Then((b, s) => {
				                   process1Value = s;
				                   values.Add(s);
			                   });

			var process2Task = new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"---sleep 100 -d process2")
			                   .Configure(ProcessManager, withInput: true)
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
		}

		[Test]
		public async Task ProcessReturningErrorThrowsException()
		{
			var success = false;
			Exception thrown = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"--sleep 100 -d ""one name""")
			           .Catch(ex => thrown = ex)
			           .Then((s, d) => output.Add(d))
			           .Then(new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"-e kaboom -r -1"))
			           .Catch(ex => thrown = ex)
			           .Then((s, d) => output.Add(d))
			           .Finally((s, e) => success = s, TaskAffinity.Concurrent);

			await task.StartAndSwallowException();

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(thrown);
			Assert.AreEqual("kaboom", thrown.Message);
		}

		[Test]
		public async Task NestedProcessShouldChainCorrectly()
		{
			var expected = new List<string>() { "BeforeProcess", "ProcessOutput", "ProcessFinally" };

			var results = new List<string>() {};

			await new ActionTask(TaskManager, _ => results.Add("BeforeProcess"))
			      .Then(
				      new FirstNonNullLineProcessTask(TaskManager, ProcessManager, TestApp, @"--sleep 1000 -d ""ok""")
					      .Then(new FuncTask<int>(TaskManager, (b, i) => {
						      results.Add("ProcessOutput");
						      return 1234;
					      }))
					      .Finally((b, exception) => results.Add("ProcessFinally"),
						      TaskAffinity.Concurrent))
			      .StartAsAsync();

			CollectionAssert.AreEqual(expected, results);
		}
	}
}
