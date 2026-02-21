using System;
using System.Collections.Generic;
using System.Reflection;

namespace BridgeWE
{
    internal static class WEModuleOptionsBridge
    {
        /// <summary>
        /// Register options to be used in the mod options panel for a WE Module.
        /// </summary>
        /// <param name="mainAssembly">The assembly of the module</param>
        /// <param name="options">
        /// A dictionary of options to register. The key is the option locale id. The value is a tuple of 2 items, first being the option type, second being the actions for the field, corresponding to the table below:
        /// <list type="table" >
        /// <item><term>0</term><description>Boolean field</description>
        /// <para>object Type: <c>(Func&lt;bool&gt; getter, Action&lt;bool&gt; setter)</c></para>
        /// </item>
        /// <item><term>1</term><description>Dropdown field</description>
        /// <para>object Type: <c>(Func&lt;string&gt; getter, Action&lt;string&gt; setter, Action&lt;Dictionary&lt;string, string&gt;&gt; listOptions_value_displayI18n)</c></para>
        /// </item>
        /// </list>
        /// </param>
        public static bool RegisterOptions(Assembly mainAssembly, Dictionary<string, (int, object)> options) => throw new NotImplementedException("Stub only!");

        public static (int, object) AsBooleanOptionData(Func<bool> getter, Action<bool> setter) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsDropdownOptionData(Func<string> getter, Action<string> setter, Func<Dictionary<string, string>> optionsLister_value_displayNameI18n) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsSectionTitleOptionData() => throw new NotImplementedException("Stub only!");
        public static (int, object) AsButtonRowOptionData(Action<string> handler, Func<Dictionary<string, string>> optionsLister_value_displayNameI18n) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsSliderOptionData(Func<float> getter, Action<float> setter, float min = float.MinValue, float max = float.MaxValue) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsFilePickerOptionData(Func<string> getter, Action<string> setter, string promptText, string initialPath, string fileExtension = "*") => throw new NotImplementedException("Stub only!");
        public static (int, object) AsColorPickerOptionData(Func<UnityEngine.Color> getter, Action<UnityEngine.Color> setter) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsSpacerOptionData() => throw new NotImplementedException("Stub only!");
        public static (int, object) AsTextInputOptionData(Func<string> getter, Action<string> setter) => throw new NotImplementedException("Stub only!");
        public static (int, object) AsMultiLineTextInputOptionData(Func<string> getter, Action<string> setter) => throw new NotImplementedException("Stub only!");
        public static void ForceReloadOptions() => throw new NotImplementedException("Stub only!");

        /// <summary>
        /// Creates a fluent builder for registering options.
        /// </summary>
        /// <param name="assembly">The assembly of the module</param>
        /// <param name="localePrefix">The prefix for locale keys (e.g., "K45::we_ptvm.weOptions")</param>
        /// <returns>A new WEOptionsBuilder instance</returns>
        internal static WEOptionsBuilder CreateBuilder(Assembly assembly, string localePrefix)
            => new WEOptionsBuilder(assembly, localePrefix);
    }
}
