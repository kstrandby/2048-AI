using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{

    public class GameEngine
    {

        // constants
        public const int PLAYER = 0;
        public const int COMPUTER = 1;
        public const int GRID_SIZE = 16;
        public const int ROWS = 4, COLUMNS = 4;
        public const int TILE2_PROBABILITY = 90;
        

        public int[][] grid { get; set; }
        public List<Cell> occupied;
        public List<Cell> available;
        public List<Cell> merged;

        public bool gameOver = false;

        public ScoreController scoreController;

        public GameEngine()
        {
            scoreController = new ScoreController();
            gameOver = false;

            grid = new int[ROWS][];
            occupied = new List<Cell>();
            available = new List<Cell>();
            merged = new List<Cell>();

            InitializeGrid();
            InitiateGame();

        }


        private void InitializeGrid()
        {
            // create empty cells for entire grid
            for (int i = 0; i < COLUMNS; i++)
            {
                for (int j = 0; j < ROWS; j++)
                {
                    this.available.Add(new Cell(i, j));
                }
            }

            // initialize the grid data structure
            for (int i = 0; i < COLUMNS; i++)
            {
                grid[i] = new int[] { 0, 0, 0, 0 };
            }
        }


        // Initiates the game by generating two 2 or 4 tiles at random positions
        void InitiateGame()
        {
            generateRandomTile();
            generateRandomTile();
        }

        public void generateRandomTile()
        {
            // generate random available position
            Random random = new Random();
            int x = random.Next(0, 4);
            int y = random.Next(0, 4);

            while (GridHelper.CellIsOccupied(grid, x, y))
            {
                x = random.Next(0, 4);
                y = random.Next(0, 4);
            }

            // generate a 2-tile with 90% probability, a 4-tile with 10%
            int rand = random.Next(0, 100);

            if (rand <= TILE2_PROBABILITY)
            {
                grid[x][y] = 2;
            }
            else
            {
                grid[x][y] = 4;
            }
            Cell cell = available.Find(item => item.x == x && item.y == y);
            available.Remove(cell);
            occupied.Add(cell);

            if (occupied.Count() == 16 && IsGameOver(grid))
            {
                gameOver = true;
            }
        }

        public static bool IsGameOver(int[][] grid)
        {
            if (GridHelper.CheckLeft(grid) || GridHelper.CheckRight(grid) || GridHelper.CheckUp(grid) || GridHelper.CheckDown(grid))
                return false;
            else
                return true;
        }



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

            if (occupied.Count() == 16 && IsGameOver(grid))
            {
                gameOver = true;
                return true;
            }
            return false;
        }

        void Reset()
        {
            merged.Clear();
        }

        private void DownPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLUMNS; j++)
                {
                    if (GridHelper.CellIsOccupied(grid, i, j) && j > 0)
                    {
                        int k = j;
                        while (k > 0 && !GridHelper.CellIsOccupied(grid, i, k - 1))
                        {
                            MoveTile(i, k, i, k - 1);
                            k = k - 1;
                            tileMoved = true;
                        }
                        if (k > 0 && GridHelper.CellIsOccupied(grid, i, k - 1) && !GridHelper.TileAlreadyMerged(merged, i, k) && !GridHelper.TileAlreadyMerged(merged, i, k - 1))
                        {
                            // check if we can merge the two tiles
                            if (grid[i][k] == grid[i][k - 1])
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

        private void UpPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = COLUMNS - 1; j >= 0; j--)
                {
                    if (GridHelper.CellIsOccupied(grid, i, j) && j < 3)
                    {
                        int k = j;
                        while (k < 3 && !GridHelper.CellIsOccupied(grid, i, k + 1))
                        {
                            MoveTile(i, k, i, k + 1);
                            k = k + 1;
                            tileMoved = true;
                        }
                        if (k < 3 && GridHelper.CellIsOccupied(grid, i, k + 1) && !GridHelper.TileAlreadyMerged(merged, i, k) && !GridHelper.TileAlreadyMerged(merged, i, k + 1))
                        {

                            // check if we can merge the two tiles
                            if (grid[i][k] == grid[i][k + 1])
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


        private void LeftPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int j = 0; j < ROWS; j++)
            {
                for (int i = 0; i < COLUMNS; i++)
                {
                    if (GridHelper.CellIsOccupied(grid, i, j) && i > 0)
                    {
                        int k = i;
                        while (k > 0 && !GridHelper.CellIsOccupied(grid, k - 1, j))
                        {
                            MoveTile(k, j, k - 1, j);
                            k = k - 1;
                            tileMoved = true;
                        }
                        if (k > 0 && GridHelper.CellIsOccupied(grid, k - 1, j) && !GridHelper.TileAlreadyMerged(merged, k, j) && !GridHelper.TileAlreadyMerged(merged, k - 1, j))
                        {
                            // check if we can merge the two tiles
                            if (grid[k][j] == grid[k - 1][j])
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

        private void RightPressed()
        {
            bool tileMoved = false; // to keep track of if a tile has been moved or not
            for (int j = 0; j < ROWS; j++)
            {
                for (int i = COLUMNS - 1; i >= 0; i--)
                {
                    if (GridHelper.CellIsOccupied(grid, i, j) && i < 3)
                    {
                        int k = i;
                        while (k < 3 && !GridHelper.CellIsOccupied(grid, k + 1, j))
                        {
                            MoveTile(k, j, k + 1, j);
                            k = k + 1;
                            tileMoved = true;
                        }
                        if (k < 3 && GridHelper.CellIsOccupied(grid, k + 1, j) && !GridHelper.TileAlreadyMerged(merged, k, j) && !GridHelper.TileAlreadyMerged(merged, k + 1, j))
                        {

                            // check if we can merge the two tiles
                            if (grid[k][j] == grid[k + 1][j])
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

        void MoveTile(int from_x, int from_y, int to_x, int to_y)
        {
            // update old cell
            int value = grid[from_x][from_y];
            grid[from_x][from_y] = 0;
            Cell old_cell = occupied.Find(item => item.x == from_x && item.y == from_y);
            
            occupied.Remove(old_cell);
            available.Add(old_cell);

            // update new cell
            grid[to_x][to_y] = value;
            Cell new_cell = available.Find(item => item.x == to_x && item.y == to_y);
            available.Remove(new_cell);
            occupied.Add(new_cell);
        }

        void MergeTiles(int tile1_x, int tile1_y, int tile2_x, int tile2_y)
        {
            // transform tile2 into a tile double the value, update sprite as well
            int newValue = grid[tile2_x][tile2_y] * 2;
            grid[tile2_x][tile2_y] = newValue;
            Cell cell = occupied.Find(item => item.x == tile2_x && item.y == tile2_y);
            merged.Add(cell);

            // delete tile1 in reference lists, destroy gameobject etc.
            Cell old_cell = occupied.Find(item => item.x == tile1_x && item.y == tile1_y);
            occupied.Remove(old_cell);
            grid[tile1_x][tile1_y] = 0;
            available.Add(old_cell);

            // update overall point score
            scoreController.updateScore(newValue);
        }
    }
}