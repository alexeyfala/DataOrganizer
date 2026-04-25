using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services;
using NSubstitute;
using Serilog;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExceptionHandler)}"" type")]
internal class ExceptionHandlerTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ExceptionHandler.HandleException" />.
	/// </summary>
	[Test]
	public void HandleException_Deduplicates_Exceptions_With_Same_Message()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		ExceptionHandler sut = mock.Create<ExceptionHandler>();

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
	/// Test of <see cref="ExceptionHandler.HandleException" />.
	/// </summary>
	[Test]
	public void HandleException_Logs_First_Occurrence_Of_Exception()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		ExceptionHandler sut = mock.Create<ExceptionHandler>();

		// Act
		sut.HandleException(new InvalidOperationException("first"));

		// Assert
		logger
			.ReceivedCalls()
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// Test of <see cref="ExceptionHandler.HandleException" />.
	/// </summary>
	[Test]
	public void HandleException_Resets_Deduplication_Set_After_Five_Unique_Exceptions()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		ExceptionHandler sut = mock.Create<ExceptionHandler>();

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
	#endregion
}
