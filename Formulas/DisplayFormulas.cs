using Colossal.Entities;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using StationSignage.BridgeWE;
using StationSignage.Components;
using StationSignage.Systems;
using StationSignage.WEBridge;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StationSignage.Formulas;

public class DisplayFormulas
{
    private static CityConfigurationSystem _cityConfigurationSystem;
    private const string Square = "LineBgSquare";
    private const string Circle = "LineBgCircle";

    private static World refWorld;

    private static EntityManager EntityManager => (refWorld ??= World.DefaultGameObjectInjectionWorld).EntityManager;

    public static int GetTvChannel(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        if (int.TryParse(channelStr, out var channel))
        {
            return channel;
        }
        return 0; // Default channel if parsing fails
    }
    public static Color GetTvBarColors(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        int.TryParse(channelStr, out var channel);
        return channel switch
        {
            1 => EntityManager.TryGetComponent<Owner>(PlatformFormulas.GetIncomingTrainDestinationForPlatform(PlatformFormulas.GetPlatform(_, vars)), out var owner)
                && EntityManager.TryGetComponent<Game.Routes.Color>(owner.m_Owner, out var routeColor)
                    ? routeColor.m_Color
                    : Color.white,
            _ => Color.white
        }; // Default channel if parsing fails
    }
    public static string GetTvChannelFooter(Entity _, Dictionary<string, string> vars)
    {
        vars.TryGetValue("tvCh", out var channelStr);
        int.TryParse(channelStr, out var channel);
        return channel switch
        {
            1 => WERouteFn.GetWaypointStaticDestinationName(PlatformFormulas.GetIncomingTrainDestinationForPlatform(PlatformFormulas.GetPlatform(_, vars))),
            _ => GetWelcomeMessage(SS_LineStatusSystem.Instance.EntityManager.GetComponentData<SS_PlatformData>(PlatformFormulas.GetPlatform(_, vars)).type)
        };
    }


    public static string GetLineBackgroundShape(Entity buildingRef)
    {
        return SS_SettingSystem.Instance.LineIndicatorShape switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };
    }
    
    public static string GetImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var idxStr);
        int.TryParse(idxStr, out var idx);
        return GetImageList(buildingRef, vars).ElementAtOrDefault(idx);
    }
    
    public static HashSet<string> GetImageList(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("images", out var images);
        if (string.IsNullOrWhiteSpace(images))
            return [];

        return images
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet();
    }

    public static string GetPlatformImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        return "Circle" + PlatformFormulas.GetPlatformInt(vars);
    }

    public static string GetWelcomeMessage(TransportType lineType)
    {
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return lineType switch
        {
            TransportType.Subway => GetName("StationSignage.WelcomeSubway").Replace("%s", _cityConfigurationSystem.cityName),
            TransportType.Train => GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName),
            _ => GetName("StationSignage.WelcomeCity").Replace("%s", _cityConfigurationSystem.cityName)
        };
    }

    public static SS_VehicleTvData GetVehicleIncomingInformation(Entity building, Dictionary<string, string> vars)
    {
        var platform = PlatformFormulas.GetPlatform(building, vars);
        return SS_IncomingVehicleSystem.Instance.GetTvInformation(platform);
    }

    public static string GetVehicleIncomingMessage(Entity building, Dictionary<string, string> vars)
    {
        var info = GetVehicleIncomingInformation(building, vars);
        if (info.subtitle == SS_IncomingVehicleSystem.VehicleStatusDescription.DistanceToStation)
        {
            if ((GameManager.instance.settings.userInterface.unitSystem == InterfaceSettings.UnitSystem.Freedom))
            {
                var valueMiles = info.distanceHm / 16.0934f; // Convert meters to miles
                return valueMiles < 1
                    ? $"{(math.ceil(valueMiles * 52.80f) * 100).ToString("#,##0", WELocalizationBridge.GetWeCultureInfo())}ft"
                    : $"{valueMiles.ToString("#,##0.0", WELocalizationBridge.GetWeCultureInfo())}mi";
            }
            else
            {
                var valueKm = info.distanceHm / 10f; // Convert meters to kilometers
                return valueKm < 1
                    ? $"{(math.ceil(valueKm * 10) * 100).ToString("#,##0", WELocalizationBridge.GetWeCultureInfo())}m"
                    : $"{valueKm.ToString("#,##0.0", WELocalizationBridge.GetWeCultureInfo())}km";
            }
        }
        if (info.subtitle == SS_IncomingVehicleSystem.VehicleStatusDescription.AverageWaitTime)
        {
            var timeSecs = info.distanceHm;
            if (timeSecs < 60)
            {
                return $"{timeSecs}s";
            }
            else if (timeSecs < 3600)
            {
                var minutes = timeSecs / 60;
                return $"{minutes}min{timeSecs % 60:00}s";
            }
            else
            {
                var hours = timeSecs / 3600;
                var minutes = timeSecs / 60 % 60;
                return $"{hours}h{minutes:00}min";
            }
        }
        return "";
    }

    public static Color GetIncomingVehicleCapacityColor(SS_VehicleTvData data, Dictionary<string, string> vars)
        => !vars.TryGetValue("$idx", out var idxStr) || !int.TryParse(idxStr, out var idx) || idx < 0 || idx >= 8 ? Color.white
            : data[idx] switch
            {
                <= 64 => Color.green,
                <= 192 => Color.yellow,
                _ => Color.red,
            };
    public static float3 GetIncomingVehicleCapacityScale(SS_VehicleTvData data, Dictionary<string, string> vars)
        => !vars.TryGetValue("$idx", out var idxStr) || !int.TryParse(idxStr, out var idx) || idx < 0 || idx >= 8 ? new float3(1, 1, 1)
            : new float3(1, data[idx] / 255f, 1);
    public static string GetIncomingVehicleImageName(Entity _, Dictionary<string, string> vars)
        => !vars.TryGetValue("$idx", out var idxStr) || !int.TryParse(idxStr, out var idx) ? "CapacityCar"
        : idx == 0 ? "CapacityStartEngine"
        : idx == 7 ? "CapacityEndEngine"
        : "CapacityCar";


    public static string GetName(Entity _, Dictionary<string, string> vars)
        => vars.TryGetValue("strKey", out var id) && GameManager.instance.localizationManager.activeDictionary.TryGetValue($"StationSignage.{id}", out var name) ? name : "";

    private static string GetName(string id)
        => GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
    public static string GetNameFromMod(string id)
        => GameManager.instance.localizationManager.activeDictionary.TryGetValue($"StationSignage.{id}", out var name) ? name : "";

    public static string GetOperatorImageIcon(ServiceOperator serviceOperator) => "SquareLogo" + (serviceOperator == ServiceOperator.Default ? "" : serviceOperator.ToString());
    public static string GetOperatorImageWide(ServiceOperator serviceOperator) => "WideSideLogo" + (serviceOperator == ServiceOperator.Default ? "" : serviceOperator.ToString());

    public static string GetLineStatusText(LineOperationStatus lineStatus) => GetName($"StationSignage.{lineStatus}");

    public static Color GetLineStatusColor(LineOperationStatus lineStatus) => lineStatus switch
    {
        LineOperationStatus.OperationStopped or LineOperationStatus.NotOperating => Color.red,
        LineOperationStatus.ReducedSpeed or LineOperationStatus.NoUsage => Color.yellow,
        _ => Color.green
    };
}