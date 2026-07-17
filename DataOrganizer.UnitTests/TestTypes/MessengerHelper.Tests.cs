using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(MessengerHelper)}"" type")]
internal class MessengerHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="MessengerHelper.FormatUnsubscriptionLog{T}" />: includes the recipient type name, the channel count and every message name.
	/// </summary>
	[Test]
	public void FormatUnsubscriptionLog_Includes_Type_Name_And_Channel_Count()
	{
		// Arrange
		MultiSubscription recipient = new();

		// Act
		string? result = MessengerHelper.FormatUnsubscriptionLog(recipient);

		// Assert
		result
			.Should()
			.Contain(nameof(MultiSubscription));

		result
			.Should()
			.Contain("2 message channel(s)");

		result
			.Should()
			.Contain(nameof(MessageA));

		result
			.Should()
			.Contain(nameof(MessageB));
	}

	/// <summary>
	/// <see cref="MessengerHelper.FormatUnsubscriptionLog{T}" />: returns null when the recipient subscribes to no channels.
	/// </summary>
	[Test]
	public void FormatUnsubscriptionLog_Returns_Null_When_No_Subscriptions()
	{
		// Arrange
		NoSubscriptions recipient = new();

		// Act
		string? result = MessengerHelper.FormatUnsubscriptionLog(recipient);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="MessengerHelper.FormatUnsubscriptionLog{T}" />: throws when the recipient is null.
	/// </summary>
	[Test]
	public void FormatUnsubscriptionLog_Throws_When_Recipient_Null()
	{
		// Act
		Action act = () => MessengerHelper.FormatUnsubscriptionLog<object>(null!);

		// Assert
		act
			.Should()
			.Throw<ArgumentNullException>();
	}
	#endregion
}

// Recipient helper types are top-level so the CommunityToolkit messenger source generator
// (IMessengerRegisterAllGenerator), which emits code referencing every IRecipient<T> type,
// can access them — a private nested type would be inaccessible to the generated code.

/// <summary>
/// Recipient subscribed to two message channels.
/// </summary>
internal sealed class MultiSubscription : IRecipient<MessageA>, IRecipient<MessageB>
{
	public void Receive(MessageA message)
	{
	}

	public void Receive(MessageB message)
	{
	}
}

/// <summary>
/// Recipient implementing no message channels.
/// </summary>
internal sealed class NoSubscriptions;

/// <summary>
/// Sample message for the subscription tests.
/// </summary>
internal sealed record MessageA;

/// <summary>
/// Sample message for the subscription tests.
/// </summary>
internal sealed record MessageB;
