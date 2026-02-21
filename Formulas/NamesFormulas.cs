using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using Game.UI;
using StationSignage.Systems;
using System;
using Unity.Entities;

namespace StationSignage.Formulas;

public class NamesFormulas
{
    private static SS_LineStatusSystem _linesSystem;
    private static NameSystem _nameSystem;
    private static TimeSystem _timeSystem;
    private static EntityManager _entityManager;

    public NamesFormulas()
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private static string GetName(string id)
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (id is null || id.Length == 0)
        {
            return "";
        }
        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
    }

    private static readonly Func<Entity, string> GetMainBuildingNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return _entityManager.TryGetComponent<Owner>(buildingRef, out var owner) ? _nameSystem.GetRenderedLabelName(owner.m_Owner) : "";
    };

    private static readonly Func<Entity, string> GetBuildingNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        return _nameSystem.GetRenderedLabelName(GetOwnerRecursive(buildingRef));
    };

    private static Entity GetOwnerRecursive(Entity entity)
    {
        return _entityManager.TryGetComponent<Owner>(entity, out var owner) ? GetOwnerRecursive(owner.m_Owner) : entity;
    }

    private static readonly Func<Entity, string> GetBuildingRoadNameBinding = (buildingRef) =>
    {
        _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.TryGetComponent<Building>(buildingRef, out var building);
        return _entityManager.TryGetComponent<Aggregated>(building.m_RoadEdge, out var aggregated) ? _nameSystem.GetRenderedLabelName(aggregated.m_Aggregate) : "";
    };
    public static string GetMainBuildingName(Entity buildingRef) => GetMainBuildingNameBinding(buildingRef);

    public static string GetBuildingName(Entity buildingRef) => GetBuildingNameBinding(buildingRef);

    public static string GetExitName(Entity buildingRef) => GetName("StationSignage.Exit");

    public static string GetBuildingRoadName(Entity buildingRef) => GetBuildingRoadNameBinding(buildingRef);

    public static string GetPlatformName(Entity buildingRef) => GetName("StationSignage.Platform");

    public static string GetTrainsToName(Entity buildingRef) => GetName("StationSignage.TrainsTo");

    public static string GetConnectionsName(Entity buildingRef) => GetName("StationSignage.Connections");

    public static string GetTransferName(Entity buildingRef) => GetName("StationSignage.Transfer");

    public static string GetBoardingName(Entity buildingRef) => GetName("StationSignage.Boarding");

    public static string GetNoSmoking(Entity buildingRef) => GetName("StationSignage.NoSmoking");

    public static string GetTimeString(Entity buildingRef)
    {
        _timeSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TimeSystem>();
        var timeFormat = SharedSettings.instance.userInterface.timeFormat switch
        {
            InterfaceSettings.TimeFormat.TwentyFourHours => "HH:mm",
            InterfaceSettings.TimeFormat.TwelveHours => "hh:mm tt",
            _ => "HH:mm"
        };
        return _timeSystem.GetCurrentDateTime().ToString(timeFormat);
    }

    public static string GetLinesStatusMessage(Entity buildingRef) =>
        GetName("StationSignage.LineStatus");
}