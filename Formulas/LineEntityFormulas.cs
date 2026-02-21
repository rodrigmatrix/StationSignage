using Colossal.Entities;
using Game.Routes;
using Game.UI;
using StationSignage.BridgeWE;
using StationSignage.Components;
using StationSignage.Systems;
using StationSignage.Utils;
using Unity.Entities;

namespace StationSignage.Formulas
{
    public static class LineEntityFormulas
    {
        private static SS_LineStatusSystem _linesSystem;
        private static NameSystem _nameSystem;
        public static SS_LineStatus GetLineStatus(Entity line)
        {
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
            _linesSystem.EntityManager.TryGetComponent(line, out SS_LineStatus status);
            return status;
        }
        public static UnityEngine.Color GetLineColor(Entity line)
        {
            if (line == Entity.Null)
            {
                return UnityEngine.Color.white;
            }
            _linesSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_LineStatusSystem>();
            _linesSystem.EntityManager.TryGetComponent(line, out Game.Routes.Color status);
            return status.m_Color;
        }

        public static string GetLineAcronym(Entity line)
        {
            if (line == Entity.Null)
            {
                return "?";
            }
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return (SS_SettingSystem.Instance.LineDisplayName) switch
            {
                Settings.LineDisplayNameOptions.Custom => GetSmallLineName(_nameSystem.GetName(line).Translate(), line),
                Settings.LineDisplayNameOptions.WriteEverywhere => WERouteFn.GetTransportLineNumber(line),
                Settings.LineDisplayNameOptions.Generated => _nameSystem.EntityManager.TryGetComponent(line, out RouteNumber routeNumber) ? routeNumber.m_Number.ToString() : "?",
                _ => "???",
            };
        }
        public static string GetLineName(Entity line)
        {
            if (line == Entity.Null)
            {
                return "";
            }
            _nameSystem ??= World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NameSystem>();
            return _nameSystem.GetName(line).Translate();
        }
        private static string GetSmallLineName(string fullLineName, Entity entity)
            => fullLineName is { Length: >= 1 and <= 3 } ? fullLineName : _nameSystem.EntityManager.TryGetComponent(entity, out RouteNumber routeNumber) ? routeNumber.m_Number.ToString() : "??";
    }
}
