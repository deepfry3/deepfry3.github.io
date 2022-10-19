#pragma once
#include <assert.h>

/* ------------------------------
LINKEDLIST CLASS

The LinkedList is a doubly linked list
-------------------------------*/

template<typename T>
class LinkedList
{
public:
	/* --- NESTED CLASSES/STRUCTS --- */
	struct Node
	{
		T data;				// The data stored in the Node
		Node* prev;			// A pointer to the previous Node in the LinkedList
		Node* next;			// A pointer to the next Node in the LinkedList
	};

	/* --- CONSTRUCTORS & DESTRUCTORS --- */
	// Initializes the LinkedList
	LinkedList()
	{
		// Store metadata
		count = 0;

		// Initialize sentinel Nodes
		first = new Node();
		last = new Node();
		first->prev = nullptr;
		first->next = last;
		last->prev = first;
		last->next = nullptr;
	}
	// Deletes the sentinel Nodes and sets their reference to nullptr
	~LinkedList()
	{
		// Destruct
		delete first;
		delete last;
		first = nullptr;
		last = nullptr;
	}
	
	/* --- ADD FUNCTIONS --- */
	// Adds a Node containing the specified value to the front of the LinkedList
	void AddFront(T value)
	{
		// Create Node with data
		Node* newNode = new Node();
		newNode->data = value;

		// Update Node pointers
		newNode->prev = first;
		newNode->next = first->next;
		first->next->prev = newNode;
		first->next = newNode;

		// Increment count
		count++;
	}
	// Adds a Node containing the specified value to the back of the LinkedList
	void AddBack(T value)
	{
		// Create Node with data
		Node* newNode = new Node();
		newNode->data = value;

		// Update Node pointers
		newNode->prev = last->prev;
		newNode->next = last;
		last->prev->next = newNode;
		last->prev = newNode;

		// Increment count
		count++;
	}
	// Adds a Node containing the specified value before the specified Node in the LinkedList
	void AddBefore(T value, Node* node)
	{
		// Create Node with data
		Node* newNode = new Node();
		newNode->data = value;

		// Update Node pointers
		newNode->prev = node->prev;
		newNode->next = node;
		node->prev->next = newNode;
		node->prev = newNode;

		// Increment count
		count++;
	}
	// Adds a Node containing the specified value after the specified Node in the LinkedList
	void AddAfter(T value, Node* node)
	{
		// Create Node with data
		Node* newNode = new Node();
		newNode->data = value;

		// Update Node pointers
		newNode->prev = node;
		newNode->next = node->next;
		node->next->prev = newNode;
		node->next = newNode;

		// Increment count
		count++;
	}
	// Adds a Node containing the specified value at the specified position in the LinkedList
	void AddAt(T value, unsigned int pos)
	{
		// Store node at specified position
		Node* node = FindAt(pos);

		// Fail if position is out of range
		if (node == nullptr)
		{
			assert("Add failed - position " && pos && " out of range " && count);
			return;
		}

		// Add before node at specified position
		AddBefore(value, node);
	}

	/* --- MOVE FUNCTIONS --- */
	// Moves the specified Node as many positions forward or backward as specified, if found
	void MoveNode(Node* node, int change)
	{
		// Get position of Node in list
		int currentPos = FindPos(node);
		int newPos = currentPos + change;

		// Fail if new position is out of range or Node not found
		if (currentPos == -1 || newPos < 0 || newPos > count)
		{
			assert("Move failed - Node not in list or new position " && newPos && " out of range " && count);
			return;
		}

		// Get position of provisional Node currently at new position
		Node* provNode = FindAt(newPos);

		// Remove Node from current position
		node->prev->next = node->next;
		node->next->prev = node->prev;

		// Add Node after provisional Node
		node->prev = provNode->prev;
		node->next = provNode;
		provNode->prev->next = node;
		provNode->prev = node;
	}
	// Moves the specified Node to the front, if found
	void MoveNodeToFront(Node* node)
	{
		// Fail if Node not found
		if (!Contains(node))
		{
			assert("Move failed - Node not in list");
			return;
		}

		// Remove Node from current position
		node->prev->next = node->next;
		node->next->prev = node->prev;

		// Add Node to front
		node->prev = first;
		node->next = first->next;
		first->next->prev = node;
		first->next = node;
	}
	// Moves the specified Node to the back, if found
	void MoveNodeToBack(Node* node)
	{
		// Fail if Node not found
		if (!Contains(node))
		{
			assert("Move failed - Node not in list");
			return;
		}

		// Remove Node from current position
		node->prev->next = node->next;
		node->next->prev = node->prev;

		// Add Node to front
		node->prev = last->prev;
		node->next = last;
		last->prev->next = node;
		last->prev = node;
	}

