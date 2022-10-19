#include <iostream>
#include "Board.h"
using namespace std;


// -----      MACROS        ----- //
#define MIN_BOARD_SIZE	2
#define MAX_BOARD_SIZE	6
#define DEF_BOARD_SIZE	3
#define INF_LOOP		while (true)


// -----     FUNCTIONS      ----- //
static int GrabValidInt(int min, int max)
{
	int returnVal;
	bool valid;
	do
	{
		// Get int from player
		cin >> returnVal;
		cin.clear();
		cin.ignore(10000, '\n');

		// Clamp to 0 if incorrect
		if (returnVal < min || returnVal > max)
		{
			valid = false;
			cout << "Invalid (must be " << min << " - " << max << "). Try again: ";
		}
		else
		{
			valid = true;
		}
	} while (!valid);

	return returnVal;
}


// -----       MAIN         ----- //
int main()
{
	bool shouldQuit = false;
	while (!shouldQuit)
	{
		/* ----- Welcome Player -----*/
		system("cls");
		cout << "Welcome to Tic-Tac-Toe!" << endl << endl;

		// Ask user for game board dimensions
		int boardWidth, boardHeight;
		cout << "Enter desired board width (default is " << DEF_BOARD_SIZE << "): ";
		boardWidth = GrabValidInt(MIN_BOARD_SIZE, MAX_BOARD_SIZE);
		cout << endl << "Enter desired board height (default is " << DEF_BOARD_SIZE << "): ";
		boardHeight = GrabValidInt(MIN_BOARD_SIZE, MAX_BOARD_SIZE);

		// Create game board with specified dimensions
		Board board(boardWidth, boardHeight);

		// Store active player and turn counter
		char activePlayer = 'X';
		int turnCounter = 0;
		system("cls");

		/* ----- Game Loop -----*/
		INF_LOOP
		{
			// Print current game state
			board.Print();
			cout << endl << "It's Player " << activePlayer << "'s turn!" << endl << endl;

			/* ----- Loop player's turn until valid move -----*/
			INF_LOOP
			{
				// Ask player which column and row to fill
				cout << "Enter column to fill: ";
				int chosenPosX = GrabValidInt(1, boardWidth);
				cout << "Enter row to fill: ";
				int chosenPosY = GrabValidInt(1, boardHeight);

				// Fill cell if unoccupied
				if (board.GetValue(chosenPosX, chosenPosY) == '-')
				{
					board.SetValue(activePlayer, chosenPosX, chosenPosY);
					turnCounter++;
					break;
				}
				cout << endl << "Position already occupied. Try again." << endl;
			}
			system("cls");

			/* ----- Check for win to end or continue game -----*/
			bool playerHasWon = board.CheckWin() ? true : false;
			
			// Game is over
			if (playerHasWon || turnCounter == (boardWidth * boardHeight))
			{
				// Print results
				board.Print();
				if (playerHasWon)
					cout << endl << "Player " << activePlayer << " won! Good job!" << endl;
				else
					cout << endl << "A draw!" << endl;

				// Ask to restart or continue
				char playerInput;
				cout << "Enter 'Q' to quit, or any other character to restart" << endl;
				cin >> playerInput;
				if (playerInput == 'q' || playerInput == 'Q')
					shouldQuit = true;

				// Destroy board and break from game loop
				board.Delete();
				break;
			}

			// Game not over - continue and change active player
			else
				activePlayer = (activePlayer == 'X') ? 'O' : 'X';
		}
	}

	return 0;
}
