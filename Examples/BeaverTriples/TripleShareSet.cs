using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

using CompactOT;
using CompactOT.DataStructures;


namespace CompactOT.Examples.BeaverTriples
{

    class TripleShareSet
    {
        public BitMatrix FirstFactorShare { get; }
        public BitMatrix SecondFactorShare { get; }
        public BitMatrix ProductShare { get; }

        public TripleShareSet(BitMatrix firstFactorShare, BitMatrix secondFactorShare, BitMatrix productShare)
        {
            if (firstFactorShare.Rows != secondFactorShare.Rows || secondFactorShare.Rows != productShare.Rows ||
                firstFactorShare.Cols != secondFactorShare.Cols || secondFactorShare.Cols != productShare.Cols)
            {
                throw new ArgumentException("All inputs must have same dimensions.");
            }

            FirstFactorShare = firstFactorShare;
            SecondFactorShare = secondFactorShare;
            ProductShare = productShare;
        }

        public int NumberOfTriples => FirstFactorShare.Rows;
        public int NumberOfTripleBits => FirstFactorShare.Cols;

        public (BitSequence, BitSequence, BitSequence) GetTripleShare(int i)
        {
            if (i < 0 || i >= NumberOfTriples)
                throw new ArgumentOutOfRangeException(nameof(i));

            return (FirstFactorShare.GetRow(i), SecondFactorShare.GetRow(i), ProductShare.GetRow(i));
        }
    }

}
