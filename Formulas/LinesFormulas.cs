using Colossal.Entities;
using Game.Prefabs;
using StationSignage.Components;
using StationSignage.Systems;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace StationSignage.Formulas;

public static class LinesFormulas
{

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
            if (SS_LineStatusSystem.Instance.EntityManager.TryGetComponent<SS_PlatformData>(PlatformFormulas.GetPlatform(buildingRef, vars), out var data))
            {
                transportType = data.type;
            }

            var cityLines = SS_LineStatusSystem.Instance.GetCityLines(transportType, false, true);

            if (statusLinesPgSize == 0 || cityLines.Count < statusLinesPgSize) return cityLines;
            var totalPages = (int)math.ceil(cityLines.Count / statusLinesPgSize);
            var simulationPage = buildingRef.Index + (int)(SS_LineStatusSystem.Instance.GameSimulationFrame >> 8);
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