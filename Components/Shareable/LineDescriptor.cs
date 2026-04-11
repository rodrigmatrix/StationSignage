using Game.Prefabs;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace StationSignage.Components.Shareable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LineDescriptor : System.IEquatable<LineDescriptor>
    {
        public Entity Entity;
        public TransportType TransportType;
        public bool IsCargo;
        public bool IsPassenger;
        public FixedString32Bytes Acronym;
        public int Number;
        public UnityEngine.Color Color;
        public FixedString32Bytes SmallName;

        public LineDescriptor(
            Entity entity,
            TransportType transportType,
            bool isCargo,
            bool isPassenger,
            FixedString32Bytes acronym,
            int number,
            UnityEngine.Color color)
        {
            Entity = entity;
            TransportType = transportType;
            IsCargo = isCargo;
            IsPassenger = isPassenger;
            Acronym = acronym;
            Number = number;
            Color = color;
        }

        public bool Equals(LineDescriptor other)
        {
            return Entity.Equals(other.Entity);
        }

        public override bool Equals(object obj)
        {
            return obj is LineDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Entity.GetHashCode();
        }
    }
}
