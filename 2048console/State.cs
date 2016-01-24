using System;
using System.Collections.Generic;
namespace _2048console
{
    // State class, representing a state of the game based
    // on the configuration of the board, the amount of points
    // and the player that has the move in the state
    public class State
    {
        private Random random;

        // Board representation
        private int[][] board;
        public int[][] Board
        {
            get
            {
                return this.board;
            }
            set
            {
                this.board = value;
            }
        }

        // Amount of points in state
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

        // Player to play in the state
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

        // Move generating this state
        private Move generatingMove;
        public Move GeneratingMove
        {
            get
            {
                return this.generatingMove;
            }
            set
            {
                this.generatingMove = value;
            }
        }


        public State(int[][] board, int points, int turn)
        {
            this.board = board;
            this.points = points;
            this.player = turn;

             random = new Random();
        }

        public State()
        {
            // TODO: Complete member initialization
        }

        // Returns a clone of the state
        public State Clone()
        {
            return new State(BoardHelper.CloneBoard(this.board), this.points, this.player);
        }

        // Returns a random move available in the state
        public Move GetRandomMove()
        {
            if (player == GameEngine.COMPUTER)
            {
                List<Cell> availableCells = GetAvailableCells();
                // choose cell at random
                Cell cell = availableCells[random.Next(0, availableCells.Count)];
                // choose tile value according to probabilities
                int prob = random.Next(0, 100);
                if (prob < GameEngine.TILE2_PROBABILITY)
                {
                    return new ComputerMove(new Tuple<int, int>(cell.x, cell.y), 2);
                }
                else
                {
                    return new ComputerMove(new Tuple<int, int>(cell.x, cell.y), 4);
                }
            }
            else
            {
                List<Move> moves = GetMoves();
                if (moves.Count != 0)
                {
                    int randomIndex = random.Next(0, moves.Count);
                    return moves[randomIndex];
                }
                else return null;
                
            }
        }

