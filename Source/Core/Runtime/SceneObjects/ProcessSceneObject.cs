/// Guid based Reference copyright © 2018 Unity Technologies ApS
/// Licensed under the Unity Companion License for Unity-dependent projects--see
/// Unity Companion License http://www.unity3d.com/legal/licenses/Unity_Companion_License.
/// Unless expressly provided otherwise, the Software under this license is made available strictly on an
/// “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.

/// Modifications copyright (c) 2021-2024 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#elif GODOT
using Godot;
using Godot.Collections;
using VRBuilder.Core.Godot.Attributes;
#endif

using VRBuilder.Core.Configuration;
using VRBuilder.Core.Exceptions;
using VRBuilder.Core.Properties;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Utils.Logging;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using VRBuilder.Unity;

#endif

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// This component gives a GameObject a stable, non-replicatable Globally Unique Identifier.
    /// It can be used to reference a specific instance of an object no matter where it is.
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class ProcessSceneObject : MonoBehaviour, ISerializationCallbackReceiver, ISceneObject
#elif GODOT
    [Tool, GlobalClass, Icon("res://addons/TinkerFlow/TinkerFlow/Core/Resources/TinkerFlow_Color.svg")]
    public partial class ProcessSceneObject : Node, ISceneObject //todo it is Node3D instead of Node, because in Unity it has "visible" check below
