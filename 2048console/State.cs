using System;
using System.Collections.Generic;
namespace _2048console
{
    // State class
    public class State
    {
        private int[][] grid;
        public int[][] Grid
        {
            get
            {
                return this.grid;
            }
            set
            {
                this.grid = value;
            }
        }

        private int points;
        public int Points
        {
            get
            {
                return this.points;
            }
            set
            {
                this.points = value;
            }
        }

        private int player;
        public int Player {
            get
            {
                return this.player;
            }
            set
            {
                this.player = value;
            }
        }


        public State(int[][] grid, int points, int turn)
        {
            this.grid = grid;
            this.points = points;
            this.player = turn;
        }

        public override bool Equals(System.Object obj)
        {

            if (obj == null)
            {
                return false;
            }

            State s = obj as State;
            if ((System.Object)s == null)
            {
                return false;
            }

            for (int i = 0; i < GameEngine.ROWS; i++)
            {
                for (int j = 0; j < GameEngine.COLUMNS; j++)
                {
                    if (grid[i][j] != s.grid[i][j])
                        return false;
                }
            }
            return true;
        }

        public bool Equals(State s)
        {
            if ((object)s == null)
            {
                return false;
            }
            for (int i = 0; i < GameEngine.ROWS; i++)
            {
                for (int j = 0; j < GameEngine.COLUMNS; j++)
                {
                    if (grid[i][j] != s.grid[i][j])
                        return false;
                }
            }
            return true;
        }

        // for good practice recommended by Microsoft
        public override int GetHashCode()
        {
            return grid.GetHashCode() * points;
        }

        // checks if the state is a winning state (has tile 2048)
        public bool IsWin()
        {
            if (GridHelper.HighestTile(grid) >= 2048)
                return true;
            else
                return false;
        }

        // checks if the state is a game over state
        public  bool IsGameOver()
        {
            if (GridHelper.CheckLeft(grid) || GridHelper.CheckRight(grid) || GridHelper.CheckUp(grid) || GridHelper.CheckDown(grid))
                return false;
            else
                return true;
        }

        // returns all available moves in the state; will be computer moves if the computer is next to move in this state,
        // and player moves if the player it is the player's turn
        public List<Move> GetMoves()
        {
            if (player == GameEngine.PLAYER)
            {
                return GetAllPlayerMoves();
            }
            else if (player == GameEngine.COMPUTER)
            {
                List<Cell> cells = GetAvailableCells();
                return GetAllComputerMoves(cells);
            }
            else
            {
                throw new Exception();
            }
        }
        // returns a list of all the possible actions the computer can take
        // an action for the computer will be adding a 2- or 4- tile to some cell on the grid
        public List<Move> GetAllComputerMoves(List<Cell> cells)
        {
            List<Move> moves = new List<Move>();
            foreach (Cell cell in cells)
            {
                Move move2 = new ComputerMove(new Tuple<int, int>(cell.x, cell.y), 2);
                Move move4 = new ComputerMove(new Tuple<int, int>(cell.x, cell.y), 4);
                moves.Add(move2);
                moves.Add(move4);
            }
            return moves;
        
        }

        // returns the list of possible actions for the player
        // note that only actual possible actions in the state is added, i.e. if no tiles can be 
        // moved by pressing down, down is not added
        private List<Move> GetAllPlayerMoves()
        {
            List<Move> moves = new List<Move>();
            
            if (GridHelper.CheckLeft(grid))
            {
                Move move = new PlayerMove(DIRECTION.LEFT);
                moves.Add(move);
            }
            if (GridHelper.CheckRight(grid))
            {
                Move move = new PlayerMove(DIRECTION.RIGHT);
                moves.Add(move);
            }
            if (GridHelper.CheckDown(grid))
            {
                Move move = new PlayerMove(DIRECTION.DOWN);
                moves.Add(move);
            }
            if (GridHelper.CheckUp(grid))
            {
                Move move = new PlayerMove(DIRECTION.UP);
                moves.Add(move);
            }
            return moves;
        }

