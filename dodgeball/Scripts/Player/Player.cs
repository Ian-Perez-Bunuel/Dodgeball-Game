using Godot;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;

public partial class Player : CharacterBody3D
{
    public const float Speed = 5.0f;
    public const float JumpVelocity = 4.5f;

    public const float MAX_RANGE = 30.0f;

    [Export] float sensitivity = 0.3f;

    // Nodes
    private Node3D pivot;
    [Export] private Camera3D camera;
    private ProjectilePool projectilePool;
    [Export] private Node3D projectileSpawnPos;

    // Debug
    Vector3 lineStart;
    Vector3 lineEnd;

    public override void _Ready()
    {
        // Capture the mouse on screen
        Input.MouseMode = Input.MouseModeEnum.Captured;

        pivot = GetNode<Node3D>("CameraPivot");

        projectilePool = GetNode<ProjectilePool>("ProjectilePool");
    }

    public override void _Input(InputEvent t_event)
    {
        if (t_event is InputEventMouseMotion mouseMotion)
        {
            float deltaX = mouseMotion.Relative.X;
            float deltaY = mouseMotion.Relative.Y;

            RotateY(Mathf.DegToRad(-deltaX * sensitivity));
            pivot.RotateX(Mathf.DegToRad(-deltaY * sensitivity));
            // Lock camera - Rotation.X cannot be changed directly
            Vector3 rot = pivot.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90), Mathf.DegToRad(45));
            pivot.Rotation = rot;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && Input.IsActionJustPressed("Shoot"))
        {
            Vector2 mousePos = mouseEvent.Position;

            // Get the origin and direction of the ray
            Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
            Vector3 rayDirection = camera.ProjectRayNormal(mousePos);
            Vector3 rayEnd = rayOrigin + rayDirection * 1000f;

            // Perform the raycast
            var spaceState = GetWorld3D().DirectSpaceState;

            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                Vector3 hitPos = (Vector3)result["position"];
                //GD.Print("Hit object: ", result["collider"]);
                //GD.Print("Hit position: ", result["position"]);

                Shoot(hitPos);
            }
            else // Shooting at nothing
            {
                float distance = MAX_RANGE; // Or whatever distance you want
                Vector3 pointAlongRay = rayOrigin + rayDirection * distance;

                Shoot(pointAlongRay);
            }
        }
    }

    static Ball.CurveType GetCurveType()
    {
        if (Input.IsActionPressed("Forward"))
        {
            GD.Print("Up");
            return Ball.CurveType.Up;
        }
        else if (Input.IsActionPressed("Backward"))
        {
            GD.Print("Down");
            return Ball.CurveType.Down;
        }
        else if (Input.IsActionPressed("Left"))
        {
            GD.Print("Left");
            return Ball.CurveType.Left;
        }
        else if (Input.IsActionPressed("Right"))
        {
            GD.Print("Right");
            return Ball.CurveType.Right;
        }
        else
        {
            GD.Print("None");
            return Ball.CurveType.None;
        }
    }

    private void Shoot(Vector3 t_targetPos)
    {
        var projectile = projectilePool.GetProjectile();
        if (projectile != null)
        {
            lineStart = projectileSpawnPos.GlobalPosition;
            lineEnd = t_targetPos;

            if (projectile is Ball p)
            {
                float distToTarget = (projectileSpawnPos.GlobalPosition - t_targetPos).Length();
                if (distToTarget <= MAX_RANGE)
                {
                    Ball.CurveType curveType = GetCurveType();

                    p.Shoot(projectileSpawnPos.GlobalPosition, t_targetPos, curveType);
                }
                else
                {
                    GD.Print("TOO FAR");
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        // Handle Jump.
        if (Input.IsActionJustPressed("Jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("Left", "Right", "Forward", "Backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();


        // Quit the game
        if (Input.IsActionJustPressed("Quit"))
        {
            GetTree().Quit();
        }
    }
}
