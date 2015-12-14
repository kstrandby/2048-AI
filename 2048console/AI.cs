using _2048console.GeneticAlgorithm;
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
        const double upper_points = 21.90686858; //Math.Log(3932100) / Math.Log(2)
        const double lower_points = 0;
        const double upper_trappedpenalty = 0;
        const double lower_trappedpenalty = -8;

        // weights
        const double smoothness_weight = 0.1;
        const double monotonicity_weight = 1.0;
        const double emptycells_weight = 0.5;
        const double highestvalue_weight = 1.0;
        const double snake_weight = 0.5;
        const double trappedpenalty_weight = 2.0;

        // for Expectimax Star1 pruning
        internal static double GetUpperBound(WeightVector weights)
        {
            double bound = weights.Empty_cells * upper_emptycells + weights.Highest_tile * upper_highestvalue + weights.Monotonicity * upper_monotonicity 
               + weights.Points * upper_points + weights.Smoothness * upper_smoothness + weights.Trapped_penalty * upper_trappedpenalty;
            return bound + 10;
        }

        // for Expectimax Star1 pruning
        internal static double GetLowerBound(WeightVector weights)
        {
            double bound = weights.Empty_cells * lower_emptycells + weights.Highest_tile * lower_highestvalue + weights.Monotonicity * lower_monotonicity
                + weights.Points * lower_points + weights.Smoothness * lower_smoothness + weights.Trapped_penalty * lower_trappedpenalty;
            return bound - 10;
        }

        // for Expectimax Star1 pruning
        internal static double GetUpperBound()
        {
            return 0;
        }

        // for Expectimax Star1 pruning
        internal static double GetLowerBound()
        {
            return 0;
        }

        public static double EvaluateWithWeights(State state, WeightVector weights)
        {
            if (state.IsGameOver()) return GetLowerBound(weights) - 10;
            else
            {
                double emptycells = EmptyCells(state);
                double highestvalue = HighestValue(state);
                double monotonicity = Monotonicity(state);
                double points = Points(state);
                double smoothness = Smoothness(state);
                double trappedpenalty = TrappedPenalty(state);

                double eval = weights.Empty_cells * emptycells + weights.Highest_tile * highestvalue + weights.Monotonicity * monotonicity + weights.Points * points + weights.Smoothness * smoothness - weights.Trapped_penalty * trappedpenalty;

                if (state.IsWin())
                {
                    return eval + 10;
                }
                else return eval;
            }
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

                //double smoothness = Smoothness(state);
                //double monotonicity = Monotonicity(state);
                //double emptycells = EmptyCells(state);
                //double highestvalue = HighestValue(state);
                //double trappedpenalty = TrappedPenalty(state);
                //eval = smoothness_weight * smoothness + monotonicity_weight * monotonicity + emptycells_weight * emptycells + highestvalue_weight * highestvalue -trappedpenalty_weight * trappedpenalty;
                eval = Smoothness(state);

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
                    if (state.Board[i][j] != 0)
                    {
                        // check neighbours in vertical direction
                        int neighbourRowAbove = j + 1;
                        int neighbourRowBelow = j - 1;
                        if ((neighbourRowAbove < GameEngine.ROWS && state.Board[i][neighbourRowAbove] > state.Board[i][j] && j == 0) // trapped between wall below and higher card above
                            || (neighbourRowBelow >= 0 && state.Board[i][neighbourRowBelow] > state.Board[i][j] && j == 3) // trapped between wall above and higher card below
                            || (neighbourRowAbove < GameEngine.ROWS && state.Board[i][neighbourRowAbove] > state.Board[i][j] // trapped between two higher cards
                                && neighbourRowBelow >= 0 && state.Board[i][neighbourRowBelow] > state.Board[i][j]))
                        {
                            trapped++;
                        }

                        // check neighbours in horizontal direction
                        int neighbourColumnToRight = i + 1;
                        int neighbourColumnToLeft = i - 1;
                        if ((neighbourColumnToRight < GameEngine.COLUMNS && state.Board[neighbourColumnToRight][j] > state.Board[i][j] && i == 0) // trapped between wall to the left and higher card to the right
                            || (neighbourColumnToLeft >= 0 && state.Board[neighbourColumnToLeft][j] > state.Board[i][j] && i == 3) // trapped between wall to the right and higher card to the left
                            || (neighbourColumnToRight < GameEngine.COLUMNS && state.Board[neighbourColumnToRight][j] > state.Board[i][j] // trapped between two higher cards
                                && neighbourColumnToLeft >= 0 && state.Board[neighbourColumnToLeft][j] > state.Board[i][j]))
                        {
                            trapped++;
                        }
                    }
                }
            }
            return trapped;
        }

        // The highest value on the board (in log2)
        // range: {1, 17}
        public static double HighestValue(State state)
        {
            return Math.Log(BoardHelper.HighestTile(state.Board)) / Math.Log(2);
        }

        // returns the number of empty cells on the board
        // ranging between 0 and 16
        public static double EmptyCells(State state)
        {
            int emptyCells = 0;
            for (int i = 0; i < state.Board.Length; i++)
            {
                for (int j = 0; j < state.Board.Length; j++)
                {
                    if (state.Board[i][j] == 0)
                        emptyCells++;
                }
            }
            return emptyCells;
        }


        // returns the number of points in the state
        public static double Points(State state)
        {
            if (state.Points == 0) return 0;
            else return Math.Log(state.Points) / Math.Log(2);
        }


        // This heuristic measures the "smoothness" of the board
        // It does so by measuring the difference between neighbouring tiles (the log2 difference) and summing these
        //
        // The range of this heuristic is: {-384, 0}
        
        public static double Smoothness(State state)
        {
            double smoothness = 0;
            for (int i = 0; i < state.Board.Length; i++)
            {
                for (int j = 0; j < state.Board.Length; j++)
                {
                    if (state.Board[i][j] != 0)
                    {
                        double currentValue = Math.Log(state.Board[i][j]) / Math.Log(2);

                        // we only check right and up for each tile
                        Cell nearestTileRight = FindNearestTile(new Cell(i, j), DIRECTION.RIGHT, state.Board);
                        Cell nearestTileUp = FindNearestTile(new Cell(i, j), DIRECTION.UP, state.Board);

                        // check that we found a tile (do not take empty cells into account)
                        if (nearestTileRight.IsValid() && state.Board[nearestTileRight.x][nearestTileRight.y] != 0) {
                            double neighbourValue = Math.Log(state.Board[nearestTileRight.x][nearestTileRight.y]) / Math.Log(2);
                            smoothness += Math.Abs(currentValue - neighbourValue);
                        }

                        if (nearestTileUp.IsValid() && state.Board[nearestTileUp.x][nearestTileUp.y] != 0)
                        {
                            double neighbourValue = Math.Log(state.Board[nearestTileUp.x][nearestTileUp.y]) / Math.Log(2);
                            smoothness += Math.Abs(currentValue - neighbourValue);
                        }
                    }
                }
            }
            return -smoothness;
        }


        public static Cell FindNearestTile(Cell from, DIRECTION dir, int[][] board)
        {
            int x = from.x, y = from.y;
            if (dir == DIRECTION.LEFT)
            {
                x -= 1;
                while (x >= 0 && board[x][y] == 0)
                {
                    x--;
                }
            }
            else if (dir == DIRECTION.RIGHT)
            {
                x += 1;
                while (x < board.Length && board[x][y] == 0)
                {
                    x++;
                }
            } 
            else if(dir == DIRECTION.UP) 
            {
                y += 1;
                while (y < board.Length && board[x][y] == 0)
                {
                    y++;
                }
            }
            else if (dir == DIRECTION.DOWN) 
            {
                y -= 1;
                while (y >= 0 && board[x][y] == 0)
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

 
        // Arranges tiles up against a corner
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

            return MaxProductMatrix(state.Board, weightMatrices);
        }




        // Arranges the tiles in a "snake"
        // As there are 8 different ways the tiles can be arranged in a "snake" on the board, this method
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

            return MaxProductMatrix(state.Board, weightMatrices);
        }


        // Helper method for the WeightSnake heuristic - finds the weight matrix that gives the greatest
        // sum when multiplied with the board and summed up, returns this sum
        private static double MaxProductMatrix(int[][] board, List<double[][]> weightMatrices)
        {
            List<double> sums = new List<double>(){0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0};
            
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board.Length; j++)
                {
                    for(int k = 0; k < weightMatrices.Count; k++)
                    {
                        double mult = weightMatrices[k][i][j] * board[i][j];
                        weightMatrices[k][i][j] = mult;
                        sums[k] += mult;
                    }
                }
            }
            // find the largest sum
            return sums.Max();
        }

        // returns the number of possible merges of tiles on the board
        // ranging between 0 and 24 (24 is when every square is occupied by a tile of the same value)
        public static int Mergeability(State state)
        {
            int mergePoints = 0;
            for (int i = 0; i < state.Board.Length; i++)
            {
                for (int j = 0; j < state.Board.Length; j++)
                {
                    if (i < state.Board.Length - 1 && state.Board[i][j] != 0)
                    {
                        int k = i + 1;
                        while (k < state.Board.Length)
                        {
                            if (state.Board[k][j] == 0)
                            {
                                k++;
                            }
                            else if (state.Board[k][j] == state.Board[i][j])
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
                    if (j < state.Board.Length - 1 && state.Board[i][j] != 0)
                    {
                        int k = j + 1;
                        while (k < state.Board.Length)
                        {
                            if (state.Board[i][k] == 0)
                            {
                                k++;
                            }
                            else if (state.Board[i][k] == state.Board[i][j])
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
            for (int i = 0; i < state.Board.Length; i++)
            {
                int current = 0;
                int next = current + 1;
                while (next < state.Board.Length)
                {
                    // skip empty cells
                    while (next < state.Board.Length && state.Board[i][next] == 0)
                        next++;
                    // check boundaries
                    if (next >= state.Board.Length)
                        next--;

                    // only count instances where both cells are occupied
                    if (state.Board[i][current] != 0 && state.Board[i][next] != 0)
                    {
                        double currentValue = Math.Log(state.Board[i][current]) / Math.Log(2);
                        double nextValue = Math.Log(state.Board[i][next]) / Math.Log(2);
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
            for (int j = 0; j < state.Board.Length; j++)
            {
                int current = 0;
                int next = current + 1;
                while (next < state.Board.Length)
                {
                    // skip empty cells
                    while (next < state.Board.Length && state.Board[next][j] == 0)
                        next++;
                    // check boundaries
                    if (next >= state.Board.Length)
                        next--;

                    // only consider instances where both cells are occupied
                    if (state.Board[current][j] != 0 && state.Board[next][j] != 0)
                    {
                        double currentValue = Math.Log(state.Board[current][j]) / Math.Log(2);
                        double nextValue = Math.Log(state.Board[next][j]) / Math.Log(2);
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
