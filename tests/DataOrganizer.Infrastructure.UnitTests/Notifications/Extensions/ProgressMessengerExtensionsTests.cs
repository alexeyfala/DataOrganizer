using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Infrastructure.Notifications.Extensions;
using DataOrganizer.Infrastructure.Notifications.Helpers;
using DataOrganizer.Infrastructure.Notifications.Messages;
using System.Collections.Generic;

namespace DataOrganizer.Infrastructure.UnitTests.Notifications.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ProgressMessengerExtensions)}"" type")]
internal class ProgressMessengerExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ProgressMessengerExtensions.ShowProgress" />: the progress bar is shown and stays while the scope is open.
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
