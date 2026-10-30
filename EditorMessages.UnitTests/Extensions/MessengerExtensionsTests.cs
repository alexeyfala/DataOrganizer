using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using EditorMessages.Extensions;
using EditorMessages.Helpers;
using EditorMessages.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EditorMessages.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(MessengerExtensions)}"" type")]
internal class MessengerExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="MessengerExtensions.FlushEditorsAsync" />: the flush fails when one of the editors could not save.
	/// </summary>
	[Test]
	public async Task FlushEditorsAsync_Returns_False_When_An_Editor_Did_Not_Save()
	{
		// Arrange
		// Holds its recipients strongly, so the recipients made here live through the test.
		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(true));

		messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(false));

		// Act
		bool result = await messenger.FlushEditorsAsync();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MessengerExtensions.FlushEditorsAsync" />: the flush succeeds when every editor saved.
	/// </summary>
	[Test]
	public async Task FlushEditorsAsync_Returns_True_When_Every_Editor_Saved()
	{
		// Arrange
		// Holds its recipients strongly, so the recipients made here live through the test.
		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(true));

		messenger.Register<FlushEditorsMessage>(new object(), static (_, message) => message.Reply(true));

		// Act
		bool result = await messenger.FlushEditorsAsync();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="MessengerExtensions.ShowProgress" />: the progress bar is shown and stays while the scope is open.
	/// </summary>
	[Test]
	public void ShowProgress_Shows_The_Progress_Bar()
	{
		// Arrange
		List<bool> visibility = [];

		// Holds its recipients strongly, so the recipient made here lives through the test.
		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ShowProgressBarMessage>(new object(), (_, message) => visibility.Add(message.IsVisible));

		// Act
		using ProgressScope scope = messenger.ShowProgress();

		// Assert
		visibility
			.Should()
			.Equal(true);
	}
	#endregion
}
