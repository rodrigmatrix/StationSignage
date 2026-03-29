using Colossal.Logging;
using Game;
using Game.Common;
using Game.Serialization;
using Game.Tools;
using System;
using System.Linq;

namespace StationSignage.Systems
{
    public interface IBasicSystem { }

    public abstract partial class SS_BasicSystem : GameSystemBase, IBasicSystem
    {
        public static ILog log = LogManager.GetLogger($"{nameof(StationSignage)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public enum AllowedPhase
        {
            Modification1 = SystemUpdatePhase.Modification1,
            Modification2 = SystemUpdatePhase.Modification2,
            Modification2B = SystemUpdatePhase.Modification2B,
            Modification3 = SystemUpdatePhase.Modification3,
            Modification4 = SystemUpdatePhase.Modification4,
            Modification4B = SystemUpdatePhase.Modification4B,
            Modification5 = SystemUpdatePhase.Modification5,
            ModificationEnd = SystemUpdatePhase.ModificationEnd,
            ToolUpdate = SystemUpdatePhase.ToolUpdate,
            PostTool = SystemUpdatePhase.PostTool,
            Deserialize = SystemUpdatePhase.Deserialize,
            EndFrame = SystemUpdatePhase.MainLoop
        }

        protected SafeCommandBufferSystem Barrier { get; private set; }
        protected abstract AllowedPhase UpdatePhase { get; }

        protected abstract void OnCreateWithBarrier();
        protected sealed override void OnCreate()
        {
            base.OnCreate();
            RegisterSystem();
            OnCreateWithBarrier();
        }

        private void RegisterSystem()
        {
            var updateSystem = World.GetOrCreateSystemManaged<UpdateSystem>();

            if (UpdatePhase == AllowedPhase.EndFrame)
            {

                var UpdateAfter = typeof(UpdateSystem).GetMethods().First(x => x.Name == "UpdateAfter" && x.GetGenericArguments().Length == 1).MakeGenericMethod(GetType());
                UpdateAfter.Invoke(updateSystem, [SystemUpdatePhase.MainLoop]);
                Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
                log.Info($"Registered system {GetType()} at after MainLoop");
                return;
            }
            var UpdateAt = typeof(UpdateSystem).GetMethod("UpdateAt").MakeGenericMethod(GetType());
            var updatePhaseMapping = UpdatePhase switch
            {
                (AllowedPhase)SystemUpdatePhase.Modification1 => typeof(ModificationBarrier1),
                (AllowedPhase)SystemUpdatePhase.Modification2 => typeof(ModificationBarrier2),
                (AllowedPhase)SystemUpdatePhase.Modification2B => typeof(ModificationBarrier2B),
                (AllowedPhase)SystemUpdatePhase.Modification3 => typeof(ModificationBarrier3),
                (AllowedPhase)SystemUpdatePhase.Modification4 => typeof(ModificationBarrier4),
                (AllowedPhase)SystemUpdatePhase.Modification4B => typeof(ModificationBarrier4B),
                (AllowedPhase)SystemUpdatePhase.Modification5 => typeof(ModificationBarrier5),
                (AllowedPhase)SystemUpdatePhase.ModificationEnd => typeof(ModificationEndBarrier),
                (AllowedPhase)SystemUpdatePhase.ToolUpdate => typeof(ToolOutputBarrier),
                (AllowedPhase)SystemUpdatePhase.PostTool => typeof(ToolReadyBarrier),
                (AllowedPhase)SystemUpdatePhase.Deserialize => typeof(DeserializationBarrier),
                _ => throw new Exception($"Unsupported barrier phase {(SystemUpdatePhase)UpdatePhase}")
            };
            UpdateAt.Invoke(updateSystem, [(SystemUpdatePhase)UpdatePhase]);
            Barrier = World.GetOrCreateSystemManaged(updatePhaseMapping) as SafeCommandBufferSystem;
            log.Info($"Registered system {GetType()} at {UpdatePhase}");
        }

    }
}

