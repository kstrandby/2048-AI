using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2048console
{
    class Logger
    {
        StreamWriter writer;
        private string path;
        private int depth;
        private string[][] output;


        public Logger(string path, int depth)
        {
            this.path = path;
            this.writer = new StreamWriter(path);
            this.depth = depth;
            this.output = new string[depth][];

            for (int i = 1; i <= depth; i++)
            {
                output[i - 1] = new string[6];
                output[i - 1][0] = "Depth = " + i + ": ";
            }
        }



        public void WriteLog(bool close)
        {
            foreach (string[] lines in output)
            {
                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
            writer.WriteLine("\n");
            for (int i = 1; i <= depth; i++)
            {
                Array.Clear(output[i - 1], 0, output[i - 1].Length);
                output[i - 1][0] = "Depth = " + i + ": ";
            }
            if (close)
                writer.Close();
        }


        public void writeParent(State state, int depth)
        {
            output[depth][1] += "Parent:                                ||     ";
            for (int i = 2; i < 6; i++)
            {
                string line = "";

                for (int j = 0; j < 4; j++)
                {
                    string append = "";
                    if (state.Grid[j][i - 2] < 10)
                        append = "|   " + state.Grid[j][i - 2] + "  ";
                    else if (state.Grid[j][i - 2] >= 10 && state.Grid[j][i - 2] < 100)
                        append = "|  " + state.Grid[j][i - 2] + "  ";
                    else if (state.Grid[j][i - 2] >= 100 && state.Grid[j][i - 2] < 1000)
                        append = "|  " + state.Grid[j][i - 2] + " ";
                    else if (state.Grid[j][i - 2] >= 1000 && state.Grid[j][i - 2] < 10000)
                        append = "| " + state.Grid[j][i - 2] + " ";

                    line += append;
                    if (j == 3)
                        line += "|          ||     ";
                }
                output[depth][7 - i] += line;
            }
        }


        public void writeChild(State state, int depth, double score)
        {
            if (score < 0)
                output[depth][1] += "Score = " + string.Format("{0:0.00}", Math.Round(score, 2)) + "                         ";
            else
                output[depth][1] += "Score = " + string.Format("{0:0.00}", Math.Round(score, 2)) + "                          ";
            for (int i = 2; i < 6; i++)
            {
                string line = "";

                for (int j = 0; j < 4; j++)
                {
                    string append = "";
                    if (state.Grid[j][i - 2] < 10)
                        append = "|   " + state.Grid[j][i - 2] + "  ";
                    else if (state.Grid[j][i - 2] >= 10 && state.Grid[j][i - 2] < 100)
                        append = "|  " + state.Grid[j][i - 2] + "  ";
                    else if (state.Grid[j][i - 2] >= 100 && state.Grid[j][i - 2] < 1000)
                        append = "|  " + state.Grid[j][i - 2] + " ";
                    else if (state.Grid[j][i - 2] >= 1000 && state.Grid[j][i - 2] < 10000)
                        append = "| " + state.Grid[j][i - 2] + " ";

                    line += append;
                    if (j == 3)
                        line += "|          ";
                }
                output[depth][7 - i] += line;
            }
        }

    }
}
