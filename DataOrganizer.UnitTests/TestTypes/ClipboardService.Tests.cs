using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Services;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardService)}"" type")]
internal class ClipboardServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ClipboardService.SetTextAsync" />.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Does_Not_Post_To_Dispatcher_When_Clipboard_Not_Available()
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dispatcher));

		ClipboardService sut = mock.Create<ClipboardService>();

		// Act
		await sut.SetTextAsync("payload");

		// Assert
		dispatcher
			.Received(0)
			.Post(Arg.Any<Action>());
	}

	/// <summary>
	/// Test of <see cref="ClipboardService.SetTextAsync" />.
	/// </summary>
	[Test]
	public async Task SetTextAsync_Returns_False_When_Clipboard_Not_Available()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ClipboardService sut = mock.Create<ClipboardService>();

		// Act
		bool result = await sut.SetTextAsync("payload");

		// Assert
		result
			.Should()
			.BeFalse();
	}
	#endregion
}
