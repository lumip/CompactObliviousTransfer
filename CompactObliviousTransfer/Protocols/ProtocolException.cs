// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace CompactOT
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message)
        {

        }
    }
}
