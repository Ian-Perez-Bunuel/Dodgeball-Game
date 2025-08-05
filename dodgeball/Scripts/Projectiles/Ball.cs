using Godot;
using System;
using System.Collections.Generic;
using System.Data;

public partial class Ball : Area3D
{
	[Export] public float speed = 50.0f;
	private bool active = false;

	// Curve
	[Export] float curveAmount;
	private Vector3 curveDirection;
	[Export] Curve curve;
	private Vector3 targetPos;
	private Vector3 startPos;
	private Vector3 forwardDir;
	private float totalDistance;
	private float traveledDistance;
	public enum CurveType
	{
		None,
		Left,
		Right,
		Up,
		Down
	};
	CurveType curveType;

	private GpuParticles3D explosion;
	private GpuParticles3D trail;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnCollision;

		explosion = GetNode<GpuParticles3D>("Explosion");
		trail = GetNode<GpuParticles3D>("Trail");
	}


	private void OnCollision(Node3D body)
	{
		GD.Print("HIT: " + body.Name);
		EndTargetReached();

		// ReturnToQueue();
	}

	private void EndTargetReached()
	{
		active = false;
		SetPhysicsProcess(false);

		//Particles
		trail.Emitting = false;
		explosion.Emitting = true;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		float deltaSpeed = speed * (float)delta;
		traveledDistance += deltaSpeed;

		float progress = traveledDistance / totalDistance;
		progress = Mathf.Clamp(progress, 0, 1);

		// Base position along the straight path
		Vector3 basePos = startPos + forwardDir * traveledDistance;

		curveDirection = GetCurveDirection(curveType); // Right
		// Get offset from curve 
		Vector3 offset = curveDirection * (curve.Sample(progress) * curveAmount);

		// Apply offset in Y (you can change to lateral if needed)
		Vector3 curvedPos = basePos + offset;

		GlobalPosition = curvedPos;
		Vector3 movement = curvedPos - GlobalPosition; // Face the direction its going 
		if (movement.LengthSquared() > 0.0001f)
		{
			LookAt(GlobalPosition + movement.Normalized(), Vector3.Up);
		}

		if (progress >= 1) // Reach destination
		{
			EndTargetReached();
		}
	}

	private Vector3 GetCurveDirection(CurveType t_curveType)
    {
		switch (t_curveType)
		{
			case CurveType.None:
				return new Vector3(0, 0, 0);

			case CurveType.Left:
				return -forwardDir.Cross(Vector3.Up).Normalized();

			case CurveType.Right:
				return forwardDir.Cross(Vector3.Up).Normalized();

			case CurveType.Up:
				return Vector3.Up;

			case CurveType.Down:
				return -Vector3.Up;
				
			default:
    			return new Vector3(0, 0, 0);
        }
    }

	public void Shoot(Vector3 t_pos, Vector3 t_targetPos, CurveType t_curveType)
	{
		GlobalPosition = t_pos;

		startPos = t_pos;
		targetPos = t_targetPos;
		forwardDir = (targetPos - startPos).Normalized();
		totalDistance = (targetPos - startPos).Length();
		curveAmount *= NormalizeFloat(totalDistance, 0, Player.MAX_RANGE);
		traveledDistance = 0;
		active = true;
		// Particles
		trail.Emitting = true;
		explosion.Emitting = false;

		curveType = t_curveType;
	}

	public void ReturnToQueue()
	{
		// Return to pool instead of QueueFree
		SetPhysicsProcess(false);
		GetParent<ProjectilePool>()?.ReturnProjectile(this);
	}

	private void OnTimerTimeout()
	{
		ReturnToQueue();
	}
	
	public static float NormalizeFloat(float value, float min, float max)
	{
		return Mathf.Clamp((value - min) / (max - min), 0f, 1f);
	}
}
