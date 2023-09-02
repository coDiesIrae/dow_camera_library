using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace CameraLibrary;

public record SettingsEntry( int Priority, string Mode, CameraSettings Settings );

public class CameraControls< T > : EntityComponent< T >
  where T : ModelEntity, WithViewAngles
{
  private Dictionary< string, CameraSettings > settingsMap;
  private CameraComponent< T >                 cameraComponent;

  private readonly List< SettingsEntry > settingsEntries = new();

  public CameraComponent< T > CameraComponent
  {
    get => cameraComponent;
    set
    {
      cameraComponent = value;

      settingsEntries.Clear();
      AddMode( "default", -1 );
      Update();
    }
  }

  public CameraControls()
  {
    SetCameraSettings( CameraSettings.All );
  }

  private void SetCameraSettings( IEnumerable< CameraSettings > settings )
  {
    settingsMap = new Dictionary< string, CameraSettings >();

    foreach ( var setting in settings )
    {
      if ( settingsMap.TryGetValue( setting.ResourceName, out var otherSetting ) )
      {
        Log.Warning(
            $"Duplicate camera setting '{
              setting.ResourceName
            }' found. First entry: '{
              otherSetting.ResourcePath
            }'. Second entry: '{
              setting.ResourcePath
            }'. Using second."
          );
      }

      settingsMap[ setting.ResourceName ] = setting;
    }
  }

  public void AddMode( string mode, int priority = 0 )
  {
    if ( !settingsMap.TryGetValue( mode, out var setting ) )
    {
      Log.Warning( $"Attempted to add camera mode '{mode}' but it does not exist." );
      return;
    }

    var index = GetInsertIndexForPriority( priority );

    settingsEntries.Insert( index, new SettingsEntry( priority, mode, setting ) );

    Update();
  }

  public void RemoveMode( string mode )
  {
    if ( RemoveAllEntriesWithMode( mode ) <= 0 )
    {
      Log.Warning( $"Attempted to remove camera mode '{mode}' not present in settings." );
    }

    Update();
  }

  private int GetInsertIndexForPriority( int priority )
  {
    for ( var i = settingsEntries.Count - 1; i >= 0; i-- )
    {
      if ( settingsEntries[ i ].Priority <= priority )
      {
        return i + 1;
      }
    }

    return settingsEntries.Count;
  }

  private int RemoveAllEntriesWithMode( string mode )
  {
    var count = 0;

    for ( var i = settingsEntries.Count - 1; i >= 0; i-- )
    {
      if ( settingsEntries[ i ].Mode != mode )
      {
        continue;
      }

      settingsEntries.RemoveAt( i );
      count++;
    }

    return count;
  }

  private void Update()
  {
    var settings = settingsEntries.Last().Settings;

    CameraComponent.HorizontalOffset = settings.HorizontalOffset;
    CameraComponent.VerticalOffset   = settings.VerticalOffset;
    CameraComponent.TargetDistance   = settings.TargetDistance;
  }
}
