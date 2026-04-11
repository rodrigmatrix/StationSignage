using Game.Prefabs;
using Game.Simulation;
using StationSignage.Components.Shareable;
using StationSignage.WE_TFMBridge;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace StationSignage.Formulas;

public static class LinesFormulas
{
    private static SimulationSystem _simulationSystem;
    private static SimulationSystem SimulationSystem => _simulationSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SimulationSystem>();

    public const string LINETYPE_VAR = "lineType";
    public const string CURRENT_INDEX_VAR = "$idx";
    public const string PLATFORM_VAR = "platform";
    public const string TRAIN_HALF_VAR = "trainHalf";
    public const string LINES_EACH_SCREEN_TVSTATUS = "tvStatusSz";

    public static List<Entity> GetCityLines(Entity buildingRef, Dictionary<string, string> vars)
    {
        if (vars.TryGetValue(LINES_EACH_SCREEN_TVSTATUS, out var statusLinesStr) && byte.TryParse(statusLinesStr, out var statusLinesPgSize))
        {
            TransportType transportType = TransportType.None;
            if (WE_TFMComponentGetterBridge.TryGetComponent(PlatformFormulas.GetPlatform(buildingRef, vars), out SS_PlatformData data))
            {
                transportType = data.type;
            }

            var cityLines = WE_TFMLineStatusBridge.GetCityLines(transportType, false, true);

            if (statusLinesPgSize == 0 || cityLines.Count < statusLinesPgSize) return cityLines;
            var totalPages = (int)math.ceil(cityLines.Count / statusLinesPgSize);
            var simulationPage = buildingRef.Index + (int)(SimulationSystem.frameIndex >> 8);
            return [.. cityLines
                .Skip(simulationPage % totalPages * statusLinesPgSize)
                .Take(statusLinesPgSize)];
        }
        return [];
    }

    public static Entity GetCityLineAtIdx(Entity buildingRef, Dictionary<string, string> vars)
    {
        if (vars.TryGetValue(CURRENT_INDEX_VAR, out var idxStr) && byte.TryParse(idxStr, out var idx))
        {
            var cityLines = GetCityLines(buildingRef, vars);
            if (idx < cityLines.Count)
            {
                return cityLines[idx];
            }
        }
        return Entity.Null;
    }
}