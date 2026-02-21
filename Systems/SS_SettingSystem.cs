using Colossal.Serialization.Entities;
using Unity.Entities;
using static StationSignage.Settings;

namespace StationSignage.Systems
{
    public partial class SS_SettingSystem : SystemBase, IDefaultSerializable
    {
        private const uint CURRENT_VERSION = 0;

        public static SS_SettingSystem Instance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
        }

        public LineIndicatorShapeOptions LineIndicatorShape { get; set; } = LineIndicatorShapeOptions.Square;
        public LineOperatorCityOptions LineOperatorCity { get; set; } = LineOperatorCityOptions.Generic;
        public LineDisplayNameOptions LineDisplayName { get; set; } = LineDisplayNameOptions.Custom;

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for {nameof(SS_SettingSystem)}");
            }
            reader.Read(out int lineIndicatorShape);
            LineIndicatorShape = (LineIndicatorShapeOptions)lineIndicatorShape;
            reader.Read(out int lineOperatorCity);
            LineOperatorCity = (LineOperatorCityOptions)lineOperatorCity;
            reader.Read(out int lineDisplayName);
            LineDisplayName = (LineDisplayNameOptions)lineDisplayName;

        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write((int)LineIndicatorShape);
            writer.Write((int)LineOperatorCity);
            writer.Write((int)LineDisplayName);
        }

        public void SetDefaults(Context context)
        {
            LineIndicatorShape = Mod.m_Setting.LineIndicatorShapeDropdown;
            LineOperatorCity = Mod.m_Setting.LineOperatorCityDropdown;
            LineDisplayName = Mod.m_Setting.LineDisplayNameDropdown;
        }

        protected override void OnUpdate()
        {            
        }
    }
}