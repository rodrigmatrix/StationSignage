using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_LineStopData : IComponentData
    {
        public Entity lineEntity;
        
    }
    public struct SS_LineStopForwardConnection : IBufferElementData
    {
        public Entity lineEntity;
        
    }
}
