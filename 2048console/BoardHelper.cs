using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    // Static class containing helper methods concerning dealing with the board
    public static class BoardHelper
    {
        // clones a given board, returns the clone
        public static int[][] CloneBoard(int[][] board)
        {
            int[][] newboard = new int[board.Length][];
            for (int i = 0; i < board.Length; i++)
            {
                newboard[i] = (int[])board[i].Clone();
            }
            return newboard;
        }

        // constructs a string representation of the board
        public static string ToString(int[][] array)
        {
            string representation = "";
            for (int y = GameEngine.ROWS - 1; y >= 0; y--)
            {
                for (int x = 0; x < GameEngine.COLUMNS; x++)
                {

                    string append = " " + array[x][y] + " ";
                    representation += append;

                    if (x != 3)
                    {
                        representation += "|";
                    }
                    else
                    {
                        representation += "\n";
                    }
                }
                if (y != 0)
                {
                    representation += "-------------\n";
                }
            }
            return representation;
        }

        // checks if the tile at column x, row y has already been merged
        public static bool TileAlreadyMerged(List<Cell> merged, int x, int y)
        {
            if (merged.Exists(item => item.x == x && item.y == y))
                return true;
            else
                return false;
        }

        // checks if the cell at column x, row y is occupied
        public static bool CellIsOccupied(int[][] board, int x, int y)
        {
            if (board[x][y] == 0)
                return false;
            else
                return true;
        }

        // multiplies the values of two boards cell by cell
        public static double[][] Multiplyboards(int[][] board1, double[][] board2)
        {
            double[][] result = new double[4][] {
				new double[board1.Length],
				new double[board1.Length],
				new double[board1.Length],
				new double[board1.Length]
			};
            for (int i = 0; i < board1.Length; i++)
            {
                for (int j = 0; j < board1.Length; j++)
                {
                    result[i][j] = (double)board1[i][j] * board2[i][j];
                }
            }
            return result;
        }

        // computes the sum of all cell values on a given board
        public static double boardSum(double[][] board)
        {
            double sum = 0;
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board.Length; j++)
                {
                    sum += board[i][j];
                }
            }
            return sum;
        }

        // returns the value of the highest tile on the board
        public static int HighestTile(int[][] board)
        {
            int highest = 0;
            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (board[i][j] > highest)
                        highest = board[i][j];
                }
            }
            return highest;
        }


        // Checks if a given board is in a game over state
        public static bool IsGameOver(int[][] board)
        {
            if (CheckLeft(board) || CheckRight(board) || CheckUp(board) || CheckDown(board))
                return false;
            else
                return true;
        }

        // This method checks if it is possile to move left in the given board
        // The method uses several tricks to speed up the check, such as realizing that if the first column is empty,
        // left is possible no matter what the rest of the board looks like. These tricks means that the method will
        // only run through the double for-loop in the worst case (if the only tiles that can be moved are in the top right corner
        public static bool CheckLeft(int[][] board)
        {
            int occupied = 0;
            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (i == 0 && board[i][j] != 0)
                        occupied++;
                    else if (i > 0 && board[i][j] != 0 && board[i - 1][j] == 0)
                    {
                        return true;
                    }
                    else if (i > 0 && board[i][j] != 0 && board[i][j] == board[i - 1][j])
                    {
                        return true;
                    }
                }
                if (i == 0 && occupied == 0)
                {
                    return true;
                }
            }
            return false;
        }

        // THis method checks if it is possible to move right in the given board
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckRight(int[][] board)
        {
            int occupied = 0;
            for (int i = GameEngine.COLUMNS - 1; i >= 0; i--)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (i == GameEngine.COLUMNS - 1 && board[i][j] != 0)
                        occupied++;
                    else if (i < GameEngine.COLUMNS - 1 && board[i][j] != 0 && board[i + 1][j] == 0)
                    { // empty to the right
                        return true;
                    }
                    else if (i < GameEngine.COLUMNS - 1 && board[i][j] != 0 && board[i][j] == board[i + 1][j])
                    { // mergeable
                        return true;
                    }
                }
                if (i == GameEngine.COLUMNS - 1 && occupied == 0)
                {
                    return true;
                }
            }
            return false;
        }

        // THis method checks if it is possible to move down in the given board
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckDown(int[][] board)
        {
            int occupied = 0;
            for (int j = 0; j < GameEngine.ROWS; j++)
            {
                for (int i = 0; i < GameEngine.COLUMNS; i++)
                {
                    if (j == 0 && board[i][j] != 0)
                        occupied++;
                    else if (j > 0 && board[i][j] != 0 && board[i][j - 1] == 0)
                        return true;
                    else if (j > 0 && board[i][j] != 0 && board[i][j] == board[i][j - 1])
                        return true;
                }
                if (j == 0 && occupied == 0)
                    return true;
            }
            return false;
        }

        // THis method checks if it is possible to move up in the given board
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckUp(int[][] board)
        {
            int occupied = 0;
            for (int j = GameEngine.ROWS - 1; j >= 0; j--)
            {
                for (int i = 0; i < GameEngine.COLUMNS; i++)
                {
                    if (j == GameEngine.ROWS - 1 && board[i][j] != 0)
                        occupied++;
                    else if (j < GameEngine.ROWS - 1 && board[i][j] != 0 && board[i][j + 1] == 0)
                        return true;
                    else if (j < GameEngine.ROWS - 1 && board[i][j] != 0 && board[i][j] == board[i][j + 1])
                        return true;
                }
                if (j == GameEngine.ROWS - 1 && occupied == 0)
                    return true;
            }
            return false;
        }
    }
}
