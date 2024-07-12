// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    public abstract class BaseEnumerator<T> : IEnumerator<T>
    {
        public abstract T Current { get; }

        object? IEnumerator.Current => ((BaseEnumerator<T>)this).Current;

        public virtual void Dispose() { }

        public abstract bool MoveNext();

        public abstract void Reset();
    }

}
