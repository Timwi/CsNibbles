using System;

namespace Nibbles.Bas
{
    struct Snake
    {
        public int Head;
        public int Length;
        public int Row;
        public int Col;
        public Direction Direction;
        public int Lives;
        public int Score;
        public ConsoleColor Color;
        public bool Alive;
        public SnakePiece[] Body;
    }
}
