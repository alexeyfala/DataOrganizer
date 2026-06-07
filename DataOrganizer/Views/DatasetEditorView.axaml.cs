using Avalonia.Controls;
using DataOrganizer.DTO.Dataset;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Views;

internal partial class DatasetEditorView : UserControl
{
	#region Constructors
	public DatasetEditorView() => InitializeComponent();

	public DatasetEditorView(DatasetEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="RecyclingElementFactory.SelectTemplateKey" /> handler. Routes
	/// each record to its own recycle pool by item type so that
	/// <see cref="ItemsRepeater" /> never reassigns a pooled element built from
	/// one template (e.g. <c>RecordsGroupTemplate</c>) to a record of a different
	/// type — that cross-template recycling caused the "invisible elements"
	/// glitch where bindings silently fail against a mismatched visual tree.
	/// </summary>
	private void OnSelectRecordTemplateKey(object? sender, SelectTemplateEventArgs e)
	{
		e.TemplateKey = e.DataContext switch
		{
			KeyValueRecord => nameof(KeyValueRecord),
			ValueRecord => nameof(ValueRecord),
			RecordsGroup => nameof(RecordsGroup),
			_ => string.Empty
		};
	}
	#endregion
}
