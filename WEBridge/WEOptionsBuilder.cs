using System;
using System.Collections.Generic;
using System.Reflection;

namespace BridgeWE
{
    /// <summary>
    /// Fluent builder for constructing WE module options.
    /// </summary>
    internal class WEOptionsBuilder
    {
        private readonly Assembly _assembly;
        private readonly string _localePrefix;
        private readonly Dictionary<string, (int, object)> _options = new Dictionary<string, (int, object)>();

        internal WEOptionsBuilder(Assembly assembly, string localePrefix)
        {
            _assembly = assembly;
            _localePrefix = localePrefix;
        }

        /// <summary>
        /// Adds a section title to the options.
        /// </summary>
        /// <param name="sectionName">The name of the section (will be wrapped in locale prefix)</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder Section(string sectionName)
        {
            _options[$"{_localePrefix}[{sectionName}]"] = WEModuleOptionsBridge.AsSectionTitleOptionData();
            return this;
        }

        /// <summary>
        /// Adds a boolean option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current value</param>
        /// <param name="setter">Action to set the value</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder Boolean(string optionName, Func<bool> getter, Action<bool> setter)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsBooleanOptionData(getter, setter);
            return this;
        }

        /// <summary>
        /// Adds a dropdown option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current value as string</param>
        /// <param name="setter">Action to set the value from string</param>
        /// <param name="optionsProvider">Function that provides the available options (value -> display locale key)</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder Dropdown(string optionName, Func<string> getter, Action<string> setter, Func<Dictionary<string, string>> optionsProvider)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsDropdownOptionData(getter, setter, optionsProvider);
            return this;
        }

        /// <summary>
        /// Adds a button row option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="handler">Action to handle button clicks (receives button key)</param>
        /// <param name="buttonsProvider">Function that provides the available buttons (key -> display locale key)</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder ButtonRow(string optionName, Action<string> handler, Func<Dictionary<string, string>> buttonsProvider)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsButtonRowOptionData(handler, buttonsProvider);
            return this;
        }

        /// <summary>
        /// Adds a slider option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current value</param>
        /// <param name="setter">Action to set the value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder Slider(string optionName, Func<float> getter, Action<float> setter, float min = float.MinValue, float max = float.MaxValue)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsSliderOptionData(getter, setter, min, max);
            return this;
        }

        /// <summary>
        /// Adds a file picker option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current path</param>
        /// <param name="setter">Action to set the path</param>
        /// <param name="promptText">Prompt text for the file picker</param>
        /// <param name="initialPath">Initial path to show</param>
        /// <param name="fileExtension">File extension filter</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder FilePicker(string optionName, Func<string> getter, Action<string> setter, string promptText, string initialPath, string fileExtension = "*")
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsFilePickerOptionData(getter, setter, promptText, initialPath, fileExtension);
            return this;
        }

        /// <summary>
        /// Adds a color picker option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current color</param>
        /// <param name="setter">Action to set the color</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder ColorPicker(string optionName, Func<UnityEngine.Color> getter, Action<UnityEngine.Color> setter)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsColorPickerOptionData(getter, setter);
            return this;
        }

        /// <summary>
        /// Adds a spacer to the options.
        /// </summary>
        /// <param name="optionName">The name of the spacer (will be wrapped in locale prefix)</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder Spacer(string optionName)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsSpacerOptionData();
            return this;
        }

        /// <summary>
        /// Adds a text input option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current text</param>
        /// <param name="setter">Action to set the text</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder TextInput(string optionName, Func<string> getter, Action<string> setter)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsTextInputOptionData(getter, setter);
            return this;
        }

        /// <summary>
        /// Adds a multi-line text input option.
        /// </summary>
        /// <param name="optionName">The name of the option (will be wrapped in locale prefix)</param>
        /// <param name="getter">Function to get the current text</param>
        /// <param name="setter">Action to set the text</param>
        /// <returns>This builder instance for chaining</returns>
        public WEOptionsBuilder MultiLineTextInput(string optionName, Func<string> getter, Action<string> setter)
        {
            _options[$"{_localePrefix}[{optionName}]"] = WEModuleOptionsBridge.AsMultiLineTextInputOptionData(getter, setter);
            return this;
        }

        /// <summary>
        /// Registers all options that have been added to this builder.
        /// </summary>
        /// <returns>True if registration was successful</returns>
        public bool Register()
        {
            return WEModuleOptionsBridge.RegisterOptions(_assembly, _options);
        }
    }
}
