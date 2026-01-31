using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(PasswordBoxViewModel)}"" type")]
internal class PasswordBoxViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="PasswordBoxViewModel.Password" />.
	/// </summary>
	[Test]
	public void Password_Should_Be_Null_After_Default_Button_Pressed()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		PasswordBoxViewModel sut = mock.Create<PasswordBoxViewModel>();

		sut.Password = "SomePassword";

		// Act
		sut
			.DefaultPressedCommand
			.Execute(null);

		// Assert
		sut
			.Password
			.Should()
			.BeNull();
	}
	#endregion
}
