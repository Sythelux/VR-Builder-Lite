#if UNITY_5_3_OR_NEWER
using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// Lets the user select between a VideoClip asset (stored as asset path) and a string URL.
    /// </summary>
    public class VideoClipOrURLSelectableValue : SelectableValue<string, string>
    {
        /// <inheritdoc/>
        public override string FirstValueLabel => "Video clip resource";

        /// <inheritdoc/>
        public override string SecondValueLabel => "URL";

        /// <inheritdoc/>
        [DataMember]
        [UsesSpecificProcessDrawer("VideoClipResourceDrawer")]
        public override string FirstValue { get; set; }

        public VideoClipOrURLSelectableValue()
        {
            IsFirstValueSelected = true;
            FirstValue = string.Empty;
            SecondValue = string.Empty;
        }
    }
}

#elif GODOT
using Godot;
//TODO
#endif
