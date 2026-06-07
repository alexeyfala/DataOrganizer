using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.ViewModels;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(YesNoCancelBoxViewModel)}"" type")]
internal class YesNoCancelBoxViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: each variant shows the expected buttons and cancel flags.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Controls_Buttons([Values] YesNoCancelVariant variant)
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
			case YesNoCancelVariant.YesNo:
				sut.NoButtonVisible
					.Should()
					.BeTrue();

				sut.NoIsCancel
					.Should()
					.BeTrue();
				break;

			case YesNoCancelVariant.YesCancel:
				sut.CancelButtonVisible
					.Should()
					.BeTrue();

				sut.CancelIsCancel
					.Should()
					.BeTrue();
				break;

			case YesNoCancelVariant.YesNoCancel:
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
	/// Test of <see cref="YesNoCancelBoxViewModel.GetResultAsync" />: the pressed button determines the returned result.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Does_Work([Values] YesNoCancelResult expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		_ = Task.Run(() =>
		{
			switch (expected)
			{
				case YesNoCancelResult.No:
					sut
						.NoButtonPressedCommand
						.Execute(null);
					break;

				case YesNoCancelResult.Cancel:
					sut
						.CancelButtonPressedCommand
						.Execute(null);
					break;

				case YesNoCancelResult.Yes:
					sut
						.YesButtonPressedCommand
						.Execute(null);
					break;

				default:
					throw new NotImplementedException();
			}
		});

		YesNoCancelResult result = await sut.GetResultAsync(YesNoCancelVariant.YesNoCancel);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
