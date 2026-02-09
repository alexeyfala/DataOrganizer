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
	/// Test of <see cref="YesNoCancelBoxViewModel.GetResultAsync" />.
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
	#endregion
}