        // Checks if two states are equal
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
                    if (board[i][j] != s.board[i][j])
                        return false;
                }
            }
            return true;
        }

        // Checks if two states are equal
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
                    if (board[i][j] != s.board[i][j])
                        return false;
                }
            }
            return true;
        }

        // for good practice recommended by Microsoft
        public override int GetHashCode()
        {
            return board.GetHashCode() * points;
        }

        // Used by MCTS (only called for terminal states)
        public double GetResult()
        {
            return this.Points;
        }

        // checks if the state is a winning state (has tile 2048)
        public bool IsWin()
        {
            if (BoardHelper.HighestTile(board) >= 2048)
                return true;
            else
                return false;
        }

        // checks if the state is a game over state
        public  bool IsGameOver()
        {
            if (BoardHelper.IsGameOver(board))
                return true;
            else
                return false;
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
        // an action for the computer will be adding a 2- or 4- tile to some cell on the board
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
            
            if (BoardHelper.CheckLeft(board))
            {
                Move move = new PlayerMove(DIRECTION.LEFT);
                moves.Add(move);
            }
            if (BoardHelper.CheckRight(board))
            {
                Move move = new PlayerMove(DIRECTION.RIGHT);
                moves.Add(move);
            }
            if (BoardHelper.CheckDown(board))
            {
                Move move = new PlayerMove(DIRECTION.DOWN);
                moves.Add(move);
            }
            if (BoardHelper.CheckUp(board))
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
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board.Length; j++)
                {
                    if (board[i][j] == 0)
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
                int[][] clonedBoard = BoardHelper.CloneBoard(this.board);
                if (((PlayerMove)move).Direction == DIRECTION.LEFT){
                    State state = ApplyLeft(clonedBoard);
                    state.GeneratingMove = move;
                    return state;
                }
                else if (((PlayerMove)move).Direction == DIRECTION.RIGHT) {
                    State state = ApplyRight(clonedBoard);
                    state.GeneratingMove = move;
                    return state;
                }
                    
                else if (((PlayerMove)move).Direction == DIRECTION.DOWN) {
                    State state = ApplyDown(clonedBoard);
                    state.GeneratingMove = move;
                    return state;
                }
                else if (((PlayerMove)move).Direction == DIRECTION.UP) {
                    State state = ApplyUp(clonedBoard);
                    state.GeneratingMove = move;
                    return state;
                }
                else throw new Exception();
            }
            else if (move is ComputerMove)
            {
                State result = new State(BoardHelper.CloneBoard(this.board), points, GameEngine.PLAYER);
                int xPosition = ((ComputerMove)move).Position.Item1;
                int yPosition = ((ComputerMove)move).Position.Item2;
                int tileValue = ((ComputerMove)move).Tile;
                result.Board[xPosition][yPosition] = tileValue;
                result.GeneratingMove = move;
                return result;
            }
            else
            {
                throw new Exception();
            }
        }

        private State ApplyUp(int[][] clonedBoard)
        {
            List<Cell> merged = new List<Cell>();

            for (int i = 0; i < clonedBoard.Length; i++)
            {
                for (int j = clonedBoard.Length - 1; j >= 0; j--)
                {
                    if (BoardHelper.CellIsOccupied(clonedBoard, i, j) && j < 3)
                    {
                        int k = j;
                        while (k < 3 && !BoardHelper.CellIsOccupied(clonedBoard, i, k + 1))
                        {
                            int value = clonedBoard[i][k];
                            clonedBoard[i][k] = 0;
                            clonedBoard[i][k + 1] = value;
                            k = k + 1;
                        }
                        if (k < 3 && BoardHelper.CellIsOccupied(clonedBoard, i, k + 1) 
                            && !BoardHelper.TileAlreadyMerged(merged, i, k)
                            && !BoardHelper.TileAlreadyMerged(merged, i, k + 1))
                        {
                            // check if we can merge the two tiles
                            if (clonedBoard[i][k] == clonedBoard[i][k + 1])
                            {
                                int value = clonedBoard[i][k + 1] * 2;
                                clonedBoard[i][k + 1] = value;
                                merged.Add(new Cell(i, k + 1));
                                clonedBoard[i][k] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }
            State result = new State(clonedBoard, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyDown(int[][] clonedBoard)
        {
            List<Cell> merged = new List<Cell>();

            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board.Length; j++)
                {
                    if (BoardHelper.CellIsOccupied(clonedBoard, i, j) && j > 0)
                    {
                        int k = j;
                        while (k > 0 && !BoardHelper.CellIsOccupied(clonedBoard, i, k - 1))
                        {
                            int value = clonedBoard[i][k];
                            clonedBoard[i][k] = 0;
                            clonedBoard[i][k - 1] = value;
                            k = k - 1;
                        }
                        if (k > 0 && BoardHelper.CellIsOccupied(clonedBoard, i, k - 1)
                            && !BoardHelper.TileAlreadyMerged(merged, i, k)
                            && !BoardHelper.TileAlreadyMerged(merged, i, k - 1))
                        {
                            // check if we can merge the two tiles
                            if (clonedBoard[i][k] == clonedBoard[i][k - 1])
                            {
                                int value = clonedBoard[i][k - 1] * 2;
                                clonedBoard[i][k - 1] = value;
                                merged.Add(new Cell(i, k - 1));
                                clonedBoard[i][k] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }
            State result = new State(clonedBoard, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyRight(int[][] clonedBoard)
        {

            List<Cell> merged = new List<Cell>();

            for (int j = 0; j < board.Length; j++)
            {
                for (int i = board.Length - 1; i >= 0; i--)
                {
                    if (BoardHelper.CellIsOccupied(clonedBoard, i, j) && i < 3)
                    {
                        int k = i;
                        while (k < 3 && !BoardHelper.CellIsOccupied(clonedBoard, k + 1, j))
                        {
                            int value = clonedBoard[k][j];
                            clonedBoard[k][j] = 0;
                            clonedBoard[k + 1][j] = value;
                            k = k + 1;
                        }
                        if (k < 3 && BoardHelper.CellIsOccupied(clonedBoard, k + 1, j) 
                            && !BoardHelper.TileAlreadyMerged(merged, k, j) 
                            && !BoardHelper.TileAlreadyMerged(merged, k + 1, j))
                        {
                            // check if we can merge the two tiles
                            if (clonedBoard[k][j] == clonedBoard[k + 1][j])
                            {
                                int value = clonedBoard[k + 1][j] * 2;
                                clonedBoard[k + 1][j] = value;
                                merged.Add(new Cell(k + 1, j));
                                clonedBoard[k][j] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }

            State result = new State(clonedBoard, this.points, GameEngine.COMPUTER);
            return result;
        }

        private State ApplyLeft(int[][] clonedBoard)
        {
            List<Cell> merged = new List<Cell>();

            for (int j = 0; j < board.Length; j++)
            {
                for (int i = 0; i < board.Length; i++)
                {
                    if (BoardHelper.CellIsOccupied(clonedBoard, i, j) && i > 0)
                    {
                        int k = i;
                        while (k > 0 && !BoardHelper.CellIsOccupied(clonedBoard, k - 1, j))
                        {
                            int value = clonedBoard[k][j];
                            clonedBoard[k][j] = 0;
                            clonedBoard[k - 1][j] = value;
                            k = k - 1;
                        }
                        if (k > 0 && BoardHelper.CellIsOccupied(clonedBoard, k - 1, j) 
                            && !BoardHelper.TileAlreadyMerged(merged, k, j)
                            && !BoardHelper.TileAlreadyMerged(merged, k - 1, j))
                        {
                            // check if we can merge the two tiles
                            if (clonedBoard[k][j] == clonedBoard[k - 1][j])
                            {
                                int value = clonedBoard[k - 1][j] * 2;
                                clonedBoard[k - 1][j] = value;
                                merged.Add(new Cell(k - 1, j));
                                clonedBoard[k][j] = 0;
                                points += value;
                            }
                        }
                    }
                }
            }

            State result = new State(clonedBoard, this.points, GameEngine.COMPUTER);
            return result;
        }
    }
}
