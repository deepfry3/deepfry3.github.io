#include "raylib.h"
#include <string>
#include "Node.h"

/* ------------------------------
BINARYTREE CLASS

The BinaryTree is a rooted binary search tree, made up of a nodes
and edges (connections to other nodes).
Data is automatically ordered, but not necessarily balanced.
-------------------------------*/

class BinaryTree
{
public:
	/* ---- CONSTRUCTORS & DESTRUCTORS ---- */
	// Initializes the BinaryTree
	BinaryTree()
	{
		// Initialize root Node as null
		root = nullptr;
	}
	// Deletes the BinaryTree
	~BinaryTree()
	{
		// Continue to remove the root Node from the BinaryTree until all values deleted
		while (root != nullptr)
			Remove(root->GetData());
	}

	/* ---- INSERT & REMOVE FUNCTIONS ---- */
	// Inserts a Node containing the specified value to the BinaryTree
	void Insert(int value)
	{
		// Create a new Node with the value specified
		Node* newNode = new Node(value);

		// If BinaryTree is empty, add new Node to the root
		if (root == nullptr)
		{
			root = newNode;
			return;
		}

		// If BinaryTree not empty, iterate through to find a valid position for the new Node
		/* -- Note: I'm not sure how to best handle inserting an already-existant value, so it just does nothing -- */
		Node* iterNode = root;
		Node* iterNodeParent = nullptr;
		while (iterNode != nullptr)
		{
			iterNodeParent = iterNode;

			if (value == iterNode->GetData())
			{
				delete newNode;
				return;
			}
			else if (value < iterNode->GetData())
				iterNode = iterNode->GetLeft();
			else if (value > iterNode->GetData())
				iterNode = iterNode->GetRight();
		}

		// Set the parent of the new Node to point to the new Node
		if (value > iterNodeParent->GetData())
			iterNodeParent->SetRight(newNode);
		else if (value < iterNodeParent->GetData())
			iterNodeParent->SetLeft(newNode);
	}
	// Removes the Node containing the specified value from the BinaryTree, if it exists
	void Remove(int value)
	{
		// Assign found Node and its parent to variables, or return if not found
		Node* node = nullptr;
		Node* parent = nullptr;
		if (!FindNode(value, node, parent)) return;

		// Count Node's children
		int nodeChildren = node->HasLeft() + node->HasRight();

		// Deletion: Node has 2 children
		if (nodeChildren == 2)
		{
			// Find smallest value in tree greater than value to remove
			Node* iterNode = node->GetRight();
			while (iterNode->GetLeft() != nullptr)
				iterNode = iterNode->GetLeft();

			// Copy data from that value and delete its Node
			int dataToCopy = iterNode->GetData();
			Remove(dataToCopy);

			// Insert the data to copy into the Node to "delete"
			node->SetData(dataToCopy);
			return;
		}

		// Deletion: Node has one child
		else if (nodeChildren == 1)
		{
			// Get a pointer to the child
			Node* child = (node->HasRight()) ? node->GetRight() : node->GetLeft();

			// Add the child as a child of the Node's parent
			if (node == root)
				root = child;
			else if (parent->GetRight() == node)
				parent->SetRight(child);
			else if (parent->GetLeft() == node)
				parent->SetLeft(child);

			// Delete the Node
			delete node;
			node = nullptr;
		}

		// Deletion: Node has no children
		else
		{
			// Set Node's parent's pointer to null
			if (node == root)
				root = nullptr;
			else if (parent->GetLeft() == node)
				parent->SetLeft(nullptr);
			else if (parent->GetRight() == node)
				parent->SetRight(nullptr);

			// Delete the Node
			delete node;
			node = nullptr;
		}
	}

	/* ---- FIND FUNCTIONS ---- */
	// Returns a pointer to a Node containing the specified value, or nullptr if not found
	Node* Find(int value)
	{
		Node* node = nullptr;
		Node* parent = nullptr;

		return FindNode(value, node, parent) ? node : nullptr;
	}
	// Returns a pointer to the parent of a Node containing the specified value, or nullptr if not found
	Node* FindParent(int value)
	{
		Node* node = nullptr;
		Node* parent = nullptr;

		return FindNode(value, node, parent) ? parent : nullptr;
	}

	/* ---- MISC. FUNCTIONS ---- */
	// Returns if the BinaryTree is empty, by checking if the root Node is null
	bool IsEmpty()
	{
		return (root == nullptr);
	}
	// Public Draw Function
	void Draw(Node* selected)
	{
		/* -- Note: I didn't write this, it was included in the AIE BinaryTree sample -- */

		Draw(root, 400, 40, 400, selected);
	}

private:
	/* ---- VARIABLES ---- */
	Node* root;

	/* ---- FUNCTIONS ---- */
	// Returns true if the value is found in the BinaryTree, and passes references to the Node and its parent, or nullptr if not found
	bool FindNode(int value, Node*& node, Node*& parent)
	{
		// Return false if list is empty, then initialize variables
		if (root == nullptr) return false;
		node = root;
		parent = nullptr;
		int nodeData = node->GetData();

		// Iterate through BinaryTree until data or nullptr is found
		while (nodeData != value && node != nullptr)
		{
			// Iterate parent
			parent = node;

			// Iterate Node based on less than / greater than search value
			if (nodeData < value)
				node = node->GetRight();
			else if (nodeData > value)
				node = node->GetLeft();

			// Re-grab new Node's data
			if (node != nullptr)
				nodeData = node->GetData();
		}

		// Return if value was found
		if (node == nullptr)
		{
			parent = nullptr;
			return false;
		}
		else
		{
			return true;
		}
	}
	// Private Draw Function
	void Draw(Node* node, int x, int y, int hSpacing, Node* selected)
	{
		if (node == nullptr) return;
		hSpacing /= 2;

		if (node->GetLeft() != nullptr)
		{
			DrawLine(x, y, x - hSpacing, y + 80, RED);
			Draw(node->GetLeft(), x - hSpacing, y + 80, hSpacing, selected);
		}
		if (node->GetRight() != nullptr)
		{
			DrawLine(x, y, x + hSpacing, y + 80, RED);
			Draw(node->GetRight(), x + hSpacing, y + 80, hSpacing, selected);
		}

		node->Draw(x, y, (selected == node));
	}
};