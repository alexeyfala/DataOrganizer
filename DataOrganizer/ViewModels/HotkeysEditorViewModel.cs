using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Repository.DTO;
using Shared.Extensions;
using Shared.Properties;
using SharpHook;
using SharpHook.Data;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="HotkeysEditorView" />.
/// </summary>
public sealed partial class HotkeysEditorViewModel : ObservableDisposable
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
	/// Returns <c>True</c> if user has saved the hotkeys.
	/// </summary>
	public bool IsSaved { get; private set; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Hotkey list preview text.
	/// </summary>
	[ObservableProperty]
	private string _preview = Strings.AssigningHotkeys;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears <see cref=Buffer"" />.
	/// </summary>
	[RelayCommand]
	public void Clear() => Buffer.Clear();

	/// <summary>
	/// <see cref="InputElement.KeyUp" /> event handler of <see cref="UserControl" />.
	/// </summary>
	[RelayCommand]
	public void KeyUp(KeyEventArgs? e)
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
	public void SaveAndClose()
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
	/// <inheritdoc cref="IGlobalHook" />
	private readonly IGlobalHook _hook;

	/// <summary>
	/// Returns <c>True</c> if the <see cref="Buffer" /> should be cleared.
	/// </summary>
	private bool _isClearBuffer;
	#endregion

	#region Constructors
	public HotkeysEditorViewModel(IGlobalHook hook)
	{
		_hook = hook;

		Observable.FromEventPattern<KeyboardHookEventArgs>(
			x => hook.KeyReleased += x,
			x => hook.KeyReleased -= x)
			.Subscribe(Hook_KeyReleased)
			.DisposeWith(_disposables);

		Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
			x => Buffer.CollectionChanged += x,
			x => Buffer.CollectionChanged -= x)
			.Subscribe(Buffer_CollectionChanged)
			.DisposeWith(_disposables);

		_ = hook.RunAsync();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ObservableCollection{T}.CollectionChanged" /> event handler of <see cref="Buffer" />.
	/// </summary>
	private void Buffer_CollectionChanged(EventPattern<NotifyCollectionChangedEventArgs> e)
	{
		MakePreview();
	}

	/// <summary>
	/// <see cref="GlobalHookBase.KeyReleased" /> event handler.
	/// </summary>
	private void Hook_KeyReleased(EventPattern<KeyboardHookEventArgs> e)
	{
		AddToBuffer(
			e.EventArgs.RawEvent.Mask,
			e.EventArgs.Data.KeyCode);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Adds values to <see cref="Buffer" /> or ignores them.
	/// </summary>
	public void AddToBuffer(in EventMask rawMask, in KeyCode code)
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
	public void MakePreview()
	{
		Preview = Buffer.Count > 0
			? Buffer.ToArray().GetHotkeysPresentation()
			: Strings.AssigningHotkeys;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		_hook.Dispose();

		Buffer.Clear();
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns <c>True</c> if <see cref="KeyCode" /> is mask.
	/// </summary>
	private static bool IsMask(in KeyCode code) => code switch
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
