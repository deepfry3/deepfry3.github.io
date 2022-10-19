#include "Pathfinder.h"

/* ---- CONSTRUCTORS & DESTRUCTORS ---- */
// Initialize the Pathfinder object and fill with GraphNodes
Pathfinder::Pathfinder()
{
	// Create all GraphNodes in a grid and initialize them
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			// Create GraphNode
			nodes[x][y] = new GraphNode();

			// Initialize variables
			nodes[x][y]->indexX = x;											// Index of the node in the grid
			nodes[x][y]->indexY = y;											// "
			nodes[x][y]->position.x = (float)(POS_XOFFSET + (x * NODE_W));		// World position
			nodes[x][y]->position.y = (float)(POS_YOFFSET + (y * NODE_H));		// "
			nodes[x][y]->prev = nullptr;										// The previous node in the path (when found)
			nodes[x][y]->scoreG = 0;											// Score for A* algorithm
			nodes[x][y]->scoreF = 0;											// "
			nodes[x][y]->scoreH = 0;											// "
			nodes[x][y]->isBlocked = false;										// Whether or not the node can be traversed through
			nodes[x][y]->colour = COL_BLACK;									// The colour of the node
		}
	}

	// Connect all GraphNodes to their neighbours
	// [0][1][2]
	// [7][X][3]
	// [6][5][4]
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			for (int n = 0; n < NEIGHBOUR_COUNT; n++)
			{
				// Set neighbour
				nodes[x][y]->neighbours[n] = GetNeighbour(x, y, n);
				
				// Set cost to neighbour depending upon diagonal or straight
				nodes[x][y]->costs[n] = (n == 0 || n == 2 || n == 4 || n == 6) ? WALKA : WALK;
			}
		}
	}