#endif
    {
        /// <summary>
        /// Unity's serialization system doesn't know about System.Guid, so we convert to a byte array
        /// Using strings allocates memory is was twice as slow
        /// </summary>
#if UNITY_5_3_OR_NEWER
        [SerializeField]
        private SerializableGuid serializedGuid;
#elif GODOT
        [Export]
        private Resource? serializedGuid;
#endif

        /// <summary>
        /// We use this Guid for comparison, generation and caching.
        /// </summary>
        /// <remarks>
        /// When the <see cref="serializedGuid"/> is modified by the Unity editor
        /// (e.g.: reverting a prefab) this will be used to revert it back canaling the changes of the editor.
        /// </remarks>
        protected Guid guid = Guid.Empty;

        /// <inheritdoc />
        public Guid Guid
        {
            get
            {
                if (!IsGuidAssigned())
                {
                    // if our serialized data is invalid, then we are a new object and need a new GUID
                    var serializableGuid = serializedGuid as SerializableGuid;
                    if (SerializableGuid.IsValid(serializableGuid))
                    {
                        guid = serializableGuid.Guid;
                    }
                    else
                    {
                        SetObjectId(Guid.NewGuid());
                    }
                }

                return guid;
            }
        }

#if UNITY_5_3_OR_NEWER
        [SerializeField]
        protected List<SerializableGuid> guids = new List<SerializableGuid>();
#elif GODOT
        [Export]
        protected Array<SerializableGuid> guids = new Array<SerializableGuid>();
#endif

        /// <inheritdoc />
        public IEnumerable<Guid> Guids => guids.Select(bytes => bytes.Guid);

        /// <inheritdoc />
#if UNITY_5_3_OR_NEWER
        public GameObject GameObject => gameObject;
#elif GODOT
        public Node GameObject => GetParent();
#endif

        /// <summary>
        /// Properties associated with this scene object.
        /// </summary>
#if UNITY_5_3_OR_NEWER
        public ICollection<ISceneObjectProperty> Properties
        {
            get { return GetComponents<ISceneObjectProperty>(); }
        }
#elif GODOT
        public IEnumerable<ISceneObjectProperty> Properties => GetChildren().OfType<ISceneObjectProperty>();
#endif


        private List<IStepData> unlockers = new List<IStepData>();

        /// <inheritdoc />
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Tracks state swishes prefab edit mode and the main scene.
        /// </summary>
        /// <remarks>
        /// Needs to be static to keep track of the state between different instances of ProcessSceneObject
        /// </remarks>
        private static bool hasDirtySceneObject = false;

#if UNITY_5_3_OR_NEWER
        public event EventHandler<LockStateChangedEventArgs> Locked;
        public event EventHandler<LockStateChangedEventArgs> Unlocked;
        public event EventHandler<GuidContainerEventArgs> GuidAdded;
        public event EventHandler<GuidContainerEventArgs> GuidRemoved;
        public event EventHandler<UniqueIdChangedEventArgs> ObjectIdChanged;
#elif GODOT
        [Signal]
        public delegate void LockedEventHandler(Node sender, LockStateChangedEventArgs eventArgs);

        [Signal]
        public delegate void UnlockedEventHandler(Node sender, LockStateChangedEventArgs eventArgs);

        [Signal]
        public delegate void GuidAddedEventHandler(Node sender, GuidContainerEventArgs eventArgs);

        [Signal]
        public delegate void GuidRemovedEventHandler(Node sender, GuidContainerEventArgs eventArgs);
        
        [Signal]
        public delegate void ObjectIdChangedEventHandler(Node sender, UniqueIdChangedEventArgs eventArgs);
#endif

        private bool IsRegistered => RuntimeConfigurator.Configuration != null && RuntimeConfigurator.Configuration.SceneObjectRegistry.ContainsGuid(Guid);

#if UNITY_5_3_OR_NEWER
        private void Awake()
#elif GODOT
        public override void _Ready()
#endif
        {
            Init();

            // Register inactive ProcessSceneObjects
#if UNITY_5_3_OR_NEWER
            var processSceneObjects = GetComponentsInChildren<ProcessSceneObject>(true);
            for (int i = 0; i < processSceneObjects.Length; i++)
            {
                if (!processSceneObjects[i].isActiveAndEnabled)
                {
                    processSceneObjects[i].Init();
                }
            }
#elif GODOT
            IEnumerable<ProcessSceneObject> processSceneObjects = FindChildren("*", recursive: true).OfType<ProcessSceneObject>();
            foreach (ProcessSceneObject pso in processSceneObjects)
                if (!pso.IsProcessing())
                    pso.Init();
#endif
        }

#if UNITY_5_3_OR_NEWER
        private void Update()
#elif GODOT
        public override void _Process(double delta)
#endif

        {
#if UNITY_EDITOR
            // TODO We need to move this to another update e.g. something in the RuntimeConfigurator
            CheckRefreshRegistry();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // similar to OnSerialize, but gets called on Copying a Component or Applying a Prefab
            if (!IsInTheScene())
            {
                // This catches all cases adding, removing, creating, deleting
                // It also adds overhead e.g. it is also called when entering prefab edit mode or entering the scene
                MarkPrefabDirty(this);
                SetGuidDefaultValues();
            }
#endif
        }

        /// <summary>
        /// Implement this method to receive a callback before Unity serializes your object.
        /// </summary>
        /// <remarks>
        /// We use this to prevent the GUID to be saved into a prefab on disk.
        /// Be aware this is called more often than you would think (e.g.: about once per frame if the object is selected in the editor)
        /// - https://discussions.unity.com/t/onbeforeserialize-is-getting-called-rapidly/115546,
        /// - https://blog.unity.com/engine-platform/serialization-in-unity </remarks>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // This lets us detect if we are a prefab instance or a prefab asset.
            // A prefab asset cannot contain a GUID since it would then be duplicated when instanced.
            if (!IsInTheScene())
            {
                SetGuidDefaultValues();
                return;
            }
#endif
#if UNITY_5_3_OR_NEWER
if (IsGuidAssigned() && !serializedGuid.Equals(guid))
            {
                Guid previousGuid = Guid;
                serializedGuid.SetGuid(guid);

                ObjectIdChanged?.Invoke(this, new UniqueIdChangedEventArgs(previousGuid, Guid));
            }
#elif GODOT
            //TODO: implement ... actually we do that in OnPropertyChanged etc.
#endif
        }

        /// <summary>
        /// Implement this method to receive a callback after Unity deserializes your object.
        /// </summary>
        /// <remarks>
        /// We use this to restore the <see cref="serializedGuid"/> when it was unwanted changed by the editor
        /// or assign <see cref="guid"> from the stored <see cref="serializedGuid"/>.
        /// </remarks>
        public void OnAfterDeserialize()
        {
#if UNITY_5_3_OR_NEWER
            if (IsGuidAssigned())
            {
                /// Restore Guid:
                /// - Editor Prefab Overrides -> Revert
                serializedGuid.SetGuid(guid);
            }
            else if (SerializableGuid.IsValid(serializedGuid))
            {
                /// Apply Serialized Guid:
                /// - Open scene
                /// - Recompile
                /// - Editor Prefab Overrides -> Apply
                /// - Start Playmode
                guid = serializedGuid.Guid;
            }
            else
            {
                /// - New GameObject we initialize guid lazy
                /// - Drag and drop prefab into scene
                /// - Interacting with the prefab outside of the scene
            }
#elif GODOT
            //Is in <Godot>/Core/Editor/SceneObjects/ProcessSceneObject.cs
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Overriding the Reset context menu entry in order to unregister the object before invalidating the object ID.
        /// </summary>
        [ContextMenu("Reset", false, 0)]
        protected void ResetContextMenu()
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);
            }

            // On Reset, we want to generate a new Guid
            SetObjectId(Guid.NewGuid());
            guids = new List<SerializableGuid>();
            Init();
        }

        [ContextMenu("Reset Object ID")]
        protected void MakeUnique()
        {
            if (EditorUtility.DisplayDialog("Reset Object ID", "Warning! This will change the object's unique ID.\n" +
                "All reference to this object in the Process Editor will become invalid.\n" +
                "Proceed?", "Yes", "No"))
            {
                ResetUniqueId();
            }
        }
