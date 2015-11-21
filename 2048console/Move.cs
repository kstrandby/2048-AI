using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{

    public enum DIRECTION
    {
        LEFT = 0,
        RIGHT = 1,
        DOWN = 2,
        UP = 3
    }

    // Class representing a move
    public class Move 
    {
        private double score;
        public double Score
        {
            get
            {
                return score;
            }
            set
            {
                score = value;
            }
        }

        public Move()
        {
        }
    }

    // Subclass of move, representing a move made by the computer,
    // i.e. an insertion of a random tile at a random position
    public class ComputerMove : Move
    {
        private Tuple<int, int> position;
        public Tuple<int, int> Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        private int tile;
        public int Tile
        {
            get
            {
                return tile;
            }
            set
            {
                tile = value;
            }
        }

        public ComputerMove(Tuple<int, int> position, int tile)
        {
            this.position = position;
            this.tile = tile;
        }

        // default constructor has dummy position and tile
        public ComputerMove()
        {
            this.position = new Tuple<int, int>(-1, -1);
            this.tile = -1;
        }
    }

    // Subclass of move, representing a move made by the player,
    // i.e. a swipe direction (left, right, up, down)
    public class PlayerMove : Move
    {
        private DIRECTION direction;
        public DIRECTION Direction
        {
            get
            {
                return direction;
            }
            set
            {
                direction = value;
            }
        }

        public PlayerMove(DIRECTION direction)
        {
            this.direction = direction;
        }

        // default constructor has dummy direction
        public PlayerMove()
        {
            this.direction = (DIRECTION)(-1);
        }
    }
}
