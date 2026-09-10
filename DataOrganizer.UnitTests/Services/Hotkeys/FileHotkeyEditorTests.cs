using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Services.Hotkeys;
using Entities.Models;
using MapsterMapper;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Extensions;
using System;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Hotkeys;

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
		FileDto dto = TestData.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(TestData.CreateHotkeysDto(5));

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

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
			.Received()
			.DeleteHotkeysAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: hotkeys already used by another file return AlreadyInUse.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_AlreadyInUse()
	{
		// Arrange
		KeyStroke[] newHotkeys = [.. TestData.CreateKeyStrokes(5)];

		FileDto owner = TestData.CreateFileDto();

		owner
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyDtos());

		using AutoMock mock = AutoMock.GetLoose();

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		ExplorerItemDtoBase[] hierarchy = [owner];

		// Act
		OverwriteHotkeysOutcome result = await sut.OverwriteAsync(TestData.CreateFileDto(), newHotkeys, hierarchy);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysOutcome.AlreadyInUse);
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: new hotkeys are saved to the database, the tooltip is set, and Rewritten is returned.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_Rewritten()
	{
		// Arrange
		FileDto dto = TestData.CreateFileDto();

		KeyStroke[] newHotkeys = [.. TestData.CreateKeyStrokes(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<HotkeyEntity[], HotkeyDto[]>(Arg.Any<HotkeyEntity[]>())
				.Returns([.. TestData.CreateHotkeysDto(newHotkeys.Length)]);

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

		dto.HotkeysToolTip
			.Should()
			.NotBeNullOrEmpty();

		await dbAccess
			.Received()
			.AddHotkeysAsync(Arg.Any<Guid>(), Arg.Any<KeyStroke[]>());
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: passing the file's existing hotkeys returns SameHotkeys.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_SameHotkeys()
	{
		// Arrange
		KeyStroke[] newHotkeys = [.. TestData.CreateKeyStrokes(5)];

		FileDto dto = TestData.CreateFileDto();

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
