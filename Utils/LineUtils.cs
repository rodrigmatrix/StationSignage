﻿using System;
using System.Collections.Specialized;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Routes;
using Game.UI;
using Game.Vehicles;
using StationSignage.BridgeWE;
using Unity.Entities;

namespace StationSignage.Utils;

public static class LineUtils
{
    
    private static LinesSystem _linesSystem;
    private static NameSystem _nameSystem;
    private static EntityManager _entityManager;
        
    public const string Transparent = "Transparent";

    public const string Empty = "";
    
    private static readonly StringDictionary ModelsDictionary = new()
    {
        { "SubwayCar01", "A" },
        { "SubwayEngine01", "A" },
        { "EU_TrainPassengerCar01", "B" },
        { "EU_TrainPassengerEngine01", "B" },
        { "NA_TrainPassengerCar01", "C" },
        { "NA_TrainPassengerEngine01", "C" },
    };
    
    public static Tuple<string, string> GetRouteName(Entity entity)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var nameSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        var fullLineName = nameSystem.GetName(entity).Translate();
        entityManager.TryGetComponent<RouteNumber>(entity, out var routeNumber);
        var routeName = Mod.m_Setting.LineDisplayNameDropdown switch
        {
            Settings.LineDisplayNameOptions.Custom => GetSmallLineName(fullLineName, routeNumber),
            Settings.LineDisplayNameOptions.WriteEverywhere => WERouteFn.GetTransportLineNumber(entity),
            Settings.LineDisplayNameOptions.Generated => routeNumber.m_Number.ToString(),
            _ => GetSmallLineName(fullLineName, routeNumber)
        };
        return Tuple.Create(fullLineName, routeName);
    }
    
    private static string GetSmallLineName(string fullLineName, RouteNumber routeNumber)
    {
        return fullLineName is { Length: >= 1 and <= 3 } ? fullLineName : routeNumber.m_Number.ToString();
    }

    public static string GetTrainName(Entity entity)
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
        _entityManager.TryGetComponent<Controller>(entity, out var controller);
        _entityManager.TryGetComponent<Owner>(controller.m_Controller, out var owner);
        _entityManager.TryGetBuffer<LayoutElement>(controller.m_Controller, true, out var layoutElements);
        _entityManager.TryGetBuffer<OwnedVehicle>(owner.m_Owner, true, out var ownerVehicles);

        var index = 0;
    
        for (var i = 0; i < ownerVehicles.Length; ++i)
        {
            if (ownerVehicles[i].m_Vehicle == controller.m_Controller)
            {
                index = i;
            }
        }

        index++;
        var entityDebugName = _nameSystem.GetDebugName(entity);
        var entityName = entityDebugName.TrimEnd(' ').Remove(entityDebugName.LastIndexOf(' ') + 1);
        var letter = "F";
        if (ModelsDictionary.ContainsKey(entityName))
        {
            letter = ModelsDictionary[entityName];
        }

        if (entityName.Contains("Subway"))
        {
            letter = "A";
        }
        if (entityName.Contains("EU_Train"))
        {
            letter = "B";
        }
        if (entityName.Contains("NA_Train"))
        {
            letter = "C";
        }
        if (index < 10)
        {
            return letter + "0" + index;
        }

        return letter + index;
    }
}