	/* --- REMOVE FUNCTIONS --- */
	// Removes the specified Node in the LinkedList, if found (if specified (default of true))
	void Remove(Node* node, bool ifFound = true)
	{
		// Remove Node if found, or if found check is bypassed
		if (!ifFound || Contains(node))
		{
			// Update Node pointers and delete
			node->prev->next = node->next;
			node->next->prev = node->prev;
			delete node;

			// Decrement count
			count--;
		}
	}
	// Removes the first-found Node containing the specified value in the LinkedList, if found
	void Remove(T value)
	{
		// Store node at specified position
		Node* node = Find(value);

		// Remove Node if found
		if (node != nullptr)
			Remove(node, false);
	}
	// Removes all found instances of Nodes containing the specified value in the LinkedList, if found
	void RemoveAll(T value)
	{
		for (Node* node = Find(value); node != nullptr; node = Find(value))
			Remove(node, false);
	}
	// Removes the last-found Node containing the specified value in the LinkedList, if found
	void RemoveLast(T value)
	{
		// Store node at specified position
		Node* node = FindLast(value);

		// Remove Node if found
		if (node != nullptr)
			Remove(node, false);
	}
	// Removes the Node at the specified position in the LinkedList, if within range
	void RemoveAt(unsigned int pos)
	{
		// Store node at specified position
		Node* node = FindAt(pos);

		// Remove Node if found
		if (node != nullptr)
			Remove(node, false);
	}

	/* --- FIND FUNCTIONS --- */
	// Returns the first-found Node containing the specified value, or nullptr if not found
	Node* Find(T value)
	{
		// Iterate through nodes and return first matching node
		Node* iterNode = first->next;
		while (iterNode != last)
		{
			if (iterNode->data == value)
				return iterNode;
			iterNode = iterNode->next;
		}

		// If not found, return nullptr
		return nullptr;
	}
	// Returns the last-found Node containing the specified value, or nullptr if not found
	Node* FindLast(T value)
	{
		// Iterate through nodes and return first matching node
		Node* iterNode = last->prev;
		while (iterNode != first)
		{
			if (iterNode->data == value)
				return iterNode;
			iterNode = iterNode->prev;
		}

		// If not found, return nullptr
		return nullptr;
	}
	// Returns the first-found position of the specified value, or -1 if not found
	int FindPos(T value)
	{
		// Iterate until count is reached and return position of first matching node
		Node* iterNode = first->next;
		for (int i = 0; i < count; i++)
		{
			if (iterNode->data == value)
				return i;
			iterNode = iterNode->next;
		}

		// If not found, return -1
		return -1;
	}
	// Returns the first-found position of the specified Node, or -1 if not found
	int FindPos(Node* node)
	{
		// Iterate until count is reached and return position of first matching node
		Node* iterNode = first->next;
		for (int i = 0; i < count; i++)
		{
			if (iterNode == node)
				return i;
			iterNode = iterNode->next;
		}

		// If not found, return -1
		return -1;
	}
	// Returns the last-found position of the specified value, or -1 if not found
	int FindPosLast(T value)
	{
		// Iterate until count is reached and return position of first matching node
		Node* iterNode = last->prev;
		for (int i = count - 1; i >= 0; i++)
		{
			if (iterNode->data == value)
				return i;
			iterNode = iterNode->prev;
		}

		// If not found, return -1
		return -1;
	}
	// Returns the last-found position of the specified Node, or -1 if not found
	int FindPosLast(Node* node)
	{
		// Iterate until count is reached and return position of first matching node
		Node* iterNode = last->prev;
		for (int i = count - 1; i >= 0; i++)
		{
			if (iterNode == node)
				return i;
			iterNode = iterNode->prev;
		}

		// If not found, return -1
		return -1;
	}
	// Returns the Node at the specified position, or nullptr if out of range
	Node* FindAt(unsigned int pos)
	{
		// Return nullptr if out of range
		if (pos > count)
			return nullptr;

		// Iterate until position is reached and return Node
		Node* iterNode = first->next;
		for (int i = 0; i < pos; i++)
			iterNode = iterNode->next;

		// Return node
		return iterNode;
	}
	// Returns true if a Node containing the specified value is found, or false if not found
	bool Contains(T value)
	{
		// Use Find to search for Node and return true or false depending on if Node is returned
		return Find(value) != nullptr;
	}
	// Returns true if the specified Node is found, or false if not found
	bool Contains(Node* node)
	{
		// Iterate until count is reached and return true if node is found
		Node* iterNode = first->next;
		for (int i = 0; i < count; i++)
		{
			if (iterNode == node)
				return true;
			iterNode = iterNode->next;
		}

		// If not found, return false
		return false;
	}

