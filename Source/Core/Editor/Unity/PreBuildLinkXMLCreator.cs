using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace VRBuilder.Editor
{
  /// <summary>
  /// Generates a <see cref="LinkXML"/> file before the build process to preserve all Behaviors and Conditions from VR Builder add-ons.
  /// </summary>
  /// <remarks>
  /// The existing <see cref="LinkXML"/> in the <see cref="VRBuilderRootFolder"/> will be overwritten.
  /// </remarks>
  public class PreBuildLinkXMLCreator : IPreprocessBuildWithReport
  {
    public int callbackOrder => 3;
    public const string VRBuilderRootFolder = "Assets/MindPort/VR Builder";
    public const string LinkXML = "link.xml";

    public void OnPreprocessBuild(BuildReport report)
    {
      if (!Directory.Exists(VRBuilderRootFolder))
      {
        Directory.CreateDirectory(VRBuilderRootFolder);
      }

      string linkXmlPath = Path.Combine(VRBuilderRootFolder, LinkXML);
      string linkXmlContent = @"<!-- Preserve all Behaviors and Conditions from VR Builder add-ons. -->
<!-- The managed stripping (even set to low) will remove the add-on assembly if no entities of the add-on are used in the scene. -->
<!-- This file is automatically generated by PreBuildLinkXMLCreator.cs and placed here when building. -->
<linker>
  <assembly fullname='VRBuilder.Animations' preserve='all' ignoreIfMissing='1'>
    <namespace fullname='VRBuilder.Animations.Behaviors' preserve='all'/>
  </assembly>
  <assembly fullname='VRBuilder.Randomization' preserve='all' ignoreIfMissing='1'>
    <namespace fullname='VRBuilder.Randomization.Behaviors' preserve='all'/>
    <namespace fullname='VRBuilder.Randomization.Conditions' preserve='all'/>
  </assembly>
  <assembly fullname='VRBuilder.StatesAndData' preserve='all' ignoreIfMissing='1'>
    <namespace fullname='VRBuilder.StatesAndData.Behaviors' preserve='all'/>
    <namespace fullname='VRBuilder.StatesAndData.Conditions' preserve='all'/>
  </assembly>
  <assembly fullname='VRBuilder.TrackAndMeasure' preserve='all' ignoreIfMissing='1'>
    <namespace fullname='VRBuilder.TrackAndMeasure.Behaviors' preserve='all'/>
    <namespace fullname='VRBuilder.TrackAndMeasure.Conditions' preserve='all'/>
  </assembly>
  <assembly fullname='VRBuilder.Cognitive3D' preserve='all' ignoreIfMissing='1'>
    <namespace fullname='VRBuilder.Cognitive3DIntegration.Behaviors' preserve='all'/>
  </assembly>
</linker>";

      File.WriteAllText(linkXmlPath, linkXmlContent);
    }
  }
}