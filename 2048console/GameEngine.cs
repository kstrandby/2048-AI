using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    // Class handling all game logic
    public class GameEngine
    {

        // constants
        public const int PLAYER = 0;
        public const int COMPUTER = 1;
        public const int board_SIZE = 16;
        public const int ROWS = 4, COLUMNS = 4;
        public const int TILE2_PROBABILITY = 90;
        

        public int[][] board { get; set; }
        public List<Cell> occupied;
        public List<Cell> available;
        public List<Cell> merged;
        public ScoreController scoreController;

        // Constructor sets up necessary data structures and objects
        // calls initial methods to setup the board and start the game
        public GameEngine()
        {
            scoreController = new ScoreController();
            board = new int[ROWS][];
            occupied = new List<Cell>();
            available = new List<Cell>();
            merged = new List<Cell>();

            Initializeboard();
            StartGame();
        }

        // Sets up data structures used for managing the board
        private void Initializeboard()
        {
            // initialize the board data structure
            for (int i = 0; i < COLUMNS; i++)
            {
                board[i] = new int[] { 0, 0, 0, 0 };
                
                for (int j = 0; j < ROWS; j++)
                {
                    // add empty cells for entire board to list of available cells
                    this.available.Add(new Cell(i, j));
                }
            }
        }


        // Starts the game by generating two random tiles
        void StartGame()
        {
            generateRandomTile();
            generateRandomTile();
        }

        // Generates a random tile at a random empty cell on the board
        // The tile is created with the probability 90% for a 2-tile and 
        // 10% for a 4-tile
        public void generateRandomTile()
        {
            // generate random available position
            Random random = new Random();
            int x = random.Next(0, 4);
            int y = random.Next(0, 4);

            while (BoardHelper.CellIsOccupied(board, x, y))
            {
                x = random.Next(0, 4);
                y = random.Next(0, 4);
            }

            // generate a 2-tile with 90% probability, a 4-tile with 10%
            int rand = random.Next(0, 100);

            if (rand <= TILE2_PROBABILITY)
            {
                board[x][y] = 2;
            }
            else
            {
                board[x][y] = 4;
            }
            Cell cell = available.Find(item => item.x == x && item.y == y);
            available.Remove(cell);
            occupied.Add(cell);
        }

        // Executes the user action by updating the board representation
        public bool SendUserAction(PlayerMove action)
        {
            if (action.Direction == DIRECTION.DOWN)
            {
                DownPressed();
            }
            if (action.Direction == DIRECTION.UP)
            {
                UpPressed();
            }
            if (action.Direction == DIRECTION.LEFT)
            {
                LeftPressed();
            }
            if (action.Direction == DIRECTION.RIGHT)
            {
                RightPressed();
            }

            Reset();

            if (occupied.Count() == 16 && BoardHelper.IsGameOver(board))
            {
                return true;
            }
            return false;
        }

        // Deletes all cells in our list of merged cells
        void Reset()
        {
            merged.Clear();
        }

        // Executes the user action DOWN
        private void DownPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLUMNS; j++)
                {
                    if (BoardHelper.CellIsOccupied(board, i, j) && j > 0)
                    {
                        int k = j;
                        while (k > 0 && !BoardHelper.CellIsOccupied(board, i, k - 1))
                        {
                            MoveTile(i, k, i, k - 1);
                            k = k - 1;
                            tileMoved = true;
                        }
                        if (k > 0 && BoardHelper.CellIsOccupied(board, i, k - 1) && !BoardHelper.TileAlreadyMerged(merged, i, k) && !BoardHelper.TileAlreadyMerged(merged, i, k - 1))
                        {
                            // check if we can merge the two tiles
                            if (board[i][k] == board[i][k - 1])
                            {
                                MergeTiles(i, k, i, k - 1);
                                tileMoved = true;
                            }
                        }
                    }
                }
            }
            if (tileMoved)
                generateRandomTile();
        }

        // Executes the user action UP
        private void UpPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = COLUMNS - 1; j >= 0; j--)
                {
                    if (BoardHelper.CellIsOccupied(board, i, j) && j < 3)
                    {
                        int k = j;
                        while (k < 3 && !BoardHelper.CellIsOccupied(board, i, k + 1))
                        {
                            MoveTile(i, k, i, k + 1);
                            k = k + 1;
                            tileMoved = true;
                        }
                        if (k < 3 && BoardHelper.CellIsOccupied(board, i, k + 1) && !BoardHelper.TileAlreadyMerged(merged, i, k) && !BoardHelper.TileAlreadyMerged(merged, i, k + 1))
                        {

                            // check if we can merge the two tiles
                            if (board[i][k] == board[i][k + 1])
                            {
                                MergeTiles(i, k, i, k + 1);
                                tileMoved = true;
                            }
                        }
                    }
                }
            }
            if (tileMoved)
                generateRandomTile();
        }

        // Executes the user action LEFT
        private void LeftPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int j = 0; j < ROWS; j++)
            {
                for (int i = 0; i < COLUMNS; i++)
                {
                    if (BoardHelper.CellIsOccupied(board, i, j) && i > 0)
                    {
                        int k = i;
                        while (k > 0 && !BoardHelper.CellIsOccupied(board, k - 1, j))
                        {
                            MoveTile(k, j, k - 1, j);
                            k = k - 1;
                            tileMoved = true;
                        }
                        if (k > 0 && BoardHelper.CellIsOccupied(board, k - 1, j) && !BoardHelper.TileAlreadyMerged(merged, k, j) && !BoardHelper.TileAlreadyMerged(merged, k - 1, j))
                        {
                            // check if we can merge the two tiles
                            if (board[k][j] == board[k - 1][j])
                            {
                                MergeTiles(k, j, k - 1, j);
                                tileMoved = true;
                            }
                        }
                    }
                }
            }
            if (tileMoved)
                generateRandomTile();
        }

        // Executes the user action RIGHT
        private void RightPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int j = 0; j < ROWS; j++)
            {
                for (int i = COLUMNS - 1; i >= 0; i--)
                {
                    if (BoardHelper.CellIsOccupied(board, i, j) && i < 3)
                    {
                        int k = i;
                        while (k < 3 && !BoardHelper.CellIsOccupied(board, k + 1, j))
                        {
                            MoveTile(k, j, k + 1, j);
                            k = k + 1;
                            tileMoved = true;
                        }
                        if (k < 3 && BoardHelper.CellIsOccupied(board, k + 1, j) && !BoardHelper.TileAlreadyMerged(merged, k, j) && !BoardHelper.TileAlreadyMerged(merged, k + 1, j))
                        {

                            // check if we can merge the two tiles
                            if (board[k][j] == board[k + 1][j])
                            {
                                MergeTiles(k, j, k + 1, j);
                                tileMoved = true;
                            }
                        }
                    }
                }
            }
            if (tileMoved)
                generateRandomTile();
        }

        // Moves a tile from column from_x, row from_y to column to_x, row to_y
        void MoveTile(int from_x, int from_y, int to_x, int to_y)
        {
            // update old cell
            int value = board[from_x][from_y];
            board[from_x][from_y] = 0;
            Cell old_cell = occupied.Find(item => item.x == from_x && item.y == from_y);
            
            occupied.Remove(old_cell);
            available.Add(old_cell);

            // update new cell
            board[to_x][to_y] = value;
            Cell new_cell = available.Find(item => item.x == to_x && item.y == to_y);
            available.Remove(new_cell);
            occupied.Add(new_cell);
        }

        // Merges tile at column tile1_x, row tile1_y with tile at column tile2_x, row tile2_y 
        void MergeTiles(int tile1_x, int tile1_y, int tile2_x, int tile2_y)
        {
            // transform tile2 into a tile double the value, update sprite as well
            int newValue = board[tile2_x][tile2_y] * 2;
            board[tile2_x][tile2_y] = newValue;
            Cell cell = occupied.Find(item => item.x == tile2_x && item.y == tile2_y);
            merged.Add(cell);

            // delete tile1 in reference lists, destroy gameobject etc.
            Cell old_cell = occupied.Find(item => item.x == tile1_x && item.y == tile1_y);
            occupied.Remove(old_cell);
            board[tile1_x][tile1_y] = 0;
            available.Add(old_cell);

            // update overall point score
            scoreController.updateScore(newValue);
        }
    }
}