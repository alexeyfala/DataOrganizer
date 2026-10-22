using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Models.Dataset;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
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
