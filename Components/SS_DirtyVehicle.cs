using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_DirtyVehicle : IComponentData
    {
        public Entity oldTarget;
    }
}