#pragma region PTV Train Network
	/*
	This sets the nodes in the pathfinder to represent the metropolitan
	PTV train network.
	
	The 'Train Lines' section sets the colour of the line, and the
	weighting to use (NA for crossing a line, TRAIN for training laterally,
	TRAINA for training diagonally).
	
	The 'Train Stations' section sets the colour to white and sets
	the weighting to use (WALK for walking through laterally,
	WALKA for walking through diagonally, TRAIN for training laterally,
	TRAINA for training diagonally).
	
	If this project was anything other than a quick school assessment I
	would have developed a system to do this instead of doing it manually,
	because this took forever and I hate it.
	*/


	/* --- TRAIN LINES --- */
	
	// Red - South Morang & Hurstbridge Line
	nodes[75][46]->SetNode(COL_RED, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);					// Join with City Loop
	nodes[76][46]->SetNode(COL_RED, NA, NA, TRAINA, NA, NA, NA, NA, TRAIN);
	for (int i = 0; i < 8; i++)
		nodes[77 + i][47 + i]->SetNode(COL_RED, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[86][56]->SetNode(COL_RED, NA, TRAIN, TRAINA, NA, NA, NA, TRAINA, NA);				// South Morang & Hurstridge Line split
	for (int i = 0; i < 10; i++)															// South Morang Line...
		nodes[86][57 + i]->SetNode(COL_RED, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[86][67]->SetNode(COL_RED, NA, NA, TRAINA, NA, NA, TRAIN, NA, NA);
	for (int i = 0; i < 5; i++)
		nodes[87 + i][68+ i]->SetNode(COL_RED, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[92][73]->SetNode(COL_RED, NA, NA, NA, TRAIN, NA, NA, TRAINA, NA);
	for (int i = 0; i < 9; i++)
		nodes[93 + i][73]->SetNode(COL_RED, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	for (int i = 0; i < 10; i++)															// Hurstbridge Line...
		nodes[87 + i][57 + i]->SetNode(COL_RED, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[97][67]->SetNode(COL_RED, NA, NA, NA, TRAIN, NA, NA, TRAINA, NA);
	for (int i = 0; i < 25; i++)
		nodes[98 + i][67]->SetNode(COL_RED, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);

	// Yellow - Sunbury, Craigieburn & Upfield Line
	for (int i = 0; i < 8; i++)																// Sunbury Line...
		nodes[40][66 - i]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[40][58]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, TRAINA, NA, NA, NA);
	nodes[41][57]->SetNode(COL_YELLOW, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 11; i++)
		nodes[43 + i][56]->SetNode(COL_YELLOW, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	for (int i = 0; i < 18; i++)															// Craigieburn Line...
		nodes[56][79 - i]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[56][61]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 4; i++)
		nodes[57 + i][60 - i]->SetNode(COL_YELLOW, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 10; i++)															// Upfield Line...
		nodes[69][75 - i]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[69][65]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, NA, NA, TRAINA, NA);
	for (int i = 0; i < 5; i++)
		nodes[68 - i][64 - i]->SetNode(COL_YELLOW, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[63][59]->SetNode(COL_YELLOW, NA, NA, TRAINA, NA, NA, TRAIN, NA, NA);
	for (int i = 0; i < 3; i++)
		nodes[63][58 - i]->SetNode(COL_YELLOW, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);

	// Blue - Lilydale, Melbrave, Alamein & Glen Waverley Line
	for (int i = 0; i < 7; i++)
		nodes[78 + i][43]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[86][43]->SetNode(COL_BLUE, NA, NA, TRAINA, NA, TRAINA, NA, NA, TRAIN);
	for (int i = 0; i < 17; i++)															// Lilydale/Belgrave Line...
		nodes[87 + i][44 + i]->SetNode(COL_BLUE, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[104][61]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, TRAINA, NA);
	for (int i = 0; i < 24; i++)
		nodes[105 + i][61]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[118][61]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, TRAINA, NA, NA, TRAIN);
	for (int i = 0; i < 10; i++)
		nodes[119 + i][60 - i]->SetNode(COL_BLUE, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[93][50]->SetNode(COL_BLUE, NA, NA, TRAINA, TRAIN, NA, NA, TRAINA, TRAIN);			// Alamein Line...
	nodes[94][50]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[95][50]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[96][50]->SetNode(COL_BLUE, NA, NA, NA, NA, TRAINA, NA, NA, TRAIN);
	for (int i = 0; i < 7; i++)
		nodes[97 + i][49 - i]->SetNode(COL_BLUE, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 7; i++)																// Glen Waverley Line...
		nodes[87 + i][42 - i]->SetNode(COL_BLUE, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[94][35]->SetNode(COL_BLUE, TRAINA, NA, NA, TRAIN, NA, NA, NA, NA);
	for (int i = 0; i < 17; i++)
		nodes[95 + i][35]->SetNode(COL_BLUE, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);

	// Cyan - Pakenham & Cranbourne Line
	for (int i = 0; i < 15; i++)
		nodes[88 + i][32 - i]->SetNode(COL_CYAN, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[104][16]->SetNode(COL_CYAN, TRAINA, NA, NA, TRAIN, NA, TRAIN, NA, NA);			// Cranbourne Line...
	nodes[104][15]->SetNode(COL_CYAN, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[104][14]->SetNode(COL_CYAN, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	for (int i = 0; i < 23; i++)															// Pakenham Line...
		nodes[105 + i][16]->SetNode(COL_CYAN, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);

	// Green - Frankston, Werribee & Williamstown Line
	for (int i = 0; i < 7; i++)																// Werribee Line...
		nodes[30 + i][51 - i]->SetNode(COL_GREEN, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 10; i++)
		nodes[35 + i][47]->SetNode(COL_GREEN, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[37][44]->SetNode(COL_GREEN, TRAINA, NA, NA, TRAIN, NA, NA, NA, NA);
	for (int i = 0; i < 4; i++)
		nodes[38 + i][44]->SetNode(COL_GREEN, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[42][44]->SetNode(COL_GREEN, NA, NA, TRAINA, NA, NA, NA, NA, TRAIN);
	for (int i = 0; i < 11; i++)
		nodes[43 + i][45 + i]->SetNode(COL_GREEN, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[45][47]->SetNode(COL_GREEN, NA, NA, TRAINA, NA, NA, TRAIN, TRAINA, TRAIN);		// Williamstown Line...
	for (int i = 0; i < 5; i++)
		nodes[45][46 - i]->SetNode(COL_GREEN, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	for (int i = 0; i < 19; i++)															// Frankston Line...
		nodes[87][32 - i]->SetNode(COL_GREEN, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);
	nodes[87][13]->SetNode(COL_GREEN, NA, TRAIN, NA, NA, TRAINA, NA, NA, NA);
	nodes[88][12]->SetNode(COL_GREEN, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[89][11]->SetNode(COL_GREEN, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[90][10]->SetNode(COL_GREEN, TRAINA, NA, NA, TRAIN, NA, NA, NA, NA);
	for (int i = 0; i < 9; i++)
		nodes[91 + i][10]->SetNode(COL_GREEN, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);

	// Pink - Sandringham Line
	nodes[78][40]->SetNode(COL_PINK, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[77][39]->SetNode(COL_PINK, NA, NA, TRAINA, NA, NA, NA, TRAINA, NA);
	nodes[76][38]->SetNode(COL_PINK, NA, NA, TRAINA, NA, NA, TRAIN, NA, NA);
	for (int i = 0; i < 14; i++)
		nodes[76][37 - i]->SetNode(COL_PINK, NA, TRAIN, NA, NA, NA, TRAIN, NA, NA);

	// Grey - Multiple Lines
	nodes[54][56]->SetNode(COL_GREY, NA, NA, NA, TRAIN, NA, NA, TRAINA, TRAIN);				// North-West suburbs...
	for (int i = 0; i < 6; i++)
		nodes[55 + i][56]->SetNode(COL_GREY, NA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[61][56]->SetNode(COL_GREY, TRAINA, NA, NA, TRAIN, NA, NA, NA, TRAIN);
	nodes[62][56]->SetNode(COL_GREY, NA, NA, NA, NA, TRAINA, NA, NA, TRAIN);
	nodes[63][55]->SetNode(COL_GREY, TRAINA, TRAIN, NA, NA, TRAINA, NA, NA, NA);
	nodes[64][54]->SetNode(COL_GREY, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 12; i++)															// South-East suburbs...
		nodes[75 + i][45 - i]->SetNode(COL_GREY, TRAINA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[77][43]->SetNode(COL_GREY, TRAINA, NA, NA, TRAIN, TRAINA, NA, NA, NA);
	nodes[79][41]->SetNode(COL_GREY, TRAINA, NA, NA, NA, TRAINA, NA, TRAINA, NA);
	nodes[87][33]->SetNode(COL_GREY, TRAINA, NA, NA, NA, TRAINA, TRAIN, NA, NA);

	// Grey - City Loop (weights to force counter-clockwise around the loop)
	nodes[66][54]->SetNode(COL_GREY, NA, NA, NA, NA, NA, NA, TRAINA, NA);
	for (int i = 0; i < 6; i++)
		nodes[67 + i][54]->SetNode(COL_GREY, NA, NA, NA, NA, NA, NA, NA, TRAIN);
	nodes[73][54]->SetNode(COL_GREY, NA, NA, NA, NA, NA, NA, NA, TRAIN);
	nodes[74][53]->SetNode(COL_GREY, TRAINA, NA, NA, NA, NA, NA, NA, NA);
	nodes[75][52]->SetNode(COL_GREY, TRAINA, NA, NA, NA, NA, NA, NA, NA);
	for (int i = 0; i < 4; i++)
		nodes[75][51 - i]->SetNode(COL_GREY, NA, TRAIN, NA, NA, NA, NA, NA, NA);
	nodes[75][47]->SetNode(COL_GREY, NA, TRAIN, NA, NA, NA, NA, NA, NA);
	nodes[74][46]->SetNode(COL_GREY, NA, NA, TRAINA, NA, TRAINA, NA, NA, NA);
	nodes[73][45]->SetNode(COL_GREY, NA, NA, TRAINA, NA, NA, NA, NA, NA);
	for (int i = 0; i < 6; i++)
		nodes[67 + i][45]->SetNode(COL_GREY, NA, NA, NA, TRAIN, NA, NA, NA, NA);
	nodes[66][45]->SetNode(COL_GREY, NA, NA, NA, TRAIN, NA, NA, NA, NA);
	nodes[65][46]->SetNode(COL_GREY, NA, NA, NA, NA, TRAINA, NA, NA, NA);
	nodes[64][47]->SetNode(COL_GREY, NA, NA, NA, NA, TRAINA, NA, NA, NA);
	for (int i = 0; i < 4; i++)
		nodes[64][51 - i]->SetNode(COL_GREY, NA, NA, NA, NA, NA, TRAIN, NA, NA);
	nodes[64][52]->SetNode(COL_GREY, NA, NA, NA, NA, NA, TRAIN, NA, NA);
	nodes[65][53]->SetNode(COL_GREY, TRAINA, NA, NA, NA, NA, NA, TRAINA, NA);

	/* --- TRAIN STATIONS --- */

	// Red - South Morang & Hurstbridge Line
	nodes[85][55]->SetNode(COL_WHITE, WALKA, WALK, TRAINA, WALK, WALKA, WALK, TRAINA, WALK);			// Clifton Hill
	nodes[102][73]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// South Morang
	nodes[123][67]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// Hurstbridge

	// Yellow - Sunbury, Craigieburn & Upfield Line
	nodes[42][56]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, WALK);			// Sunshine
	nodes[40][64]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN, WALKA, WALK);			// Watergardens
	nodes[40][67]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, TRAIN, WALKA, WALK);				// Sunbury
	nodes[56][76]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN, WALKA, WALK);			// Broadmeadows
	nodes[56][80]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, WALK);				// Craigieburn
	nodes[69][76]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, TRAIN, WALKA, WALK);				// Upfield

	// Blue - Lilydale, Melbrave, Alamein & Glen Waverley Line
	nodes[85][43]->SetNode(COL_WHITE, WALKA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN);			// Burnley
	nodes[112][35]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// Glen Waverley
	nodes[92][49]->SetNode(COL_WHITE, WALKA, WALK, TRAINA, WALK, WALKA, WALK, TRAINA, WALK);			// Camberwell
	nodes[104][42]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, WALKA, WALK, WALKA, WALK);			// Alamein
	nodes[117][61]->SetNode(COL_WHITE, WALKA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN);			// Ringwood
	nodes[129][50]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, WALKA, WALK, WALKA, WALK);			// Belgrave
	nodes[129][61]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// Lilydale

	// Cyan - Pakenham & Cranbourne Line
	nodes[95][25]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// Clayton
	nodes[103][17]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// Dandenong
	nodes[104][13]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, WALK, WALKA, WALK);			// Cranbourne
	nodes[128][16]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// Pakenham

	// Green - Frankston, Werribee & Williamstown Line
	nodes[47][49]->SetNode(COL_WHITE, WALKA, WALK, TRAINA, WALK, WALKA, WALK, TRAINA, WALK);			// Newport
	nodes[45][41]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, WALK, WALKA, WALK);				// Williamstown
	nodes[39][44]->SetNode(COL_WHITE, WALKA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN);			// Altona
	nodes[34][47]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, TRAIN, TRAINA, WALK, WALKA, WALK);			// Laverton
	nodes[29][52]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);				// Werribee
	nodes[100][10]->SetNode(COL_WHITE, WALKA, WALK, WALKA, WALK, WALKA, WALK, WALKA, TRAIN);			// Frankston
	
	// Pink - Sandringham Line
	nodes[76][23]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, WALK, WALKA, WALK);				// Sandringham
	
	// Grey - Multiple Lines
	nodes[55][56]->SetNode(COL_WHITE, WALKA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, TRAIN);			// Footscray
	nodes[63][55]->SetNode(COL_WHITE, TRAINA, TRAIN, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// North Melbourne
	nodes[76][44]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// Richmond
	nodes[78][42]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// South Yarra
	nodes[86][34]->SetNode(COL_WHITE, TRAINA, WALK, WALKA, WALK, TRAINA, WALK, WALKA, WALK);			// Caulfield

	// Grey - City Loop
	nodes[68][54]->SetNode(COL_WHITE, WALKA, WALK, WALKA, NA, WALKA, WALK, WALKA, TRAIN);				// Flagstaff
	nodes[71][54]->SetNode(COL_WHITE, WALKA, WALK, WALKA, NA, WALKA, WALK, WALKA, TRAIN);				// Melbourne Central
	nodes[75][49]->SetNode(COL_WHITE, WALKA, TRAIN, WALKA, WALK, WALKA, NA, WALKA, WALK);				// Parliament
	nodes[69][45]->SetNode(COL_WHITE, WALKA, WALK, WALKA, TRAIN, WALKA, WALK, WALKA, NA);				// Flinders Street
	nodes[64][49]->SetNode(COL_WHITE, WALKA, NA, WALKA, WALK, WALKA, TRAIN, WALKA, WALK);				// Southern Cross

	/* --- ADJACENT NODES --- */

	/*
	This algorithm 'outlines' the train lines with a very high cost to prevent agents
	walking straight onto the train from anywhere.
	It's still technically possible, but will almost always be more efficient to walk
	to the train station.
	*/
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			if (nodes[x][y]->colour != COL_BLACK)
				continue;

			for (int n = 0; n < NEIGHBOUR_COUNT; n++)
			{
				GraphNode* neighbour = nodes[x][y]->neighbours[n];
				if (neighbour == nullptr) continue;

				// If this neighbour is a train line, set cost to NA
				else if (neighbour->colour != COL_BLACK && neighbour->colour != COL_WHITE)
					nodes[x][y]->costs[n] = NA;

				// If a diagonal line through the train line is a free node, set that cost to NA
				else if (neighbour->colour == COL_BLACK)
				{
					GraphNode* node1 = nullptr;
					GraphNode* node2 = nullptr;
					switch (n)
					{
					case 0:		node1 = nodes[x][y]->neighbours[1];		node2 = nodes[x][y]->neighbours[7];		break;
					case 2:		node1 = nodes[x][y]->neighbours[1];		node2 = nodes[x][y]->neighbours[3];		break;
					case 4:		node1 = nodes[x][y]->neighbours[3];		node2 = nodes[x][y]->neighbours[5];		break;
					case 6:		node1 = nodes[x][y]->neighbours[5];		node2 = nodes[x][y]->neighbours[7];		break;
					}

					if (node1 == nullptr || node2 == nullptr)
						continue;

					if (node1->colour != COL_BLACK && node1->colour != COL_WHITE && node2->colour != COL_BLACK && node2->colour != COL_WHITE)
						nodes[x][y]->costs[n] = NA;
				}
			}
		}
	}

#pragma endregion PTV Train Network
}

// Delete all GraphNodes in the Pathfinder object
Pathfinder::~Pathfinder()
{
	// Delete all GraphNodes and set their pointers to null
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			delete nodes[x][y];
			nodes[x][y] = nullptr;
		}
	}
}

