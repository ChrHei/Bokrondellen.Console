using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bokrondellen.Console.Util
{
    class ProgressBar
    {
        private readonly char progressCharacter;
        private readonly int barSize;
        private readonly int maxValue;

        public bool ShowPercentage { get; set; }
        public bool ShowCounter { get; set; }

        public ProgressBar(char progressCharacter, int maxValue, int barSize)
        {
            this.progressCharacter = progressCharacter;
            this.maxValue = maxValue;
            this.barSize = barSize;

            ShowPercentage = false;
            ShowCounter = true;
        }

        public void Draw(int complete)
        {
            System.Console.CursorVisible = false;
            int left = System.Console.CursorLeft;
            decimal perc = (decimal)complete / (decimal)maxValue;
            int chars = (int)Math.Floor(perc / ((decimal)1 / (decimal)barSize));
            string p1 = String.Empty, p2 = String.Empty;

            for (int i = 0; i < chars; i++) p1 += progressCharacter;
            for (int i = 0; i < barSize - chars; i++) p2 += progressCharacter;

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write(p1);
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.Write(p2);

            System.Console.ResetColor();
            if (ShowPercentage)
                System.Console.Write(" {0}%", (perc * 100).ToString("N2"));

            if (ShowCounter)
                System.Console.Write(" {0}/{1}", complete, maxValue);

            System.Console.CursorLeft = left;
        }
    }
}
