using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;
using Unity.Entities;

namespace StationSignage;

[FileLocation("ModsSettings/" + nameof(StationSignage) + "/" + nameof(StationSignage))]
[SettingsUIGroupOrder(MainTab, DeveloperTab)]
[SettingsUIShowGroupName(MainTab, DeveloperTab)]
public class Settings(IMod mod) : ModSetting(mod)
{
    public const string MainTab = "Main";

    public const string DeveloperTab = "Developer";

    private const string GeneralSettingsGroup = "GeneralSettings";

    private const string DeveloperSettingsGroup = "DeveloperSettings";

    [SettingsUISection(MainTab, GeneralSettingsGroup)]
    public LineIndicatorShapeOptions LineIndicatorShapeDropdown { get; set; } = LineIndicatorShapeOptions.Square;

    [SettingsUISection(MainTab, GeneralSettingsGroup)]
    public LineOperatorCityOptions LineOperatorCityDropdown { get; set; } = LineOperatorCityOptions.Generic;

    [SettingsUISection(MainTab, GeneralSettingsGroup)]
    public LineDisplayNameOptions LineDisplayNameDropdown { get; set; } = LineDisplayNameOptions.Custom;

    [SettingsUISection(DeveloperTab, DeveloperSettingsGroup)]
    public bool EnableLayoutValidation { get; set; } = false;

    [SettingsUISection(DeveloperTab, DeveloperSettingsGroup)]
    public int SubwayLines { get; set; } = 6;

    [SettingsUISection(DeveloperTab, DeveloperSettingsGroup)]
    public int TrainLines { get; set; } = 6;

    [SettingsUISection(MainTab, GeneralSettingsGroup)]
    [SettingsUIButton]
    public bool ResetConnections
    {
        set
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SS_PlatformMappingSystem>().MarkToResetWaypointsDestinations();
        }
    }

    public override void SetDefaults()
    {

    }

    public enum LineIndicatorShapeOptions
    {
        Square,
        Circle,
    }

    public enum LineOperatorCityOptions
    {
        Generic,
        SaoPaulo,
        NewYork,
        London,
    }

    public enum LineDisplayNameOptions
    {
        Custom,
        WriteEverywhere,
        Generated,
    }
}

public class LocaleEn(Settings setting) : IDictionarySource
{
    public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
    {
        return new Dictionary<string, string>
        {
            { setting.GetSettingsLocaleID(), "Station Signage" },
            { setting.GetOptionTabLocaleID(Settings.MainTab), "Settings" },
            { setting.GetOptionTabLocaleID(Settings.DeveloperTab), "Developer Settings" },

            { setting.GetOptionLabelLocaleID(nameof(Settings.LineIndicatorShapeDropdown)), "Line Indicator Shape (New cities)" },
            { setting.GetOptionDescLocaleID(nameof(Settings.LineIndicatorShapeDropdown)), $"Choose what shape you want your lines to be displayed.\n\nThis is the default value when a city is created, after that the setting is stored per city and can be changed on Write Everywhere window, Prefab Templates Setup tab." },

            { setting.GetOptionLabelLocaleID(nameof(Settings.EnableLayoutValidation)), "Enable Layout Validation" },
            { setting.GetOptionDescLocaleID(nameof(Settings.EnableLayoutValidation)), $"Mock lines for every station to validate the layouts." },

            { setting.GetOptionLabelLocaleID(nameof(Settings.SubwayLines)), "Subway Lines" },
            { setting.GetOptionDescLocaleID(nameof(Settings.SubwayLines)), $"Number of subway lines to display." },

            { setting.GetOptionLabelLocaleID(nameof(Settings.TrainLines)), "Train Lines" },
            { setting.GetOptionDescLocaleID(nameof(Settings.TrainLines)), $"Number of train lines to display." },

            { setting.GetOptionLabelLocaleID(nameof(Settings.ResetConnections)), "Reset connections mapped" },
            { setting.GetOptionDescLocaleID(nameof(Settings.ResetConnections)), $"Use this if you have changed to use new mods or remove mods that may affect how connection lines are affected (like XTM)." },

            { setting.GetEnumValueLocaleID(Settings.LineIndicatorShapeOptions.Square), "Square Shape" },
            { setting.GetEnumValueLocaleID(Settings.LineIndicatorShapeOptions.Circle), "Circle Shape" },

            { setting.GetOptionLabelLocaleID(nameof(Settings.LineOperatorCityDropdown)), "Line Operators (New cities)" },
            { setting.GetOptionDescLocaleID(nameof(Settings.LineOperatorCityDropdown)), $"Choose what should be the line operators. If you choose generic, you can replace the operator with a custom atlas on Write Everywhere settings. If you choose a specific city, it will always have the same atlas and logic for that city.\n\nThis is the default value when a city is created, after that the setting is stored per city and can be changed on Write Everywhere window, Prefab Templates Setup tab." },

            { setting.GetEnumValueLocaleID(Settings.LineOperatorCityOptions.Generic), "Generic Operator" },
            { setting.GetEnumValueLocaleID(Settings.LineOperatorCityOptions.SaoPaulo), "Sao Paulo Operators" },
            { setting.GetEnumValueLocaleID(Settings.LineOperatorCityOptions.NewYork), "New York Operators" },
            { setting.GetEnumValueLocaleID(Settings.LineOperatorCityOptions.London), "London Operators" },

            { setting.GetOptionLabelLocaleID(nameof(Settings.LineDisplayNameDropdown)), "Line Display Name (New cities)" },
            { setting.GetOptionDescLocaleID(nameof(Settings.LineDisplayNameDropdown)), $"Allows you to change what should be displayed as the line name.\n\nThis is the default value when a city is created, after that the setting is stored per city and can be changed on Write Everywhere window, Prefab Templates Setup tab." },

            { setting.GetEnumValueLocaleID(Settings.LineDisplayNameOptions.Custom), "Custom (Based on the name of the line)" },
            { setting.GetEnumValueLocaleID(Settings.LineDisplayNameOptions.WriteEverywhere), "Write Everywhere (Based on the Write Everywhere name of the line)" },
            { setting.GetEnumValueLocaleID(Settings.LineDisplayNameOptions.Generated), "Generated (Based on the generated number of the line)" },
        };
    }

    public void Unload()
    {

    }
}