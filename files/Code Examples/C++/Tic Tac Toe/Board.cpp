#include "Board.h"
#include <iostream>
using namespace std;

// -----    CONSTRUCTORS    ----- //
Board::Board(int width, int height)
{
	// Set width and height as specified
	this->width = width;
	this->height = height;

	// Initialize data array
	data = new char*[width];
	for (int i = 0; i < width; i++)
		data[i] = new char[height];

	// Initialize all cells to '-'
	for (int posX = 0; posX < width; posX++)
	{
		for (int posY = 0; posY < height; posY++)
		{
			data[posX][posY] = '-';
		}
	}
}

// -----     FUNCTIONS      ----- //
void Board::Delete()
{
	// Delete data array
	for (int i = 0; i < width; i++)
		delete[] data[i];
	delete[] data;
}
void Board::Print()
{
	// Print header and column numbers
	cout << "Game Board:" << endl << endl;
	cout << "    ";
	for (int x = 0; x < width; x++)
		cout << "  " << x + 1 << "   ";
	cout << endl << endl;

	// Print each row number, row and dividing lines
	for (int y = 0; y < height; y++)
	{
		cout << " " << y + 1 << "  ";
		for (int x = 0; x < width; x++)
		{
			cout << "  " << data[x][y] << "  ";
			if (x < width - 1)
				cout << "|";
		}
		if (y < height - 1)
		{
			cout << endl << "    ";
			for (int x = 0; x < (width * 6) - 1; x++)
				cout << "_";
			cout << endl << endl;
		}
	}

	// Add whitespace
	cout << endl << endl;
}
bool Board::CheckWin()
{
	// Check if each value diagonally matches the first value
	// If board is not 1:1 ratio, a diagonal win is not possible

	char first;
	if (width == height)
	{
		// Top left to bottom right
		first = data[0][0];
		if (first != '-')
		{
			for (int i = 1; i < width; i++)
			{
				if (data[i][i] != first)
					break;
				if (i == width - 1)
					return true;
			}
		}
		// Bottm left to top right
		first = data[0][height - 1];
		if (first != '-')
		{
			for (int i = 1; i < width; i++)
			{
				if (data[i][height - i - 1] != first)
					break;
				if (i == width - 1)
					return true;
			}
		}
	}

	// Check if each value vertically matches the first value
	for (int x = 0; x < width; x++)
	{
		first = data[x][0];
		if (first != '-')
		{
			for (int y = 1; y < height; y++)
			{
				if (data[x][y] != first)
					break;
				if (y == height - 1)
					return true;
			}
		}
		
	}

	// Check if each value horizontally matches the first value
	for (int y = 0; y < height; y++)
	{
		first = data[0][y];
		if (first != '-')
		{
			for (int x = 1; x < width; x++)
			{
				if (data[x][y] != first)
					break;
				if (x == width - 1)
					return true;
			}
		}
	}

	// Return false if no win found
	return false;
}

// ----- GETTERS / SETTERS  ----- //
char Board::GetValue(int posX, int posY)
{
	// Return the character from the specified position
	// Note that data array is 0-indexed, but presented to the user as 1-indexed
	return data[posX - 1][posY - 1];
}
void Board::SetValue(char value, int posX, int posY)
{
	// Set the specified character at the specified position
	// Note that data array is 0-indexed, but presented to the user as 1-indexed
	data[posX - 1][posY - 1] = value;
}