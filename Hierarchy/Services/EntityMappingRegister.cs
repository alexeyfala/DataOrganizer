using Entities.Models;
using Hierarchy.Dto;
using Mapster;

namespace Hierarchy.Services;

/// <summary>
/// Rules that map the stored entities to the objects of the explorer tree.
/// </summary>
public sealed class EntityMappingRegister : IRegister
{
	#region Methods
	/// <inheritdoc />
	public void Register(TypeAdapterConfig config)
	{
		config.NewConfig<HotkeyEntity, HotkeyDto>();

		config
			.NewConfig<FileEntity, FileDto>()
			.Ignore(dest => dest.Parent!);

		config
			.NewConfig<FolderEntity, FolderDto>()
			.Ignore(dest => dest.Parent!)
			.Ignore(dest => dest.Children);

		config
			.NewConfig<ExplorerItemBase, ExplorerItemDtoBase>()
			.MapWith(src => src.GetType() == typeof(FileEntity)
				? ((FileEntity)src).Adapt<FileDto>(config)
				: ((FolderEntity)src).Adapt<FolderDto>(config));
	}
	#endregion
}
