// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    public class BaseEnumeratorExhaustedException : Exception
    {
        public BaseEnumeratorExhaustedException() : base("The base enumerator was exhausted while the derived enumerator expects more elements.")
        {
            
        }
    }

}
