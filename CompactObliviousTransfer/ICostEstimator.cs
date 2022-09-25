namespace CompactOT
{
    public interface ICostEstimator
    {
        double EstimateCost(ObliviousTransferUsageProjection usageProjection);
    }
}