        // Returns a list of the empty cells in this state
        public List<Cell> GetAvailableCells()
        {
            List<Cell> cells = new List<Cell>();
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid.Length; j++)
                {
                    if (grid[i][j] == 0)
                        cells.Add(new Cell(i, j));
                }
            }
            return cells;
        }

        // Applies the move to this state and returns the resulting state
        public State ApplyMove(Move move)
        {
            if (move is PlayerMove)
            {
                int[][] clonedGrid = GridHelper.CloneGrid(this.grid);
                if (((PlayerMove)move).Direction == DIRECTION.LEFT) return ApplyLeft(clonedGrid);
                else if (((PlayerMove)move).Direction == DIRECTION.RIGHT) return ApplyRight(clonedGrid);
                else if (((PlayerMove)move).Direction == DIRECTION.DOWN) return ApplyDown(clonedGrid);
                else if (((PlayerMove)move).Direction == DIRECTION.UP) return ApplyUp(clonedGrid);
                else throw new Exception();
            }
            else if (move is ComputerMove)
            {
                State result = new State(GridHelper.CloneGrid(this.grid), points, GameEngine.PLAYER);
                int xPosition = ((ComputerMove)move).Position.Item1;
                int yPosition = ((ComputerMove)move).Position.Item2;
                int tileValue = ((ComputerMove)move).Tile;
                result.Grid[xPosition][yPosition] = tileValue;
                return result;
            }
            else
            {
                throw new Exception();
            }
        }

        private State ApplyUp(int[][] clonedGrid)
        {
            List<Cell> merged = new List<Cell>();

            for (int i = 0; i < clonedGrid.Length; i++)
            {
                for (int j = clonedGrid.Length - 1; j >= 0; j--)
                {
                    if (GridHelper.CellIsOccupied(clonedGrid, i, j) && j < 3)
                    {
                        int k = j;
                        while (k < 3 && !GridHelper.CellIsOccupied(clonedGrid, i, k + 1))
                        {
                            int value = clonedGrid[i][k];
                            clonedGrid[i][k] = 0;
                            clonedGrid[i][k + 1] = value;
                            k = k + 1;
                        }
                        if (k < 3 && GridHelper.CellIsOccupied(clonedGrid, i, k + 1) 
                            && !GridHelper.TileAlreadyMerged(merged, i, k)
                            && !GridHelper.TileAlreadyMerged(merged, i, k + 1))
                        {
                            // check if we can merge the two tiles
                            if (clonedGrid[i][k] == clonedGrid[i][k + 1])
                            {
                                int value = clonedGrid[i][k + 1] * 2;
                                clonedGrid[i][k + 1] = value;
                                merged.Add(new Cell(i, k + 1));
                                clonedGrid[i][k] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }
            State result = new State(clonedGrid, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyDown(int[][] clonedGrid)
        {
            List<Cell> merged = new List<Cell>();

            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid.Length; j++)
                {
                    if (GridHelper.CellIsOccupied(clonedGrid, i, j) && j > 0)
                    {
                        int k = j;
                        while (k > 0 && !GridHelper.CellIsOccupied(clonedGrid, i, k - 1))
                        {
                            int value = clonedGrid[i][k];
                            clonedGrid[i][k] = 0;
                            clonedGrid[i][k - 1] = value;
                            k = k - 1;
                        }
                        if (k > 0 && GridHelper.CellIsOccupied(clonedGrid, i, k - 1)
                            && !GridHelper.TileAlreadyMerged(merged, i, k)
                            && !GridHelper.TileAlreadyMerged(merged, i, k - 1))
                        {
                            // check if we can merge the two tiles
                            if (clonedGrid[i][k] == clonedGrid[i][k - 1])
                            {
                                int value = clonedGrid[i][k - 1] * 2;
                                clonedGrid[i][k - 1] = value;
                                merged.Add(new Cell(i, k - 1));
                                clonedGrid[i][k] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }
            State result = new State(clonedGrid, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyRight(int[][] clonedGrid)
        {

            List<Cell> merged = new List<Cell>();

            for (int j = 0; j < grid.Length; j++)
            {
                for (int i = grid.Length - 1; i >= 0; i--)
                {
                    if (GridHelper.CellIsOccupied(clonedGrid, i, j) && i < 3)
                    {
                        int k = i;
                        while (k < 3 && !GridHelper.CellIsOccupied(clonedGrid, k + 1, j))
                        {
                            int value = clonedGrid[k][j];
                            clonedGrid[k][j] = 0;
                            clonedGrid[k + 1][j] = value;
                            k = k + 1;
                        }
                        if (k < 3 && GridHelper.CellIsOccupied(clonedGrid, k + 1, j) 
                            && !GridHelper.TileAlreadyMerged(merged, k, j) 
                            && !GridHelper.TileAlreadyMerged(merged, k + 1, j))
                        {
                            // check if we can merge the two tiles
                            if (clonedGrid[k][j] == clonedGrid[k + 1][j])
                            {
                                int value = clonedGrid[k + 1][j] * 2;
                                clonedGrid[k + 1][j] = value;
                                merged.Add(new Cell(k + 1, j));
                                clonedGrid[k][j] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }

            State result = new State(clonedGrid, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyLeft(int[][] clonedGrid)
        {
            List<Cell> merged = new List<Cell>();

            for (int j = 0; j < grid.Length; j++)
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    if (GridHelper.CellIsOccupied(clonedGrid, i, j) && i > 0)
                    {
                        int k = i;
                        while (k > 0 && !GridHelper.CellIsOccupied(clonedGrid, k - 1, j))
                        {
                            int value = clonedGrid[k][j];
                            clonedGrid[k][j] = 0;
                            clonedGrid[k - 1][j] = value;
                            k = k - 1;
                        }
                        if (k > 0 && GridHelper.CellIsOccupied(clonedGrid, k - 1, j) 
                            && !GridHelper.TileAlreadyMerged(merged, k, j)
                            && !GridHelper.TileAlreadyMerged(merged, k - 1, j))
                        {
                            // check if we can merge the two tiles
                            if (clonedGrid[k][j] == clonedGrid[k - 1][j])
                            {
                                int value = clonedGrid[k - 1][j] * 2;
                                clonedGrid[k - 1][j] = value;
                                merged.Add(new Cell(k - 1, j));
                                clonedGrid[k][j] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }

            State result = new State(clonedGrid, this.points, GameEngine.COMPUTER);
            return result;
        }
    }
}
