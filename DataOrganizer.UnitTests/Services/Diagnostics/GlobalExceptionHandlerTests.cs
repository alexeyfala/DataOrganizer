using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Diagnostics;
using NSubstitute;
using Serilog;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Services.Diagnostics;

[TestFixture(Description = $@"Tests of ""{nameof(GlobalExceptionHandler)}"" type")]
internal class GlobalExceptionHandlerTests
{
	#region Methods
	/// <summary>
	/// <see cref="GlobalExceptionHandler.AreAllLeaves" />: verifies an aggregate whose nested leaves all match is accepted.
	/// </summary>
	[Test]
	public void AreAllLeaves_Accepts_Nested_Matching_Leaves()
	{
		// Arrange
		const string benign = "benign";

		AggregateException aggregate = new(
			new InvalidOperationException(benign),
			new AggregateException(new InvalidOperationException(benign)));

		// Act
		bool result = GlobalExceptionHandler.AreAllLeaves(aggregate, x => x.Message == benign);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.AreAllLeaves" />: verifies one other leaf among the matching ones keeps the aggregate from being swallowed.
	/// </summary>
	[Test]
	public void AreAllLeaves_Rejects_Aggregate_With_Other_Leaf()
	{
		// Arrange
		const string benign = "benign";

		AggregateException aggregate = new(
			new InvalidOperationException(benign),
			new InvalidOperationException("real failure"));

		// Act
		bool result = GlobalExceptionHandler.AreAllLeaves(aggregate, x => x.Message == benign);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.AreAllLeaves" />: verifies an aggregate without leaves is not taken for a benign one.
	/// </summary>
	[Test]
	public void AreAllLeaves_Rejects_Empty_Aggregate()
	{
		// Act
		bool result = GlobalExceptionHandler.AreAllLeaves(new AggregateException(), _ => true);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.HandleException" />: a repeated exception with the same message is not logged twice.
	/// </summary>
	[Test]
	public void HandleException_Deduplicates_Exceptions_With_Same_Message()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		GlobalExceptionHandler sut = mock.Create<GlobalExceptionHandler>();

		// Act
		sut.HandleException(new InvalidOperationException("repeated"));

		int afterFirst = logger.ReceivedCalls().Count();

		sut.HandleException(new InvalidOperationException("repeated"));

		int afterSecond = logger.ReceivedCalls().Count();

		// Assert
		afterFirst
			.Should()
			.BeGreaterThan(0);

		afterSecond
			.Should()
			.Be(afterFirst);
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.HandleException" />: the first occurrence of an exception is logged.
	/// </summary>
	[Test]
	public void HandleException_Logs_First_Occurrence_Of_Exception()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		GlobalExceptionHandler sut = mock.Create<GlobalExceptionHandler>();

		// Act
		sut.HandleException(new InvalidOperationException("first"));

		// Assert
		logger
			.ReceivedCalls()
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.HandleException" />: after five unique exceptions the deduplication set resets, so a previously seen exception is logged again.
	/// </summary>
	[Test]
	public void HandleException_Resets_Deduplication_Set_After_Five_Unique_Exceptions()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		GlobalExceptionHandler sut = mock.Create<GlobalExceptionHandler>();

		// Act: feed 5 unique exceptions to fill the deduplication set, then a repeat of the first.
		for (int i = 1; i <= 5; i++)
		{
			sut.HandleException(new InvalidOperationException($"unique-{i}"));
		}

		int afterFiveUnique = logger.ReceivedCalls().Count();

		sut.HandleException(new InvalidOperationException("unique-1"));

		int afterReplay = logger.ReceivedCalls().Count();

		// Assert: after the set was reset, the previously seen "unique-1" must be logged again.
		afterReplay
			.Should()
			.BeGreaterThan(afterFiveUnique);
	}

	/// <summary>
	/// <see cref="GlobalExceptionHandler.IsPlatformSettingsFailure" />: verifies a failure is detected by the settings reader in its stack.
	/// </summary>
	[TestCase("   at Avalonia.FreeDesktop.DBusPlatformSettings.<ReadAccentColorAsync>d__9.MoveNext()", ExpectedResult = true)]
	[TestCase("   at Avalonia.FreeDesktop.DBusMenuExporter.<RegisterAsync>d__7.MoveNext()", ExpectedResult = false)]
	[TestCase(null, ExpectedResult = false)]
	public bool IsPlatformSettingsFailure_Detects_Settings_Reader_In_Stack(string? stackTrace)
	{
		// Act
		return GlobalExceptionHandler.IsPlatformSettingsFailure(stackTrace);
	}
	#endregion
}
