namespace CompactOT
{

    public static class MathUtil
    {
        public static int DivideAndCeiling(int x, int y)
        {
            return (x + (y - 1)) / y;
        }
    }
}
