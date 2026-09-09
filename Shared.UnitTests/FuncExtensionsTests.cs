using AwesomeAssertions;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FuncExtensions)}"" type")]
internal class FuncExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="FuncExtensions.WaitAsync" />: returns false when the maximum number of repetitions is reached.
	/// </summary>
	[Test]
	public async Task WaitAsync_Returns_False_When_Max_Repeat_Reached()
	{
		// Arrange
		Func<bool> condition = () => false;

		// Act
		bool result = await condition.WaitAsync(millisecondsDelay: 1, maxRepeat: 3);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FuncExtensions.WaitAsync" />: returns true once the condition becomes satisfied.
	/// </summary>
	[Test]
	public async Task WaitAsync_Returns_True_When_Condition_Becomes_True()
	{
		// Arrange
		int calls = 0;

		Func<bool> condition = () => ++calls >= 3;

		// Act
		bool result = await condition.WaitAsync(millisecondsDelay: 1, maxRepeat: 10);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FuncExtensions.WaitAsync" />: returns true immediately when the condition is already met.
	/// </summary>
	[Test]
	public async Task WaitAsync_Returns_True_When_Condition_Already_Met()
	{
		// Arrange
		Func<bool> condition = () => true;

		// Act
		bool result = await condition.WaitAsync(millisecondsDelay: 1, maxRepeat: 5);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FuncExtensions.WaitAsync" />: propagates cancellation while the condition is still unmet.
	/// </summary>
	[Test]
	public async Task WaitAsync_Throws_When_Token_Cancelled()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		Func<bool> condition = () => false;

		// Act
		Func<Task> act = async () => await condition.WaitAsync(10, 5, cancellation.Token);

		// Assert
		await act
			.Should()
			.ThrowAsync<OperationCanceledException>();
	}
	#endregion
}
