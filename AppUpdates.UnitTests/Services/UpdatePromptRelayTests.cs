using AppUpdates.Messages;
using AppUpdates.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;

namespace AppUpdates.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(UpdatePromptRelay)}"" type")]
internal class UpdatePromptRelayTests
{
	#region Methods
	/// <summary>
	/// <see cref="UpdatePromptRelay.ConfirmUpdateAsync" />: with no window to answer, the update is declined.
	/// </summary>
	[Test]
	public async Task ConfirmUpdateAsync_Declines_Without_A_Window_To_Answer()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IMessenger>(new StrongReferenceMessenger()));

		UpdatePromptRelay sut = mock.Create<UpdatePromptRelay>();

		// Act
		bool answer = await sut.ConfirmUpdateAsync("Version 0.2.0 is available.");

		// Assert
		answer
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="UpdatePromptRelay.ConfirmUpdateAsync" />: the question goes to the window that answers it, and its answer
	/// is returned.
	/// </summary>
	[Test]
	public async Task ConfirmUpdateAsync_Returns_The_Answer_Of_The_Window()
	{
		// Arrange
		const string text = "Version 0.2.0 is available.";

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ConfirmUpdateMessage>(this, (_, message) => message.Reply(message.Text == text));

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger));

		UpdatePromptRelay sut = mock.Create<UpdatePromptRelay>();

		// Act
		bool answer = await sut.ConfirmUpdateAsync(text);

		// Assert
		answer
			.Should()
			.BeTrue();
	}
	#endregion
}
