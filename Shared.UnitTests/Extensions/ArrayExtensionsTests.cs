using Shared.Extensions;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ArrayExtensions)}"" type")]
internal class ArrayExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ArrayExtensions.IsEmpty{T}" />: an array is empty when it has no items.
	/// </summary>
	[TestCase(0, ExpectedResult = true)]
	[TestCase(1, ExpectedResult = false)]
	public bool IsEmpty_Tells_An_Array_Without_Items(int length)
	{
		return new int[length].IsEmpty();
	}

	/// <summary>
	/// <see cref="ArrayExtensions.IsNotEmpty{T}" />: an array is not empty when it has an item.
	/// </summary>
	[TestCase(0, ExpectedResult = false)]
	[TestCase(1, ExpectedResult = true)]
	public bool IsNotEmpty_Tells_An_Array_With_Items(int length)
	{
		return new int[length].IsNotEmpty();
	}
	#endregion
}
