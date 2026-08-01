using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using DialogHostAvalonia;
using Repository.DTO;
using Shared.Extensions;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>HotkeysEditorView</c>.
/// </summary>
public sealed partial class HotkeysEditorViewModel :
	ObservableDisposableBase,
	IRecipient<GlobalKeyReleasedMessage>
{
	#region Properties
	/// <summary>
	/// Maximum number of hotkeys.
	/// </summary>
	public static int MaxHotkeys { get; } = IKeyboardInputHook.MaxHotkeys;

	/// <summary>
	/// Buffer of keys for which the mask is used.
	/// </summary>
	public ObservableCollection<CodeMaskPair> Buffer { get; } = [];

	/// <summary>
	/// <c>True</c> when the user has saved the hotkeys.
	/// </summary>
	public bool IsSaved { get; private set; }

	/// <summary>
	/// Hotkey list preview text.
	/// </summary>
	[ObservableProperty]
	public partial string Preview { get; set; } = Strings.AssigningHotkeys;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears <see cref=Buffer"" />.
	/// </summary>
	[RelayCommand]
	internal void Clear() => Buffer.Clear();

	/// <summary>
	/// <see cref="InputElement.KeyUp" /> event handler of <see cref="UserControl" />.
	/// </summary>
	[RelayCommand]
	internal void KeyUp(KeyEventArgs? e)
	{
		if (e is null
			|| e.KeyModifiers != KeyModifiers.None
			|| e.Key != Key.Enter)
		{
			return;
		}

		SaveAndClose();
	}

	/// <summary>
	/// Saves hotkeys and closes the view.
	/// </summary>
	[RelayCommand]
	internal void SaveAndClose()
	{
		IsSaved = true;

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		DialogHost.Close(null);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IGlobalHookRunner" />
	private readonly IGlobalHookRunner _hookRunner;

	/// <summary>
	/// <c>True</c> when the <see cref="Buffer" /> should be cleared.
	/// </summary>
	private bool _isClearBuffer;
	#endregion

	#region Constructors
	public HotkeysEditorViewModel(
		IGlobalHookRunner hookRunner,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler)
	{
		messenger.RegisterAll(this);

		Buffer.CollectionChanged += Buffer_CollectionChanged;

		Disposable.Create(() =>
		{
			messenger.UnregisterAll(this);
			Buffer.CollectionChanged -= Buffer_CollectionChanged;
		}).DisposeWith(_disposables);

		_hookRunner = hookRunner;

		exceptionHandler.Watch(hookRunner.StartAsync());
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ObservableCollection{T}.CollectionChanged" /> event handler of <see cref="Buffer" />.
	/// </summary>
	private void Buffer_CollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e)
	{
		MakePreview();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Receive(GlobalKeyReleasedMessage message)
	{
		HandleKeyReleased(
			message.Mask,
			message.Code);
	}

	/// <summary>
	/// Stops the global hook and waits until it is actually stopped.
	/// </summary>
	public Task StopHookAsync(CancellationToken token = default) => _hookRunner.StopAsync(token);

	/// <summary>
	/// Handles a released key.
	/// </summary>
	internal void HandleKeyReleased(EventMask rawMask, KeyCode code)
	{
		EventMask mask = rawMask.RemoveFlag(EventMask.NumLock);

		if (mask.IsDefault()
			|| IsMask(code)
			|| (Buffer.Any() && mask != Buffer.Last().Mask))
		{
			return;
		}

		if (!_isClearBuffer)
		{
			_isClearBuffer = true;

			Buffer.Clear();
		}

		if (Buffer.Count == MaxHotkeys)
		{
			return;
		}

		Buffer.Add(new()
		{
			Code = code,
			Mask = mask
		});
	}

	/// <summary>
	/// Controls creation value for <see cref="Preview" />.
	/// </summary>
	internal void MakePreview()
	{
		Preview = Buffer.Count > 0
			? Buffer.ToArray().GetHotkeysPresentation()
			: Strings.AssigningHotkeys;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		Buffer.Clear();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <see cref="KeyCode" /> is mask.
	/// </summary>
	private static bool IsMask(KeyCode code) => code switch
	{
		KeyCode.VcCapsLock => true,
		KeyCode.VcScrollLock => true,
		KeyCode.VcNumLock => true,
		KeyCode.VcLeftShift => true,
		KeyCode.VcRightShift => true,
		KeyCode.VcLeftControl => true,
		KeyCode.VcRightControl => true,
		KeyCode.VcLeftAlt => true,
		KeyCode.VcRightAlt => true,
		KeyCode.VcLeftMeta => true,
		KeyCode.VcRightMeta => true,
		_ => false
	};
	#endregion
}
