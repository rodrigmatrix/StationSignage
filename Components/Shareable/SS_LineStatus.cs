using Game.Prefabs;
using Game.Routes;
using StationSignage.Enums;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace StationSignage.Components.Shareable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SS_LineStatus 
    {
        public TransportType type;
        public LineOperationStatus lineOperationStatus;
        public int expectedInterval;
        public int actualInterval;
        public bool isPassenger;
        public bool isCargo;
    }

}