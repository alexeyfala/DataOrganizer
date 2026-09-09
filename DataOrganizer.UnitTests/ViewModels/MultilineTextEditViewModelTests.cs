using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(MultilineTextEditViewModel)}"" type")]
internal class MultilineTextEditViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="MultilineTextEditViewModel.CancelCommand" />: executing it resolves the result as false.
	/// </summary>
	[Test]
	public async Task CancelCommand_Sets_False_Result()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		MultilineTextEditViewModel sut = mock.Create<MultilineTextEditViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelCommand.Execute(null));

		bool result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MultilineTextEditViewModel.SaveCommand" />: executing it resolves the result as true.
	/// </summary>
	[Test]
	public async Task SaveCommand_Sets_True_Result()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		MultilineTextEditViewModel sut = mock.Create<MultilineTextEditViewModel>();

		// Act
		_ = Task.Run(() => sut.SaveCommand.Execute(null));

		bool result = await sut.GetResultAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}
	#endregion
}
