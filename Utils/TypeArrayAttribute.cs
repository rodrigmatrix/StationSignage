using System;

namespace StationSignage.Utils
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PatchGenericMethod : Attribute
    {
        public Type[] Types { get; }
        public string OriginalMethodName { get; }

        public PatchGenericMethod(params Type[] types)
        {
            Types = types;
        }
        public PatchGenericMethod(string originalMethodName, params Type[] types)
        {
            OriginalMethodName = originalMethodName;
            Types = types;
        }
    }
}
