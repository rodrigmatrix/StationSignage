using Game.Prefabs;
using System;

namespace StationSignage.Enums
{
    public enum TransportTypeByImportance : byte
    {
        MostPrioritary = 0x00,
        Airplane = 0x80,
        Ship = 0x90,
        Ferry = 0x97,
        Train = 0xA0,
        Subway = 0xA8,
        Tram = 0xC0,
        Bus = 0xF0,
        LessPrioritary = 0xFF
    }

    public static class TransportTypeByImportanceExtensions
    {

        public static TransportTypeByImportance ToImportance(this TransportType tt)
        {
            return tt switch
            {
                TransportType.Airplane => TransportTypeByImportance.Airplane,
                TransportType.Ship => TransportTypeByImportance.Ship,
                //TransportType.Ferry => TransportTypeByImportance.Ferry,
                TransportType.Train => TransportTypeByImportance.Train,
                TransportType.Subway => TransportTypeByImportance.Subway,
                TransportType.Tram => TransportTypeByImportance.Tram,
                TransportType.Bus => TransportTypeByImportance.Bus,
                _ => throw new ArgumentOutOfRangeException(nameof(tt), tt, null)
            };
        }
    }
}
