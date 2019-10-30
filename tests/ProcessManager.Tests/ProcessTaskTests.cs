namespace SpoiledCat.ProcessManager.Tests
{
	using System.Threading.Tasks;
	using NUnit.Framework;

	public partial class ProcessTaskTests
	{
		[Test]
		public void NestedProcessShouldChainCorrectly_()
		{
			RunTest(NestedProcessShouldChainCorrectly);
		}

		[Test]
		public void MultipleFinallyOrder_()
		{
			RunTest(MultipleFinallyOrder);
		}

		[Test]
		public void ProcessOnStartOnEndTaskOrder_()
		{
			RunTest(ProcessOnStartOnEndTaskOrder);
		}

		[Test]
		public void ProcessReadsFromStandardInput_()
		{
			RunTest(ProcessReadsFromStandardInput);
		}

		[Test]
		public void ProcessReturningErrorThrowsException_()
		{
			RunTest(ProcessReturningErrorThrowsException);
		}
	}
}
