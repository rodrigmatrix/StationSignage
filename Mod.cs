using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using HarmonyLib;
using StationSignage.Utils;
using StationSignage.WEBridge;

namespace StationSignage
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(StationSignage)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private static readonly BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.GetProperty;
        public static Settings m_Setting;
        
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");
            m_Setting = new Settings(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEn(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(StationSignage), m_Setting, new Settings(this));
            
            updateSystem.UpdateAt<LinesSystem>(SystemUpdatePhase.GameSimulation);

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
        }

        private bool DoPatches()
        {
            var weAsset = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(asset => asset.isLoaded && asset.name.Equals("BelzontWE")));
            if (weAsset?.assembly is null)
            {
                log.Error($"The module {GetType().Assembly.GetName().Name} requires Write Everywhere mod to work!");
                return false;
            }

            var exportedTypes = weAsset.assembly.ExportedTypes;
            foreach (var (type, sourceClassName) in new List<(Type, string)>() {
                    (typeof(WEFontManagementBridge), "FontManagementBridge"),
                    (typeof(WEImageManagementBridge), "ImageManagementBridge"),
                    (typeof(WETemplatesManagementBridge), "TemplatesManagementBridge"),
                })
            {
                var targetType = exportedTypes.First(x => x.Name == sourceClassName);
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var srcMethod = targetType.GetMethod(method.Name, allFlags, null, method.GetParameters().Select(x => x.ParameterType).ToArray(), null);
                    if (srcMethod != null) Harmony.ReversePatch(srcMethod, new HarmonyMethod(method));
                    else log.Warn($"Method not found while patching WE: {targetType.FullName} {srcMethod.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.ParameterType}"))})");
                }
            }
            return true;
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
