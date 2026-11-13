using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Extensions;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.SystemIntegration.Clipboard.Interfaces;
using DataOrganizer.SystemIntegration.Clipboard.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.SystemIntegration.Clipboard.Services;

/// <summary>
/// The files whose contents were copied, the latest first, with the one chosen in their list; kept in a file of its own.
/// </summary>
public sealed class CopyHistory : ICopyHistory, ICopyHistoryRecorder, ICopyHistoryStore
{
	#region Properties
	/// <inheritdoc />
	public ReadOnlyObservableCollection<Guid> ItemIds { get; }

	/// <inheritdoc />
	public Guid SelectedItemId { get; set; }
	#endregion

	#region Data
	/// <summary>
	/// Source of <see cref="ItemIds" />.
	/// </summary>
	private readonly ObservableCollection<Guid> _itemIds = [];

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IWindowSettingsStore" />
	private readonly IWindowSettingsStore _windowSettings;
	#endregion

	#region Constructors
	public CopyHistory(IMessenger messenger, IWindowSettingsStore windowSettings)
	{
		_messenger = messenger;

		_windowSettings = windowSettings;

		ItemIds = new(_itemIds);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Add(FileDto file, bool showAtOnce)
	{
		int index = _itemIds.IndexOf(file.Id);

		if (index >= 0)
		{
			_itemIds.Move(index, 0);
		}
		else
		{
			_itemIds.Insert(0, file.Id);
		}

		if (!showAtOnce)
		{
			return;
		}

		_messenger.Send(new CopyHistoryFileAddedMessage(file));
	}

	/// <inheritdoc />
	public void Clear()
	{
		_itemIds.Clear();

		SelectedItemId = default;

		_messenger.Send(new CopyHistoryClearedMessage());
	}

	/// <inheritdoc />
	public void Load(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		if (_windowSettings.Read<CopyHistoryViewSettings>() is not { } settings)
		{
			return;
		}

		hierarchy
			.FilterFilesById(settings.ItemIds)
			.ForEach(x => _itemIds.Add(x.Id));

		if (_itemIds.Count == 0)
		{
			return;
		}

		SelectedItemId = settings.SelectedItemId;
	}

	/// <inheritdoc />
	public void Remove(FileDto file)
	{
		_itemIds.Remove(file.Id);

		_messenger.Send(new CopyHistoryFileRemovedMessage(file));
	}

	/// <inheritdoc />
	public void Save()
	{
		_windowSettings.Write(new CopyHistoryViewSettings
		{
			ItemIds = [.. _itemIds],
			SelectedItemId = SelectedItemId
		});
	}
	#endregion
}