/* ---- PATHFINDING ALGORITHMS ---- */
// Performs pathfinding between the two specified world positions using A* algorithm, returning true if a path was found, otherwise false
bool Pathfinder::AStarPath(Vector2 start, Vector2 end, List<Vector2>& finalPath)
{
	/* --- INITIALIZATION --- */

	// Get start and end Nodes and clear the path
	GraphNode* nodeStart = GetNodeByPos(start);
	GraphNode* nodeEnd = GetNodeByPos(end);
	finalPath.Clear();

	/* --- EARLY EXIT --- */

	// Return false if immediately no path is possible
	if (nodeStart == nullptr || nodeEnd == nullptr || nodeEnd->isBlocked)
		return false;

	// Return true if path ends on itself
	if (nodeStart == nodeEnd)
	{
		finalPath.Add(nodeEnd->position);
		return true;
	}

	/* --- FULL PATHFINDING ALGORITHM --- */

	// Clear open and closed lists
	listOpen.Clear();
	memset(listClosed, 0, sizeof(bool) * (GRID_W * GRID_H));

	// Reset start node
	nodeStart->scoreF = 0;
	nodeStart->scoreG = 0;
	nodeStart->scoreH = 0;
	nodeStart->prev = nullptr;
	listOpen.Push(nodeStart);

	// Perform pathfinding using A* algorithm
	while (listOpen.GetCount() > 0)
	{
		// Remove current node from top of list and add to closed list
		GraphNode* current = listOpen.Pop();
		listClosed[current->indexX][current->indexY] = true;

		// If we've just added end node to the closed list then path has been found
		if (current == nodeEnd)
		{
			finalPath.Add(current->position);
			while (current->prev != nullptr)
			{
				current = current->prev;
				finalPath.Add(current->position);
			}
			return true;
		}

		// Process all neighbours
		for (int n = 0; n < NEIGHBOUR_COUNT; n++)
		{
			// Get neighbour
			GraphNode* neighbour = current->neighbours[n];

			// Sanity-checking
			if (neighbour == nullptr || neighbour->isBlocked)
				continue;
			if (listClosed[neighbour->indexX][neighbour->indexY])
				continue;

			// Check if in open list
			int listPos = listOpen.Find(neighbour);
			if (listPos != -1)
			{
				// Check if we found a cheaper path
				int newG = current->scoreG + current->costs[n];
				if (newG < neighbour->scoreG)
				{
					// Found a cheaper path, store new values and re-order Heap
					neighbour->scoreG = newG;
					neighbour->scoreF = neighbour->scoreG + neighbour->scoreH;
					neighbour->prev = current;
					listOpen.SiftUp(listPos);
				}
			}
			else
			{
				// Calculate costs and add to open list
				neighbour->scoreG = current->scoreG + current->costs[n];
				neighbour->scoreH = GetHeuristic(neighbour, nodeEnd);
				neighbour->scoreF = neighbour->scoreG + neighbour->scoreH;
				neighbour->prev = current;
				listOpen.Push(neighbour);
			}
		}
	}

	return false;
}

