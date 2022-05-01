namespace SpoiledCat.Threading
{
	/// <summary>
	/// Takes a string, raises an event with it, discards the result
	/// </summary>
	public class RaiseAndDiscardOutputProcessor : BaseOutputProcessor<string>
	{
		public override string Result => string.Empty;
	}
}
