// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace CompactOT
{
    public interface ICostEstimator
    {
        double EstimateCost(ObliviousTransferUsageProjection usageProjection);
    }
}
