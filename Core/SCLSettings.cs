﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Reductech.EDR.Core.Entities;

namespace Reductech.EDR.Core
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// External settings for running the step.
/// </summary>
public record SCLSettings(Entity Entity)
{
    public const string VersionKey = "Version";
    public const string ConnectorsKey = "Connectors";
    public const string FeaturesKey = "Features";

    public static readonly SCLSettings EmptySettings =
        new(new Entity(ImmutableDictionary<string, EntityProperty>.Empty));

    public static SCLSettings CreateFromIConfiguration(IConfiguration configuration)
    {
        var properties = new List<EntityProperty>();

        foreach (var section in configuration.GetChildren())
        {
            var property = CreateObject(section);

            properties.Add(property);
        }

        var entity = new Entity(properties);

        return new SCLSettings(entity);

        static EntityProperty CreateObject(IConfigurationSection section)
        {
            if (section.GetChildren().Any())
            {
                var properties = section.GetChildren().Select(CreateObject).ToList();

                var entity = new Entity(properties);

                return new EntityProperty(section.Key, new EntityValue(entity), null, 0);
            }
            else
            {
                var ev = new EntityValue(section.Value);
                return new EntityProperty(section.Key, ev, null, 0);
            }
        }
    }

    public static SCLSettings CreateFromString(string jsonString)
    {
        var entity = CreateEntityFromString(jsonString);
        return new SCLSettings(entity);
    }

    private static Entity CreateEntityFromString(string s)
    {
        var entity = JsonConvert.DeserializeObject<Entity>(s, EntityJsonConverter.Instance)!;
        return entity;
    }
}

}