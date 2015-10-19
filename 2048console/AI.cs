using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{

    // Static class used to hold method for calculating different heuristics
    public static class AI
    {
        private const int WEIGHT_SUM = 16 + 15 + 14 + 13 + 12 + 11 + 10 + 9 + 8 + 7 + 6 + 5 + 4 + 3 + 2 + 1;

        internal static double GetUpperBound(int highestTile)
        {
            return 15;
        }

        internal static double GetLowerBound(int highestTile)
        {
            return -1;
        }

        public static double Evaluate(GameEngine gameEngine, State state)
        {
            if (state.IsGameOver())
            {
                return -1000;
            }
            else
            {
                double eval = 0;

                eval = 0.1 * Smoothness(state) + Monotonicity(state) + 0.5 * EmptyCells(state) + GridHelper.HighestTile(state.Grid) - 1.5 * IsolationPenalty(state);
                //double snake = WeightSnake(state);
                //eval = snake - Math.Log(snake) * IsolationPenalty(state);

                if (state.IsWin())
                    return 100000000 + eval;
                else
                {
                    return eval;
                }
            }
        }

        public static double EvaluateAnnoyingState(GameEngine gameEngine, State state)
        {
            return Evaluate(gameEngine, state);

        }

        // Arranges the tiles in the bottom row and left-most column
        // The higher the tiles in cells that are NOT in left-most column or bottom row, the higher this heuristic score will be
        //
        // NOTE: Use this heuristic as a penalty
        public static double LargeValuesAtBorder(State state)
        {
            double score = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] != 0)
                    {
                        int dist = Math.Min(i, j);
                        score += dist * Math.Log(state.Grid[i][j]) / Math.Log(2);
                    }
                }
            }
            return score;
        }

        // This heuristic measures the "smoothness" of the grid
        // 
        public static double Smoothness(State state)
        {
            double smoothness = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] != 0)
                    {
                        double currentValue = Math.Log(state.Grid[i][j]) / Math.Log(2);

                        // we only check right and up for each tile
                        Cell nearestTileRight = FindNearestTile(new Cell(i, j), DIRECTION.RIGHT, state.Grid);
                        Cell nearestTileUp = FindNearestTile(new Cell(i, j), DIRECTION.UP, state.Grid);

                        // check that we found a tile (do not take empty cells into account)
                        if (nearestTileRight.IsValid() && state.Grid[nearestTileRight.x][nearestTileRight.y] != 0) {
                            double neighbourValue = Math.Log(state.Grid[nearestTileRight.x][nearestTileRight.y]) / Math.Log(2);
                            smoothness -= Math.Abs(currentValue - neighbourValue);
                        }

                        if (nearestTileUp.IsValid() && state.Grid[nearestTileUp.x][nearestTileUp.y] != 0)
                        {
                            int neighbourValue = state.Grid[nearestTileUp.x][nearestTileUp.y];
                            smoothness -= Math.Abs(currentValue - neighbourValue);
                        }
                    }
                }
            }
            return smoothness;
        }


        public static Cell FindNearestTile(Cell from, DIRECTION dir, int[][] grid)
        {
            int x = from.x, y = from.y;
            if (dir == DIRECTION.LEFT)
            {
                x -= 1;
                while (x >= 0 && grid[x][y] == 0)
                {
                    x--;
                }
            }
            else if (dir == DIRECTION.RIGHT)
            {
                x += 1;
                while (x < grid.Length && grid[x][y] == 0)
                {
                    x++;
                }
            } 
            else if(dir == DIRECTION.UP) 
            {
                y += 1;
                while (y < grid.Length && grid[x][y] == 0)
                {
                    y++;
                }
            }
            else if (dir == DIRECTION.DOWN) 
            {
                y -= 1;
                while (y >= 0 && grid[x][y] == 0)
                {
                    y--;
                }
            }
            return new Cell(x, y);
        }


        // This heuristic counts the number of isolated tiles
        // Here an isolated tile is considered a tile, where all neighbours are occupied by other tiles with higher values
        //
        // NOTE: Use this heuristic as a penalty
        public static double IsolationPenalty(State state)
        {
            int isolatedTiles = 0;

            // check that all neighbours have higher values
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] != 0)
                    {
                        List<Tuple<int,int>> neighbours = GetNeighbours(state, i, j);
                        int numberOfNeighbours = neighbours.Count;
                        int numberOfHigherNeighbours = 0;
                        foreach (Tuple<int, int> neighbour in neighbours)
                        {
                            if (state.Grid[i][j] < state.Grid[neighbour.Item1][neighbour.Item2]) numberOfHigherNeighbours++;
                        }
                        if (numberOfHigherNeighbours == numberOfNeighbours) isolatedTiles++;
                    }
                }
            }
            return isolatedTiles;
        }

        // Helper method to get a list of neighbours to a specific cell
        private static List<Tuple<int, int>> GetNeighbours(State state, int i, int j)
        {
            List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();

            if (i == 0)
            {
                if (j == 0)
                {
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
                else if (j == 3)
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
                else
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
            }
            else if (i == 3)
            {
                if (j == 0)
                {
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                }
                else if (j == 3)
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                }
                else
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                }
            }
            else
            {
                if (j == 0)
                {
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
                else if (j == 3)
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
                else
                {
                    neighbours.Add(new Tuple<int, int>(i, j - 1));
                    neighbours.Add(new Tuple<int, int>(i, j + 1));
                    neighbours.Add(new Tuple<int, int>(i - 1, j));
                    neighbours.Add(new Tuple<int, int>(i + 1, j));
                }
            }
            return neighbours;
        }

 

        public static double CornerMountain(State state)
        {
            double[][] mountainMatrix = new double[][] {
				new double[]{0.7,0.6,0.5,0.4},
				new double[]{0.6,0.5,0.4,0.3},
				new double[]{0.5,0.4,0.3,0.2},
				new double[]{0.4,0.3,0.2,0.1}
			};

            double[][] mountainMatrix2 = new double[][] {
                new double[]{15.0,14.0,13.0,12.0},
				new double[]{14.0,10.0,8.0,6.0},
				new double[]{13.0,8.0,6.0,4.0},
				new double[]{12.0,6.0,4.0,2.0}
            };

            return GridHelper.GridSum(GridHelper.MultiplyGrids(state.Grid, mountainMatrix2));
        }



        // Arranges the tiles in a "snake"
        // As there are 8 different ways the tiles can be arranged in a "snake" on the grid, this method
        // finds the one that fits the best, allowing the AI to adjust to a different "snake" pattern
        public static double WeightSnake(State state)
        {
            double[][] snake1 = new double[][] {
                new double[]{16,9,8,1},
                new double[]{15,10,7,2},
                new double[]{14,11,6,3},
                new double[]{13,12,5,4}
            };

            double[][] snake2 = new double[][] {
                new double[]{16,15,14,13},
                new double[]{9,10,11,12},
                new double[]{8,7,6,5},
                new double[]{1,2,3,4}
            };

            double[][] snake3 = new double[][]{
                new double[]{13,12,5,4},
                new double[]{14,11,6,3},
                new double[]{15,10,7,2},
                new double[]{16,9,8,1}
            };

            double[][] snake4 = new double[][] {
                new double[]{13,14,15,16},
                new double[]{12,11,10,9},
                new double[]{5,6,7,8},
                new double[]{4,3,2,1}
            };

            double[][] snake5 = new double[][] {
                new double[]{1,2,3,4},
                new double[]{8,7,6,5},
                new double[]{9,10,11,12},
                new double[]{16,15,14,13}
            };

            double[][] snake6 = new double[][] {
                new double[]{1,8,9,16},
                new double[]{2,7,10,15},
                new double[]{3,6,11,14},
                new double[]{4,5,12,13}
            };

            double[][] snake7 = new double[][] {
                new double[]{4,3,2,1},
                new double[]{5,6,7,8},
                new double[]{12,11,10,9},
                new double[]{13,14,15,16}
            };

            double[][] snake8 = new double[][] {
                new double[]{4,5,12,13},
                new double[]{3,6,11,14},
                new double[]{2,7,10,15},
                new double[]{1,8,9,16}
            };

            List<double[][]> weightMatrices = new List<double[][]>();
            weightMatrices.Add(snake1);
            weightMatrices.Add(snake2);
            weightMatrices.Add(snake3);
            weightMatrices.Add(snake4);
            weightMatrices.Add(snake5);
            weightMatrices.Add(snake6);
            weightMatrices.Add(snake7);
            weightMatrices.Add(snake8);

            return MaxProductMatrix(state.Grid, weightMatrices);
        }


        // Helper method for the WeightSnake heuristic - finds the weight matrix that gives the greatest
        // sum when multiplied with the grid and summed up, returns this sum
        private static double MaxProductMatrix(int[][] grid, List<double[][]> weightMatrices)
        {
            List<double> sums = new List<double>(){0,0,0,0,0,0,0,0};
            
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid.Length; j++)
                {
                    for(int k = 0; k < weightMatrices.Count; k++)
                    {
                        double mult = weightMatrices[k][i][j] * grid[i][j];
                        weightMatrices[k][i][j] = mult;
                        sums[k] += mult;
                    }
                }
            }
            // find the largest sum
            return sums.Max();
        }

        // returns the number of possible merges of tiles on the grid
        // ranging between 0 and 24 (24 is when every square is occupied by a tile of the same value)
        public static int Mergeability(State state)
        {
            int mergePoints = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (i < state.Grid.Length - 1 && state.Grid[i][j] != 0)
                    {
                        int k = i + 1;
                        while (k < state.Grid.Length)
                        {
                            if (state.Grid[k][j] == 0)
                            {
                                k++;
                            }
                            else if (state.Grid[k][j] == state.Grid[i][j])
                            {
                                mergePoints++;
                                break;
                            }
                            else
                            { // other value, no more possible merges in row
                                break;
                            }
                        }
                    }
                    if (j < state.Grid.Length - 1 && state.Grid[i][j] != 0)
                    {
                        int k = j + 1;
                        while (k < state.Grid.Length)
                        {
                            if (state.Grid[i][k] == 0)
                            {
                                k++;
                            }
                            else if (state.Grid[i][k] == state.Grid[i][j])
                            {
                                mergePoints++;
                                break;
                            }
                            else
                            { // other value, no more possible merges in column
                                break;
                            }
                        }
                    }
                }
            }
            return mergePoints;
        }

        // returns the number of empty cells on the grid
        // ranging between 2 and 16
        public static double EmptyCells(State state)
        {
            int emptySquares = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] == 0)
                        emptySquares++;
                }
            }
            return emptySquares;
        }


        // returns the number of points in the state
        public static double Points(State state)
        {
            return state.Points;
        }


        // returns a score ranking a state according to how bad tiles with similar values are positioned next to each other
        // tiles with a huge difference in value next to each other will get a large score, whereas tiles with similar values will get a lower score
        // 
        // Note: use this heuristic as a penalty - i.e. subtract it, or take the negative of it
        public static int MonotonicityValDiff(State state)
        {
            int score = 0;

            int[] directions = { -1, 0, 1 };

            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    // don't consider empty cells
                    if (state.Grid[i][j] != 0)
                    {

                        // find the distance from each neighbour (difference between values of tiles)
                        // and count the number of neighbours
                        int neighbours = 0;
                        int valueDiffs = 0;
                        foreach (int x_direction in directions)
                        {
                            // check x boundaries
                            int next_x = i + x_direction;
                            if (next_x < 0 || next_x >= state.Grid.Length)
                                continue;

                            foreach (int y_direction in directions)
                            {
                                // check y boundaries
                                int next_y = j + y_direction;
                                if (next_y < 0 || next_y >= state.Grid.Length)
                                    continue;

                                // don't consider empty cells
                                if (state.Grid[next_x][next_y] > 0)
                                {
                                    neighbours++;
                                    valueDiffs += Math.Abs(state.Grid[i][j] - state.Grid[next_x][next_y]);
                                }
                            }
                        }
                        score += valueDiffs / neighbours;
                    }
                }
            }
            return score;
        }


        // returns a score ranking a state according to how the tiles are increasing/decreasing in all directions
        // increasing/decreasing in the same directions (for example generally increasing in up and right direction) will return higher score 
        // than a state increasing in one row and decreasing in another row
        public static double Monotonicity(State state)
        {
            double left = 0;
            double right = 0;
            double up = 0;
            double down = 0;

            // up/down direction
            for (int i = 0; i < state.Grid.Length; i++)
            {
                int current = 0;
                int next = current + 1;
                while (next < state.Grid.Length)
                {
                    // skip empty cells
                    while (next < state.Grid.Length && state.Grid[i][next] == 0)
                        next++;
                    // check boundaries
                    if (next >= state.Grid.Length)
                        next--;

                    // only count instances where both cells are occupied
                    if (state.Grid[i][current] != 0 && state.Grid[i][next] != 0)
                    {
                        double currentValue = Math.Log(state.Grid[i][current]);
                        double nextValue = Math.Log(state.Grid[i][next]);
                        if (currentValue > nextValue) // increasing in down direction
                            down += nextValue - currentValue;
                        else if (nextValue > currentValue) // increasing in up direction
                            up += currentValue - nextValue;
                    }

                    current = next;
                    next++;
                }
            }

            // left/right direction
            for (int j = 0; j < state.Grid.Length; j++)
            {
                int current = 0;
                int next = current + 1;
                while (next < state.Grid.Length)
                {
                    // skip empty cells
                    while (next < state.Grid.Length && state.Grid[next][j] == 0)
                        next++;
                    // check boundaries
                    if (next >= state.Grid.Length)
                        next--;

                    // only consider instances where both cells are occupied
                    if (state.Grid[current][j] != 0 && state.Grid[next][j] != 0)
                    {
                        double currentValue = Math.Log(state.Grid[current][j]);
                        double nextValue = Math.Log(state.Grid[next][j]);
                        if (currentValue > nextValue) // increasing in left direction
                            left += nextValue - currentValue;
                        else if (nextValue > currentValue) // increasing in right direction
                            right += currentValue - nextValue;
                    }

                    current = next;
                    next++;
                }
            }
            return Math.Max(up, down) + Math.Max(left, right);
        }

        // This heuristic only gives points if the highest tile is in the bottom left corner
        // It then gives more points if the second highest tile is next to (on the bottom row)
        // and more again if the third highest tile is next to the second highest (also on bottom row)
        public static double ThreeTilesStatic(State state)
        {
            double points = 0;
            int highest = 0, secondHighest = 0, thirdHighest = 0;
            int highest_x = 0, highest_y = 0, secondHighest_x = 0, secondHighest_y = 0, thirdHighest_x = 0, thirdHighest_y = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] > highest)
                    {
                        highest = state.Grid[i][j];
                        highest_x = i;
                        highest_y = j;
                    }
                    else if (state.Grid[i][j] <= highest && state.Grid[i][j] > secondHighest)
                    {
                        secondHighest = state.Grid[i][j];
                        secondHighest_x = i;
                        secondHighest_y = j;
                    }
                    else if (state.Grid[i][j] <= secondHighest && state.Grid[i][j] > thirdHighest)
                    {
                        thirdHighest = state.Grid[i][j];
                        thirdHighest_x = i;
                        thirdHighest_y = j;
                    }
                }
            }

            if (highest_x == 0 && highest_y == 0)
            {
                points += highest;

                if (secondHighest_x == 1 && secondHighest_y == 0)
                {
                    points += secondHighest;

                    if (thirdHighest_x == 2 && thirdHighest_y == 0)
                    {
                        points += thirdHighest;
                    }
                }
            }
            return points;
        }
    }
}
