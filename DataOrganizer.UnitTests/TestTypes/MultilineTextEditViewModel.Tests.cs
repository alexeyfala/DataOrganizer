using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(MultilineTextEditViewModel)}"" type")]
internal class MultilineTextEditViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="MultilineTextEditViewModel.CancelCommand" />.
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
	/// Test of <see cref="MultilineTextEditViewModel.SaveCommand" />.
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
