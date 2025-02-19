#if UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEngine.Scripting;
#elif GODOT
using Godot;
#endif
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Behavior that waits for `DelayTime` seconds before finishing its activation.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/delay")]
    public partial class DelayBehavior : Behavior<DelayBehavior.EntityData>
    {
        /// <summary>
        /// The data class for a delay behavior.
        /// </summary>
        [DisplayName("Delay")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            [DataMember]
            [DisplayName("Delay (in seconds)")]
            public float DelayTime { get; set; }

            public Metadata Metadata { get; set; }

            [IgnoreDataMember]
            public string Name => $"Wait for {DelayTime} seconds";
        }

        [JsonConstructor, Preserve]
        public DelayBehavior() : this(0)
        {
        }

        public DelayBehavior(float delayTime)
        {
            if (delayTime < 0f)
            {
#if UNITY_6000_0_OR_NEWER
                Debug.LogWarningFormat("DelayTime has to be zero or positive, but it was {0}. Setting to 0 instead.", delayTime);
#elif GODOT
                GD.PushWarning($"DelayTime has to be zero or positive, but it was {delayTime}. Setting to 0 instead.");
#endif
                delayTime = 0f;
            }

            Data.DelayTime = delayTime;
        }

        private class ActivatingProcess : StageProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
            }

            /// <inheritdoc />
            public override IEnumerator Update()
            {
#if UNITY_6000_0_OR_NEWER
                float timeStarted = Time.time;

                while (Time.time - timeStarted < Data.DelayTime)
                {
                    yield return null;
                }
#elif GODOT
                ulong timeStarted = Time.GetTicksMsec();

                while (Time.GetTicksMsec() - timeStarted < Data.DelayTime)
                {
                    yield return null;
                }
#endif
            }

            /// <inheritdoc />
            public override void End()
            {
            }

            /// <inheritdoc />
            public override void FastForward()
            {
            }
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }
    }
}
