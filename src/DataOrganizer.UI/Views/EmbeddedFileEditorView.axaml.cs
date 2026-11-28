using Avalonia.Controls;
using DataOrganizer.UI.ViewModels;
using System;

namespace DataOrganizer.UI.Views;

/// <summary>
/// Text editor of a file opened in the built-in editor.
/// </summary>
internal partial class EmbeddedFileEditorView : UserControl, IDisposable
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="EmbeddedFileEditorView" /> class.
	/// </summary>
	public EmbeddedFileEditorView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="EmbeddedFileEditorView" /> class.
	/// </summary>
	public EmbeddedFileEditorView(EmbeddedFileEditorViewModel viewModel) : this() => DataContext = viewModel;
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();
	#endregion
}
