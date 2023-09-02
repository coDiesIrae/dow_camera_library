using System.Collections.Generic;
using Sandbox;

namespace CameraLibrary;

[ GameResource(
    "Camera Settings",
    "slrcam",
    "Describes camera settings for a given mode",
    Icon = "videocam"
  ) ]
public class CameraSettings : GameResource
{
  private static readonly List< CameraSettings >          all = new();
  public static           IReadOnlyList< CameraSettings > All => all;

  protected override void PostLoad()
  {
    base.PostLoad();

    if ( !all.Contains( this ) )
    {
      all.Add( this );
    }
  }

  public float HorizontalOffset { get; set; }
  public float VerticalOffset   { get; set; }
  public float TargetDistance   { get; set; }
}
