using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(YesNoCancelBoxViewModel)}"" type")]
internal class YesNoCancelBoxViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: every variant shows exactly the buttons it needs, and marks the one the Escape key activates.
	/// </summary>
	[TestCase(YesNoCancelButtons.YesNo, true, true, false, false)]
	[TestCase(YesNoCancelButtons.YesCancel, false, false, true, true)]
	[TestCase(YesNoCancelButtons.YesNoCancel, true, false, true, true)]
	public async Task GetResultAsync_Controls_Buttons(
		YesNoCancelButtons variant,
		bool isNoButtonVisible,
		bool noButtonHandlesEscape,
		bool isCancelButtonVisible,
		bool cancelButtonHandlesEscape)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelButtonPressedCommand.Execute(null));

		await sut.GetResultAsync(variant);

		// Assert
		sut.IsNoButtonVisible
			.Should()
			.Be(isNoButtonVisible);

		sut.NoButtonHandlesEscape
			.Should()
			.Be(noButtonHandlesEscape);

		sut.IsCancelButtonVisible
			.Should()
			.Be(isCancelButtonVisible);

		sut.CancelButtonHandlesEscape
			.Should()
			.Be(cancelButtonHandlesEscape);
	}

	/// <summary>
	/// <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: the pressed button determines the returned result.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Returns_The_Answer_Of_The_Pressed_Button([Values] YesNoCancelAnswer expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		_ = Task.Run(() =>
		{
			switch (expected)
			{
				case YesNoCancelAnswer.No:
					sut
						.NoButtonPressedCommand
						.Execute(null);
					break;

				case YesNoCancelAnswer.Cancel:
					sut
						.CancelButtonPressedCommand
						.Execute(null);
					break;

				case YesNoCancelAnswer.Yes:
					sut
						.YesButtonPressedCommand
						.Execute(null);
					break;

				default:
					throw new NotImplementedException();
			}
		});

		YesNoCancelAnswer result = await sut.GetResultAsync(YesNoCancelButtons.YesNoCancel);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
