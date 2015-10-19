using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    public static class GridHelper
    {
        public static int[][] CloneGrid(int[][] grid)
        {
            int[][] newGrid = new int[grid.Length][];
            for (int i = 0; i < grid.Length; i++)
            {
                newGrid[i] = (int[])grid[i].Clone();
            }
            return newGrid;
        }

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

        public static bool TileAlreadyMerged(List<Cell> merged, int x, int y)
        {
            if (merged.Exists(item => item.x == x && item.y == y))
                return true;
            else
                return false;
        }

        public static bool CellIsOccupied(int[][] grid, int x, int y)
        {
            if (grid[x][y] == 0)
                return false;
            else
                return true;
        }

        public static double[][] MultiplyGrids(int[][] grid1, double[][] grid2)
        {
            double[][] result = new double[4][] {
				new double[grid1.Length],
				new double[grid1.Length],
				new double[grid1.Length],
				new double[grid1.Length]
			};
            for (int i = 0; i < grid1.Length; i++)
            {
                for (int j = 0; j < grid1.Length; j++)
                {
                    result[i][j] = (double)grid1[i][j] * grid2[i][j];
                }
            }
            return result;
        }


        public static double GridSum(double[][] grid)
        {
            double sum = 0;
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid.Length; j++)
                {
                    sum += grid[i][j];
                }
            }
            return sum;
        }


        // returns the value of the highest tile on the grid
        public static int HighestTile(int[][] grid)
        {
            int highest = 0;
            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (grid[i][j] > highest)
                        highest = grid[i][j];
                }
            }
            return highest;
        }

        // This method checks if it is possile to move left in the given grid
        // The method uses several tricks to speed up the check, such as realizing that if the first column is empty,
        // left is possible no matter what the rest of the grid looks like. These tricks means that the method will
        // only run through the double for-loop in the worst case (if the only tiles that can be moved are in the top right corner
        public static bool CheckLeft(int[][] grid)
        {
            int occupied = 0;
            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (i == 0 && grid[i][j] != 0)
                        occupied++;
                    else if (i > 0 && grid[i][j] != 0 && grid[i - 1][j] == 0)
                    {
                        return true;
                    }
                    else if (i > 0 && grid[i][j] != 0 && grid[i][j] == grid[i - 1][j])
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

        // THis method checks if it is possible to move right in the given grid
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckRight(int[][] grid)
        {
            int occupied = 0;
            for (int i = GameEngine.COLUMNS - 1; i >= 0; i--)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (i == GameEngine.COLUMNS - 1 && grid[i][j] != 0)
                        occupied++;
                    else if (i < GameEngine.COLUMNS - 1 && grid[i][j] != 0 && grid[i + 1][j] == 0)
                    { // empty to the right
                        return true;
                    }
                    else if (i < GameEngine.COLUMNS - 1 && grid[i][j] != 0 && grid[i][j] == grid[i + 1][j])
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

        // THis method checks if it is possible to move down in the given grid
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckDown(int[][] grid)
        {
            int occupied = 0;
            for (int j = 0; j < GameEngine.ROWS; j++)
            {
                for (int i = 0; i < GameEngine.COLUMNS; i++)
                {
                    if (j == 0 && grid[i][j] != 0)
                        occupied++;
                    else if (j > 0 && grid[i][j] != 0 && grid[i][j - 1] == 0)
                        return true;
                    else if (j > 0 && grid[i][j] != 0 && grid[i][j] == grid[i][j - 1])
                        return true;
                }
                if (j == 0 && occupied == 0)
                    return true;
            }
            return false;
        }

        // THis method checks if it is possible to move up in the given grid
        // The method uses similar tricks as described for method CheckLeft to speed up the process
        public static bool CheckUp(int[][] grid)
        {
            int occupied = 0;
            for (int j = GameEngine.ROWS - 1; j >= 0; j--)
            {
                for (int i = 0; i < GameEngine.COLUMNS; i++)
                {
                    if (j == GameEngine.ROWS - 1 && grid[i][j] != 0)
                        occupied++;
                    else if (j < GameEngine.ROWS - 1 && grid[i][j] != 0 && grid[i][j + 1] == 0)
                        return true;
                    else if (j < GameEngine.ROWS - 1 && grid[i][j] != 0 && grid[i][j] == grid[i][j + 1])
                        return true;
                }
                if (j == GameEngine.ROWS - 1 && occupied == 0)
                    return true;
            }
            return false;
        }
    }
}
