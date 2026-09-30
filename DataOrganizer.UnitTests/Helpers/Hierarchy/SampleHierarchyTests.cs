using AwesomeAssertions;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Helpers.Hierarchy;
using DataOrganizer.Helpers.Text;
using Entities.Enums;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(SampleHierarchy)}"" type")]
internal class SampleHierarchyTests
{
	#region Methods
	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: every object comes after the folder that holds it.
	/// </summary>
	[Test]
	public void Create_Adds_Each_Object_After_Its_Folder()
	{
		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, DateTime.Now);

		// Assert
		HashSet<Guid> earlierFolderIds = [];

		List<string> misplaced = [];

		foreach (ExplorerItemBase item in items)
		{
			if (item.ParentId is { } parentId && !earlierFolderIds.Contains(parentId))
			{
				misplaced.Add(item.Name);
			}

			if (item is FolderEntity)
			{
				earlierFolderIds.Add(item.Id);
			}
		}

		misplaced
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: the files of the random branch have extensions that neither a grammar nor the
	/// check of executables knows.
	/// </summary>
	[Test]
	public void Create_Gives_The_Random_Files_Unknown_Extensions()
	{
		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, DateTime.Now);

		// Assert
		// The objects come after their folders, so one pass finds the whole branch.
		HashSet<Guid?> branchIds = [items.Single(x => x.Name == "Random").Id];

		List<string> fileNames = [];

		foreach (ExplorerItemBase item in items)
		{
			if (!branchIds.Contains(item.ParentId))
			{
				continue;
			}

			if (item is FolderEntity)
			{
				branchIds.Add(item.Id);
			}
			else if (item.Kind == EntityKind.File)
			{
				fileNames.Add(item.Name);
			}
		}

		fileNames
			.Should()
			.NotBeEmpty();

		fileNames.Where(x => SyntaxRegistry.Instance.FindLanguage(x) is not null || ExecutableFileDetector.IsExecutable(x))
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: every folder holds something.
	/// </summary>
	[Test]
	public void Create_Leaves_No_Folder_Empty()
	{
		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, DateTime.Now);

		// Assert
		HashSet<Guid?> parentIds = [.. items.Select(x => x.ParentId)];

		items.OfType<FolderEntity>().Where(x => !parentIds.Contains(x.Id))
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: the children of each folder are numbered from zero in the order they come.
	/// </summary>
	[Test]
	public void Create_Numbers_The_Children_Of_Each_Folder_From_Zero()
	{
		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, DateTime.Now);

		// Assert
		items.Where(x => x.ParentId is not null).GroupBy(x => x.ParentId)
			.Should()
			.AllSatisfy(x => x.Select(y => y.Index).Should().Equal(Enumerable.Range(0, x.Count())));
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: only the folder of the run stands in the root, at the given position and named
	/// after the time of the run.
	/// </summary>
	[Test]
	public void Create_Puts_The_Objects_Into_The_Folder_Of_The_Run()
	{
		// Arrange
		DateTime now = new(2026, 9, 30, 14, 5, 37);

		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 7, now);

		// Assert
		items.Where(x => x.ParentId is null)
			.Should()
			.Equal(items[0]);

		items[0]
			.Should()
			.BeEquivalentTo(new
			{
				Index = 7,
				Kind = EntityKind.Folder,
				Name = "Samples 2026-09-30 14-05-37"
			});
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: the objects get the same names in every run.
	/// </summary>
	[Test]
	public void Create_Repeats_The_Names_From_Run_To_Run()
	{
		// Arrange
		DateTime now = DateTime.Now;

		string[] first = [.. SampleHierarchy.Create(rootIndex: 0, now).Select(x => x.Name)];

		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, now);

		// Assert
		items.Select(x => x.Name)
			.Should()
			.Equal(first);
	}

	/// <summary>
	/// <see cref="SampleHierarchy.Create" />: every object is created and updated at the time of the run.
	/// </summary>
	[Test]
	public void Create_Stamps_Every_Object_With_The_Time_Of_The_Run()
	{
		// Arrange
		DateTime now = new(2026, 9, 30, 14, 5, 37);

		// Act
		ExplorerItemBase[] items = SampleHierarchy.Create(rootIndex: 0, now);

		// Assert
		items
			.Should()
			.OnlyContain(x => x.CreatedAt == now && x.UpdatedAt == now);
	}
	#endregion
}
