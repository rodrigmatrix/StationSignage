﻿using System.Collections.Generic;
using System.Linq;
using Game.City;
using Game.SceneFlow;
using StationSignage.Utils;
using Unity.Entities;

namespace StationSignage.Formulas;

public class DisplayFormulas
{
    private static CityConfigurationSystem _cityConfigurationSystem;
    private const string Square = "LineBgSquare";
    private const string Circle = "LineBgCircle";
    private const string ViaMobilidadeOperator = "Operator02";
    private const string ViaQuatroOperator = "Operator03";
    private const string CptmOperator = "Operator05";
    private const string MetroOperator = "Operator01";
    private const string LinhaUniOperator = "Operator04";
    private const string GenericSubwayOperator = "GenericSubwayOperator";
    private const string GenericTrainOperator = "GenericTrainOperator";
    private static readonly string[] ViaMobilidadeLines = ["4", "5", "8", "9", "15"];
    private static readonly string[] LinhaUniLines = ["6"];

    public static string GetLineBackgroundShape(Entity buildingRef)
    {
        return Mod.m_Setting.LineIndicatorShapeDropdown switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };
    }
    
    public static string GetShapeIcon(Entity buildingRef)
    {
        return Mod.m_Setting.LineIndicatorShapeDropdown switch
        {
            Settings.LineIndicatorShapeOptions.Square => Square,
            Settings.LineIndicatorShapeOptions.Circle => Circle,
            _ => Square
        };
    }
    
    public static string GetWelcomeMessage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("lineType", out var lineType);
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        return GetWelcomeMessage(lineType);
    }
    
    public static string GetImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("$idx", out var idxStr);
        int.TryParse(idxStr, out var idx);
        return GetImageList(buildingRef, vars).ElementAtOrDefault(idx);
    }
    
    public static string GetPlatformImage(Entity buildingRef, Dictionary<string, string> vars)
    {
        vars.TryGetValue("platform", out var platformStr);
        int.TryParse(platformStr, out var idx);
        return "Circle" + (idx + 1);
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
    
    public static string GetWelcomeMessage(string lineType)
    {
        _cityConfigurationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CityConfigurationSystem>();
        if (lineType == "Subway")
        {
            return GetName("StationSignage.WelcomeSubway").Replace("%s", _cityConfigurationSystem.cityName); 
        }

        return GetName("StationSignage.WelcomeTrain").Replace("%s", _cityConfigurationSystem.cityName);
    }
    
    private static string GetName(string id)
    {
        return GameManager.instance.localizationManager.activeDictionary.TryGetValue(id, out var name) ? name : "";
    }
    
    public static string GetSubwayOperator(string routeName)
    {
        return Mod.m_Setting.LineOperatorCityDropdown switch
        {
            Settings.LineOperatorCityOptions.Generic => GenericSubwayOperator,
            Settings.LineOperatorCityOptions.SaoPaulo => GetSaoPauloSubwayOperator(routeName),
            Settings.LineOperatorCityOptions.NewYork => GetNewYorkSubwayOperator(routeName),
            Settings.LineOperatorCityOptions.London => GetLondonSubwayOperator(routeName),
            _ => GenericSubwayOperator
        };
    }
    
    public static string GetTrainOperator(string routeName)
    {
        return Mod.m_Setting.LineOperatorCityDropdown switch
        {
            Settings.LineOperatorCityOptions.Generic => GenericTrainOperator,
            Settings.LineOperatorCityOptions.SaoPaulo => GetSaoPauloTrainOperator(routeName),
            _ => GenericTrainOperator
        };
    }

    private static string GetSaoPauloSubwayOperator(string lineOperator)
    {
        if (lineOperator == ViaQuatroOperator)
        {
            return ViaQuatroOperator;
        }
        if (ViaMobilidadeLines.Where(y => y == lineOperator).ToList().Count > 0)
        {
            return ViaMobilidadeOperator;
        }

        if (LinhaUniLines.Where(y => y == lineOperator).ToList().Count > 0)
        {
            return LinhaUniOperator;
        }

        return MetroOperator;
    }
    
    private static string GetSaoPauloTrainOperator(string lineOperator)
    {
        if (ViaMobilidadeLines.Where(y => y == lineOperator).ToList().Count > 0)
        {
            return ViaMobilidadeOperator;
        }

        return CptmOperator;
    }
    
    private static string GetNewYorkSubwayOperator(string lineOperator)
    {
        return "MTAOperator";
    }
    
    private static string GetLondonSubwayOperator(string lineOperator)
    {
        return "UndergroundOperator";
    }
}