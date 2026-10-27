using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataProtection.Helpers;
using DatasetRecords.Models;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Shared.Common;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Templates;

[TestFixture(Description = @"Tests of ""ContextMenuTemplates.axaml"" resources")]
internal class ContextMenuTemplatesTests
{
	#region Data
	/// <summary>
	/// Key of the template of the menu of a key and value record.
	/// </summary>
	private const string KeyValueRecordTemplateKey = "KeyValueRecordContextMenuTemplate";

	/// <summary>
	/// Key of the template of the menu of a group of records.
	/// </summary>
	private const string RecordsGroupTemplateKey = "RecordsGroupContextMenuTemplate";

	/// <summary>
	/// Key of the template of the menu of a value record.
	/// </summary>
	private const string ValueRecordTemplateKey = "ValueRecordContextMenuTemplate";
	#endregion

	#region Methods
	/// <summary>
	/// <c>FolderInTreeViewContextMenuTemplate</c> and <c>FileInTreeViewContextMenuTemplate</c>: the menu of an object of the
	/// tree starts with its name and the two buttons that rename and delete it.
	/// </summary>
	[AvaloniaTest]
	public void ExplorerItemContextMenuTemplates_Start_With_The_Name_And_The_Buttons_Of_The_Object(
		[Values("FolderInTreeViewContextMenuTemplate", "FileInTreeViewContextMenuTemplate")] string templateKey)
	{
		// Arrange
		ExplorerItemDtoBase dto = templateKey == "FileInTreeViewContextMenuTemplate"
			? ItemDtoFactory.CreateFileDto()
			: ItemDtoFactory.CreateFolderDto();

		ContentControl menu = new()
		{
			Content = dto,
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(templateKey)!
		};

		Window window = new()
		{
			Content = menu
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		menu.GetVisualDescendants()
			.OfType<TextBlock>()
			.First()
			.Text
			.Should()
			.Be(dto.Name);

		menu.GetVisualDescendants()
			.OfType<FlyoutButton>()
			.Take(2)
			.Select(static x => x.CommandParameter)
			.Should()
			.Equal(dto, dto);
	}

	/// <summary>
	/// <c>KeyValueRecordContextMenuTemplate</c>, <c>RecordsGroupContextMenuTemplate</c>,
	/// <c>ValueRecordContextMenuTemplate</c>: the header of a menu of an encrypted dataset gives none of its text to UI
	/// Automation, and the header of an open one gives it.
	/// </summary>
	[AvaloniaTest]
	public void RecordContextMenuTemplates_Keep_The_Header_Of_An_Encrypted_Dataset_Out_Of_UI_Automation(
		[Values(KeyValueRecordTemplateKey, RecordsGroupTemplateKey, ValueRecordTemplateKey)] string templateKey,
		[Values] bool isEncrypted)
	{
		// Arrange
		string text = RandomString.Create(16);

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		ContentControl menu = new()
		{
			Content = CreateItem(templateKey, text),
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(templateKey)!
		};

		Window window = new()
		{
			Content = new UserControl
			{
				Content = menu,
				DataContext = viewModel
			}
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		string?[] names =
		[
			.. menu
				.GetVisualDescendants()
				.OfType<Control>()
				.Select(static x => ControlAutomationPeer
					.CreatePeerForElement(x)
					.GetName())
		];

		// Assert
		names
			.Count(x => x != null && x.Contains(text))
			.Should()
			.Be(isEncrypted ? 0 : 1);
	}

	/// <summary>
	/// <c>ValueRecordContextMenuTemplate</c>: the header shows what the field shows, the mask of a hidden value and an open
	/// value itself.
	/// </summary>
	[AvaloniaTest]
	public void ValueRecordContextMenuTemplate_Shows_The_Mask_Of_A_Hidden_Value([Values] bool isHidden)
	{
		// Arrange
		string value = RandomString.Create(16);

		using AutoMock mock = AutoMock.GetLoose();

		using DatasetEditorViewModel viewModel = mock.Create<DatasetEditorViewModel>();

		ContentControl menu = new()
		{
			Content = new ValueRecord
			{
				IsHidden = isHidden,
				Value = value
			},
			ContentTemplate = (IDataTemplate)Application.Current!.FindResource(ValueRecordTemplateKey)!
		};

		Window window = new()
		{
			Content = new UserControl
			{
				Content = menu,
				DataContext = viewModel
			}
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		string? header = menu
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.First()
			.Text;

		header
			.Should()
			.Be(SecretMask.Apply(value, isHidden));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the item a menu template shows, with <paramref name="text" /> in its header.
	/// </summary>
	private static object CreateItem(string templateKey, string text)
	{
		return templateKey switch
		{
			KeyValueRecordTemplateKey => new KeyValueRecord
			{
				Key = text
			},
			RecordsGroupTemplateKey => new RecordsGroup
			{
				Name = text
			},
			_ => new ValueRecord
			{
				Value = text
			}
		};
	}
	#endregion
}
