// ProtoController.cs
// ProtoController v1.0 original by Brackeys, re-written in C# by SamsterJam
// CC0 License
// Intended for rapid prototyping of first-person games.

using Godot;

public partial class ProtoController : CharacterBody3D {
	[Export] public bool canMove = true;
	[Export] public bool hasGravity = true;
	[Export] public bool canJump = true;
	[Export] public bool canSprint = true;
	[Export] public bool canFreefly = true;

	// -----------------------
	// Speeds
	// -----------------------
	[Export] public float lookSpeed = 0.002f;
	[Export] public float baseSpeed = 7.0f;
	[Export] public float jumpVelocity = 4.5f;
	[Export] public float sprintSpeed = 10.0f;
	[Export] public float freeflySpeed = 25.0f;

	// -----------------------
	// Input Actions
	// -----------------------
	[Export] public string inputLeft    = "ui_left";
	[Export] public string inputRight   = "ui_right";
	[Export] public string inputForward = "ui_up";
	[Export] public string inputBack    = "ui_down";
	[Export] public string inputJump    = "ui_accept";
	[Export] public string inputSprint  = "sprint";
	[Export] public string inputFreefly = "freefly";

	private bool mouseCaptured = false;
	private Vector2 lookRotation = Vector2.Zero;
	private float moveSpeed = 0.0f;
	private bool freeflying = false;

	// IMPORTANT REFERENCES
	private Node3D head;
	private CollisionShape3D collider;

	public override void _Ready() {
		// Grab references from the scene
		head = GetNode<Node3D>("Head");
		collider = GetNode<CollisionShape3D>("Collider");

		CheckInputMappings();

		// Initialize the look rotation
		lookRotation.Y = Rotation.Y; 
		lookRotation.X = head.Rotation.X;
	}

	public override void _UnhandledInput(InputEvent @event) {
		// Mouse capturing
		if (Input.IsMouseButtonPressed(MouseButton.Left))
			CaptureMouse();
		if (Input.IsKeyPressed(Key.Escape))
			ReleaseMouse();

		// Look around
		if (mouseCaptured && @event is InputEventMouseMotion mouseMotion)
			RotateLook(mouseMotion.Relative);

		// Toggle freefly mode
		if (canFreefly && Input.IsActionJustPressed(inputFreefly)) {
			if (!freeflying)
				EnableFreefly();
			else
				DisableFreefly();
		}
	}

	public override void _PhysicsProcess(double delta) {
		// If freeflying, handle freefly and ignore normal movement
		if (canFreefly && freeflying) {
			Vector2 inputDir = Input.GetVector(inputLeft, inputRight, inputForward, inputBack);
			Vector3 motion = (head.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			motion *= freeflySpeed * (float)delta;
			MoveAndCollide(motion);
			return;
		}

		// Apply gravity to Velocity
		if (hasGravity) {
			if (!IsOnFloor())
				Velocity += GetGravity() * (float)delta;
		}

		// Handle jump
		if (canJump && IsOnFloor() && Input.IsActionJustPressed(inputJump)) {
			Velocity = new Vector3(Velocity.X, jumpVelocity, Velocity.Z);
		}

		// Modify speed based on sprinting
		if (canSprint && Input.IsActionPressed(inputSprint))
			moveSpeed = sprintSpeed;
		else
			moveSpeed = baseSpeed;

		// Apply movement
		if (canMove) {
			Vector2 inputDir = Input.GetVector(inputLeft, inputRight, inputForward, inputBack);
			Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

			if (moveDir.Length() > 0.001f) {
				Velocity = new Vector3(moveDir.X * moveSpeed, Velocity.Y, moveDir.Z * moveSpeed);
			} else {
				// Lerp velocity down towards zero if no input
				float newX = Mathf.MoveToward(Velocity.X, 0.0f, moveSpeed);
				float newZ = Mathf.MoveToward(Velocity.Z, 0.0f, moveSpeed);
				Velocity = new Vector3(newX, Velocity.Y, newZ);
			}
		} else {
			// Movement disabled
			Velocity = new Vector3(0, 0, 0);
		}

		// Actually move
		MoveAndSlide();
	}

	// Rotate us to look around.
	// Base of controller rotates around y (left/right). Head rotates around x (up/down).
	private void RotateLook(Vector2 rotInput) {
		// Update our stored look angles
		lookRotation.X -= rotInput.Y * lookSpeed; // pitch
		lookRotation.X = Mathf.Clamp(lookRotation.X, Mathf.DegToRad(-85), Mathf.DegToRad(85));

		lookRotation.Y -= rotInput.X * lookSpeed; // yaw

		// Rotate only around y on this body
		Rotation = new Vector3(Rotation.X, lookRotation.Y, Rotation.Z);

		// Rotate only around x on the head
		head.Rotation = new Vector3(lookRotation.X, head.Rotation.Y, head.Rotation.Z);
	}

	private void EnableFreefly() {
		collider.Disabled = true;
		freeflying = true;
		Velocity = Vector3.Zero;
	}

	private void DisableFreefly() {
		collider.Disabled = false;
		freeflying = false;
	}

	private void CaptureMouse() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		mouseCaptured = true;
	}

	private void ReleaseMouse() {
		Input.MouseMode = Input.MouseModeEnum.Visible;
		mouseCaptured = false;
	}

	// Checks if some Input Actions haven't been created.
	// Disables functionality accordingly.
	private void CheckInputMappings() {
		if (canMove && !InputMap.HasAction(inputLeft)) {
			GD.PushError("Movement disabled. No InputAction found for inputLeft: " + inputLeft);
			canMove = false;
		}
		if (canMove && !InputMap.HasAction(inputRight)) {
			GD.PushError("Movement disabled. No InputAction found for inputRight: " + inputRight);
			canMove = false;
		}
		if (canMove && !InputMap.HasAction(inputForward)) {
			GD.PushError("Movement disabled. No InputAction found for inputForward: " + inputForward);
			canMove = false;
		}
		if (canMove && !InputMap.HasAction(inputBack)) {
			GD.PushError("Movement disabled. No InputAction found for inputBack: " + inputBack);
			canMove = false;
		}
		if (canJump && !InputMap.HasAction(inputJump)) {
			GD.PushError("Jumping disabled. No InputAction found for inputJump: " + inputJump);
			canJump = false;
		}
		if (canSprint && !InputMap.HasAction(inputSprint)) {
			GD.PushError("Sprinting disabled. No InputAction found for inputSprint: " + inputSprint);
			canSprint = false;
		}
		if (canFreefly && !InputMap.HasAction(inputFreefly)) {
			GD.PushError("Freefly disabled. No InputAction found for inputFreefly: " + inputFreefly);
			canFreefly = false;
		}
	}
}
