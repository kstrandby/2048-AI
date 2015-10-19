using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2048console
{
    public class ScoreController
    {
        private int score;

        public ScoreController()
        {
            score = 0;
        }
        internal void updateScore(int newValue)
        {
            this.score += newValue;
        }

        public int getScore()
        {
            return score;
        }
    }
}
