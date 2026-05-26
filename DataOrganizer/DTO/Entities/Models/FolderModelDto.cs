using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Messages;
using Entities.Models;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="FolderModel" />
public sealed partial class FolderModelDto : ExplorerModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="FolderModel.Children" />
	public override ObservableCollection<ExplorerModelBaseDto> Children { get; } = [];

	/// <inheritdoc cref="FolderModel.EncryptedDek" />
	public byte[]? EncryptedDek { get; set; }

	/// <inheritdoc cref="FolderModel.IsExpanded" />
	[ObservableProperty]
	public override partial bool IsExpanded { get; set; }

	/// <inheritdoc cref="FolderModel.PasswordHash" />
	public string? PasswordHash { get; set; }

	/// <summary>
	/// Encrypted within the session DEK.
	/// </summary>
	public byte[]? SessionEncryptedDek { get; set; }
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
	/// Returns <c>True</c> if any child satisfies the condition.
	/// </summary>
	public bool AnyChild(Predicate<ExplorerModelBaseDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (condition(item))
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Returns <c>True</c> if any child file satisfies the condition.
	/// </summary>
	public bool AnyFile(Predicate<FileModelDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FileModelDto file && condition(file))
			{
				return true;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Checks self by <see cref="IsPasswordKeeper" /> and returns or tries to find parent
	/// that returns <c>True</c> on <see cref="IsPasswordKeeper" />.
	/// </summary>
	public FolderModelDto? FindPasswordKeeperOrSelf()
	{
		if (IsPasswordKeeper())
		{
			return this;
		}

		return FindParent(x => x.IsPasswordKeeper());
	}

	/// <summary>
	/// Returns a flat sequence of all child objects.
	/// </summary>
	public IEnumerable<ExplorerModelBaseDto> GetAllChildren()
	{
		Stack<ExplorerModelBaseDto> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			yield return item;

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Filters child objects of <see cref="FolderModelDto" /> by condition.
	/// </summary>
	public IEnumerable<FileModelDto> GetFiles(Predicate<FileModelDto> condition)
	{
		Stack<ExplorerModelBaseDto> stack = new(Children);

		while (stack.Count > 0)
		{
			ExplorerModelBaseDto item = stack.Pop();

			if (item is FileModelDto file && condition(file))
			{
				yield return file;
			}

			if (item is FolderModelDto folder)
			{
				foreach (ExplorerModelBaseDto child in folder.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Returns <c>True</c> if <see cref="EncryptedDek" />, <see cref="PasswordHash" /> have values.
	/// </summary>
	public bool IsPasswordKeeper()
	{
		return EncryptedDek is { } dek
			&& dek.IsNotEmpty()
			&& !string.IsNullOrEmpty(PasswordHash);
	}
	#endregion
}
