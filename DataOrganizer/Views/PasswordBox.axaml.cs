using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.ViewModels;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Views;

public sealed partial class PasswordBox : UserControl
{
	#region Properties
	/// <summary>
	/// Header.
	/// </summary>
	public string? Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Label.
	/// </summary>
	public string? Label
	{
		get => GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Header" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty
		.Register<PasswordBox, string?>(name: nameof(Header));

	/// <summary>
	/// Identifies the <see cref="Label" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty
		.Register<PasswordBox, string?>(name: nameof(Label));
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Apply.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteApply))]
	private Task Apply() => _viewModel.SetResultAsync(true);

	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => _viewModel.SetResultAsync(false);
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];

	/// <inheritdoc cref="PasswordBoxViewModel" />
	private readonly PasswordBoxViewModel _viewModel;
	#endregion

	#region Constructors
	public PasswordBox(PasswordBoxViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;

		TextBox
			.GetObservable(TextBox.TextProperty)
			.Subscribe(TextBox_TextPropertyChanged)
			.DisposeWith(_disposables);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextBox.TextProperty" /> changed handler.
	/// </summary>
	private void TextBox_TextPropertyChanged(string? value) => ApplyCommand.NotifyCanExecuteChanged();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromLogicalTree(e);

		_disposables.Dispose();
	}

	public Task<bool> GetResultAsync(in CancellationToken token = default)
	{
		return _viewModel.GetResultAsync(token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="ApplyCommand" />.
	/// </summary>
	private bool CanExecuteApply()
	{
		const char space = ' ';

		return !string.IsNullOrWhiteSpace(TextBox.Text)
			&& !TextBox.Text.StartsWith(space)
			&& !TextBox.Text.EndsWith(space);
	}
	#endregion
}