using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Author: Cameron Bennetts
 * My custom Verlet Rope class. Several sections in here are left WIP, and are
 * documented as such.
 */
[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
	#region Variables/Properties
	// -- Public --
	[Header("Design")]
	[Min(2)] public int nodeCount = 15;
	[Min(0.01f)] public float ropeLength = 1.0f;

	[Header("Position")]
	public Transform pointA;
	public Transform pointB;
	public bool lockPointA = true;
	public bool lockPointB = true;

	[Header("Attachments")]
	public bool lerpAttachPoint = false;
	public bool detachOnBreak = true;
	public List<Attachment> attachmentList = new List<Attachment>();

	[Header("Simulation")]
	[Min(1)] public int constraintIterations = 50;
	public float gravityMultiplier = 0.15f;
	public Vector3 ropeForce = new Vector3(0.0f, 0.0f, 0.0f);
	public bool allowTearing = true;
	[Min(0.01f)] public float tearTension = 0.5f;

	[Header("Rigidbody Simulation")]
	public bool pullRigidbodies = false;
	public Rigidbody pointARigidbody;
	public Rigidbody pointBRigidbody;
	[Min(0.01f)] public float tensionThreshold = 0.1f;
	[Min(0.01f)] public float pullForce = 100.0f;

	[Header("Collisions")]
	public SphereCollider nodeCollider;
	public bool checkCollisions = true;
	[Range(-1.0f, 1.0f)] public float collisionPadding = 1.0f;

	[Header("Debugging")]
	public bool reportTension = false;
	public bool drawSegments = false;
	public bool drawColliders = false;

	[Header("Rendering")]
	[Range(0.0f, 5.0f)] public float ropeWidth = 0.1f;
	[Range(0, 4)] public int smoothingIterations = 0;

	[Header("Spicy Rendering")]
	public bool drawMesh = false;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;
	[Range(2, 20)] public int ropeSides = 8;

	// -- Private --
	private LineRenderer lineRenderer;
	private Mesh mesh;
	private List<VerletNode> nodeList = new List<VerletNode>();
	private float segmentLength = 1.0f;
	private float collisionRadius = 1.0f;
	private float tearTimer = 0.0f;
	#endregion

	#region Unity Functions
	/// <summary>
	/// Initialize rope with a list of nodes, and perform error-checking, etc.
	/// </summary>
	void Start()
	{
		// Initialize mesh and line renderer components
		mesh = new Mesh();
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.startWidth = ropeWidth;
		lineRenderer.endWidth = ropeWidth;

		// Early-exit if node list has already been created
		if (nodeList.Count >= 2)
			return;

		#region Error-checking
		// Ensure that only PointA or PointA & PointB are assigned, destroying self if invalidly assigned
		if (pointA == null && pointB == null)
		{
			pointA = transform;
			print("Verlet Rope: No Transform points assigned - assigned as self.");
		}
		else if (pointA == null)
		{
			pointA = pointB;
			pointB = null;
			print("Verlet Rope: 'Point A' undefined - replaced with value from 'Point B'.");
		}

		// Ensure points aren't locked if unassigned
		if (pointA == null && lockPointA)
		{
			lockPointA = false;
			print("Verlet Rope: 'Lock Point A' disabled, as 'Point A' is undefined.");
		}
		if (pointB == null && lockPointB)
		{
			lockPointB = false;
			print("Verlet Rope: 'Lock Point B' disabled, as 'Point B' is undefined.");
		}

		// Disable collisions if required
		if (checkCollisions && nodeCollider == null)
		{
			checkCollisions = false;
			print("Verlet Rope: 'Node Collider' undefined - disabled collision checking.");
		}
		#endregion

		// Create node list
		nodeList = new List<VerletNode>();

		// Evenly distribute segments from start to end position, or straight down if only one is defined
		segmentLength = ropeLength / nodeCount;
		for (int i = 0; i < nodeCount; i++)
		{
			Vector3 nodePosition = Vector3.zero;
			if (pointB == null)
				nodePosition = pointA.position + (Vector3.down * segmentLength * i);
			else
				nodePosition = Vector3.Lerp(pointA.position, pointB.position, i / (nodeCount - 1.0f));

			nodeList.Add(new VerletNode(nodePosition));
		}
	}

	/// <summary>
	/// Draw the rope each Update using LineRenderer or MeshRenderer.
	/// </summary>
	void Update()
	{
		// Draw rope using Mesh Renderer
		if (drawMesh)
		{
			// Error-checking
			if (meshFilter.mesh != mesh)
			{
				meshFilter.mesh = mesh;
			}
			if (meshRenderer == null)
			{
				print("Verlet Rope: No Mesh Renderer component assigned - Draw Mesh disabled.");
				drawMesh = false;
			}

			// Ensure correct renderer is enabled
			if (!meshRenderer.enabled)
				meshRenderer.enabled = true;
			if (lineRenderer.enabled)
				lineRenderer.enabled = false;

			// Draw mesh
			DrawMesh();
		}

		// Draw rope using Line Renderer
		else
		{
			// Ensure correct renderer is enabled
			if (!lineRenderer.enabled)
				lineRenderer.enabled = true;
			if (meshRenderer != null && meshRenderer.enabled)
				meshRenderer.enabled = false;

			// Draw line
			DrawLine();
		}

		// Process tear timer (prevents rope from tearing twice too fast)
		if (tearTimer > 0.0f)
			tearTimer = Mathf.Max(tearTimer - Time.deltaTime, 0.0f);
	}

	/// <summary>
	/// Simulate the rope each FixedUpdate.
	/// </summary>
	void FixedUpdate()
	{
		// Ensure segment length and node count are updated, redistributing if required
		segmentLength = ropeLength / nodeCount;
		if (nodeCount != nodeList.Count)
			RedistributeNodes();

		// Update collision size
		float ropeRadius = ropeWidth * 0.5f;
		collisionRadius = ropeRadius + (ropeRadius * collisionPadding);
		if (nodeCollider != null)
			nodeCollider.radius = collisionRadius;

		// Simulate physics and tearing, then update attachments
		SimulatePhysics();
		if (allowTearing)
			CheckTear();
		UpdateAttachments();

		// Update Rigidbody pulling if enabled
		if (pullRigidbodies)
			UpdateRigidbodies();

		// Log rope tension if required (for debugging purposes)
		if (reportTension)
		{
			List<float> ropeTension = CalculateTensions();
			string minTension = System.Linq.Enumerable.Min(ropeTension).ToString("0.000");
			string averageTension = System.Linq.Enumerable.Average(ropeTension).ToString("0.000");
			string maxTension = System.Linq.Enumerable.Max(ropeTension).ToString("0.000");

			Debug.Log("Verlet Rope: Tension - " +
				minTension + " min, " +
				averageTension + " avg, " +
				maxTension + " max");
		}
	}
	#endregion

	#region Editor Functions
#if UNITY_EDITOR
	/// <summary>
	/// Draws spheres at each node position.
	/// </summary>
	void OnDrawGizmos()
	{
		if (!drawSegments && !drawColliders)
			return;

		if (UnityEditor.Selection.Contains(gameObject))
			Gizmos.color = Color.yellow;
		else
			Gizmos.color = Color.white;

		// Draw a yellow sphere at the transform's position
		if (drawSegments)
		{
			float sphereRadius = Mathf.Clamp(ropeWidth * 0.75f, 0.15f, (segmentLength / 2.0f) - 0.05f);
			for (int i = 0; i < nodeList.Count; i++)
				Gizmos.DrawWireSphere(nodeList[i].position, sphereRadius);
		}
		if (drawColliders)
		{
			for (int i = 0; i < nodeList.Count; i++)
				Gizmos.DrawWireSphere(nodeList[i].position, collisionRadius);
		}
	}
#endif
	#endregion

	#region Private Functions
	// -- Physics Simulation --
	/// <summary>
	/// Performs rope simulation using Verlet physics.
	/// </summary>
	private void SimulatePhysics()
	{
		for (int i = 0; i < nodeList.Count; i++)
		{
			// Update velocity of each node using Verlet physics
			VerletNode node = nodeList[i];
			Vector3 velocity = node.position - node.prevPosition;

			node.prevPosition = node.position;
			node.position += velocity;
			node.position += (Physics.gravity * gravityMultiplier) * Time.fixedDeltaTime;
			node.position += ropeForce * Time.fixedDeltaTime;

			// Update the position of each attachment
			// Note: not quite finished, as this probably isn't the most efficient way to do this,
			// and the ability to the rope to be affected by the attachment is WIP.
			foreach (Attachment attachment in attachmentList)
			{
				if (attachment.attachTransform == null)
					continue;

				int nodeIndex = Mathf.RoundToInt((nodeList.Count - 1) * attachment.attachPoint);
				if (nodeIndex == i)
					node.position = attachment.attachTransform.position;

				// WIP:
				//if (nodeIndex == i)
				//	node.position += (gravityForce * attachment.mass) * Time.fixedDeltaTime;
			}
		}

		// Apply constraints and resolve collisions
		// Note: Performing both constraint and collision resolution several times each frame is
		// unnecessary, but rewriting the collision system to accomodate would take too long.
		for (int i = 0; i < constraintIterations; i++)
		{
			ResolveConstraints();

			if (checkCollisions)
				ResolveCollisions();
		}
	}

	/// <summary>
	/// Constrains nodes according to segment length and locking.
	/// </summary>
	private void ResolveConstraints()
	{
		// Clone node list, ensuring each iteration isn't affected by the prior one
		List<VerletNode> nodeListCloned = new List<VerletNode>();
		foreach (VerletNode node in nodeList)
			nodeListCloned.Add(new VerletNode(node.position, node.prevPosition));

		// Iterate through each node, constraining their distance to target the segment length
		for (int i = 0; i < nodeList.Count - 1; i++)
		{
			VerletNode nodeA = nodeListCloned[i];
			VerletNode nodeB = nodeListCloned[i + 1];

			Vector3 difference = nodeA.position - nodeB.position;
			float distance = difference.magnitude;
			float delta = (distance > 0.0f) ? (segmentLength - distance) / distance : 0.0f;

			Vector3 translation = difference * (delta * 0.5f);
			nodeList[i].position += translation;
			nodeList[i + 1].position -= translation;
		}

		// Lock start and end points if required
		if (lockPointA && pointA != null)
			nodeList[0].position = pointA.position;
		if (lockPointB && pointB != null)
			nodeList[nodeList.Count - 1].position = pointB.position;
	}

	/// <summary>
	/// Resolves collision checks per node.
	/// </summary>
	private void ResolveCollisions()
	{
		// Early-exit if node collider is null
		if (nodeCollider == null)
			return;

		// Iterate through each node and offset its position by its collision penetration
		int minI = (pointA != null && lockPointA == true) ? 1 : 0;
		int maxI = (pointB != null && lockPointB == true) ? nodeList.Count : nodeList.Count - 1;
		for (int i = minI; i < maxI; i++)
		{
			Collider[] colliders = Physics.OverlapSphere(nodeList[i].position, ropeWidth);

			foreach (Collider collider in colliders)
			{
				if (collider.isTrigger)
					continue;

				Vector3 direction = Vector3.zero;
				float distance = 0.0f;
				if (Physics.ComputePenetration(nodeCollider, nodeList[i].position, Quaternion.identity,
					collider, collider.transform.position, collider.transform.rotation,
					out direction, out distance))
					nodeList[i].position += (direction * distance);
			}
		}
	}

	/// <summary>
	/// Updates the node list to include the target node count,
	/// redistributing the new node list to follow the shape of the current node list.
	/// </summary>
	private void RedistributeNodes()
	{
		// Initialize return list and add the first node
		List<VerletNode> outputList = new List<VerletNode>();
		outputList.Add(nodeList[0]);

		// Iterate through each node and interpolate to find new positions
		for (int i = 1; i < nodeCount - 1; i++)
		{
			float iEquivalentF = (nodeList.Count - 1.0f) * (i / (nodeCount - 1.0f));
			int iEquivalent = (int)iEquivalentF;
			float difference = iEquivalentF - Mathf.Floor(iEquivalentF);

			Vector3 position = Vector3.Lerp(nodeList[iEquivalent].position, nodeList[iEquivalent + 1].position, difference);
			outputList.Add(new VerletNode(position));
		}

		// Add the last node and return
		outputList.Add(nodeList[nodeList.Count - 1]);
		nodeList = outputList;
	}

	/// <summary>
	/// Checks whether the rope should tear, and splits into two GameObjects if necessary.
	/// </summary>
	private void CheckTear()
	{
		// In theory, I think this code should support tearing for a two-node rope, but I occasionally get
		// a bug where copies instantly spawn, so for now I'm disabling it.
		if (nodeCount == 2)
			return;

		// Calculate tensions, and find the node with greatest tension
		// (excluding start and end as you can't split there)
		List<float> tensionList = CalculateTensions();
		float maxTension = 0.0f;
		int maxTensionNode = 0;
		for (int i = 1; i < tensionList.Count - 1; i++)
		{
			if (tensionList[i] < maxTension)
				continue;

			maxTension = tensionList[i];
			maxTensionNode = i;
		}

		// Get tension from start and end nodes solely if there are two nodes
		if (nodeCount == 2)
			maxTension = tensionList[0] > tensionList[1] ? tensionList[0] : tensionList[1];

		// Early exit if tension is insufficient
		if (maxTension < tearTension)
			return;

		// Create copy of current GameObject with no children
		GameObject newObject = Instantiate(gameObject, transform.parent);
		VerletRope newRope = newObject.GetComponent<VerletRope>();
		foreach (Transform child in newObject.transform)
			Destroy(child.gameObject);

		// If node count is only two, create a new node in the middle and halve segment length
		if (nodeCount == 2)
		{
			Vector4 newPosition = Vector3.Lerp(nodeList[0].position, nodeList[1].position, 0.5f);
			nodeList.Insert(1, new VerletNode(newPosition));
			nodeCount++;
			segmentLength *= 0.5f;
			maxTensionNode = 1;
		}

		// Set parameters of new and current rope as necessary
		newRope.segmentLength = segmentLength;
		newRope.pointA = pointB;
		newRope.pointB = null;
		newRope.lockPointA = lockPointB;
		newRope.lockPointB = false;
		newRope.nodeCount = nodeCount - maxTensionNode;
		newRope.nodeList = new List<VerletNode>();
		newRope.nodeCollider = nodeCollider;
		newRope.pullRigidbodies = false;
		newRope.pointARigidbody = pointBRigidbody;
		newRope.pointBRigidbody = null;
		newRope.tearTimer = 1.0f;
		pointB = null;
		lockPointB = false;
		nodeCount = maxTensionNode + 1;
		pullRigidbodies = false;
		pointBRigidbody = null;
		tearTimer = 1.0f;

		// Shift attachments as necessary
		List<Attachment> attachmentsToDetach = new List<Attachment>();
		foreach (Attachment attachment in attachmentList)
		{
			float nodePoint = maxTensionNode / (nodeCount - 1);
			int nodeIndex = Mathf.RoundToInt((nodeList.Count - 1) * attachment.attachPoint);

			// Add any attachments to detach
			if (detachOnBreak)
			{
				if ((lerpAttachPoint && attachment.attachPoint == nodePoint) ||
					(!lerpAttachPoint && nodeIndex == maxTensionNode))
				{
					attachmentsToDetach.Add(attachment);
					continue;
				}
			}

			// If on latter portion of rope, shift to new rope object
			if (attachment.attachPoint >= nodePoint)
			{

				attachment.attachPoint = Mathf.InverseLerp(nodePoint, 1.0f, attachment.attachPoint);
				newRope.attachmentList.Add(attachment);
			}

			// If in former half, keep on current rope
			else
			{
				attachment.attachPoint = Mathf.InverseLerp(0, nodePoint, attachment.attachPoint);
			}
		}
		foreach (Attachment attachment in attachmentsToDetach)
			attachmentList.Remove(attachment);

		// Pass positions backwards from latter half of current rope to new rope,
		// and remove nodes from current rope accordingly
		for (int i = nodeList.Count - 1; i >= maxTensionNode; i--)
		{
			newRope.nodeList.Add(new VerletNode(nodeList[i].position, nodeList[i].prevPosition));
			nodeList.RemoveAt(i);
		}
	}

	/// <summary>
	/// Calculates 'tension' at the specified node index, based on the delta between current and target segment length.
	/// </summary>
	/// <param name="nodeIndex">Node index to check tension at.</param>
	/// <returns>Tension value.</returns>
	private float CalculateTension(int nodeIndex)
	{
		// Early exit if the specified node index is invalid
		if (nodeIndex < 0 || nodeIndex >= nodeList.Count)
			return -1.0f;
		if (nodeIndex == 0)
			nodeIndex = 1;

		// Calculate and return the tension
		float distance = (nodeList[nodeIndex - 1].position - nodeList[nodeIndex].position).magnitude;
		float tension = Mathf.Abs(distance - segmentLength);

		return tension;
	}

	/// <summary>
	/// Calculates 'tensions' for each node, based on the delta between current and target segment length.
	/// </summary>
	/// <returns>Tension value.</returns>
	private List<float> CalculateTensions()
	{
		List<float> tensions = new List<float>();

		for (int i = 1; i < nodeList.Count; i++)
		{
			float distance = (nodeList[i - 1].position - nodeList[i].position).magnitude;
			float tension = Mathf.Abs(distance - segmentLength);

			// Add tension to list, including a duplicate for node 0 to 1 and 1 to 0
			if (i == 1) tensions.Add(tension);
			tensions.Add(tension);
		}

		return tensions;
	}

	/// <summary>
	/// Applies forces to 'Point A' and 'Point B' rigidbodies according to rope tension.
	/// </summary>
	private void UpdateRigidbodies()
	{
		if (pointARigidbody != null)
		{
			Vector3 pullDirection = nodeList[1].position - nodeList[0].position;
			float tension = Mathf.Abs(pullDirection.magnitude - segmentLength);
			pullDirection.Normalize();

			if (tension > tensionThreshold)
			{
				Vector3 force = pullDirection * tension * pullForce;
				pointARigidbody.AddForceAtPosition(force, pointA.position);
			}
		}

		if (pointBRigidbody != null)
		{
			Vector3 pullDirection = nodeList[nodeList.Count - 2].position - nodeList[nodeList.Count - 1].position;
			float tension = Mathf.Abs(pullDirection.magnitude - segmentLength);
			pullDirection.Normalize();

			if (tension > tensionThreshold)
			{
				Vector3 force = pullDirection * tension * pullForce;
				pointBRigidbody.AddForceAtPosition(force, pointB.position);
			}
		}
	}

	/// <summary>
	/// Generates and returns a string.
	/// </summary>
	/// <returns>What we in the biz call a 'Finngleton'.</returns>
	private string Finngleton()
	{
		return
			@"
















			";
	}

	// -- Attachments --
	/// <summary>
	/// Updates the Transform of each attachment in attachmentList according to the simulated rope nodes.
	/// </summary>
	private void UpdateAttachments()
	{
		foreach (Attachment attachment in attachmentList)
		{
			if (attachment.attachTransform == null || attachment.simulatedFrom != Attachment.SimulationType.rope)
				continue;

			// Update attach transform based on node position
			if (!lerpAttachPoint || attachment.attachPoint == 0.0f || attachment.attachPoint == 1.0f)
			{
				int nodeIndex = Mathf.RoundToInt((nodeList.Count - 1) * attachment.attachPoint);
				attachment.attachTransform.position = nodeList[nodeIndex].position;
				continue;
			}

			// Update attach transform based on lerped node position, if required
			float nodeIndexF = (nodeList.Count - 1.0f) * attachment.attachPoint;
			int nodeIndexI = (int)nodeIndexF;
			float difference = nodeIndexF - Mathf.Floor(nodeIndexF);
			attachment.attachTransform.position = Vector3.Lerp(nodeList[nodeIndexI].position, nodeList[nodeIndexI + 1].position, difference);
		}
	}

	/// <summary>
	/// WIP, so not yet in use. This function as it stands is just a draft and is kinda broken.
	/// The idea of this was to have the attachment position physically simulated from the rope,
	/// e.g., an attachment on a U-shaped rope would slide down into the bottom trough.
	/// </summary>
	private void SimulateAttachments()
	{
		foreach (Attachment attachment in attachmentList)
		{
			if (attachment.attachTransform == null || attachment.mass == 0.0f || attachment.friction == 1.0f || attachment.lockAttachPoint || !lerpAttachPoint)
				continue;

			// Get current closest node
			int nodeIndex = Mathf.RoundToInt((nodeList.Count - 1) * attachment.attachPoint);

			// Get direction to previous and next node, and the dot product of those directions
			float prevDot = -1.0f, nextDot = -1.0f;
			Vector3 ropeForce = (Physics.gravity * gravityMultiplier) * Time.fixedDeltaTime;
			ropeForce += this.ropeForce;
			if (nodeIndex > 0)
			{
				Vector3 prevDirection = (nodeList[nodeIndex - 1].position - nodeList[nodeIndex].position).normalized;
				prevDot = Vector3.Dot(ropeForce, prevDirection);
			}
			if (nodeIndex < nodeList.Count - 1)
			{
				Vector3 nextDirection = (nodeList[nodeIndex + 1].position - nodeList[nodeIndex].position).normalized;
				nextDot = Vector3.Dot(ropeForce, nextDirection);
			}

			// Early exit if already balanced on the correct node (either on a balanced on the tip, or in a pit)
			if (prevDot == nextDot || (prevDot < 0.0f && nextDot < 0.0f)) continue;

			// Get the attach point value and dot value of the target node based on which further towards gravity
			float targetPoint = (nextDot > prevDot) ? nodeIndex + 1 : nodeIndex - 1;
			targetPoint /= nodeList.Count - 1;
			float targetDot = Mathf.Max(prevDot, nextDot);

			// Get how much to travel towards the target node
			float lerpAmount = (targetDot * (1.0f - attachment.friction) * attachment.mass) * Time.fixedDeltaTime;

			// Perform lerp
			attachment.attachPoint = Mathf.LerpUnclamped(attachment.attachPoint, targetPoint, lerpAmount);
		}
	}


	// -- Rendering --
	/// <summary>
	/// Smoothens the node list using Chaikin's corner-cutting algorithm
	/// </summary>
	/// <param name="iterations">Number of times to perform corner-cutting.</param>
	/// <returns>Smoothened list of nodes.</returns>
	private List<VerletNode> SmoothenRope(List<VerletNode> nodeList, int iterations)
	{
		// Early-exit conditions
		if (nodeList == null || nodeList.Count == 0)
			return nodeList;

		// Perform corner-cutting and return the resulting list
		for (int i = 0; i < iterations; i++)
		{
			List<VerletNode> tempList = new List<VerletNode>();
			tempList.Add(nodeList[0]);

			for (int j = 0; j < nodeList.Count - 1; j++)
			{
				Vector3 positionA = Vector3.Lerp(nodeList[j].position, nodeList[j + 1].position, 0.25f);
				Vector3 positionB = Vector3.Lerp(nodeList[j].position, nodeList[j + 1].position, 0.75f);

				if (j != 0)
					tempList.Add(new VerletNode(positionA));
				if (j != nodeList.Count - 2)
					tempList.Add(new VerletNode(positionB));
			}

			tempList.Add(nodeList[nodeList.Count - 1]);
			nodeList = tempList;
		}

		return nodeList;
	}

	/// <summary>
	/// Updates the LineRenderer component's values according to node positions, to render a smoothened rope.
	/// </summary>
	private void DrawLine()
	{
		// Update width according to current rope width
		lineRenderer.startWidth = ropeWidth;
		lineRenderer.endWidth = ropeWidth;

		// Smoothen the node list if required and update the LineRenderer component's values
		List<VerletNode> smoothNodeList = SmoothenRope(nodeList, smoothingIterations);

		lineRenderer.positionCount = smoothNodeList.Count;
		for (int i = 0; i < smoothNodeList.Count; i++)
			lineRenderer.SetPosition(i, smoothNodeList[i].position);
	}

	/// <summary>
	/// Generates a smoothened rope mesh, and updates the MeshFilter component accordingly to render a smoothened rope.
	/// </summary>
	private void DrawMesh()
	{
		// Initialize variables, and smoothen the node list if required
		List<Vector3> vertices = new List<Vector3>();
		List<int> indices = new List<int>();
		List<VerletNode> smoothNodeList = SmoothenRope(nodeList, smoothingIterations);

		#region Generate vertices for each node
		for (int pointI = 0; pointI < smoothNodeList.Count; pointI++)
		{
			// Get the forward, up and right directions of this point
			Vector3 nodeAPos = smoothNodeList[Mathf.Max(0, pointI - 1)].position;
			Vector3 nodeBPos = smoothNodeList[Mathf.Min(pointI + 1, smoothNodeList.Count - 1)].position;
			Vector3 forwardDirection = (nodeBPos - nodeAPos).normalized;
			Vector3 rightDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized;
			Vector3 upDirection = Vector3.Cross(forwardDirection, rightDirection).normalized;

			// Create a ring of vertices around the node (doubling up the first/last vert)
			for (int vertI = 0; vertI <= ropeSides; vertI++)
			{
				// For whatever reason, it seems I need to mash explicit float casts here or it doesn't work
				float angle = 2.0f * Mathf.PI * ((float)vertI / (float)ropeSides);

				// Add a vertex at half rope width from center of the node to the edge
				Vector3 normal = (Mathf.Cos(angle) * upDirection) + (Mathf.Sin(angle) * rightDirection);
				vertices.Add(smoothNodeList[pointI].position + (normal * 0.5f * ropeWidth));
			}

			// Create triangles from the vertices
			for (int segmentI = 0; segmentI < smoothNodeList.Count - 1; segmentI++)
			{
				for (int sideI = 0; sideI < ropeSides; sideI++)
				{
					int sideCount = ropeSides + 1;

					int topLeft = (segmentI * sideCount) + sideI;
					int topRight = ((segmentI + 1) * sideCount) + sideI;
					int bottomLeft = (segmentI * sideCount) + (sideI + 1);
					int bottomRight = ((segmentI + 1) * sideCount) + (sideI + 1);

					indices.Add(topLeft);
					indices.Add(bottomLeft);
					indices.Add(topRight);

					indices.Add(topRight);
					indices.Add(bottomLeft);
					indices.Add(bottomRight);
				}
			}
		}
		#endregion

		// Pass the generated vertices to the mesh, and reverse the triangles (went the wrong way apparently)
		indices.Reverse();
		mesh.Clear();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = indices.ToArray();
	}
	#endregion

	/// <summary>
	/// Stores the current and previous position of the node.
	/// </summary>
	public class VerletNode
	{
		public Vector3 position;
		public Vector3 prevPosition;

		public VerletNode(Vector3 position, Vector3 prevPosition)
		{
			this.position = position;
			this.prevPosition = prevPosition;
		}

		public VerletNode(Vector3 position)
		{
			this.position = position;
			prevPosition = position;
		}

		public VerletNode()
		{
			position = Vector3.zero;
			prevPosition = Vector3.zero;
		}
	}

	/// <summary>
	/// Describes an object that is 'attached' to the rope at a specified point, and will have its
	/// position updated by the rope simulation.
	/// 
	/// Note: some stuff in here is WIP and isn't used, (simulatedFrom, mass, friction) and was
	/// related to a system that would concurrently simulate the rope from the attachment.
	/// </summary>
	[System.Serializable]
	public class Attachment
	{
		public enum SimulationType
		{
			attachmentPosition,
			attachmentRigidbody,
			rope
		};

		public Transform attachTransform;
		public SimulationType simulatedFrom;
		public bool lockAttachPoint;
		[Range(0.0f, 1.0f)] public float attachPoint;
		public float mass;
		[Range(0.0f, 1.0f)] public float friction;
	}
}
