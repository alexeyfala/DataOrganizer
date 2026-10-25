using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Diagnostics.Interfaces;
using Entities.Models;
using GlobalHotkeys.Enums;
using GlobalHotkeys.Extensions;
using GlobalHotkeys.Services;
using GlobalHotkeys.UnitTests.Factories;
using Hierarchy.Dto;
using MapsterMapper;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Extensions;
using System;
using System.Threading.Tasks;
using TestSupport.Dto;

namespace GlobalHotkeys.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileHotkeyEditor)}"" type")]
internal class FileHotkeyEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: an empty sequence clears the file's hotkeys, deletes them in the database, and returns EmptySequence.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Deletes_Hotkeys_In_Database_And_Returns_EmptySequence()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(HotkeyFactory.CreateHotkeyDtos(5));

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// A substitute maps the empty result into an empty list, where a Moq fake gives null.
			builder.RegisterInstance(Substitute.For<IMapper>());

			builder.RegisterInstance(dbAccess);
		});

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(dto, [], []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.EmptySequence);

		dto.Hotkeys
			.Should()
			.BeEmpty();

		await dbAccess
			.Received(1)
			.ReplaceHotkeysAsync(dto.Id, Arg.Is<KeyStroke[]>(x => x.Length == 0));
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: a write the database fails leaves the hotkeys of the file in the
	/// session as they were, and the failure goes to the reporter of database failures.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Keeps_The_Hotkeys_When_The_Database_Fails()
	{
		// Arrange
		InvalidOperationException failure = new();

		HotkeyDto[] oldHotkeys = [.. HotkeyFactory.CreateHotkeyDtos(2)];

		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(oldHotkeys);

		IDbFailureReporter failureReporter = Substitute.For<IDbFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ReplaceHotkeysAsync(dto.Id, Arg.Any<KeyStroke[]>())
				.ThrowsAsync(failure);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(failureReporter);
		});

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(dto, [.. KeyStrokeFactory.CreateKeyStrokes(3)], []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.ExceptionThrown);

		dto.Hotkeys
			.Should()
			.Equal(oldHotkeys);

		failureReporter
			.Received(1)
			.Report(failure, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: hotkeys already used by another file return AlreadyInUse.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_AlreadyInUse()
	{
		// Arrange
		KeyStroke[] newHotkeys = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		FileDto owner = ItemDtoFactory.CreateFileDto();

		owner
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyDtos());

		using AutoMock mock = AutoMock.GetLoose();

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		ExplorerItemDtoBase[] hierarchy = [owner];

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(ItemDtoFactory.CreateFileDto(), newHotkeys, hierarchy);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.AlreadyInUse);
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: new hotkeys are saved to the database and the file, and Rewritten is returned.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_Rewritten()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] newHotkeys = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<HotkeyEntity[], HotkeyDto[]>(Arg.Any<HotkeyEntity[]>())
				.Returns([.. HotkeyFactory.CreateHotkeyDtos(newHotkeys.Length)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(dto, newHotkeys, []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.Rewritten);

		dto.Hotkeys
			.Should()
			.HaveCount(newHotkeys.Length);

		await dbAccess
			.Received(1)
			.ReplaceHotkeysAsync(dto.Id, newHotkeys);
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: passing the file's existing hotkeys returns SameHotkeys.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_SameHotkeys()
	{
		// Arrange
		KeyStroke[] newHotkeys = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		FileDto dto = ItemDtoFactory.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyDtos());

		using AutoMock mock = AutoMock.GetLoose();

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(dto, newHotkeys, []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.SameHotkeys);
	}
	#endregion
}
