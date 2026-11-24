using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.UI.ViewModels.Hierarchy;
using DataOrganizer.Workflows.AppLifecycle.Services;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Threading;

namespace DataOrganizer.UI.UnitTests.ViewModels.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchySearchViewModel)}"" type")]
internal sealed class HierarchySearchViewModelTests
{
	#region Data
	/// <summary>
	/// Time longer than the pause after which a changed search is applied.
	/// </summary>
	private static readonly TimeSpan PastThePause = TimeSpan.FromSeconds(1.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="HierarchySearchViewModel.Clear" />: the search box empties and every object shows at once, with no pause
	/// to wait for.
	/// </summary>
	[Test]
	public void Clear_Shows_Every_Object_At_Once()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.Hierarchy.Add(ItemDtoFactory.CreateNamedFileDto("needle"));

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder.RegisterInstance<TimeProvider>(time);
		});

		HierarchySearchViewModel sut = mock.Create<HierarchySearchViewModel>();

		sut.Text = "needle";

		time.Advance(PastThePause);

		// Act
		sut.Clear();

		// Assert
		sut.Text
			.Should()
			.BeNull();

		sut.Query
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="HierarchySearchViewModel.IsNothingFound" />: a search that matches no object has found nothing.
	/// </summary>
	[Test]
	public void IsNothingFound_Tells_A_Search_That_Matches_No_Object([Values] bool isFound)
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.Hierarchy.Add(ItemDtoFactory.CreateNamedFileDto("needle"));

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder.RegisterInstance<TimeProvider>(time);
		});

		HierarchySearchViewModel sut = mock.Create<HierarchySearchViewModel>();

		sut.Text = isFound ? "needle" : "other";

		// Act
		time.Advance(PastThePause);

		// Assert
		sut.IsNothingFound
			.Should()
			.Be(!isFound);
	}

	/// <summary>
	/// <see cref="HierarchySearchViewModel.Text" />: a typed search is applied only after its pause.
	/// </summary>
	[Test]
	public void Text_Applies_The_Search_After_Its_Pause()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			session.Hierarchy.Add(ItemDtoFactory.CreateNamedFileDto("needle"));

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder.RegisterInstance<TimeProvider>(time);
		});

		HierarchySearchViewModel sut = mock.Create<HierarchySearchViewModel>();

		sut.Text = "needle";

		string? queryBeforeThePause = sut.Query;

		// Act
		time.Advance(PastThePause);

		// Assert
		queryBeforeThePause
			.Should()
			.BeNull();

		sut.Query
			.Should()
			.Be("needle");
	}
	#endregion
}
