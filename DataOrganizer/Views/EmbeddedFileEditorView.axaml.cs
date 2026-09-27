using Avalonia.Controls;
using DataOrganizer.ViewModels;
using System;

namespace DataOrganizer.Views;

internal partial class EmbeddedFileEditorView : UserControl, IDisposable
{
	#region Constructors
	public EmbeddedFileEditorView() => InitializeComponent();

	public EmbeddedFileEditorView(EmbeddedFileEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();
	#endregion
}
