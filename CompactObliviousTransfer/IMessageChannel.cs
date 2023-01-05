// SPDX-FileCopyrightText: 2018 Jonas Nagy-Kuhlen <jonas.nagy-kuhlen@rwth-aachen.de>, 2023 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: MIT
// Adopted from CompactMPC: https://github.com/jnagykuhlen/CompactMPC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompactOT
{
    /// <summary>
    /// A full-duplex channel to exchange messages in the form of raw byte arrays between two endpoints.
    /// </summary>
    public interface IMessageChannel
    {
        /// <summary>
        /// Read a message represented by a byte array from the channel, input at the other end.
        /// </summary>
        /// <returns>A byte array containing the message received via the message channel.</returns>
        Task<byte[]> ReadMessageAsync();

        /// <summary>
        /// Write a message represented by a byte array to the channel, to be received at the other end.
        /// </summary>
        /// <param name="message">A byte array containing the message to be sent via the message channel.</param>
        Task WriteMessageAsync(byte[] message);
    }
}
