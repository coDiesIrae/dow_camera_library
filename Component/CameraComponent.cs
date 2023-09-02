using Sandbox;

namespace CameraLibrary;

public class CameraComponent< T > : EntityComponent< T >
  where T : ModelEntity, WithViewAngles
{
  private float targetDistance = 80.0f;
  private float verticalOffset;
  private float horizontalOffset;

  private float smoothTargetDistance;
  private float smoothVerticalOffset;
  private float smoothHorizontalOffset;

  private Vector3 attachPosition;
  private Vector3 endCameraPosition;

  private bool    obstacleHit;
  private float   obstacleHitDistance;
  private Vector3 obstacleHitVector;

  private float lastCameraDistance;
  private bool  lastObstacleHit;

  private CameraControls< T > controls;

  public float MoveSmoothing     { get; set; } = 5.0f;
  public float ObstacleSmoothing { get; set; } = 20.0f;

  public float TargetDistance
  {
    get => targetDistance;
    set
    {
      targetDistance = value;

      if ( smoothTargetDistance <= 0 )
      {
        smoothTargetDistance = value;
        lastCameraDistance   = value;
      }
    }
  }

  public float VerticalOffset
  {
    get => verticalOffset;
    set
    {
      verticalOffset = value;

      if ( smoothVerticalOffset <= 0 )
      {
        smoothVerticalOffset = value;
      }
    }
  }

  public float HorizontalOffset
  {
    get => horizontalOffset;
    set
    {
      horizontalOffset = value;

      if ( smoothHorizontalOffset <= 0 )
      {
        smoothHorizontalOffset = value;
      }
    }
  }

  private float Scale => Entity.Scale;

  protected override void OnActivate()
  {
    base.OnActivate();

    controls                 = Entity.Components.Create< CameraControls< T > >();
    controls.CameraComponent = this;
  }

  public void AddMode( string mode, int priority = 0 )
  {
    controls.AddMode( mode, priority );
  }

  public void RemoveMode( string mode )
  {
    controls.RemoveMode( mode );
  }

  public void FrameSimulate()
  {
    UpdateCameraRotationAndFow();
    UpdateAttachPosition();

    LerpSmoothValues();
    UpdateObstacleTrace();

    if ( ShouldSmoothCamera() )
    {
      SmoothMoveCamera();
    }
    else
    {
      SnapCameraToEndPos();
    }
  }

  private void UpdateCameraRotationAndFow()
  {
    Camera.Rotation    = Entity.ViewAngles.ToRotation();
    Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
  }

  private void UpdateAttachPosition()
  {
    attachPosition = Entity.Position + Entity.Model.PhysicsBounds.Center;
  }

  private void LerpSmoothValues()
  {
    smoothTargetDistance   = LerpWithSmoothing( smoothTargetDistance, TargetDistance );
    smoothVerticalOffset   = LerpWithSmoothing( smoothVerticalOffset, VerticalOffset );
    smoothHorizontalOffset = LerpWithSmoothing( smoothHorizontalOffset, HorizontalOffset );
  }

  private float LerpWithSmoothing( float current, float target )
  {
    return current.LerpTo( target, Time.Delta * MoveSmoothing );
  }

  private void UpdateObstacleTrace()
  {
    var targetCameraPosition = GetTargetCameraPosition();

    var tr = Trace
             .Ray( attachPosition, targetCameraPosition )
             .WithAnyTags( "solid" )
             .Ignore( Entity )
             .Radius( 8 )
             .Run();

    endCameraPosition = tr.EndPosition;
    obstacleHit       = tr.Hit;

    obstacleHitVector   = endCameraPosition - attachPosition;
    obstacleHitDistance = obstacleHitVector.Length;
  }

  private Vector3 GetTargetCameraPosition()
  {
    var rot = Camera.Rotation;

    var distance           = smoothTargetDistance * Scale;
    var verticalDistance   = smoothVerticalOffset * Scale;
    var horizontalDistance = smoothHorizontalOffset * Scale;

    var targetPos = attachPosition;

    targetPos += rot.Up * verticalDistance;
    targetPos += rot.Right * horizontalDistance;
    targetPos += rot.Forward * -distance;

    return targetPos;
  }

  private bool ShouldSmoothCamera()
  {
    return !lastCameraDistance.AlmostEqual( obstacleHitDistance );
  }

  private void SmoothMoveCamera()
  {
    float smoothDistance;

    if ( !CameraMovementCausedByObstacle() )
    {
      smoothDistance = obstacleHitDistance;
    }
    else
    {
      smoothDistance = lastCameraDistance.LerpTo(
          obstacleHitDistance,
          Time.Delta * ObstacleSmoothing
        );
    }

    Camera.Position = attachPosition + obstacleHitVector.Normal * smoothDistance;

    lastObstacleHit    = obstacleHit || IsMovingAwayFromObstacle();
    lastCameraDistance = smoothDistance;
  }

  private bool CameraMovementCausedByObstacle()
  {
    return IsMovingAwayFromObstacle() || IsMovingTowardsObstacle();
  }

  private bool IsMovingAwayFromObstacle()
  {
    return lastCameraDistance < obstacleHitDistance && lastObstacleHit;
  }

  private bool IsMovingTowardsObstacle()
  {
    return lastCameraDistance > obstacleHitDistance && obstacleHit;
  }

  private void SnapCameraToEndPos()
  {
    Camera.Position    = endCameraPosition;
    lastCameraDistance = obstacleHitDistance;
  }
}