#endif

        public void ResetUniqueId()
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);

                SetObjectId(Guid.NewGuid());
                Init();
            }
        }

#if UNITY_5_3_OR_NEWER
        private void OnDestroy()
#elif GODOT
        public override void _ExitTree()
#endif
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);
            }
        }

        /// <inheritdoc />
        public void SetObjectId(Guid guid)
        {
            SerializableGuid? serializableGuid = serializedGuid as SerializableGuid;

            Guid previousGuid = serializedGuid != null && serializableGuid.IsValid() ? serializableGuid.Guid : Guid.Empty;
#if UNITY_EDITOR
            Undo.RecordObject(this, "Changed GUID");
#endif
            serializableGuid?.SetGuid(guid);
            this.guid = guid;

#if UNITY_5_3_OR_NEWER
            ObjectIdChanged?.Invoke(this, new UniqueIdChangedEventArgs(previousGuid, Guid));
#elif GODOT
            EmitSignal(SignalName.ObjectIdChanged, new UniqueIdChangedEventArgs(previousGuid, Guid)); //TODO: find correct SignalName class
#endif
        }

        /// <summary>
        /// Checks if the Guid was assigned a value and not <c>System.Guid.Empty</c>.
        /// </summary>
        /// <returns><c>true</c> if the Guid is assigned; otherwise, <c>false</c>.</returns>
        protected bool IsGuidAssigned()
        {
            return guid != System.Guid.Empty;
        }

        /// <summary>
        /// Initializes the ProcessSceneObject by registering it with the SceneObjectRegistry.
        /// It will not register if in prefab mode edit mode or if we are a prefab asset.
        /// </summary>
        protected void Init()
        {
            if (RuntimeConfigurator.Exists == false)
            {
#if UNITY_5_3_OR_NEWER
                Debug.LogWarning($"Not registering {gameObject.name} due to runtime configurator not present.");
#elif GODOT
                GD.PushWarning($"Not registering {GameObject.Name} due to runtime configurator not present.");
#endif
                return;
            }

#if UNITY_EDITOR
            // if in editor, make sure we aren't a prefab of some kind
            if (!IsInTheScene())
            {
                return;
            }
#endif

#if UNITY_EDITOR
            //TODO This is from the Unity code for some edge case I do not know about yet
            // If we are creating a new GUID for a prefab instance of a prefab, but we have somehow lost our prefab connection
            // force a save of the modified prefab instance properties
            // if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
            // {
            //     PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            // }
#endif
            RuntimeConfigurator.Configuration.SceneObjectRegistry.Register(this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Checks if the current object is in the scene and tracks stage transitions.
        /// </summary>
        /// <returns><c>true</c> if the object is in the scene; otherwise, <c>false</c>.</returns>
        private bool IsInTheScene()
        {
            bool isSceneObject = AssetUtility.IsComponentInScene(this);
            return isSceneObject;
        }

        /// <summary>
        /// Refreshes the scene object registry when we have <see cref="hasDirtySceneObject"/ and are in the main scene>
        /// </summary>
        private static void CheckRefreshRegistry()
        {
            bool isMainStage = StageUtility.GetCurrentStageHandle() == StageUtility.GetMainStageHandle();

            if (isMainStage && hasDirtySceneObject)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Refresh();
                hasDirtySceneObject = false;
                //TODO if we have an open PSO in the Inspector we should redraw it
            }
        }

        /// <summary>
        /// Marks the specified scene object as dirty, indicating that its prefab has been modified outside of the scene.
        /// </summary>
        /// <param name="sceneObject">The scene object to mark as dirty.</param>
        private static void MarkPrefabDirty(ProcessSceneObject sceneObject)
        {
            // Potentially keep track of all changed prefabs in a separate class and only update those in the registry
            // https://chat.openai.com/share/736f3640-b884-4bf3-aabb-01af50e44810
            //PrefabDirtyTracker.MarkPrefabDirty(sceneObject);

            hasDirtySceneObject = true;
        }
#endif

        /// <summary>
        /// Sets the default values for the serializedGuid and guid variables.
        /// </summary>
        private void SetGuidDefaultValues()
        {
            serializedGuid = null;
            guid = System.Guid.Empty;
        }

        /// <inheritdoc />
        public bool CheckHasProperty<T>() where T : ISceneObjectProperty
        {
            return CheckHasProperty(typeof(T));
        }

        /// <inheritdoc />
        public bool CheckHasProperty(Type type)
        {
            return FindProperty(type) != null;
        }

        /// <inheritdoc />
        public T GetProperty<T>() where T : ISceneObjectProperty
        {
            ISceneObjectProperty property = FindProperty(typeof(T));
            if (property == null)
            {
                throw new PropertyNotFoundException(this, typeof(T));
            }

            return (T)property;
        }

        /// <inheritdoc />
        public void ValidateProperties(IEnumerable<Type> properties)
        {
            bool hasFailed = false;
            foreach (Type propertyType in properties)
            {
                // ReSharper disable once InvertIf
                if (CheckHasProperty(propertyType) == false)
                {
#if UNITY_5_3_OR_NEWER
                    Debug.LogErrorFormat("Property of type '{0}' is not attached to SceneObject '{1}'", propertyType.Name, gameObject.name);
#elif GODOT
                    GD.PrintErr($"Property of type '{propertyType.Name}' is not attached to SceneObject '{SceneFilePath}'");
#endif
                    hasFailed = true;
                }
            }

            if (hasFailed)
            {
                throw new PropertyNotFoundException("One or more SceneObjectProperties could not be found, check your log entries for more information.");
            }
        }

        /// <inheritdoc />
        public void SetLocked(bool lockState)
        {
            if (IsLocked == lockState)
            {
                return;
            }

            IsLocked = lockState;

            if (IsLocked)
            {
#if UNITY_5_3_OR_NEWER
if (Locked != null)
                {
                    Locked.Invoke(this, new LockStateChangedEventArgs(IsLocked));
                }
#elif GODOT
                EmitSignal(SignalName.Locked, new LockStateChangedEventArgs(IsLocked));
#endif
            }
            else
            {
#if UNITY_5_3_OR_NEWER
                if (Unlocked != null)
                {
                    Unlocked.Invoke(this, new LockStateChangedEventArgs(IsLocked));
                }
#elif GODOT
                EmitSignal(SignalName.Unlocked, new LockStateChangedEventArgs(IsLocked));
#endif
            }
        }

        /// <inheritdoc/>
        public virtual void RequestLocked(bool lockState, IStepData stepData)
        {
            if (lockState == false && unlockers.Contains(stepData) == false)
            {
                unlockers.Add(stepData);
            }

            if (lockState && unlockers.Contains(stepData))
            {
                unlockers.Remove(stepData);
            }

            bool canLock = unlockers.Count == 0;

            if (LifeCycleLoggingConfig.Instance.LogLockState)
            {
                string lockType = lockState ? "lock" : "unlock";
                string requester = stepData == null ? "NULL" : stepData.Name;
                StringBuilder unlockerList = new StringBuilder();

                foreach (IStepData unlocker in unlockers)
                {
                    unlockerList.Append($"\n<i>{unlocker.Name}</i>");
                }

                string listUnlockers = unlockers.Count == 0 ? "" : $"\nSteps keeping this object unlocked:{unlockerList}";

#if UNITY_5_3_OR_NEWER
                Debug.Log($"<i>{this.GetType().Name}</i> on <i>{gameObject.name}</i> received a <b>{lockType}</b> request from <i>{requester}</i>." +
                    $"\nCurrent lock state: <b>{IsLocked}</b>. Future lock state: <b>{lockState && canLock}</b>{listUnlockers}");
#elif GODOT
                GD.Print($"<i>{GetType().Name}</i> on <i>{Name}</i> received a <b>{lockType}</b> request from <i>{requester}</i>." +
                         $"\nCurrent lock state: <b>{IsLocked}</b>. Future lock state: <b>{lockState && canLock}</b>{listUnlockers}");
#endif
            }

            SetLocked(lockState && canLock);
        }

        /// <inheritdoc/>
        public bool RemoveUnlocker(IStepData data)
        {
            return unlockers.Remove(data);
        }

        /// <summary>
        /// Tries to find property which is assignable to given type, this method
        /// will return null if none is found.
        /// </summary>
        private ISceneObjectProperty FindProperty(Type type)
        {
#if UNITY_5_3_OR_NEWER
            return GetComponent(type) as ISceneObjectProperty;
#elif GODOT
            return GetChildren().FirstOrDefault(c => c.GetType() == type) as ISceneObjectProperty;
#endif
        }

        /// <inheritdoc />
        public void AddGuid(Guid guid)
        {
            var serializableGuid = new SerializableGuid(guid.ToByteArray());
            if (!HasGuid(guid))
            {
                guids.Add(serializableGuid);
#if UNITY_5_3_OR_NEWER
                GuidAdded?.Invoke(this, new GuidContainerEventArgs(guid));
#elif GODOT
                EmitSignal("GuidAdded", new GuidContainerEventArgs(guid)); //TODO: find correct SignalName class
#endif
            }
        }

        /// <inheritdoc />
        public bool HasGuid(Guid guid)
        {
            return guids.Any(serializableGuid => serializableGuid.Equals(guid));
        }

        /// <inheritdoc />
        public bool RemoveGuid(Guid guid)
        {
            var serializableGuid = guids.FirstOrDefault(t => t.Equals(guid));
            if (serializableGuid != null)
            {
                guids.Remove(serializableGuid);
#if UNITY_5_3_OR_NEWER
                GuidRemoved?.Invoke(this, new GuidContainerEventArgs(guid));
#elif GODOT
                EmitSignal(SignalName.GuidRemoved, new GuidContainerEventArgs(guid));
#endif
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
#if UNITY_5_3_OR_NEWER
            return GameObject.name;
#elif GODOT
            return GameObject.Name;
#endif
        }
    }
}