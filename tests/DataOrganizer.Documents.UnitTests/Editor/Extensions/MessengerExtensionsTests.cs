using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Documents.Editor.Extensions;
using DataOrganizer.Documents.Editor.Messages;
using System.Threading.Tasks;

namespace DataOrganizer.Documents.UnitTests.Editor.Extensions;

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
	#endregion
}
