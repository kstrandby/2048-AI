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
            if (GridHelper.CheckDown(gameEngine.grid)) return DIRECTION.DOWN;
            else if (GridHelper.CheckLeft(gameEngine.grid)) return DIRECTION.LEFT;
            else if (GridHelper.CheckRight(gameEngine.grid)) return DIRECTION.RIGHT;
            else return DIRECTION.UP;
        }
    }
}
