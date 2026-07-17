using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Services;
using Entities.Models;
using MapsterMapper;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Extensions;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

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
		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(TestUtils.CreateHotkeysDto(5));

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteAsync(dto, [], []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.EmptySequence);

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
		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		FileModelDto owner = TestUtils.CreateFileDto();

		owner
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyModelsDto());

		using AutoMock mock = AutoMock.GetLoose();

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		ExplorerModelBaseDto[] hierarchy = [owner];

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteAsync(TestUtils.CreateFileDto(), newHotkeys, hierarchy);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.AlreadyInUse);
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: new hotkeys are saved to the database, the tooltip is set, and Rewritten is returned.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_Rewritten()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<HotkeyModel[], HotkeyModelDto[]>(Arg.Any<HotkeyModel[]>())
				.Returns([.. TestUtils.CreateHotkeysDto(newHotkeys.Length)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteAsync(dto, newHotkeys, []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.Rewritten);

		dto.Hotkeys
			.Should()
			.HaveCount(newHotkeys.Length);

		dto.HotkeysToolTip
			.Should()
			.NotBeNullOrEmpty();

		await dbAccess
			.Received()
			.AddHotkeysAsync(Arg.Any<Guid>(), Arg.Any<CodeMaskPair[]>());
	}

	/// <summary>
	/// <see cref="FileHotkeyEditor.OverwriteAsync" />: passing the file's existing hotkeys returns SameHotkeys.
	/// </summary>
	[Test]
	public async Task OverwriteAsync_Returns_SameHotkeys()
	{
		// Arrange
		CodeMaskPair[] newHotkeys = [.. TestUtils.CreateCodeMaskPairs(5)];

		FileModelDto dto = TestUtils.CreateFileDto();

		dto
			.Hotkeys
			.AddRange(newHotkeys.ToHotkeyModelsDto());

		using AutoMock mock = AutoMock.GetLoose();

		FileHotkeyEditor sut = mock.Create<FileHotkeyEditor>();

		// Act
		OverwriteHotkeysResult result = await sut.OverwriteAsync(dto, newHotkeys, []);

		// Assert
		result
			.Should()
			.Be(OverwriteHotkeysResult.SameHotkeys);
	}
	#endregion
}
