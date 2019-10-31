using System.Threading.Tasks;
using NUnit.Framework;

namespace ProcessManagerTests
{
	public partial class ProcessTaskTests
	{
		[Test]
		public void NestedProcessShouldChainCorrectly_()
		{
			RunTest(NestedProcessShouldChainCorrectly);
		}

		[Test]
		// not sure why this is flip flopping in appveyor
		[Category("DoNotRunOnAppVeyor")]
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
