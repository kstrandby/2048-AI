using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{

    // Static class used for methods to calculate heuristics
    public static class AI
    {
        // Constants

        // upper and lower bounds for heuristics
        const double upper_smoothness = 0;
        const double lower_smoothness = -384;
        const double upper_monotonicity = 0;
        const double lower_monotonicity = -192;
        const double upper_emptycells = 15;
        const double lower_emptycells = 0;
        const double upper_highestvalue = 17;
        const double lower_highestvalue = 1;

        // weights
        const double smoothness_weight = 0.1;
        const double monotonicity_weight = 1.0;
        const double emptycells_weight = 0.5;
        const double highestvalue_weight = 1.0;
        const double snake_weight = 0.5;
        const double trappedpenalty_weight = 2.0;

        // for Expectimax Star1 pruning
        // NB: Remember to change this according to the heuristic in use
        internal static double GetUpperBound()
        {
            double bound = smoothness_weight * upper_smoothness + monotonicity_weight * upper_monotonicity + emptycells_weight * upper_emptycells + highestvalue_weight * upper_highestvalue;
            return bound + 10;
        }

        // for Expectimax Star1 pruning
        // NB: Remember to change this according to the heuristic in use
        internal static double GetLowerBound()
        {
            double bound = smoothness_weight * lower_smoothness + monotonicity_weight * lower_monotonicity + emptycells_weight * lower_emptycells + highestvalue_weight * lower_highestvalue;
            return bound - 10;
        }

        public static double Evaluate(GameEngine gameEngine, State state)
        {
            if (state.IsGameOver())
            {
                return -1000;
                //return GetLowerBound();
            }
            else
            {
                double eval = 0;

                double smoothness = Smoothness(state);
                double monotonicity = Monotonicity(state);
                double emptycells = EmptyCells(state);
                double highestvalue = HighestValue(state);
                double trappedpenalty = TrappedPenalty(state);
                eval = smoothness_weight * smoothness + monotonicity_weight * monotonicity + emptycells_weight * emptycells + highestvalue_weight * highestvalue -trappedpenalty_weight * trappedpenalty;


                if (state.IsWin())
                    return 1000 + eval;
                else
                {
                    return eval;
                }
            }
        }

        public static double TrappedPenalty(State state)
        {
            double trapped = 0;

            for (int i = 0; i < GameEngine.COLUMNS; i++)
            {
                for (int j = 0; j < GameEngine.ROWS; j++)
                {
                    if (state.Grid[i][j] != 0)
                    {
                        // check neighbours in vertical direction
                        int neighbourRowAbove = j + 1;
                        int neighbourRowBelow = j - 1;
                        if ((neighbourRowAbove < GameEngine.ROWS && state.Grid[i][neighbourRowAbove] > state.Grid[i][j] && j == 0) // trapped between wall below and higher card above
                            || (neighbourRowBelow >= 0 && state.Grid[i][neighbourRowBelow] > state.Grid[i][j] && j == 3) // trapped between wall above and higher card below
                            || (neighbourRowAbove < GameEngine.ROWS && state.Grid[i][neighbourRowAbove] > state.Grid[i][j] // trapped between two higher cards
                                && neighbourRowBelow >= 0 && state.Grid[i][neighbourRowBelow] > state.Grid[i][j]))
                        {
                            trapped++;
                        }

                        // check neighbours in horizontal direction
                        int neighbourColumnToRight = i + 1;
                        int neighbourColumnToLeft = i - 1;
                        if ((neighbourColumnToRight < GameEngine.COLUMNS && state.Grid[neighbourColumnToRight][j] > state.Grid[i][j] && i == 0) // trapped between wall to the left and higher card to the right
                            || (neighbourColumnToLeft >= 0 && state.Grid[neighbourColumnToLeft][j] > state.Grid[i][j] && i == 3) // trapped between wall to the right and higher card to the left
                            || (neighbourColumnToRight < GameEngine.COLUMNS && state.Grid[neighbourColumnToRight][j] > state.Grid[i][j] // trapped between two higher cards
                                && neighbourColumnToLeft >= 0 && state.Grid[neighbourColumnToLeft][j] > state.Grid[i][j]))
                        {
                            trapped++;
                        }
                    }
                }
            }
            return trapped;
        }

        // The highest value on the grid (in log2)
        // range: {1, 17}
        public static double HighestValue(State state)
        {
            return Math.Log(GridHelper.HighestTile(state.Grid)) / Math.Log(2);
        }

        // returns the number of empty cells on the grid
        // ranging between 0 and 16
        public static double EmptyCells(State state)
        {
            int emptyCells = 0;
            for (int i = 0; i < state.Grid.Length; i++)
            {
                for (int j = 0; j < state.Grid.Length; j++)
                {
                    if (state.Grid[i][j] == 0)
                        emptyCells++;
                }
            }
            return emptyCells;
        }


        // returns the number of points in the state
        public static double Points(State state)
        {
            return state.Points;
        }


        // This heuristic measures the "smoothness" of the grid
        // It does so by measuring the difference between neighbouring tiles (the log2 difference) and summing these
        //
        // The range of this heuristic is: {-384, 0}
        
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
                            smoothness += Math.Abs(currentValue - neighbourValue);
                        }

                        if (nearestTileUp.IsValid() && state.Grid[nearestTileUp.x][nearestTileUp.y] != 0)
                        {
                            double neighbourValue = Math.Log(state.Grid[nearestTileUp.x][nearestTileUp.y]) / Math.Log(2);
                            smoothness += Math.Abs(currentValue - neighbourValue);
                        }
                    }
                }
            }
            return -smoothness;
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

 

        public static double Corner(State state)
        {
            double[][] corner1 = new double[][] {
                new double[]{20,12,4,0.4},
                new double[]{19,11,3,0.3},
                new double[]{18,10,2,0.2},
                new double[]{17,9,1,0.1}
            };

            double[][] corner2 = new double[][] {
                new double[]{0.4,4,12,20},
                new double[]{0.3,3,11,19},
                new double[]{0.2,2,10,18},
                new double[]{0.1,1,9,17}
            };

            double[][] corner3 = new double[][] {
                new double[]{17,9,1,0.1},
                new double[]{18,10,2,0.2},
                new double[]{19,11,3,0.3},
                new double[]{20,12,4,0.4}
            };

            double[][] corner4 = new double[][] {
                new double[]{0.1,1,9,17},
                new double[]{0.2,2,10,18},
                new double[]{0.3,3,11,19},
                new double[]{0.4,4,12,20}
            };

            double[][] corner5 = new double[][] {
                new double[]{20,19,18,17},
                new double[]{12,11,10,9},
                new double[]{4,3,2,1},
                new double[]{0.4,0.3,0.2,0.1}
            };

            double[][] corner6 = new double[][] {
                new double[]{17,18,19,20},
                new double[]{9,10,11,12},
                new double[]{1,2,3,4},
                new double[]{0.1,0.2,0.3,0.4}
            };

            double[][] corner7 = new double[][] {
                new double[]{0.4,0.3,0.2,0.1},
                new double[]{4,3,2,1},
                new double[]{12,11,10,9},
                new double[]{20,19,18,17}
            };

            double[][] corner8 = new double[][] {
                new double[]{0.1,0.2,0.3,0.4},
                new double[]{1,2,3,4},
                new double[]{9,10,11,12},
                new double[]{17,18,19,20}
            };
            List<double[][]> weightMatrices = new List<double[][]>();

            weightMatrices.Add(corner1);
            weightMatrices.Add(corner2);
            weightMatrices.Add(corner3);
            weightMatrices.Add(corner4);
            weightMatrices.Add(corner5);
            weightMatrices.Add(corner6);
            weightMatrices.Add(corner7);
            weightMatrices.Add(corner8);

            return MaxProductMatrix(state.Grid, weightMatrices);
        }




        // Arranges the tiles in a "snake"
        // As there are 8 different ways the tiles can be arranged in a "snake" on the grid, this method
        // finds the one that fits the best, allowing the AI to adjust to a different "snake" pattern
        public static double WeightSnake(State state)
        {
            double[][] snake1 = new double[][] {
                new double[]{20,9,4,.1},
                new double[]{19,10,3,0.2},
                new double[]{18,11,2,0.3},
                new double[]{17,12,1,0.4}
            };

            double[][] snake2 = new double[][] {
                new double[]{20,19,18,17},
                new double[]{9,10,11,12},
                new double[]{4,3,2,1},
                new double[]{0.1,0.2,0.3,0.4}
            };

            double[][] snake3 = new double[][]{
                new double[]{17,12,1,0.4},
                new double[]{18,11,2,0.3},
                new double[]{19,10,3,0.2},
                new double[]{20,9,4,0.1}
            };

            double[][] snake4 = new double[][] {
                new double[]{17,18,19,20},
                new double[]{12,11,10,9},
                new double[]{1,2,3,4},
                new double[]{0.4,0.3,0.2,0.1}
            };

            double[][] snake5 = new double[][] {
                new double[]{0.1,0.2,0.3,0.4},
                new double[]{4,3,2,1},
                new double[]{9,10,11,12},
                new double[]{20,19,18,17}
            };

            double[][] snake6 = new double[][] {
                new double[]{0.1,4,9,20},
                new double[]{0.2,3,10,19},
                new double[]{0.3,2,11,18},
                new double[]{0.4,1,12,17}
            };

            double[][] snake7 = new double[][] {
                new double[]{0.4,0.3,0.2,0.1},
                new double[]{1,2,3,4},
                new double[]{12,11,10,9},
                new double[]{17,18,19,20}
            };

            double[][] snake8 = new double[][] {
                new double[]{0.4,1,12,17},
                new double[]{0.3,2,11,18},
                new double[]{0.2,3,10,19},
                new double[]{0.1,4,9,20}
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
            List<double> sums = new List<double>(){0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0};
            
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


        // returns a score ranking a state according to how the tiles are increasing/decreasing in all directions
        // increasing/decreasing in the same directions (for example generally increasing in up and right direction) will return higher score 
        // than a state increasing in one row and decreasing in another row

        // range: {-192, 0}
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
                        double currentValue = Math.Log(state.Grid[i][current]) / Math.Log(2);
                        double nextValue = Math.Log(state.Grid[i][next]) / Math.Log(2);
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
                        double currentValue = Math.Log(state.Grid[current][j]) / Math.Log(2);
                        double nextValue = Math.Log(state.Grid[next][j]) / Math.Log(2);
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
    }
}
