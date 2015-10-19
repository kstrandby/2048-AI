using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{
    public class Cell
    {
        public int x {get; set; }
        public int y {get; set; }

        public Cell(int column, int row)
        {
            this.x = column;
            this.y = row;
        }

        public bool IsValid()
        {
            if (x >= 0 && x < GameEngine.COLUMNS && y >= 0 && y < GameEngine.ROWS) return true;
            else return false;
        }

    }
}
