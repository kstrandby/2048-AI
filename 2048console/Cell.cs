using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{
    // class representation of a cell on the board
    public class Cell
    {
        public int x {get; set; }
        public int y {get; set; }

        public Cell(int column, int row)
        {
            this.x = column;
            this.y = row;
        }

        // checks if the cell is valid, i.e. if the column and row values are within bounds of 
        // the board size specified in GameEngine
        public bool IsValid()
        {
            if (x >= 0 && x < GameEngine.COLUMNS && y >= 0 && y < GameEngine.ROWS) return true;
            else return false;
        }

    }
}
