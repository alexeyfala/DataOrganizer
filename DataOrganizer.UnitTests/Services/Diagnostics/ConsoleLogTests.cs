using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Diagnostics;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleLog)}"" type")]
internal class ConsoleLogTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleLog.TakeAll" />: the records taken once are not taken again.
	/// </summary>
	[Test]
	public void TakeAll_Leaves_No_Record_Behind()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleLog sut = mock.Create<ConsoleLog>();

		sut.Write("record");

		sut.TakeAll();

		// Act
		string[] result = sut.TakeAll();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ConsoleLog.TakeAll" />: the records come in the order they were written.
	/// </summary>
	[Test]
	public void TakeAll_Returns_The_Records_In_The_Order_Written()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		ConsoleLog sut = mock.Create<ConsoleLog>();

		sut.Write("first");

		sut.Write("second");

		// Act
		string[] result = sut.TakeAll();

		// Assert
		result
			.Should()
			.Equal("first", "second");
	}

	/// <summary>
	/// <see cref="ConsoleLog.Write" />: records written on several threads at once are all kept.
	/// </summary>
	[Test]
	public void Write_Keeps_The_Records_Of_Several_Threads()
	{
		// Arrange
		const int count = 10_000;

		using AutoMock mock = AutoMock.GetLoose();

		ConsoleLog sut = mock.Create<ConsoleLog>();

		// Act
		Parallel.For(0, count, _ => sut.Write("record"));

		// Assert
		sut.TakeAll()
			.Should()
			.HaveCount(count);
	}

	/// <summary>
	/// <see cref="ConsoleLog.Written" />: the signal of a record comes once the record can be taken.
	/// </summary>
	[Test]
	public void Written_Comes_Once_The_Record_Is_Kept()
	{
		// Arrange
		List<string> taken = [];

		using AutoMock mock = AutoMock.GetLoose();

		ConsoleLog sut = mock.Create<ConsoleLog>();

		using IDisposable subscription = sut.Written.Subscribe(_ => taken.AddRange(sut.TakeAll()));

		// Act
		sut.Write("first");

		sut.Write("second");

		// Assert
		taken
			.Should()
			.Equal("first", "second");
	}
	#endregion
}
