using System.Collections.Generic;
using System.Linq;

namespace CompactOT.DataStructures
{
    public static class ByteEnumerableOperations
    {
        public static IEnumerable<byte> And(IEnumerable<byte> left, IEnumerable<byte> right)
        {
            return left.Zip(right, (x, y) => (byte)(x & y));
        }

        public static IEnumerable<byte> Or(IEnumerable<byte> left, IEnumerable<byte> right)
        {
            return left.Zip(right, (x, y) => (byte)(x | y));
        }

        public static IEnumerable<byte> Xor(IEnumerable<byte> left, IEnumerable<byte> right)
        {
            return left.Zip(right, (x, y) => (byte)(x ^ y));
        }

        public static IEnumerable<byte> Not(IEnumerable<byte> a)
        {
            return a.Select(x => (byte)~x);
        }

        public static byte[] And(byte[] left, IEnumerable<byte> right)
        {
            return And(left.AsEnumerable(), right).ToArray();
        }

        public static void InPlaceAnd(byte[] left, IEnumerable<byte> right)
        {
            And(left.AsEnumerable(), right).WriteInto(left);
        }

        public static byte[] Or(byte[] left, IEnumerable<byte> right)
        {
            return Or(left.AsEnumerable(), right).ToArray();
        }

        public static void InPlaceOr(byte[] left, IEnumerable<byte> right)
        {
            Or(left.AsEnumerable(), right).WriteInto(left);
        }

        public static byte[] Xor(byte[] left, IEnumerable<byte> right)
        {
            return Xor(left.AsEnumerable(), right).ToArray();
        }

        public static void InPlaceXor(byte[] left, IEnumerable<byte> right)
        {
            Xor(left.AsEnumerable(), right).WriteInto(left);
        }

        public static byte[] Not(byte[] bytes)
        {
            return Not(bytes).ToArray();
        }

        public static void InPlaceNot(byte[] bytes)
        {
            Not(bytes.AsEnumerable()).WriteInto(bytes);
        }
    }
}