	/* --- GETTER FUNCTIONS --- */
	int GetCount() // Returns the current count of the LinkedList
	{
		return count;
	}
	int GetSize() // Returns the current size (in bytes) of the data stored in the LinkedList
	{
		return (count * sizeof(T));
	}

	/* --- MISC. FUNCTIONS --- */
	// Deletes all non-sentinel Nodes from the LinkedList
	void Clear()
	{
		// Iterate through nodes and delete
		Node* iterNode = first->next;
		while (iterNode != last)
		{
			delete iterNode;
			iterNode = iterNode->next;
		}

		// Set sentinel Nodes to point to each other
		first->next = last;
		last->prev = first;

		// Update metadata
		count = 0;
	}

	/* --- OPERATOR OVERLOADING --- */
	// Returns the position specified inside []
	Node& operator[](unsigned int pos)
	{
		// Use FindAt function to return Node
		return *(FindAt(pos));
	}

	/* --- SORTING FUNCTIONS --- */
	// Sorts the LinkedList using the InsertionSort algorithm
	void Sort()
	{
		// Return if LinkedList is empty
		if (count <= 0) return;

		Node* keyNode = first->next->next;
		while (keyNode != last)
		{
			// Create Node to iterate backwards with, and a flag for if the key needs to move
			Node* iterNode = keyNode->prev;
			bool keyNeedsInsert = false;

			// Iterate backwards to check if the key Node needs to move
			while (iterNode != first && (iterNode->data > keyNode->data))
			{
				keyNeedsInsert = true;
				iterNode = iterNode->prev;
			}

			// If key Node needs to move, move to correct spot
			Node* nextKeyNode = keyNode->next;
			if (keyNeedsInsert)
			{
				nextKeyNode->prev = keyNode->prev;		// Update pointers for Node after key
				keyNode->prev = iterNode;				// Update pointers for key Node
				keyNode->next = iterNode->next;			// "
				iterNode->next->prev = keyNode;			// Update pointers for iter Node
				nextKeyNode->prev->next = nextKeyNode;	// "
				iterNode->next = keyNode;				// "
			}

			// Iterate key Node
			keyNode = nextKeyNode;
		}
	}

private:
	/* --- VARIABLES --- */
	Node* first;			// A pointer to the first Node in the LinkedList
	Node* last;				// A pointer to the last Node in the LinkedList
	unsigned int count;		// Stores the number of populated elements (Nodes) in the LinkedList
};