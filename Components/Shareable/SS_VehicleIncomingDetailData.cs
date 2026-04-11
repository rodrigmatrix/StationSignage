using StationSignage.Enums;
using System.Runtime.InteropServices;

namespace StationSignage.Components.Shareable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SS_VehicleIncomingDetailData
    {
        public VehicleStatusDescription title;
        public VehicleStatusDescription subtitle;
        public ushort distanceHm;// * 100m or sec waiting
        public byte totalCars;
        public byte occupancyLevels0;
        public byte occupancyLevels1;
        public byte occupancyLevels2;
        public byte occupancyLevels3;
        public byte occupancyLevels4;
        public byte occupancyLevels5;
        public byte occupancyLevels6;
        public byte occupancyLevels7;
        public uint cacheFrame;

        public byte this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => occupancyLevels0,
                    1 => occupancyLevels1,
                    2 => occupancyLevels2,
                    3 => occupancyLevels3,
                    4 => occupancyLevels4,
                    5 => occupancyLevels5,
                    6 => occupancyLevels6,
                    7 => occupancyLevels7,
                    _ => throw new System.IndexOutOfRangeException("Index out of range for occupancy levels.")
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        occupancyLevels0 = value;
                        break;
                    case 1:
                        occupancyLevels1 = value;
                        break;
                    case 2:
                        occupancyLevels2 = value;
                        break;
                    case 3:
                        occupancyLevels3 = value;
                        break;
                    case 4:
                        occupancyLevels4 = value;
                        break;
                    case 5:
                        occupancyLevels5 = value;
                        break;
                    case 6:
                        occupancyLevels6 = value;
                        break;
                    case 7:
                        occupancyLevels7 = value;
                        break;
                    default:
                        throw new System.IndexOutOfRangeException("Index out of range for occupancy levels.");
                }
            }
        }
    }

}
