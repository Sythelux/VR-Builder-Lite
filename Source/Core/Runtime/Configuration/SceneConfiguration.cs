#if UNITY_5_3_OR_NEWER
using UnityEngine;
#elif GODOT
using Godot;
using VRBuilder.Core.Godot.Attributes;
using Godot.Collections;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Handles configuration specific to this scene.
    /// </summary>
#if UNITY_5_3_OR_NEWER
    public class SceneConfiguration : MonoBehaviour, ISceneConfiguration
#elif GODOT
    [Tool, GlobalClass, Icon("res://addons/TinkerFlow/TinkerFlow/Core/Resources/TinkerFlow.svg")]
    public partial class SceneConfiguration : Node, ISceneConfiguration
#endif
    {
        [Tooltip("Lists all assemblies whose property extensions will be used in the current scene.")]
#if UNITY_5_3_OR_NEWER
        [SerializeField]
        private List<string> extensionAssembliesWhitelist = new List<string>();
#elif GODOT
        [Export]
        private Array<string> extensionAssembliesWhitelist = new Array<string>();
#endif

#if UNITY_5_3_OR_NEWER
        [SerializeField]
#elif GODOT
        [Export]
#endif
        [Tooltip("Default resources prefab to use for the Confetti behavior.")]
        private string defaultConfettiPrefab;

        /// <inheritdoc/>
        public IEnumerable<string> ExtensionAssembliesWhitelist => extensionAssembliesWhitelist;

        /// <inheritdoc/>
        public string DefaultConfettiPrefab
        {
            get { return defaultConfettiPrefab; }
            set { defaultConfettiPrefab = value; }
        }

        /// <inheritdoc/>
        public bool IsAllowedInAssembly(Type extensionType, string assemblyName)
        {
            if (ExtensionAssembliesWhitelist.Contains(assemblyName) == false)
            {
                return false;
            }

            PropertyExtensionExclusionList blacklist = this.GetComponents<PropertyExtensionExclusionList>()
                .FirstOrDefault(blacklist => blacklist.AssemblyFullName == assemblyName);

            if (blacklist == null)
            {
                return true;
            }
            else
            {
                return blacklist.DisallowedExtensionTypes.Any(disallowedType =>
                    disallowedType.FullName == extensionType.FullName) == false;
            }
        }

        /// <summary>
        /// Adds the specified assembly names to the extension whitelist.
        /// </summary>
        public void AddWhitelistAssemblies(IEnumerable<string> assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                if (extensionAssembliesWhitelist.Contains(assemblyName) == false)
                {
                    extensionAssembliesWhitelist.Add(assemblyName);
                }
            }
        }
    }
}
