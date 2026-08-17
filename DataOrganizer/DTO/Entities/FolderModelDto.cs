using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Extensions;
using DataOrganizer.Messages;
using Entities.Models;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.DTO.Entities;

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
	/// <c>True</c> when any child file satisfies the condition.
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

	/// <inheritdoc />
	/// <remarks>
	/// A folder protects its own contents as well, hence the check of the folder itself.
	/// </remarks>
	public override FolderModelDto? FindPasswordKeeper() => IsPasswordKeeper() ? this : base.FindPasswordKeeper();

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
	/// <c>True</c> when <see cref="EncryptedDek" /> has a value.
	/// </summary>
	public bool IsPasswordKeeper() => EncryptedDek?.IsNotEmpty() ?? false;

	/// <summary>
	/// Returns the folder itself and its immediate subfolders as one sequence.
	/// </summary>
	public IEnumerable<ExplorerModelBaseDto> WithSubfolders()
	{
		return this
			.ToEnumerable()
			.Concat(Children.GetFolders());
	}
	#endregion
}
