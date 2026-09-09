using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using Entities.Models;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.Dto.Entities;

/// <inheritdoc cref="FolderEntity" />
public sealed partial class FolderDto : ExplorerItemDtoBase, IPasswordKeeper
{
	#region Properties
	/// <inheritdoc cref="FolderEntity.Children" />
	public override ObservableCollection<ExplorerItemDtoBase> Children { get; } = [];

	/// <inheritdoc />
	public byte[]? EncryptedDek { get; set; }

	/// <inheritdoc cref="FolderEntity.IsExpanded" />
	[ObservableProperty]
	public override partial bool IsExpanded { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsExpanded" /> changes.
	/// </summary>
	partial void OnIsExpandedChanged(bool value)
	{
		if (Id == default)
		{
			return;
		}

		WeakReferenceMessenger
			.Default
			.Send(new FolderExpandedChangedMessage(Id, value));
	}
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when any child satisfies the condition.
	/// </summary>
	public bool AnyChild(Predicate<ExplorerItemDtoBase> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (condition(item))
			{
				return true;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// <c>True</c> when any child file satisfies the condition.
	/// </summary>
	public bool AnyFile(Predicate<FileDto> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item is FileDto file && condition(file))
			{
				return true;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <inheritdoc />
	/// <remarks>
	/// A folder protects its own contents as well, hence the check of the folder itself.
	/// </remarks>
	public override FolderDto? FindPasswordKeeper() => IsPasswordKeeper() ? this : base.FindPasswordKeeper();

	/// <summary>
	/// Returns a flat sequence of all child objects.
	/// </summary>
	public IEnumerable<ExplorerItemDtoBase> GetAllChildren()
	{
		Stack<ExplorerItemDtoBase> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			yield return item;

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters child objects of <see cref="FolderDto" /> by condition.
	/// </summary>
	public IEnumerable<FileDto> GetFiles(Predicate<FileDto> condition)
	{
		Stack<ExplorerItemDtoBase> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerItemDtoBase item = stack.Pop();

			if (item is FileDto file && condition(file))
			{
				yield return file;
			}

			if (item is FolderDto folder)
			{
				foreach (ExplorerItemDtoBase child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// <c>True</c> when <see cref="EncryptedDek" /> has a value.
	/// </summary>
	public bool IsPasswordKeeper() => EncryptedDek?.IsNotEmpty() ?? false;

	/// <summary>
	/// Returns the folder itself and its immediate subfolders as one sequence.
	/// </summary>
	public IEnumerable<ExplorerItemDtoBase> WithSubfolders()
	{
		return this
			.ToEnumerable()
			.Concat(Children.GetFolders());
	}
	#endregion
}
