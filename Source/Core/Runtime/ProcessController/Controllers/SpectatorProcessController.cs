﻿using System;
using System.Collections.Generic;
using VRBuilder.Core.Input;

namespace VRBuilder.UX
{
    /// <summary>
    /// Default process controller.
    /// </summary>
    public class SpectatorProcessController : BaseProcessController
    {
        /// <inheritdoc />
        public override string Name { get; } = "Spectator Camera";

        /// <inheritdoc />
        protected override string PrefabName { get; } = "SpectatorProcessController";

        /// <inheritdoc />
        public override int Priority { get; } = 64;

        /// <inheritdoc />
        public override List<Type> GetRequiredSetupComponents()
        {
            List<Type> requiredSetupComponents = base.GetRequiredSetupComponents();
#if UNITY_5_3_OR_NEWER
            requiredSetupComponents.Add(InputController.ConcreteType);
#elif GODOT
            requiredSetupComponents.Add(typeof(InputController)); //TODO: doesn't seem right
#endif
            requiredSetupComponents.Add(typeof(SpectatorController));
            return requiredSetupComponents;
        }
    }
}
