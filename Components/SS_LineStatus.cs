using Game.Prefabs;
using Game.Routes;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_LineStatus : IComponentData
    {
        public TransportType type;
        public ServiceOperator operatorSP;
        public LineOperationStatus lineOperationStatus;
        public int expectedInterval;
        public int actualInterval;
        public bool isPassenger;
        public bool isCargo;
    }

    public enum LineOperationStatus
    {
        NormalOperation,
        NotOperating,
        OperationStopped,
        ReducedSpeed,
        NoUsage,
    }

}