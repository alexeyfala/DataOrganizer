using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels.Hierarchy;
using DataOrganizer.Views;
using Shared.Extensions;
using System.Linq;

namespace DataOrganizer.UnitTests.Templates;

[TestFixture(Description = @"Tests of ""EditorTreeViewTemplates.axaml"" resources")]
internal class EditorTreeViewTemplatesTests
{
	#region Data
	/// <summary>
	/// Key of the template of a file in the tree.
	/// </summary>
	private const string FileTemplateKey = "FileInTreeViewTemplate";
	#endregion

	#region Methods
	/// <summary>
	/// <c>FileInTreeViewTemplate</c>: the tip of the hotkeys goes away once the file loses its hotkeys.
	/// </summary>
	[AvaloniaTest]
	public void FileInTreeViewTemplate_Drops_The_Tip_When_The_Hotkeys_Are_Removed()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.AddRange(HotkeyFactory.CreateHotkeyDtos(2));

		using FileNodeViewModel row = new(file, new WorkSession());

		ContentControl host = new()
		{
			Content = row,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(FileTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		IconsBlock icons = host
			.GetVisualDescendants()
			.OfType<IconsBlock>()
			.Single();

		// Act
		file
			.Hotkeys
			.Clear();

		// Assert
		icons.HotkeysToolTip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <c>FileInTreeViewTemplate</c>: the tip of the hotkeys shows the keys the file got last.
	/// </summary>
	[AvaloniaTest]
	public void FileInTreeViewTemplate_Shows_The_New_Hotkeys_After_A_Rewrite()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file
			.Hotkeys
			.AddRange(HotkeyFactory.CreateHotkeyDtos(1));

		HotkeyDto[] newHotkeys = [.. HotkeyFactory.CreateHotkeyDtos(2)];

		using FileNodeViewModel row = new(file, new WorkSession());

		ContentControl host = new()
		{
			Content = row,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(FileTemplateKey)!
		};

		Window window = new()
		{
			Content = host
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		IconsBlock icons = host
			.GetVisualDescendants()
			.OfType<IconsBlock>()
			.Single();

		// Act
		file
			.Hotkeys
			.Clear();

		file
			.Hotkeys
			.AddRange(newHotkeys);

		// Assert
		icons.HotkeysToolTip
			.Should()
			.EndWith(newHotkeys.ToKeyStrokes().ToArray().GetHotkeysPresentation());
	}
	#endregion
}