/* ---- DRAW FUNCTIONS ---- */
void Pathfinder::Draw(aie::Renderer2D* renderer)
{
	// Draw connections
	// Code is here if needed for debugging, but is not intended for use

	/*renderer->SetRenderColour(COL_LINE);
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			GraphNode* node = nodes[x][y];
			for (int n = 0; n < NEIGHBOUR_COUNT; n++)
			{
				GraphNode* neighbour = node->neighbours[n];
				if (neighbour != nullptr)
					renderer->DrawLine(node->position.x, node->position.y, neighbour->position.x, neighbour->position.y);
			}
		}
	}*/

	// Draw Nodes
	for (int y = 0; y < GRID_H; y++)
	{
		for (int x = 0; x < GRID_W; x++)
		{
			// Get node and calculate draw parameters
			GraphNode* node = nodes[x][y];
			float drawX = node->position.x;
			float drawY = node->position.y;
			float drawW = (float)(NODE_W * 0.5);
			float drawH = (float)(NODE_H * 0.5);

			// Draw node
			renderer->SetRenderColour(node->colour);
			renderer->DrawBox(drawX, drawY, drawW, drawH);
		}
	}

	// Set render colour back to default of white
	renderer->SetRenderColour(0xFFFFFFFF);
}

/* ---- MISC. FUNCTIONS ---- */
// Get the GraphNode at the specified world position
GraphNode* Pathfinder::GetNodeByPos(Vector2 pos)
{
	// Remove node offset from specified position
	pos.x -= POS_XOFFSET;
	pos.y -= POS_YOFFSET;

	// Get the index to access using the specified world position
	int x = (int)((pos.x / NODE_W) + 0.5f);
	int y = (int)((pos.y / NODE_H) + 0.5f);

	// Return null if position does not contain a GraphNode, otherwise return GraphNode
	if (x < 0 || x >= GRID_W || y < 0 || y >= GRID_H)
		return nullptr;
	else
		return nodes[x][y];
}
// Get the world position at the specified GraphNode
Vector2 Pathfinder::GetPosByNode(GraphNode* node)
{
	return Vector2(POS_XOFFSET + (NODE_W * node->indexX), POS_YOFFSET + (NODE_H * node->indexY));
}
// Get the GraphNode using the specified GraphNode position and neighbour index
// [0][1][2]
// [7][X][3]
// [6][5][4]
GraphNode* Pathfinder::GetNeighbour(int nodeX, int nodeY, int neighbourIndex)
{
	// Calcuate neighbour position
	int x = nodeX;
	int y = nodeY;
	switch (neighbourIndex)
	{
	case 0:		x--;	y++;	break;
	case 1:				y++;	break;
	case 2:		x++;	y++;	break;
	case 3:		x++;			break;
	case 4:		x++;	y--;	break;
	case 5:				y--;	break;
	case 6:		x--;	y--;	break;
	case 7:		x--;			break;
	}

	// Return null if neighbour is invalid, otherwise return neighbour GraphNode
	if (x < 0 || x >= GRID_W || y < 0 || y >= GRID_H)
		return nullptr;
	else
		return nodes[x][y];
}
// Get an estimate of how many moves between the specified GraphNodes using a Diagonal Distance algorithm
int Pathfinder::GetHeuristic(GraphNode* startNode, GraphNode* endNode)
{
	int dX = abs(startNode->indexX - endNode->indexX);		// Calculate absolute offset on x-axis
	int dY = abs(startNode->indexY - endNode->indexY);		// Calculate absolute offset on y-axis
	int min = (dX > dY) ? dY : dX;							// Calculate the smaller of the two
	
	return (10 * (dX + dY)) + (-6 * min);
}