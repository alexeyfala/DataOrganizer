using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using EditorMessages.Helpers;
using EditorMessages.Messages;
using System.Collections.Generic;

namespace EditorMessages.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ProgressScope)}"" type")]
internal class ProgressScopeTests
{
	#region Methods
	/// <summary>
	/// <see cref="ProgressScope.Dispose" />: the first disposal hides the progress bar.
	/// </summary>
	[Test]
	public void Dispose_Hides_The_Progress_Bar_At_The_First_Call()
	{
		// Arrange
		List<bool> visibility = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// Holds its recipients strongly, so the recipient made here lives through the test.
			IMessenger messenger = new StrongReferenceMessenger();

			messenger.Register<ShowProgressBarMessage>(new object(), (_, message) => visibility.Add(message.IsVisible));

			builder.RegisterInstance(messenger);
		});

		ProgressScope sut = mock.Create<ProgressScope>();

		// Act
		sut.Dispose();

		// Assert
		visibility
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="ProgressScope.Dispose" />: the progress bar is hidden once, however many times the scope is disposed.
	/// </summary>
	[Test]
	public void Dispose_Hides_The_Progress_Bar_Once()
	{
		// Arrange
		List<bool> visibility = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// Holds its recipients strongly, so the recipient made here lives through the test.
			IMessenger messenger = new StrongReferenceMessenger();

			messenger.Register<ShowProgressBarMessage>(new object(), (_, message) => visibility.Add(message.IsVisible));

			builder.RegisterInstance(messenger);
		});

		ProgressScope sut = mock.Create<ProgressScope>();

		// Act
		sut.Dispose();

		sut.Dispose();

		// Assert
		visibility
			.Should()
			.Equal(false);
	}
	#endregion
}
