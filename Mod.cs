using BridgeWE;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using StationSignage.BridgeWE;
using StationSignage.Formulas;
using StationSignage.Systems;
using StationSignage.WEBridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using static StationSignage.Settings;

namespace StationSignage
{
    public class Mod : IMod
    {

        public static ILog log = LogManager.GetLogger($"{nameof(StationSignage)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static readonly BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.GetProperty;
        public static Settings m_Setting;
        private static Harmony m_harmony;

        public static Harmony HarmonyInstance => m_harmony ??= new Harmony($"rodrigmatrix.redirectors.{typeof(Mod).Assembly.GetName().Name}");

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");
            m_Setting = new Settings(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEn(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(StationSignage), m_Setting, new Settings(this));

            updateSystem.UpdateAt<SS_LineStatusSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<SS_RoutePathWatchSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAt<SS_WaypointConnectionsSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAfter<SS_VehiclePathWatchSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<SS_PlatformMappingSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAfter<SS_IncomingVehicleSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAfter<SS_ApplyRouteWatchSystem>(SystemUpdatePhase.ApplyTool);
            updateSystem.UpdateAfter<SS_BuildingLineCacheSystem>(SystemUpdatePhase.MainLoop);

            MainThreadDispatcher.RegisterUpdater(DoWhenLoaded);
            (AssetDatabase<ParadoxMods>.instance.dataSource as ParadoxModsDataSource).onAfterActivePlaysetOrModStatusChanged += DoWhenLoaded;
        }
        private bool isLoaded = false;
        private void DoWhenLoaded()
        {
            if (isLoaded) return;
            log.Info($"Loading patches");
            if (!DoPatches()) return;
            RegisterModFiles();
            isLoaded = true;
            (AssetDatabase<ParadoxMods>.instance.dataSource as ParadoxModsDataSource).onAfterActivePlaysetOrModStatusChanged -= DoWhenLoaded;
        }

        private void RegisterModFiles()
        {
            GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset);
            var modDir = Path.GetDirectoryName(asset.path);

            var imagesDirectory = Path.Combine(modDir, "atlases");
            if (Directory.Exists(imagesDirectory))
            {
                var atlases = Directory.GetDirectories(imagesDirectory, "*", SearchOption.TopDirectoryOnly);
                foreach (var atlasFolder in atlases)
                {
                    WEImageManagementBridge.RegisterImageAtlas(typeof(Mod).Assembly, Path.GetFileName(atlasFolder), Directory.GetFiles(atlasFolder, "*.png"));
                }
            }

            var layoutsDirectory = Path.Combine(modDir, "layouts");
            WETemplatesManagementBridge.RegisterCustomTemplates(typeof(Mod).Assembly, layoutsDirectory);
            WETemplatesManagementBridge.RegisterLoadableTemplatesFolder(typeof(Mod).Assembly, layoutsDirectory);


            var fontsDirectory = Path.Combine(modDir, "fonts");
            WEFontManagementBridge.RegisterModFonts(typeof(Mod).Assembly, fontsDirectory);

            var dataSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_SettingSystem>();
            WEModuleOptionsBridge.CreateBuilder(typeof(Mod).Assembly, "StationSignage.weOptions")
                .Dropdown("LineIndicatorShape",
                    () => dataSystem.LineIndicatorShape.ToString(),
                    x =>
                    {
                        log.DebugFormat("Setting LineIndicatorShape to {0}", x);
                        dataSystem.LineIndicatorShape = Enum.TryParse<LineIndicatorShapeOptions>(x, out var result) ? result : LineIndicatorShapeOptions.Square;
                    },
                    () => Enum.GetNames(typeof(LineIndicatorShapeOptions)).ToDictionary(x => x, x => $"StationSignage.weOptions[LineIndicatorShapeOptions.{x}]"))
                .Dropdown("LineOperatorCity",
                    () => dataSystem.LineOperatorCity.ToString(),
                    x =>
                    {
                        log.DebugFormat("Setting LineOperatorCity to {0}", x);
                        dataSystem.LineOperatorCity = Enum.TryParse<LineOperatorCityOptions>(x, out var result) ? result : LineOperatorCityOptions.Generic;
                    },
                    () => Enum.GetNames(typeof(LineOperatorCityOptions)).ToDictionary(x => x, x => $"StationSignage.weOptions[LineOperatorCityOptions.{x}]"))
                .Dropdown("LineDisplayName",
                    () => dataSystem.LineDisplayName.ToString(),
                    x =>
                    {
                        log.DebugFormat("Setting LineDisplayName to {0}", x);
                        dataSystem.LineDisplayName = Enum.TryParse<LineDisplayNameOptions>(x, out var result) ? result : LineDisplayNameOptions.Generated;
                    },
                    () => Enum.GetNames(typeof(LineDisplayNameOptions)).ToDictionary(x => x, x => $"StationSignage.weOptions[LineDisplayNameOptions.{x}]"))
                .Register();
        }

        private bool DoPatches()
        {
            try
            {
                if (AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "BelzontWE") is Assembly weAssembly)
                {
                    var exportedTypes = weAssembly.ExportedTypes;
                    foreach (var (type, sourceClassName) in new List<(Type, string)>() {
                    (typeof(WEFontManagementBridge), "FontManagementBridge"),
                    (typeof(WEImageManagementBridge), "ImageManagementBridge"),
                    (typeof(WETemplatesManagementBridge), "TemplatesManagementBridge"),
                    (typeof(WERouteFn), "WERouteFn"),
                    (typeof(WELocalizationBridge), "LocalizationBridge"),
                    (typeof(WEModuleOptionsBridge), "ModuleOptionsBridge")
                })
                    {
                        var targetType = exportedTypes.First(x => x.Name == sourceClassName);
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            var srcMethod = targetType.GetMethod(method.Name, allFlags, null, method.GetParameters().Select(x => x.ParameterType).ToArray(), null);
                            if (srcMethod != null)
                            {
                                Harmony.ReversePatch(srcMethod, new HarmonyMethod(method));
                            }
                            else
                            {
                                log.Warn($"Method not found while patching WE: {targetType.FullName} {srcMethod.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.ParameterType}"))})");
                            }
                        }
                    }
                }
                else
                {
                    log.Warn("Write Everywhere dll file required for using this mod! Check if it's enabled.");
                    return false;
                }
            }
            catch
            {
                log.Warn("Write Everywhere dll file required for using this mod! Check if it's enabled.");
                return false;
            }
            return true;
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
