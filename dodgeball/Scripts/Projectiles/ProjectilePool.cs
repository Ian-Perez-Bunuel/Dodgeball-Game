using Godot;
using System;
using System.Collections.Generic;

public partial class ProjectilePool : Node
{
	[Export]
    public PackedScene ProjectileScene { get; set; }

    [Export]
    public int PoolSize = 20;

    private Queue<Node3D> pool = new Queue<Node3D>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < PoolSize; i++)
		{
			var projectile = (Node3D)ProjectileScene.Instantiate();
			projectile.Visible = false;
			projectile.SetProcess(false);
			projectile.SetPhysicsProcess(false);
			AddChild(projectile);
			pool.Enqueue(projectile);
		}
	}

	public Node3D GetProjectile()
    {
        if (pool.Count == 0)
        {
            GD.PrintErr("No projectiles left in pool!");
            return null;
        }

        var projectile = pool.Dequeue();
        projectile.Visible = true;
        projectile.SetProcess(true);
        projectile.SetPhysicsProcess(true);
        return projectile;
    }

    public void ReturnProjectile(Node3D projectile)
    {
        projectile.Visible = false;
        projectile.SetProcess(false);
        projectile.SetPhysicsProcess(false);
        pool.Enqueue(projectile);
    }
}
