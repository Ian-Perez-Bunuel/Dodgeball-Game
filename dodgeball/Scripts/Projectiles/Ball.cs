using Godot;
using System;

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

	private GpuParticles3D explosion;
	private GpuParticles3D trail;


	// Nodes
	private Timer timer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		timer = GetNode<Timer>("Timer");
		timer.Timeout += OnTimerTimeout;

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
		// SetPhysicsProcess(false);
		// timer.Stop();

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

		curveDirection = forwardDir.Cross(Vector3.Up).Normalized();
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

		if (!explosion.Emitting && !active)
		{
			GD.Print("DONE");
			ReturnToQueue();
		}
	}

	public void Shoot(Vector3 t_pos, Vector3 t_targetPos)
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

		timer.Start();
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
