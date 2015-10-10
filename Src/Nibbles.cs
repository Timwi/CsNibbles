using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace Nibbles.Bas
{
    static class Nibbles
    {
        // Constants
        public const int MAXSNAKELENGTH = 1000;

        // Global variables
        public static Arena[,] Arena = new Arena[51, 81]; // (1 to 50, 1 to 80) 
        public static Snake[] Snakes = new Snake[2];
        public static List<Direction>[] Inputs = new[] { new List<Direction>(), new List<Direction>() };

        public static int CurLevel;
        public static Random Rnd = new Random();
        public static int NumPlayers;
        public static int Speed;
        public static bool IncreaseSpeedDuringPlay;
        public static int OriginalWindowWidth, OriginalWindowHeight;

        public static ConsoleColor Sammy, Jake, Walls, Background, DlgFore, DlgBack;

        public static void Main(string[] args)
        {
            OriginalWindowWidth = Console.WindowWidth;
            OriginalWindowHeight = Console.WindowHeight;

            try
            {
                bool isTrueTypeFont = WinAPI.IsOutputConsoleFontTrueType();
                Console.Title = string.Format("C# Nibbles");
                if (isTrueTypeFont)
                    Console.OutputEncoding = Encoding.UTF8;
                Console.SetWindowSize(80, 25);
                Console.CursorVisible = false;
                Console.CancelKeyPress += (s, e) => RestoreConsole();

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                Console.SetWindowSize(80, 25);
                Console.SetBufferSize(80, 25);
                Console.CursorVisible = false;


                // INTRO

                Center(4, "C #   N i b b l e s");
                Console.ForegroundColor = ConsoleColor.Gray;
                Center(6, "(Translated from QBasic Nibbles)");
                Center(8, "Nibbles is a game for one or two players.  Navigate your snakes");
                Center(9, "around the game board trying to eat up numbers while avoiding");
                Center(10, "running into walls or other snakes.  The more numbers you eat up,");
                Center(11, "the more points you gain and the longer your snake becomes.");
                Console.ForegroundColor = ConsoleColor.White;
                Center(13, " Game Controls ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Center(15, "  General             Player 1               Player 2    ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Center(16, "                        (Up)                   (Up)      ");
                Center(17, "P - Pause                ↑                      W       ");
                Center(18, "                     (Left) ←   → (Right)   (Left) A   D (Right)  ");
                Center(19, "                         ↓                      S       ");
                Center(20, "                       (Down)                 (Down)     ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Center(24, "Press any key to continue");

                Music.Play("T160O1L8CDEDCDL4ECC", true);
                SparklePause();


                // ASK USER FOR PARAMETERS

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                Console.CursorVisible = true;

                bool success;
                do
                {
                    ConsoleSetCursorPosition(47, 5);
                    Console.Write(new string(' ', 34));
                    ConsoleSetCursorPosition(20, 5);
                    Console.Write("How many players (1 or 2)? ");
                    string num = Console.ReadLine();
                    success = int.TryParse(num, out NumPlayers);
                }
                while (!success || NumPlayers < 1 || NumPlayers > 2);

                ConsoleSetCursorPosition(21, 8);
                Console.Write("Skill level (1 to 100)? ");
                ConsoleSetCursorPosition(22, 9);
                Console.Write("1   = Novice");
                ConsoleSetCursorPosition(22, 10);
                Console.Write("90  = Expert");
                ConsoleSetCursorPosition(22, 11);
                Console.Write("100 = Twiddle Fingers");
                ConsoleSetCursorPosition(15, 12);
                Console.Write("(Computer speed may affect your skill level)");

                do
                {
                    ConsoleSetCursorPosition(45, 8);
                    Console.Write(new string(' ', 35));
                    ConsoleSetCursorPosition(45, 8);
                    string gamespeed = Console.ReadLine();
                    success = int.TryParse(gamespeed, out Speed);
                }
                while (!success || Speed < 1 || Speed > 100);

                Speed = (int) ((100 - Speed) * 2 + 10);

                string increase;
                do
                {
                    ConsoleSetCursorPosition(56, 15);
                    Console.Write(new string(' ', 25));
                    ConsoleSetCursorPosition(15, 15);
                    Console.Write("Increase game speed during play (Y or N)? ");
                    increase = Console.ReadLine().ToUpperInvariant();
                }
                while (increase != "Y" && increase != "N");
                IncreaseSpeedDuringPlay = increase == "Y";

                string monitor;
                do
                {
                    ConsoleSetCursorPosition(46, 17);
                    Console.Write(new string(' ', 34));
                    ConsoleSetCursorPosition(17, 17);
                    Console.Write("Monochrome or color monitor (M or C)? ");
                    monitor = Console.ReadLine().ToUpperInvariant();
                }
                while (monitor != "M" && monitor != "C");

                if (monitor == "M")
                {
                    Sammy = ConsoleColor.White;
                    Jake = ConsoleColor.Gray;
                    Walls = ConsoleColor.Gray;
                    Background = ConsoleColor.Black;
                    DlgFore = ConsoleColor.White;
                    DlgBack = ConsoleColor.Black;
                }
                else
                {
                    Sammy = ConsoleColor.Yellow;
                    Jake = ConsoleColor.Magenta;
                    Walls = ConsoleColor.Red;
                    Background = ConsoleColor.DarkBlue;
                    DlgFore = ConsoleColor.White;
                    DlgBack = ConsoleColor.DarkRed;
                }

                Console.CursorVisible = false;

                // Initialize arena array
                for (int row = 1; row <= 50; row++)
                    for (int col = 1; col <= 80; col++)
                        Arena[row, col] = new Arena { RealRow = (row + 1) / 2, Sister = (row % 2) * 2 - 1 };

                Console.BackgroundColor = Background;
                Console.Clear();

                do
                    PlayNibbles();
                while (StillWantsToPlay());

                RestoreConsole();
            }
            catch (Exception ex)
            {
                RestoreConsole();
                Console.WriteLine(ex);
            }
            finally
            {
                Music.Dispose();
            }
        }

        private static void RestoreConsole()
        {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.SetWindowSize(OriginalWindowWidth, OriginalWindowHeight);
            Console.Clear();
        }

        /// <summary>Centers text on given row</summary>
        private static void Center(int row, string text)
        {
            ConsoleSetCursorPosition(40 - text.Length / 2, row);
            Console.Write(text);
        }

        /// <summary>Erases snake to facilitate moving through playing field</summary>
        private static void EraseSnake(Snake snake)
        {
            for (int c = 0; c <= 9; c++)
                for (int b = snake.Length - c; b >= 0; b -= 10)
                {
                    var tail = (snake.Head + MAXSNAKELENGTH - b) % MAXSNAKELENGTH;
                    Set(snake.Body[tail].Row, snake.Body[tail].Col, Background);
                    Thread.Sleep(2);
                }
        }

        /// <summary>Initializes playing field colors</summary>
        private static void InitColors()
        {
            for (int row = 1; row <= 50; row++)
                for (int col = 1; col <= 80; col++)
                    Arena[row, col].Color = Background;

            Console.BackgroundColor = Background;
            Console.Clear();

            // Little trick: we can’t draw the bottom-right character cell without scrolling the screen up,
            // but we can make it appear in the right color by setting the background color to what it
            // should be and then causing everything to scroll up one
            Console.BackgroundColor = Walls;
            Console.SetCursorPosition(79, 24);
            Console.Write(" ");

            // Set (turn on) pixels for screen border
            for (int col = 1; col <= 80; col++)
            {
                Set(3, col, Walls);
                Set(50, col, Walls);
            }
            for (int row = 4; row <= 49; row++)
            {
                Set(row, 1, Walls);
                Set(row, 80, Walls);
            }
        }

        private enum LevelChoice
        {
            StartOver,
            NextLevel,
            SameLevel
        }

        /// <summary>Sets game level</summary>
        private static void Level(LevelChoice whatToDo, Snake[] sammy)
        {
            switch (whatToDo)
            {
                case LevelChoice.StartOver:
                    CurLevel = 1;
                    break;

                case LevelChoice.NextLevel:
                    CurLevel = CurLevel + 1;
                    break;
            }

            // Initialize Snakes
            sammy[0].Head = 1;
            sammy[0].Length = 2;
            sammy[0].Alive = true;
            sammy[1].Head = 1;
            sammy[1].Length = 2;
            sammy[1].Alive = true;
            Inputs[0].Clear();
            Inputs[1].Clear();

            InitColors();

            switch (CurLevel)
            {
                case 1:
                    sammy[0].Row = 25;
                    sammy[1].Row = 25;
                    sammy[0].Col = 50;
                    sammy[1].Col = 30;
                    sammy[0].Direction = Direction.Right;
                    sammy[1].Direction = Direction.Left;
                    break;

                case 2:
                    for (int i = 20; i <= 60; i++)
                        Set(25, i, Walls);
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 60;
                    sammy[1].Col = 20;
                    sammy[0].Direction = Direction.Left;
                    sammy[1].Direction = Direction.Right;
                    break;

                case 3:
                    for (int i = 10; i <= 40; i++)
                    {
                        Set(i, 20, Walls);
                        Set(i, 60, Walls);
                    }
                    sammy[0].Row = 25;
                    sammy[1].Row = 25;
                    sammy[0].Col = 50;
                    sammy[1].Col = 30;
                    sammy[0].Direction = Direction.Up;
                    sammy[1].Direction = Direction.Down;
                    break;

                case 4:
                    for (int i = 4; i <= 30; i++)
                    {
                        Set(i, 20, Walls);
                        Set(53 - i, 60, Walls);
                    }
                    for (int i = 2; i <= 40; i++)
                    {
                        Set(38, i, Walls);
                        Set(15, 81 - i, Walls);
                    }
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 60;
                    sammy[1].Col = 20;
                    sammy[0].Direction = Direction.Left;
                    sammy[1].Direction = Direction.Right;
                    break;

                case 5:
                    for (int i = 13; i <= 39; i++)
                    {
                        Set(i, 21, Walls);
                        Set(i, 59, Walls);
                    }
                    for (int i = 23; i <= 57; i++)
                    {
                        Set(11, i, Walls);
                        Set(41, i, Walls);
                    }
                    sammy[0].Row = 25;
                    sammy[1].Row = 25;
                    sammy[0].Col = 50;
                    sammy[1].Col = 30;
                    sammy[0].Direction = Direction.Up;
                    sammy[1].Direction = Direction.Down;
                    break;

                case 6:
                    for (int i = 4; i <= 49; i++)
                    {
                        if (i > 30 || i < 23)
                        {
                            Set(i, 10, Walls);
                            Set(i, 20, Walls);
                            Set(i, 30, Walls);
                            Set(i, 40, Walls);
                            Set(i, 50, Walls);
                            Set(i, 60, Walls);
                            Set(i, 70, Walls);
                        }
                    }
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 65;
                    sammy[1].Col = 15;
                    sammy[0].Direction = Direction.Down;
                    sammy[1].Direction = Direction.Up;
                    break;

                case 7:
                    for (int i = 4; i <= 49; i += 2)
                        Set(i, 40, Walls);
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 65;
                    sammy[1].Col = 15;
                    sammy[0].Direction = Direction.Down;
                    sammy[1].Direction = Direction.Up;
                    break;

                case 8:
                    for (int i = 4; i <= 40; i++)
                    {
                        Set(i, 10, Walls);
                        Set(53 - i, 20, Walls);
                        Set(i, 30, Walls);
                        Set(53 - i, 40, Walls);
                        Set(i, 50, Walls);
                        Set(53 - i, 60, Walls);
                        Set(i, 70, Walls);
                    }
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 65;
                    sammy[1].Col = 15;
                    sammy[0].Direction = Direction.Down;
                    sammy[1].Direction = Direction.Up;
                    break;

                case 9:
                    for (int i = 6; i <= 47; i++)
                    {
                        Set(i, i, Walls);
                        Set(i, i + 28, Walls);
                    }
                    sammy[0].Row = 40;
                    sammy[1].Row = 15;
                    sammy[0].Col = 75;
                    sammy[1].Col = 5;
                    sammy[0].Direction = Direction.Up;
                    sammy[1].Direction = Direction.Down;
                    break;

                default:
                    for (int i = 4; i <= 49; i += 2)
                    {
                        Set(i, 10, Walls);
                        Set(i + 1, 20, Walls);
                        Set(i, 30, Walls);
                        Set(i + 1, 40, Walls);
                        Set(i, 50, Walls);
                        Set(i + 1, 60, Walls);
                        Set(i, 70, Walls);
                    }
                    sammy[0].Row = 7;
                    sammy[1].Row = 43;
                    sammy[0].Col = 65;
                    sammy[1].Col = 15;
                    sammy[0].Direction = Direction.Down;
                    sammy[1].Direction = Direction.Up;
                    break;
            }
        }

        /// <summary>Main routine that controls game play</summary>
        private static void PlayNibbles()
        {
            // Initialize Snakes
            Snakes[0] = new Snake { Lives = 5, Score = 0, Color = Sammy, Body = new SnakePiece[MAXSNAKELENGTH] };
            Snakes[1] = new Snake { Lives = 5, Score = 0, Color = Jake, Body = new SnakePiece[MAXSNAKELENGTH] };

            Level(LevelChoice.StartOver, Snakes);
            var startRow1 = Snakes[0].Row;
            var startCol1 = Snakes[0].Col;
            var startRow2 = Snakes[1].Row;
            var startCol2 = Snakes[1].Col;

            var curSpeed = Speed;

            // play Nibbles until finished

            SpacePause("     Level " + CurLevel + ", Push Space");
            do
            {
                if (NumPlayers == 1)
                    Snakes[1].Row = 0;

                var number = 1; // Current number that snakes are trying to run into
                var nonum = true; // nonum = TRUE if a number is not on the screen
                int numberRow = 0, numberCol = 0, sisterRow = 0;

                var playerDied = false;
                PrintScore(NumPlayers, Snakes[0].Score, Snakes[1].Score, Snakes[0].Lives, Snakes[1].Lives);
                Music.Play("T160O1>L20CDEDCDL10ECC", true);

                do
                {
                    // Print number if no number exists
                    if (nonum)
                    {
                        do
                        {
                            numberRow = (int) (Rnd.NextDouble() * 47 + 3);
                            numberCol = (int) (Rnd.NextDouble() * 78 + 2);
                            sisterRow = numberRow + Arena[numberRow, numberCol].Sister;
                        }
                        while (PointIsThere(numberRow, numberCol, Background) || PointIsThere(sisterRow, numberCol, Background));
                        numberRow = Arena[numberRow, numberCol].RealRow;
                        nonum = false;
                        Console.ForegroundColor = Sammy;
                        Console.BackgroundColor = Background;
                        ConsoleSetCursorPosition(numberCol, numberRow);
                        Console.Write(number.ToString().Last());
                    }

                    // Delay game
                    Thread.Sleep(curSpeed);

                    // Get keyboard input & Change direction accordingly
                    while (Console.KeyAvailable)
                    {
                        var kbd = Console.ReadKey(true).Key;
                        switch (kbd)
                        {
                            case ConsoleKey.W: if (Inputs[1].Count == 0 || Inputs[1].Last() != Direction.Up) Inputs[1].Add(Direction.Up); break;
                            case ConsoleKey.S: if (Inputs[1].Count == 0 || Inputs[1].Last() != Direction.Down) Inputs[1].Add(Direction.Down); break;
                            case ConsoleKey.A: if (Inputs[1].Count == 0 || Inputs[1].Last() != Direction.Left) Inputs[1].Add(Direction.Left); break;
                            case ConsoleKey.D: if (Inputs[1].Count == 0 || Inputs[1].Last() != Direction.Right) Inputs[1].Add(Direction.Right); break;
                            case ConsoleKey.UpArrow: if (Inputs[0].Count == 0 || Inputs[0].Last() != Direction.Up) Inputs[0].Add(Direction.Up); break;
                            case ConsoleKey.DownArrow: if (Inputs[0].Count == 0 || Inputs[0].Last() != Direction.Down) Inputs[0].Add(Direction.Down); break;
                            case ConsoleKey.LeftArrow: if (Inputs[0].Count == 0 || Inputs[0].Last() != Direction.Left) Inputs[0].Add(Direction.Left); break;
                            case ConsoleKey.RightArrow: if (Inputs[0].Count == 0 || Inputs[0].Last() != Direction.Right) Inputs[0].Add(Direction.Right); break;
                            case ConsoleKey.P: SpacePause(" Game Paused ... Push Space  "); nonum = true; break;
                            default: break;
                        }
                    }

                    for (int a = 0; a < NumPlayers; a++)
                    {
                        if (Inputs[a].Count > 0)
                        {
                            var input = Inputs[a][0];
                            Inputs[a].RemoveAt(0);
                            switch (input)
                            {
                                case Direction.Up: if (Snakes[a].Direction != Direction.Down) Snakes[a].Direction = Direction.Up; break;
                                case Direction.Down: if (Snakes[a].Direction != Direction.Up) Snakes[a].Direction = Direction.Down; break;
                                case Direction.Left: if (Snakes[a].Direction != Direction.Right) Snakes[a].Direction = Direction.Left; break;
                                case Direction.Right: if (Snakes[a].Direction != Direction.Left) Snakes[a].Direction = Direction.Right; break;
                            }
                        }

                        // Move Snake
                        switch (Snakes[a].Direction)
                        {
                            case Direction.Up: Snakes[a].Row = Snakes[a].Row - 1; break;
                            case Direction.Down: Snakes[a].Row = Snakes[a].Row + 1; break;
                            case Direction.Left: Snakes[a].Col = Snakes[a].Col - 1; break;
                            case Direction.Right: Snakes[a].Col = Snakes[a].Col + 1; break;
                        }

                        // If snake hits number, respond accordingly
                        if (numberRow == (Snakes[a].Row + 1) / 2 && numberCol == Snakes[a].Col)
                        {
                            Music.Play("O0L16>CCCE", true);
                            if (Snakes[a].Length < MAXSNAKELENGTH - 30)
                                Snakes[a].Length = Snakes[a].Length + number * 4;

                            Snakes[a].Score = Snakes[a].Score + number;
                            PrintScore(NumPlayers, Snakes[0].Score, Snakes[1].Score, Snakes[0].Lives, Snakes[1].Lives);
                            number = number + 1;
                            if (number == 10)
                            {
                                EraseSnake(Snakes[0]);
                                EraseSnake(Snakes[1]);
                                ConsoleSetCursorPosition(numberCol, numberRow);
                                Console.Write(" ");
                                Level(LevelChoice.NextLevel, Snakes);
                                PrintScore(NumPlayers, Snakes[0].Score, Snakes[1].Score, Snakes[0].Lives, Snakes[1].Lives);
                                SpacePause("     Level " + CurLevel + ", Push Space");
                                if (NumPlayers == 1)
                                    Snakes[1].Row = 0;
                                number = 1;
                                if (IncreaseSpeedDuringPlay)
                                    curSpeed = Math.Max(10, curSpeed - 10);
                            }
                            nonum = true;
                        }
                    }

                    for (int a = 0; a < NumPlayers; a++)
                    {
                        // If player runs into any point, or the head of the other snake, it dies.
                        if (PointIsThere(Snakes[a].Row, Snakes[a].Col, Background) || (Snakes[0].Row == Snakes[1].Row && Snakes[0].Col == Snakes[1].Col))
                        {
                            Music.Play("O0L32EFGEFDC", true);
                            Console.BackgroundColor = Background;
                            ConsoleSetCursorPosition(numberCol, numberRow);
                            Console.Write(" ");
                            playerDied = true;
                            Snakes[a].Alive = false;
                            Snakes[a].Lives = Snakes[a].Lives - 1;
                        }
                        // Otherwise, move the snake, and erase the tail
                        else
                        {
                            Snakes[a].Head = (Snakes[a].Head + 1) % MAXSNAKELENGTH;
                            Snakes[a].Body[Snakes[a].Head].Row = Snakes[a].Row;
                            Snakes[a].Body[Snakes[a].Head].Col = Snakes[a].Col;
                            var tail = (Snakes[a].Head + MAXSNAKELENGTH - Snakes[a].Length) % MAXSNAKELENGTH;
                            Set(Snakes[a].Body[tail].Row, Snakes[a].Body[tail].Col, Background);
                            Snakes[a].Body[tail].Row = 0;
                            Set(Snakes[a].Row, Snakes[a].Col, Snakes[a].Color);
                        }
                    }
                }
                while (!playerDied);

                // reset speed to initial value
                curSpeed = Speed;

                for (int a = 0; a < NumPlayers; a++)
                {
                    // If dead, then erase snake in really cool way
                    EraseSnake(Snakes[a]);

                    if (!Snakes[a].Alive)
                    {
                        // Update score
                        Snakes[a].Score = Snakes[a].Score - 10;
                        PrintScore(NumPlayers, Snakes[0].Score, Snakes[1].Score, Snakes[0].Lives, Snakes[1].Lives);

                        if (a == 0)
                            SpacePause(" Sammy Dies! Push Space! --->");
                        else
                            SpacePause(" <--- Jake Dies! Push Space! ");
                    }
                }

                Level(LevelChoice.SameLevel, Snakes);
                PrintScore(NumPlayers, Snakes[0].Score, Snakes[1].Score, Snakes[0].Lives, Snakes[1].Lives);

                // Play next round, until either of snake's lives have run out.
            }
            while (Snakes[0].Lives != 0 && Snakes[1].Lives != 0);
        }

        /// <summary>Checks the global  arena array to see if the boolean flag is set</summary>
        private static bool PointIsThere(int row, int col, ConsoleColor color)
        {
            if (row != 0)
                return (Arena[row, col].Color != color);
            return false;
        }

        /// <summary>Prints players scores and number of lives remaining</summary>
        private static void PrintScore(int NumPlayers, int score1, int score2, int lives1, int lives2)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = Background;

            if (NumPlayers == 2)
            {
                ConsoleSetCursorPosition(1, 1);
                Console.Write(string.Format("{0,7}  Lives: {1}  <--JAKE", score2, lives2));
            }
            ConsoleSetCursorPosition(49, 1);
            Console.Write(string.Format("SAMMY-->  Lives: {0}     {1,7}", lives1, score1));
        }

        /// <summary>Sets row and column on playing field to given color to facilitate moving of snakes around the field.</summary>
        private static void Set(int row, int col, ConsoleColor color)
        {
            if (row > 48 && col == 80)
                return;
            if (row != 0)
            {
                // assign color to arena
                Arena[row, col].Color = color;
                // Get real row of pixel
                var realRow = Arena[row, col].RealRow;
                // Deduce whether pixel is on top▀, or bottom▄
                bool topFlag = Arena[row, col].Sister == 1;
                // Get arena row of sister
                var sisterRow = row + Arena[row, col].Sister;
                // Determine sister's color
                var sisterColor = Arena[sisterRow, col].Color;

                ConsoleSetCursorPosition(col, realRow);

                if (color == sisterColor)
                {
                    Console.ForegroundColor = color;
                    Console.BackgroundColor = color;
                    Console.Write("█");
                }
                else
                {
                    if (topFlag)
                    {
                        if ((int) color > 7)
                        {
                            Console.ForegroundColor = color;
                            Console.BackgroundColor = sisterColor;
                            Console.Write("▀");
                        }
                        else
                        {
                            Console.ForegroundColor = sisterColor;
                            Console.BackgroundColor = color;
                            Console.Write("▄");
                        }
                    }
                    else
                    {
                        if ((int) color > 7)
                        {
                            Console.ForegroundColor = color;
                            Console.BackgroundColor = sisterColor;
                            Console.Write("▄");
                        }
                        else
                        {
                            Console.ForegroundColor = sisterColor;
                            Console.BackgroundColor = color;
                            Console.Write("▀");
                        }
                    }
                }
            }
        }

        /// <summary>Pauses game play and waits for space bar to be pressed before continuing</summary>
        private static void SpacePause(string text)
        {
            Console.ForegroundColor = DlgFore;
            Console.BackgroundColor = DlgBack;
            Center(11, "█▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀█");
            Center(12, "█ " + (text + new string(' ', 29)).Substring(0, 29) + " █");
            Center(13, "█▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄█");
            while (Console.KeyAvailable)
                Console.ReadKey(true);
            while (Console.ReadKey(true).Key != ConsoleKey.Spacebar) { }
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = Background;

            // Restore the screen background
            for (int i = 21; i <= 26; i++)
                for (int j = 24; j <= 57; j++)
                    Set(i, j, Arena[i, j].Color);
        }

        /// <summary>Creates flashing border for intro screen</summary>
        private static void SparklePause()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;

            bool stop = false;
            var t = new Thread(() =>
            {
                string aa = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    ";

                while (!stop)
                {
                    for (int a = 1; (a <= 5) && !stop; a++)
                    {
                        Thread.Sleep(30);

                        // print horizontal sparkles
                        ConsoleSetCursorPosition(1, 1);
                        Console.Write(aa.Substring(a, 80));
                        ConsoleSetCursorPosition(1, 22);
                        Console.Write(aa.Substring(6 - a, 80));

                        // Print Vertical sparkles
                        for (int b = 2; b <= 21; b++)
                        {
                            var c = (a + b) % 5;
                            if (c == 1)
                            {
                                ConsoleSetCursorPosition(80, b);
                                Console.Write("*");
                                ConsoleSetCursorPosition(1, 23 - b);
                                Console.Write("*");
                            }
                            else
                            {
                                ConsoleSetCursorPosition(80, b);
                                Console.Write(" ");
                                ConsoleSetCursorPosition(1, 23 - b);
                                Console.Write(" ");
                            }
                        }
                    }
                }
            });
            t.Start();
            Console.ReadKey(true);
            stop = true;
            t.Join();
        }

        /// <summary>Determines if users want to play game again.</summary>
        private static bool StillWantsToPlay()
        {
            Console.ForegroundColor = DlgFore;
            Console.BackgroundColor = DlgBack;
            Center(10, "█▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀█");
            Center(11, "█       G A M E   O V E R       █");
            Center(12, "█                               █");
            Center(13, "█      Play Again?   (Y/N)      █");
            Center(14, "█▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄█");

            char kbd;
            do
                kbd = char.ToUpperInvariant(Console.ReadKey(true).KeyChar);
            while (kbd != 'Y' && kbd != 'N');

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = Background;
            Center(10, "                                 ");
            Center(11, "                                 ");
            Center(12, "                                 ");
            Center(13, "                                 ");
            Center(14, "                                 ");

            if (kbd == 'N')
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
            }

            return (kbd == 'Y');
        }

        private static void ConsoleSetCursorPosition(int x, int y)
        {
            Console.SetCursorPosition(x - 1, y - 1);
        }
    }
}
