using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
	#region Structs / Enums
	[Serializable]
	public struct MovementSettings
	{
		[Header("General")]
		[Range(0.1f, 10.0f)] public float gravityMultiplier;
		[Range(0.1f, 10.0f)] public float speed;
		[Range(0.1f, 10.0f)] public float jumpHeight;
		public Vector3 throwForce;

		[Header("Dash")]
		public AnimationCurve dashCurve;
		[Range(0.1f, 50.0f)] public float dashSpeedMultiplier;
		[Range(0.1f, 10.0f)] public float dashTimeMultiplier;
		[Range(0.1f, 10.0f)] public float dashRecoveryTime;
		public bool allowDashInAir;

		[Header("Input")]
		[Range(0.01f, 1.0f)] public float jumpEarlyTolerance;
		[Range(0.01f, 1.0f)] public float jumpLateTolerance;
		public bool allowHoldJump;
		public bool allowHoldDash;
	}

	[Serializable]
	public struct PlayerStats
	{
		public int playerIndex;
		public int robotModel;
		public int pancakesThrown;
		public int pancakesCollected;
		public int pancakesDropped;
		public int pancakesCaught;
		public int pancakesMost;
		public int playersHit;
		public int playersKilled;
		public int timesHit;
		public float survivedLength;

		public override string ToString()
		{
			// Calculate accuracy
			string accuracy;
			if (pancakesThrown == 0) accuracy = "NA";
			else
				accuracy = ((float)playersHit / (float)pancakesThrown) * 100.0f + "%";

			// Return string
			return pancakesThrown + "\n" +
				accuracy + "\n" +
				pancakesMost + "\n" +
				Mathf.Floor(survivedLength).ToString("#s");
		}
	}
	#endregion

	#region Variables / Properties
	#region Public
	// -- Properties --
	public bool IsGrounded { get; private set; }
	public bool IsDashing { get; private set; }
	public bool IsDashable { get; private set; }
	public bool LastGrounded { get; private set; }
	public bool LastDashing { get; private set; }
	public bool LastDashable { get; private set; }
	public bool Ready { get; set; }
	public int Ammo { get; private set; }
	public int ActiveRobotModel { get { return m_ActiveRobotModel; } }
	public string ActiveRobotColor { get => RobotModelToColor(m_ActiveRobotModel); }
	public float PancakeKnockbackAmount { get { return m_PancakeKnockbackAmount; } }
	public float DashKnockbackAmount { get { return m_DashKnockbackAmount; } }
	public float HazardKnockbackAmount { get { return m_HazardKnockbackAmount; } }
	public Vector3 Velocity { get { return m_Velocity; } }
	public PlayerStats Stats { get => m_Stats; }
	public Player HitBy { get => m_HitBy; set => m_HitBy = value; }
	public int TimesHit { get => m_Stats.timesHit; set => m_Stats.timesHit = value; }
	public int PlayersHit { get => m_Stats.playersHit; set => m_Stats.playersHit = value; }
	public int PlayersKilled { get => m_Stats.playersKilled; set => m_Stats.playersKilled = value; }
	public bool Active { get => m_Active; }
	public float DeathTimer { get => m_DeathTimer; }
	public bool OnEgg { get; private set; }

	public string HatName { get => (m_ActiveHatModel == -1) ? "" : m_Hats[m_ActiveHatModel].name; }
	#endregion

	#region Private
	// -- Editable in Inspector --
	[Header("Settings")]
	[SerializeField] MovementSettings m_MovementSettings = new MovementSettings();
	[Header("Prefab/Object References")]
	[SerializeField] GameObject m_Crown = null;
	[SerializeField] GameObject m_PancakePrefab = null;
	[SerializeField] GameObject m_AmmoPrefab = null;
	[SerializeField] Transform m_AmmoParent = null;
	[SerializeField] GameObject m_AmmoDribble = null;
	[SerializeField] GameObject m_AmmoTopping = null;
	[SerializeField] GameObject[] m_RobotModels = null;
	[SerializeField] GameObject[] m_Hats = null;
	[Header("Input Popup")]
	[SerializeField] GameObject m_InputPopup = null;
	[SerializeField] Sprite[] m_InputPopupSprites = null;
	[Header("Knockback")]
	[SerializeField] float m_Friction = 9.81f;
	[SerializeField] [Range(0.01f, 100.0f)] float m_PancakeKnockbackAmount = 12.0f;
	[SerializeField] [Range(0.01f, 100.0f)] float m_DashKnockbackAmount = 8.0f;
	[SerializeField] [Range(0.01f, 100.0f)] float m_HazardKnockbackAmount = 10.0f;
	[Header("Audio")]
	[SerializeField] AudioClip m_ThrowSound = null;
	[SerializeField] AudioClip m_HitSound = null;
	[SerializeField] AudioClip m_JumpSound = null;
	[SerializeField] AudioClip m_DashSound = null;
	[SerializeField] AudioClip m_DeathSound = null;
	[SerializeField] AudioClip m_LandSound = null;
	[SerializeField] AudioClip m_ReadySound = null;
	[SerializeField] AudioClip m_CatchSound = null;
	[SerializeField] AudioClip m_CollectSound = null;
	[SerializeField] AudioClip m_ForceHitSound = null;
	[SerializeField] string m_Input = "";

	// -- Cached Components
	private Transform m_Transform = null;
	private CharacterController m_CharController = null;
	private AudioSource m_AudioSource = null;
	private Animator m_Animator = null;
	private TrailRenderer m_TrailRenderer = null;

	// -- Input --
	private Vector2 m_InputMove = Vector2.zero;
	private Vector2 m_InputMoveDamp = Vector2.zero;
	private Vector2 m_InputMoveDampVelocity = Vector2.zero;
	private Vector2 m_InputLook = Vector2.zero;
	private bool m_InputJump = false;
	private bool m_InputDash = false;
	private bool m_InputThrow = false;

	// -- Misc. --
	private LinkedList<GameObject> m_AmmoObjList = new LinkedList<GameObject>();
	private LineRenderer m_ThrowLine = null;
	private int m_ActiveRobotModel = 0;
	private int m_ActiveHatModel = -1;
	private float m_Gravity = 0.0f;
	private float m_JumpEarlyToleranceTimer = 0.0f;
	private float m_JumpLateToleranceTimer = 0.0f;
	private bool m_JumpQueued = false;
	private float m_DashProgress = 0.0f;
	private float m_DashRecoveryTimer = 0.0f;
	private string m_OnMainMenuButtonName = "";
	[SerializeField] private PlayerStats m_Stats;
	private Player m_HitBy = null;
	private float m_HitByTimer = 0.0f;
	private bool m_Active = true;
	private Vector3 m_WinPosition = Vector3.zero;
	private Vector3 m_WinRotation = Vector3.zero;
	private float m_DeathTimer = 0.0f;

	// -- ReadOnly in Inspector --
	[Header("Info")]
	[SerializeField] [ReadOnlyInspector] Vector3 m_Velocity = Vector3.zero;
	#endregion
	#endregion

	#region Unity Functions
	/// <summary>
	/// Called on Awake.
	/// Caches components and initializes variables.
	/// </summary>
	void Awake()
	{
		// Cache components
		m_Transform = GetComponent<Transform>();
		m_CharController = GetComponent<CharacterController>();
		m_AudioSource = GetComponent<AudioSource>();
		m_ThrowLine = GetComponent<LineRenderer>();
		m_TrailRenderer = GetComponent<TrailRenderer>();

		// Intialize variables
		m_Gravity = Physics.gravity.y * m_MovementSettings.gravityMultiplier;
		Ammo = 0;
		PositionTopping();

		// Initialize Line Renderer color
		Gradient lineGradient = new Gradient();
		lineGradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.cyan, 0.0f), new GradientColorKey(Color.white, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
		);
		m_ThrowLine.colorGradient = lineGradient;
		m_ThrowLine.enabled = false;
		m_TrailRenderer.emitting = false;
	}

	/// <summary>
	/// Called on Update.
	/// Processes all player and camera movement.
	/// </summary>
	void Update()
	{
		m_Input = GetComponent<PlayerInput>().currentActionMap.name;

		if (!m_Active)
		{
			m_DeathTimer += Time.deltaTime;

			if (GameManager.Instance.State == GameManager.GameState.Game && m_Transform.position.y > -100.0f)
				m_Transform.Translate(0.0f, -Time.deltaTime * 1.25f, 0.0f);
			return;
		}

		#region Rotate towards camera on ready/finish
		if (GameManager.Instance.State == GameManager.GameState.MainMenu && Ready)
		{
			Vector3 fwd = (Camera.main.transform.position - transform.position).normalized;
			fwd = new Vector3(fwd.x, 0.0f, fwd.z);
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(fwd, Vector3.up), Time.deltaTime * 3.0f);
		}

		if (GameManager.Instance.State == GameManager.GameState.Finish)
		{
			if (m_TrailRenderer.emitting)
				m_TrailRenderer.emitting = false;

			transform.position = new Vector3(
				Mathf.Lerp(transform.position.x, m_WinPosition.x, Time.deltaTime * 0.95f),
				Mathf.Lerp(transform.position.y, m_WinPosition.y, Time.deltaTime * 0.20f),
				Mathf.Lerp(transform.position.z, m_WinPosition.z, Time.deltaTime * 0.50f)
				);

			Vector3 fwd = (Camera.main.transform.position - transform.position).normalized;
			fwd = new Vector3(fwd.x, 0.0f, fwd.z);
			Quaternion target = Quaternion.LookRotation(fwd, Vector3.up);
			target.eulerAngles = target.eulerAngles + m_WinRotation;
			transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * 1.0f);
			return;
		}
		#endregion

		#region States
		// Calculate hit by
		if (m_HitBy != null)
		{
			m_HitByTimer += Time.deltaTime;
			if (m_HitByTimer >= 3.0f)
			{
				m_HitByTimer = 0.0f;
				m_HitBy = null;
			}
		}

		// Store current 'Is' states as 'Last' states
		LastGrounded = IsGrounded;
		LastDashable = IsDashable;
		LastDashing = IsDashing;
		m_Stats.survivedLength += Time.deltaTime;

		// Update IsGrounded
		float castDistance = (m_CharController.height * 0.5f) - m_CharController.radius + 0.01f;
		RaycastHit groundHit;
		float slipperiness = 0.0f, speedMultiplier = 1.0f, jumpMultiplier = 1.0f;
		bool invertControls = false;
		bool wasFalling = !IsGrounded && m_Velocity.y <= -10.0f;
		IsGrounded = Physics.SphereCast(m_Transform.position, m_CharController.radius, Vector3.down, out groundHit, castDistance, int.MaxValue, QueryTriggerInteraction.Ignore);
		if (IsGrounded)
		{
			if (wasFalling)
			{
				m_AudioSource.PlayOneShot(m_LandSound);
				m_Animator.SetTrigger("HitFloor");
			}
			Transform hitTransform = groundHit.transform;
			if (hitTransform.gameObject.tag == "Syrup")
			{
				if (m_HitBy != null)
					m_HitBy.PlayersKilled++;
				Die();
				return;
			}
			else if (hitTransform.gameObject.GetComponent<Surface>() != null)
			{
				Surface surface = hitTransform.gameObject.GetComponent<Surface>();
				if (surface.DeathOnCollision)
				{
					Die();
					return;
				}
				if (surface.Bounciness > 0.0f && -m_Velocity.y >= surface.BounceMinVelocity)
				{
						m_Velocity = new Vector3(m_Velocity.x, surface.Bounciness * 50.0f, m_Velocity.z);
				}
				slipperiness = surface.Slipperiness;
				speedMultiplier = surface.SpeedMultiplier;
				jumpMultiplier = surface.JumpMultiplier;
				invertControls = surface.InvertControls;
			}
		}

		// Adjust movement according to surface
		float smoothTime = 0.05f + Mathf.Max(slipperiness * 2.5f, 0.0f);
		Vector2 targetVelocity = (slipperiness < 0.0f) ? m_InputMove / (1.0f + (slipperiness * -10.0f)) : m_InputMove;
		targetVelocity *= speedMultiplier;
		if (invertControls)
			targetVelocity = -targetVelocity;
		m_InputMoveDamp = Vector2.SmoothDamp(m_InputMoveDamp, targetVelocity, ref m_InputMoveDampVelocity, smoothTime, 15.0f);
		m_InputMoveDamp = Vector2.ClampMagnitude(m_InputMoveDamp, speedMultiplier);



		// Update IsDashable
		IsDashable = m_DashRecoveryTimer == 0.0f && (IsGrounded || m_MovementSettings.allowDashInAir);
		if (m_DashRecoveryTimer != 0.0f)
			m_DashRecoveryTimer = Mathf.Clamp(m_DashRecoveryTimer - Time.deltaTime, 0.0f, m_MovementSettings.dashRecoveryTime);

		// Update IsDashing
		if (IsDashing)
		{
			m_DashProgress += (Time.deltaTime / m_MovementSettings.dashTimeMultiplier);
			m_TrailRenderer.time = Mathf.Max(0.5f - (m_DashProgress * 0.5f), 0.0f);
			if (m_DashProgress > 1.0f)
			{
				IsDashing = false;
				m_DashProgress = 0.0f;
				m_TrailRenderer.emitting = false;
			}
		}
		#endregion

		#region Movement / Velocity
		#region Resolve gravity
		if (IsGrounded && m_Velocity.y < 0.0f)
			m_Velocity.y = 0.0f;
		else if (!IsGrounded)
			m_Velocity.y += m_Gravity * Time.deltaTime;
		#endregion

		#region Resolve lateral velocity
		if (m_Velocity.x != 0.0f || m_Velocity.z != 0.0f)
		{
			// Store current velocity then add friction
			Vector3 prevVelocity = m_Velocity;
			m_Velocity.x -= Sign(m_Velocity.x) * m_Friction * Time.deltaTime;
			m_Velocity.z -= Sign(m_Velocity.z) * m_Friction * Time.deltaTime;

			// Resolve whether to stop if velocity has passed zero
			if (Sign(m_Velocity.x) != Sign(prevVelocity.x))
				m_Velocity.x = 0.0f;
			if (Sign(m_Velocity.z) != Sign(prevVelocity.z))
				m_Velocity.z = 0.0f;
		}
		#endregion

		#region Manage jumping
		// Add jump to queue
		if (m_InputJump && !m_JumpQueued && m_MovementSettings.allowHoldJump)
			m_JumpQueued = true;

		// Progress tolerance timers and jump queues
		if (IsGrounded)
		{
			m_JumpLateToleranceTimer = 0.0f;
			m_JumpEarlyToleranceTimer = 0.0f;
		}
		else
		{
			m_JumpLateToleranceTimer += Time.deltaTime;
			if (m_JumpQueued)
			{
				m_JumpEarlyToleranceTimer += Time.deltaTime;
				if (m_JumpQueued && m_JumpEarlyToleranceTimer > m_MovementSettings.jumpEarlyTolerance)
					m_JumpQueued = false;
			}
		}

		// Perform jump
		if (m_JumpQueued && m_Velocity.y <= 1.5f &&
			m_JumpLateToleranceTimer < m_MovementSettings.jumpLateTolerance &&
			(m_JumpEarlyToleranceTimer < m_MovementSettings.jumpEarlyTolerance || m_MovementSettings.allowHoldJump))
		{
			float jumpHeight = m_MovementSettings.jumpHeight;
			if (slipperiness < 0)
				jumpHeight /= (1.0f + (slipperiness * -25.0f));
			jumpHeight *= jumpMultiplier;
			m_Velocity.y += Mathf.Sqrt(jumpHeight * -2.0f * m_Gravity);
			m_JumpLateToleranceTimer = m_MovementSettings.jumpLateTolerance;
			IsGrounded = false;
			m_JumpQueued = false;
			m_AudioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
			m_AudioSource.PlayOneShot(m_JumpSound);
			m_Animator.SetTrigger("Jump");
		}
		#endregion

		#region Manage dashing
		if (IsDashable && m_InputDash)
		{
			m_DashRecoveryTimer = m_MovementSettings.dashRecoveryTime;
			m_DashProgress = 0.0f;
			IsDashing = true;
			if (!m_MovementSettings.allowHoldDash)
				m_InputDash = false;
			m_AudioSource.PlayOneShot(m_DashSound);
			m_Animator.SetTrigger("Dash");
			m_TrailRenderer.emitting = true;
			m_TrailRenderer.time = 0.5f;
		}
		#endregion

		#region Manage movement
		// Get normalized direction virtual camera is facing
		Vector3 camForward, camRight;
		if (GameManager.Instance != null)
		{
			camForward = GameManager.Instance.ActiveVCam.transform.forward;
			camRight = GameManager.Instance.ActiveVCam.transform.right;
		}
		else
		{
			camForward = SandboxManager.Instance.VirtualCam.transform.forward;
			camRight = SandboxManager.Instance.VirtualCam.transform.right;
		}
		camForward.y = 0.0f;
		camRight.y = 0.0f;
		camForward.Normalize();
		camRight.Normalize();

		// Set aiming direction based on look or move aim
		Vector2 lookInput = (m_InputLook != Vector2.zero) ? m_InputLook : (m_InputMove != Vector2.zero) ? m_InputMove : Vector2.zero;
		if (lookInput != Vector2.zero)
		{
			Vector3 lookDirection = ((camForward * lookInput.y) + (camRight * lookInput.x)).normalized;
			m_Transform.forward = Vector3.Lerp(m_Transform.forward, lookDirection, Time.deltaTime * 40.0f);
		}

		// Set movement vector independantly of aim direction
		Vector3 moveDirection = (camForward * m_InputMoveDamp.y + camRight * m_InputMoveDamp.x).normalized;
		Vector3 moveVec = moveDirection * (m_InputMoveDamp.magnitude * m_MovementSettings.speed);

		// Add dashing velocity
		
		if (IsDashing)
		{
			float dashSpeedMultiplier = m_MovementSettings.dashSpeedMultiplier;
			if (speedMultiplier > 1.0f)
				dashSpeedMultiplier *= speedMultiplier;
			if (slipperiness < 0.0f)
				dashSpeedMultiplier /= (1.0f + (slipperiness * -5.0f));
			moveVec += m_Transform.forward * m_MovementSettings.dashCurve.Evaluate(m_DashProgress) * dashSpeedMultiplier;
		}

		// Adjust for downwards slope if grounded and not dashing
		else
		{
			Vector3 newMoveVec = Quaternion.FromToRotation(Vector3.up, groundHit.normal) * moveVec;
			if (newMoveVec.y < 0)
				moveVec = newMoveVec;
		}

		// Add velocity and slight downwards force so Move always does something (fixes RB collisions)
		moveVec += m_Velocity;
		moveVec = new Vector3(moveVec.x, moveVec.y - Time.deltaTime, moveVec.z);

		// Perform movement
		m_CharController.Move(moveVec * Time.deltaTime);

		if (IsGrounded && !IsDashing)
		{
			Vector2 localMoveVec = new Vector2(m_Transform.InverseTransformDirection(moveVec).x, m_Transform.InverseTransformDirection(moveVec).z);
			localMoveVec = Vector2.ClampMagnitude(localMoveVec, 1.0f);
			m_Animator.SetFloat("X", localMoveVec.x);
			m_Animator.SetFloat("Y", localMoveVec.y);
		}
		m_Animator.SetFloat("Velocity", moveVec.magnitude / m_MovementSettings.speed);
		#endregion
		#endregion

		#region Input Popup
		// (m_InputPopup.activeInHierarchy)
			//m_InputPopup.transform.LookAt(2 * m_InputPopup.transform.position - GameManager.Instance.ActiveVCam.transform.position);
		#endregion
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (IsDashing && hit.gameObject.tag == "Player")
		{
			// Calculate force to hit player with
			Player player = hit.gameObject.GetComponent<Player>();
			Vector3 force = m_CharController.velocity;

			// Return or normalize force depending on impact velocity
			if (force.magnitude < 10.0f || force.magnitude > 60.0f)
				return;
			else
				force = player.DashKnockbackAmount * force.normalized;

			// Perform hit and add player to no hit list temporarily
			player.PlayHitSound(0.25f);
			Debug.Log("Hitting player: " + force.magnitude);
			player.AddForce(force, true);
			IsDashing = false;
			m_TrailRenderer.emitting = false;
			m_TrailRenderer.time = 0.0f;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (IsDashing && (collision.gameObject.name == "Bomb Drop" || collision.gameObject.name == "Bomb Drop(Clone)"))
		{
			// Calculate force to hit player with
			Rigidbody rigidbody = collision.gameObject.GetComponent<Rigidbody>();
			Vector3 force = m_CharController.velocity;

			// Return or normalize force depending on impact velocity
			if (force.magnitude < 10.0f || force.magnitude > 60.0f)
				return;
			else
				force = 125.0f * force.normalized;

			// Perform hit and add player to no hit list temporarily
			PlayHitSound(0.25f);
			rigidbody.AddForce(force);
			IsDashing = false;
			m_TrailRenderer.emitting = false;
			m_TrailRenderer.time = 0.0f;
		}
	}

	/// <summary>
	/// Called on TriggerEnter.
	/// Stores if the player is on a Main Menu button.
	/// </summary>
	/// <param name="other"></param>
	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Menu Button")
		{
			m_OnMainMenuButtonName = other.transform.root.name;
			//m_InputPopup.SetActive(true);
			//m_InputPopup.GetComponent<SpriteRenderer>().sprite = m_InputPopupSprites[3];
			MenuManager.Instance.PlayerOnButton(other.gameObject);
		}

		if (other.gameObject.tag == "Egg")
		{
			GameManager.Instance.SetEggTarget(true);
			other.transform.root.GetComponent<EggChunk>().OnActivate();
			OnEgg = true;
		}
	}

	/// <summary>
	/// Calculates Pancake Drop pickups
	/// </summary>
	/// <param name="other"></param>
	void OnTriggerStay(Collider other)
	{
		if (other.gameObject.tag != "Drop")
			return;

		if (other.transform.root.name == "Pancake Drop" || other.transform.root.name == "Pancake Drop(Clone)")
		{
			// Return if pancake going too fast or is thrown
			if (other.transform.root.GetComponent<Rigidbody>().velocity.magnitude >= 20.0f ||
				other.transform.root.GetComponent<PancakeDrop>().ThrownBy != null)
				return;

			// Add ammo and stat
			Ammo++;
			m_Stats.pancakesCollected++;
			if (Ammo > m_Stats.pancakesMost)
				m_Stats.pancakesMost = Ammo;
			m_AudioSource.PlayOneShot(m_CollectSound);
			PositionTopping();

			// Instantiate ammo prefab
			m_AmmoObjList.AddLast(Instantiate(m_AmmoPrefab, m_AmmoParent, false));
			m_AmmoObjList.Last.Value.transform.localPosition = new Vector3(
				UnityEngine.Random.Range(-0.035f, 0.035f), 0.125f * Ammo, UnityEngine.Random.Range(-0.035f, 0.035f));

			// Pass color from drop to ammo object and disable
			m_AmmoObjList.Last.Value.GetComponentInChildren<Renderer>().material.color = other.GetComponentInChildren<Renderer>().material.color;
			other.transform.root.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Called on TriggerExit.
	/// Stores if the player is on a Main Menu button.
	/// </summary>
	/// <param name="other"></param>
	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "Menu Button")
		{
			if (m_OnMainMenuButtonName == "Start Button")
				m_Animator.SetTrigger("NotReady");

			m_OnMainMenuButtonName = "";
			//m_InputPopup.SetActive(false);
			MenuManager.Instance.PlayerOffButton(other.gameObject);
			if (Ready)
			{
				MenuManager.Instance.PlayerChangeReady(false);
				Ready = false;
			}
			GetComponent<PlayerInput>().SwitchCurrentActionMap("PlayerControls");
		}
	}
	#endregion

	#region Input Actions
	/// <summary>
	/// Called on Player invoking the 'Move' PlayerAction.
	/// Stores the movement vector in a private variable.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnMoveInput(InputAction.CallbackContext value)
	{
		m_InputMove = Vector2.ClampMagnitude(value.ReadValue<Vector2>(), 1.0f);
	}

	/// <summary>
	/// Player invokes the 'Look' PlayerAction
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnLookInput(InputAction.CallbackContext value)
	{
		m_InputLook = Vector2.ClampMagnitude(value.ReadValue<Vector2>(), 1.0f);
	}

	/// <summary>
	/// Called on Player invoking the 'Jump' PlayerAction.
	/// Stores button held state in a private variable, and queues a jump as required.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnJumpInput(InputAction.CallbackContext value)
	{
		// Store button held state
		m_InputJump = (value.started ? true : value.canceled ? false : m_InputJump);

		// On pressed, queue jump
		if (value.started)
		{
			m_JumpEarlyToleranceTimer = 0.0f;
			if (!m_JumpQueued)
				m_JumpQueued = true;	
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Throw' PlayerAction.
	/// Stores button held state in a private variable.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnThrowInput(InputAction.CallbackContext value)
	{
		// Store button held state
		m_InputThrow = (value.started ? true : value.canceled ? false : m_InputThrow);

		// On released, throw pancake
		if (value.started)
		{
			m_ThrowLine.enabled = true;
			if (Ammo > 0)
				m_Animator.SetTrigger("PreThrow");
		}
		if (value.canceled)
		{
			m_ThrowLine.enabled = false;
			ThrowPancake();
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Dash' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnDashInput(InputAction.CallbackContext value)
	{
		m_InputDash = (value.started ? true : value.canceled ? false : m_InputDash);
	}

	/// <summary>
	/// Called on Player invoking the 'Change' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnChangeInput(InputAction.CallbackContext value)
	{
		if (GameManager.Instance.State != GameManager.GameState.MainMenu)
			return;

		if (value.started)
		{
			Vector3 input = value.ReadValue<Vector2>();
			if (input.x != 0)
			{
				int targetModel = m_ActiveRobotModel;
				if (input.x > 0)
					targetModel++;
				else if (input.x < 0)
					targetModel--;

				if (targetModel < 0)
					targetModel = m_RobotModels.Length + targetModel;

				SetRobotModel(targetModel % m_RobotModels.Length);
			}

			if (input.y != 0)
			{
				int targetHat = m_ActiveHatModel;
				if (input.y > 0)
					targetHat++;
				else if (input.y < 0)
					targetHat--;

				if (targetHat < -1)
					targetHat = m_Hats.Length + targetHat + 1;
				else if (targetHat >= m_Hats.Length)
					targetHat = m_Hats.Length - targetHat - 1;

				SetHatModel(targetHat);
			}
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Select' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnSelectInput(InputAction.CallbackContext value)
	{
		Debug.Log("Select!");
		InputActionMap currentMap = GetComponent<PlayerInput>().currentActionMap;
		if (value.started)
		{
			switch (m_OnMainMenuButtonName)
			{
				case "Start Button":
					if (!Ready && currentMap.name == "PlayerControls")
					{
						Ready = true;
						MenuManager.Instance.PlayerChangeReady(true);
						m_AudioSource.PlayOneShot(m_ReadySound);
						GetComponent<PlayerInput>().SwitchCurrentActionMap("ReadyControls");
						GameManager.Instance.OnPlayerReady(this);
						//m_InputPopup.SetActive(false);
					}
					else if (currentMap.name == "ReadyControls")
					{
						Ready = false;
						MenuManager.Instance.PlayerChangeReady(false);
						GetComponent<PlayerInput>().SwitchCurrentActionMap("PlayerControls");
						//m_InputPopup.SetActive(true);
						m_Animator.SetTrigger("NotReady");
					}
					break;
				case "Options Button":
					if (currentMap.name != "PlayerControls")
						return;

					MenuManager.Instance.ShowOptionsMenu();
					GameManager.Instance.SwitchActionMap("MenuControls");
					break;
				case "Quit Button":
					if (currentMap.name != "PlayerControls")
						return;

					GameManager.Instance.QuitToWindows();
					break;
			}

			if (GameManager.Instance.State == GameManager.GameState.Finish)
				MenuManager.Instance.StatsUI.OnPlayerInput(m_Stats.playerIndex, 1);
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Back' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnBackInput(InputAction.CallbackContext value)
	{
		if (value.started)
		{
			if (m_OnMainMenuButtonName == "Start Button" && Ready)
			{
				Ready = false;
				GetComponent<PlayerInput>().SwitchCurrentActionMap("PlayerControls");
				//m_InputPopup.SetActive(true);
				m_Animator.SetTrigger("NotReady");
				MenuManager.Instance.PlayerChangeReady(false);
			}
			if (GameManager.Instance.State == GameManager.GameState.Finish)
				MenuManager.Instance.StatsUI.OnPlayerInput(m_Stats.playerIndex, -1);
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Alternate' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnAlternateInput(InputAction.CallbackContext value)
	{
		if (value.started)
		{
			if (GameManager.Instance.State == GameManager.GameState.Finish)
				MenuManager.Instance.StatsUI.OnPlayerInput(m_Stats.playerIndex, 0);
		}
	}

	/// <summary>
	/// Called on Player invoking the 'Pause' PlayerAction.
	/// </summary>
	/// <param name="value">Information returned on that action by the Input System</param>
	public void OnPauseInput(InputAction.CallbackContext value)
	{
		if (value.started)
		{
			MenuManager.Instance.ShowPauseMenu();
			GameManager.Instance.SwitchActionMap("MenuControls");
		}
	}
	#endregion

	#region Public Functions
	public static string RobotModelToColor(int index)
	{
		switch (index)
		{
			case 0: return "RED";
			case 1: return "GREEN";
			case 2: return "PURPLE";
			case 3: return "WHITE";
			case 4: return "PINK";
			case 5: return "BLUE";
			case 6: return "YELLOW";
			case 7: return "BLACK";
			default: return "NONE";
		}
	}

	public static int RobotColorToModel(string color)
	{
		switch (color)
		{
			case "RED": return 0;
			case "GREEN": return 1;
			case "PURPLE": return 2;
			case "WHITE": return 3;
			case "PINK": return 4;
			case "BLUE": return 5;
			case "YELLOW": return 6;
			case "BLACK": return 7;
			default: return -1;
		}
	}

	public void ThrowPancake()
	{
		// Early exit if no prefab assigned or out of ammo
		if (m_PancakePrefab == null)
		{
			Debug.Log("Unable to throw pancake - no prefab found");
			return;
		}
		if (Ammo <= 0)
			return;

		// Instantiate prefab with force
		GameObject spawnedPancake = Instantiate(m_PancakePrefab);
		spawnedPancake.transform.position = m_Transform.position + (m_Transform.forward) + (m_Transform.up * -0.45f);
		Vector3 force = (m_Transform.forward * m_MovementSettings.throwForce.z) + (m_Transform.up * m_MovementSettings.throwForce.y) + (m_Transform.right * m_MovementSettings.throwForce.x);
		spawnedPancake.GetComponent<Rigidbody>().AddForce(force);
		spawnedPancake.GetComponent<PancakeDrop>().ThrownBy = this;

		// Play sound and adjust stats
		m_AudioSource.PlayOneShot(m_ThrowSound);
		m_AudioSource.PlayOneShot(m_LandSound);
		m_Animator.SetTrigger("Throw");
		Ammo--;
		m_Stats.pancakesThrown++;
		PositionTopping();

		// Remove pancake from stack
		Destroy(m_AmmoObjList.Last.Value);
		m_AmmoObjList.RemoveLast();
	}

	public void DropPancakes(int count)
	{
		// Early-exit conditions
		if (count <= 0)
			return;

		// Remove ammo from ammo count
		if (count > Ammo) count = Ammo;
		Ammo -= count;
		m_Stats.pancakesDropped += count;
		PositionTopping();

		// Remove from stack and instantiate drop
		for (int i = 0; i < count; i++)
		{
			m_AmmoObjList.Last.Value.SetActive(false);
			Destroy(m_AmmoObjList.Last.Value);
			m_AmmoObjList.RemoveLast();

			GameObject spawnedPancake = Instantiate(m_PancakePrefab);
			spawnedPancake.transform.Rotate(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f);
			spawnedPancake.transform.position = m_Transform.position + spawnedPancake.transform.forward + (m_Transform.up * -0.45f);
			Vector3 force = (spawnedPancake.transform.forward * UnityEngine.Random.Range(5.0f, 15.0f)) + (m_Transform.up * UnityEngine.Random.Range(75.0f, 125.0f));
			spawnedPancake.GetComponent<Rigidbody>().AddForce(force);
		}
	}

	public void CatchPancake(GameObject pancake)
	{
		// Add ammo and stat
		Ammo++;
		m_Stats.pancakesCaught++;
		if (Ammo > m_Stats.pancakesMost)
			m_Stats.pancakesMost = Ammo;
		m_AudioSource.PlayOneShot(m_CatchSound);
		PositionTopping();

		// Instantiate ammo prefab
		m_AmmoObjList.AddLast(Instantiate(m_AmmoPrefab, m_AmmoParent, false));
		m_AmmoObjList.Last.Value.transform.localPosition = new Vector3(
			UnityEngine.Random.Range(-0.035f, 0.035f), 0.125f * Ammo, UnityEngine.Random.Range(-0.035f, 0.035f));

		// Pass color from drop to ammo object and disable
		m_AmmoObjList.Last.Value.GetComponentInChildren<Renderer>().material.color = pancake.GetComponentInChildren<Renderer>().material.color;
		pancake.transform.root.gameObject.SetActive(false);
	}

	/// <summary>
	/// Adds the specified amount of force as velocity to the player.
	/// </summary>
	/// <param name="force">The force to add in each direction</param>
	public void AddForce(Vector3 force, bool playHitAnimation = false)
	{
		m_Velocity += force;
		if (playHitAnimation) m_Animator.SetTrigger("Hit");
	}

	/// <summary>
	/// Plays the 'Hit' sound effect.
	/// </summary>
	public void PlayHitSound(float volume = 1.0f)
	{
		m_AudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		m_AudioSource.PlayOneShot(m_HitSound, volume);
	}

	/// <summary>
	/// Plays the 'ForceHit' sound effect.
	/// </summary>
	public void PlayForceHitSound(float volume = 1.0f)
	{
		m_AudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		m_AudioSource.PlayOneShot(m_ForceHitSound, volume);
	}
	#endregion

	public void SetRobotModel(int index)
	{
		if (index < 0 || index >= m_RobotModels.Length)
		{
			Debug.Log("Unable to set Robot model - index " + index + " is out of range.");
			return;
		}

		for (int i = 0; i < m_RobotModels.Length; i++)
		{
			m_RobotModels[i].SetActive(i == index);
		}

		m_ActiveRobotModel = index;
		m_Stats.robotModel = index;
		m_Animator = m_RobotModels[index].GetComponent<Animator>();
		
		for (int i = 0; i < m_Hats.Length; i++)
		{
			m_Hats[i].transform.parent = m_RobotModels[m_ActiveRobotModel].GetComponentInChildren<PlayerHead>().transform;
			m_Hats[i].transform.localPosition = new Vector3(0.0f, -0.455f, 0.0025f);
			m_Hats[i].transform.localRotation = Quaternion.identity;
		}

		m_Crown.transform.parent = m_RobotModels[m_ActiveRobotModel].GetComponentInChildren<PlayerHead>().transform;
		m_Crown.transform.localPosition = new Vector3(0.0f, -0.455f, 0.0025f);
		m_Crown.transform.localRotation = Quaternion.identity;

		m_AmmoParent.parent = m_RobotModels[m_ActiveRobotModel].GetComponentInChildren<PlayerHand>().transform;
		m_AmmoParent.localPosition = new Vector3(-0.08f, -0.29f, 0.14f);
		m_AmmoParent.localEulerAngles = new Vector3(0.0f, 4.5f, 90.0f);
	}

	public void SetHatModel(int index)
	{
		if (index < -1 || index >= m_Hats.Length)
		{
			Debug.Log("Unable to set Hat model - index " + index + " is out of range.");
			return;
		}

		for (int i = 0; i < m_Hats.Length; i++)
		{
			m_Hats[i].SetActive(i == index);
		}
		m_ActiveHatModel = index;
	}

	public void SetCrownActive(bool enabled)
	{
		m_Crown.SetActive(enabled);
		
		if (m_ActiveHatModel >= 0)
			m_Hats[m_ActiveHatModel].SetActive(!enabled);
	}

	/// <summary>
	/// Destroys all pancakes in the player's ammo.
	/// </summary>
	public void ClearAmmo()
	{
		foreach (GameObject pancake in m_AmmoObjList)
			Destroy(pancake);
		m_AmmoObjList.Clear();
		Ammo = 0;
		PositionTopping();
	}

	/// <summary>
	/// Resets player's ammo, ready state, and various other variables.
	/// </summary>
	public void Reset(int playerIndex)
	{
		ClearAmmo();
		Ready = false;
		GetComponent<PlayerInput>().SwitchCurrentActionMap("PlayerControls");
		m_OnMainMenuButtonName = "";
		m_HitBy = null;
		m_Stats = default;
		m_Stats.robotModel = m_ActiveRobotModel;
		m_Stats.playerIndex = playerIndex;
		m_DeathTimer = 0.0f;

		m_Animator.SetTrigger("NotReady");
		m_Animator.SetTrigger("WinComplete");

		m_InputMove = Vector2.zero;
		m_InputMoveDamp = Vector2.zero;
		m_InputMoveDampVelocity = Vector2.zero;
		m_InputLook = Vector2.zero;
		m_InputJump = false;
		m_InputDash = false;
		m_InputThrow = false;
		m_JumpQueued = false;
		m_DashProgress = 0.0f;
		m_DashRecoveryTimer = 0.0f;

		IsGrounded = false;
		IsDashing = false;
		IsDashable = false;
		LastGrounded = false;
		LastDashing = false;
		LastDashable = false;
		Ammo = 0;

		m_HitBy = null;
		m_HitByTimer = 0.0f;
		m_WinPosition = Vector3.zero;
		m_WinRotation = Vector3.zero;
		m_DeathTimer = 0.0f;
		m_TrailRenderer.emitting = false;
		m_Velocity = Vector3.zero;
	}

	public void SetPlayerActive(bool enabled)
	{
		m_Active = enabled;
		if (!enabled)
		{
			m_CharController.enabled = false;
			//m_Transform.position = new Vector3(0.0f, -100.0f, 0.0f);
			m_Velocity = Vector3.zero;
			m_TrailRenderer.emitting = false;
			m_Animator.SetTrigger("Drown");
		}

	}

	public void SetWinPosition(Vector3 pos, Vector3 rot)
	{
		m_WinPosition = pos;
		m_WinRotation = rot;
		m_TrailRenderer.emitting = false;
	}

	public void SetWinPose()
	{
		m_Animator.SetFloat("X", 0.0f);
		m_Animator.SetFloat("Y", 0.0f);
		m_Animator.StopPlayback();
		m_Animator.SetTrigger("Win");
		m_Velocity = Vector3.zero;
	}

	public void PlayReadyAnimation()
	{
		m_Animator.SetTrigger("Ready");
	}

	#region Private Functions
	/// <summary>
	/// Returns the sign of the specified float (-1, 0, or 1).
	/// </summary>
	/// <param name="value">Float to return the sign of</param>
	/// <returns>The sign of the specified float (-1, 0, or 1)</returns>
	private float Sign(float value)
	{
		return value < 0.0f ? -1.0f : value > 0.0f ? 1.0f : 0.0f;
	}

	private void PositionTopping()
	{
		float yPos = (Ammo == 0) ? 0.0f : 0.125f * (Ammo + 0.5f);
		float yScale = (Ammo == 0) ? 0.0f : Mathf.Min(0.075f * Ammo, 2.0f);
		m_AmmoDribble.transform.localPosition = new Vector3(0.0f, yPos, 0.0f);
		m_AmmoTopping.transform.localPosition = new Vector3(0.0f, yPos, 0.0f);
		m_AmmoDribble.transform.localScale = new Vector3(0.6f, yScale, 0.6f);
	}
	
	private void Die()
	{
		m_AudioSource.PlayOneShot(m_DeathSound);
		GameManager.Instance.OnPlayerDeath(gameObject);
		m_Animator.SetFloat("X", 0.0f);
		m_Animator.SetFloat("Y", 0.0f);
		m_Animator.StopPlayback();
		m_TrailRenderer.emitting = false;
		if (OnEgg)
			GameManager.Instance.SetEggTarget(false);
	}
	#endregion
}
