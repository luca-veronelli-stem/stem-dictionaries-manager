using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

public static class StandardVariableOverrideMapper
{
    public static StandardVariableOverride ToDomain(StandardVariableOverrideEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return StandardVariableOverride.Restore(
            entity.Id,
            entity.DictionaryId,
            entity.StandardVariableId,
            entity.IsEnabled,
            entity.Description);
    }

    public static StandardVariableOverrideEntity ToEntity(StandardVariableOverride domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new StandardVariableOverrideEntity
        {
            Id = domain.Id,
            DictionaryId = domain.DictionaryId,
            StandardVariableId = domain.StandardVariableId,
            IsEnabled = domain.IsEnabled,
            Description = domain.Description
        };
    }

    public static void UpdateEntity(StandardVariableOverrideEntity entity, StandardVariableOverride domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.DictionaryId = domain.DictionaryId;
        entity.StandardVariableId = domain.StandardVariableId;
        entity.IsEnabled = domain.IsEnabled;
        entity.Description = domain.Description;
    }

    public static IReadOnlyList<StandardVariableOverride> ToDomainList(
        IEnumerable<StandardVariableOverrideEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
