using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Player : CharacterBody3D
{
	public Node3D Head;
	public Camera3D Camera;

	[Export] private float mouseSensitivity = 0.01f;
	private Vector3 syncPos = new Vector3(0, 0, 0); 
	private Vector3 syncRot = new Vector3(0, 0, 0);

	public const float Speed = 5.0f;
	public const float Acceleration = 0.5f;
	public const float JumpVelocity = 4.5f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        Head = GetNode<Node3D>("Head");
		Camera = GetNode<Camera3D>("Head/Camera3D");

		Input.MouseMode = Input.MouseModeEnum.Captured;

		GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion) {
			InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
			Head.RotateY(-mouseMotion.Relative.X * mouseSensitivity);
			Camera.RotateX(-mouseMotion.Relative.Y * mouseSensitivity);

			Vector3 cameraRot = Camera.Rotation;
			cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
			Camera.Rotation = cameraRot;
		}
    }

    public override void _PhysicsProcess(double delta)
	{
		if (GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").GetMultiplayerAuthority() == Multiplayer.GetUniqueId()) {
			
			Camera.Current = true; // fix camera bug
			
			Vector3 velocity = Velocity;

			// Add the gravity.
			if (!IsOnFloor())
				velocity.Y -= gravity * (float)delta;

			// Handle Jump.
			if (Input.IsActionJustPressed("jump") && IsOnFloor())
				velocity.Y = JumpVelocity;

			// Get the input direction and handle the movement/deceleration.
			// As good practice, you should replace UI actions with custom gameplay actions.
			Vector2 inputDir = Input.GetVector("left", "right", "forward", "backward");
			Vector3 direction = (Head.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			if (direction != Vector3.Zero) {
				velocity.X = direction.X * Speed;
				velocity.Z = direction.Z * Speed;
			} else {
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Acceleration);
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Acceleration);
			}

			Velocity = velocity;
			MoveAndSlide();

			syncPos = GlobalPosition;
			syncRot = Head.Rotation;
		} else {
			//				i b leeeeeeeerpin             hehe
			GlobalPosition = GlobalPosition.Lerp(syncPos, .1f);
			Head.Rotation = Head.Rotation.Lerp(syncRot, .5f);
		}
	}

	public void SetPlayerUsername(string name)
	{
		GetNode<Label3D>("Username").Text = name;
	}
}
