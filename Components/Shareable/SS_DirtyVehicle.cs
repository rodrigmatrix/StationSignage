using System.Runtime.InteropServices;
using Unity.Entities;

namespace StationSignage.Components.Shareable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SS_DirtyVehicle 
    {
        public Entity oldTarget;
    }
}
