using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels;
using Entities.Enums;

namespace DataOrganizer.Services.Documents;

public sealed class EmbeddedEditorFactory : IEmbeddedEditorFactory
{
	#region Data
	/// <inheritdoc cref="IViewModelFactory" />
	private readonly IViewModelFactory _viewModelFactory;
	#endregion

	#region Constructors
	public EmbeddedEditorFactory(IViewModelFactory viewModelFactory) => _viewModelFactory = viewModelFactory;
	#endregion

	#region Methods
	/// <inheritdoc />
	public EmbeddedEditorViewModelBase? Create(FileDto file)
	{
		EmbeddedEditorViewModelBase? viewModel = file.Kind switch
		{
			EntityKind.Dataset => _viewModelFactory.CreateViewModel<DatasetEditorViewModel>(),
			EntityKind.File => CreateFileEditor(file),
			_ => null
		};

		if (viewModel is null)
		{
			return null;
		}

		viewModel.FileId = file.Id;

		viewModel.InitialEditorState = file.EditorState;

		viewModel.KeeperId = file.FindUnlockedKeeper()?.Id;

		viewModel.SetEditorStateCallback = state => file.EditorState = state;

		viewModel.SetUpdatedAtCallback = updatedAt => file.UpdatedAt = updatedAt;

		viewModel.Initialize();

		return viewModel;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates the editor of the text of a file, which takes the language of the text from the name of the file.
	/// </summary>
	private EmbeddedFileEditorViewModel CreateFileEditor(FileDto file)
	{
		EmbeddedFileEditorViewModel viewModel = _viewModelFactory.CreateViewModel<EmbeddedFileEditorViewModel>();

		viewModel.FileName = file.Name;

		return viewModel;
	}
	#endregion
}
