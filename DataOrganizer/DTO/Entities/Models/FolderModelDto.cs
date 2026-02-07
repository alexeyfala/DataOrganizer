using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Abstract;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="FolderModel" />
public sealed partial class FolderModelDto : ExplorerModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="FolderModel.Children" />
	public ObservableCollection<ExplorerModelBaseDto> Children { get; } = [];

	/// <summary>
	/// Encrypted password.
	/// </summary>
	public byte[] EncryptedPassword { get; set; } = [];

	/// <inheritdoc cref="FolderModel.PasswordHash" />
	public string? PasswordHash { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <inheritdoc cref="FolderModel.IsExpanded" />
	[ObservableProperty]
	private bool _isExpanded;
	#endregion

	#region Events
	/// <summary>
	/// Occurs when <see cref="IsExpanded" /> changes.
	/// </summary>
	public static event EventHandler<FolderModelDto>? IsExpandedChanged;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsExpanded" /> changes.
	/// </summary>
	partial void OnIsExpandedChanged(bool value)
	{
		if (this.Id == default)
		{
			return;
		}

		IsExpandedChanged?.Invoke(this, this);
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
	#endregion
}
