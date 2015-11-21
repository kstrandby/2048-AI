using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    public class NaiveAI
    {
        private GameEngine gameEngine;

        public NaiveAI(GameEngine gameEngine)
        {
            this.gameEngine = gameEngine;
        }

        public DIRECTION chooseAction()
        {
            if (BoardHelper.CheckDown(gameEngine.board)) return DIRECTION.DOWN;
            else if (BoardHelper.CheckLeft(gameEngine.board)) return DIRECTION.LEFT;
            else if (BoardHelper.CheckRight(gameEngine.board)) return DIRECTION.RIGHT;
            else return DIRECTION.UP;
        }
    }
}
