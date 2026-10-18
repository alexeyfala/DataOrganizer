using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Threading;
using DataOrganizer.Enums.Views;
using DataOrganizer.Services.Views;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DispatcherAccessor)}"" type")]
internal class DispatcherAccessorTests
{
	#region Methods
	/// <summary>
	/// <see cref="DispatcherAccessor.Post" />: the action goes to the Avalonia dispatcher at the matching priority.
	/// </summary>
	[TestCaseSource(nameof(Priorities))]
	public void Post_Posts_At_The_Matching_Avalonia_Priority(UiPriority priority, DispatcherPriority expected)
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dispatcher));

		DispatcherAccessor sut = mock.Create<DispatcherAccessor>();

		Action action = static () => { };

		// Act
		sut.Post(action, priority);

		// Assert
		dispatcher
			.Received(1)
			.Post(action, expected);
	}

	/// <summary>
	/// <see cref="DispatcherAccessor.PostAsync(Action, UiPriority)" />: the work goes to the Avalonia dispatcher at the
	/// matching priority.
	/// </summary>
	[TestCaseSource(nameof(Priorities))]
	public void PostAsync_Posts_At_The_Matching_Avalonia_Priority(UiPriority priority, DispatcherPriority expected)
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dispatcher));

		DispatcherAccessor sut = mock.Create<DispatcherAccessor>();

		// Act
		_ = sut.PostAsync(static () => { }, priority);

		// Assert
		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>(), expected);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Each priority of the application with the Avalonia priority it stands for.
	/// </summary>
	private static TestCaseData[] Priorities() =>
	[
		new(UiPriority.Default, default(DispatcherPriority)),
		new(UiPriority.Background, DispatcherPriority.Background)
	];
	#endregion
}
