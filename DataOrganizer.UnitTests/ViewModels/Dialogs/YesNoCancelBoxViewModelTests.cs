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
	/// <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: each variant shows the expected buttons and cancel flags.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Controls_Buttons([Values] YesNoCancelButtons variant)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		_ = Task.Run(() => sut.CancelButtonPressedCommand.Execute(null));

		await sut.GetResultAsync(variant);

		// Assert
		switch (variant)
		{
			case YesNoCancelButtons.YesNo:
				sut.NoButtonVisible
					.Should()
					.BeTrue();

				sut.NoIsCancel
					.Should()
					.BeTrue();
				break;

			case YesNoCancelButtons.YesCancel:
				sut.CancelButtonVisible
					.Should()
					.BeTrue();

				sut.CancelIsCancel
					.Should()
					.BeTrue();
				break;

			case YesNoCancelButtons.YesNoCancel:
				sut.NoButtonVisible
					.Should()
					.BeTrue();

				sut.CancelButtonVisible
					.Should()
					.BeTrue();

				sut.CancelIsCancel
					.Should()
					.BeTrue();
				break;

			default:
				throw new NotImplementedException();
		}
	}

	/// <summary>
	/// <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: the pressed button determines the returned result.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Does_Work([Values] YesNoCancelAnswer expected)
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
