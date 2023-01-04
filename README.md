# CompactObliviousTransfer

A C# library implementing oblivious transfer. Oblivious transfer (OT) is a family of cryptographic protocols which enable a receiver to select one out of several options offered by a sender, without the sender learning which (and without the receiver learning the contents of the other options offered by the sender).

## Features

- Abstract interfaces for protocol-agnostic 1-out-of-N oblivious transfer
- Implementation of the 1-out-of-N Naor-Pinkas OT protocol [1, 2]
- Implementation of the 1-out-of-N OT extension protocol [3, 4, 5]
- Interfaces and implementations of random and correlated oblivious transfer [6]

## Installation

`CompactObliviousTransfer` will soon be available from nuget. Until then, clone the git repository:

```
git clone https://github.com/lumip/CompactObliviousTransfer
```

## Usage

The core components of `CompactObliviousTransfer` are implementations of the `IObliviousTransferChannel` interface for generic 1-out-of-N oblivious transfer. These represent a local endpoint for an arbitrary number of oblivious transfer invocations between two fixed parties.

The recommended way of instantiating a channel is to use the `ObliviousTransferChannelBuilder` class. `ObliviousTransferChannelBuilder` optionally can be provided a number of arguments to configure the requirements for the OT invocations, such as the maximum number of message options provided by a sender to the receiver, how many invocations of the protocol to expect over the lifetime of the channel and how many independent messages to be exchanged in each invocation (i.e., the size of the invocation batch). The builder uses the information provided to select the most efficient protocol to instantiate.

The following code snippet creates a builder configured for 100 1-out-of-4 OTs split over 5 batches of 20 messages each, at a security level of 128 bits. Note that it is possible to not specify any of the configuration options except the maximum number of options provided by the sender. The builder will then assume a default security level of 128 bits and an unbounded number of invocations.

```C#
var channelBuilder = new ObliviousTransferChannelBuilder()
                        .WithSecurityLevel(128)
                        .WithMaximumNumberOfOptions(4)
                        .WithAverageInvocationsPerBatch(20)
                        .WithMaximumNumberOfBatches(5);
```

You can then use `channelBuilder` to instantiate OT channels with other parties/computers. In order to do so, you have to provide an implementation of a `IMessageChannel` to `channelBuilder.MakeObliviousTransferChannel`. The `IMessageChannel` represents a network connection between the two parties (the local and remote computer) and offers methods to send and receive messages to the local computer. `CompactObliviousTransfer` provides an implementation based on `System.Net.Sockets.NetworkStream` which could be used as follows to establish an *insecure* TCP connection.

```C#
// opening a port for connection
TcpListener tcpListener = new TcpListener(localEndpoint, Port);
tcpListener.Start();
using (TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync())
{
    using (NetworkStream tcpStream = tcpClient.GetStream())
    {
        IMessageChannel channel = new NetworkStreamMessageChannel(tcpStream);
        IObliviousTransferChannel otChannel = otChannelBuilder.MakeObliviousTransferChannel(channel);
    }
}

// OR connecting to a remote computer
using (TcpClient tcpClient = new TcpClient())
{
    await tcpClient.ConnectAsync(remoteEndpoint, Port);
    using (NetworkStream tcpStream = tcpClient.GetStream())
    {
        IMessageChannel channel = new NetworkStreamMessageChannel(tcpStream);
        IObliviousTransferChannel otChannel = otChannelBuilder.MakeObliviousTransferChannel(channel);
    }
}
```

**NOTE** that `IObliviousTransferChannel` implementations do not perform authentication but rather assume that the underlying `IMessageChannel` provides secure and authenticated connection. Therefore **the underlying connection should be secure via TLS**, which is not the case in the above simple example for simplicity. `CompactObliviousTransfer` does not provide certificate or key management functionality.

Once the `IObliviousTransferChannel` instance (`otChannel`) is instantiated by both of the communicating parties, it is ready to be used for oblivious transfer invocations with either party acting as sender or receiver. Currently both parties must agree on the number of messages to be transferred in a single batch invocation before invoking `IObliviousTransferChannel`s methods.

### Acting as Sender

The sender must prepare the message options. In the above example, for the first batch of messages to be exchanged, the sender must prepare an `ObliviousTransferOptions` instance with 4 options for 20 independent messages. Then the sender simply provides the prepared options to the `SendAsync` method of `otChannel` as follows, assuming that each message (option) is (at most) 32 bits long:

```C#
var options = new ObliviousTransferOptions(numberOfInvocations: 20, numberOfOptions: 4, numberOfMessageBits: 32);
for (int i = 0; i < options.NumberOfInvocations; ++i)
{
    for (int j = 0; j < options.NumberOfOptions; ++j)
    {
        BitSequence messageOption = ...; // obtain the j-th option for the i-th message in the batch
        options.SetMessage(i, j, messageOption);
    }
}

await otChannel.SendAsync(options);
```

### Acting as Receiver

The sender must prepare its selections of options. In the above example, for the first batch of messages to be exchanged, the receiver must prepare an integer array of 20 indices from 0 to 3, indicating one of the 4 options selected from the options provided by the sender as each message. The receiver then invokes the `ReceiveAsync` method of `otChannel` as follows and obtains an instance of `ObliviousTransferResult` that contains the received option for each of the 20 messages:

```C#
int[] selectionIndices = new int[] { /* a list of 20 selection indices from 0 to 3 */ };
ObliviousTransferResult otResult = await otChannel.ReceiveAsync(new int[] { selectionIndex }, numberOfOptions: 4, numberOfMessageBits: 32);

for (int i = 0; i otResult.NumberOfInvocations; ++i)
{
    BitSequence message = otResult.GetInvocationResult(i);
    // ... do something with the received message
}
```

### Handling Messages and Message Options

Since oblivious transfer is often used for very small messages, `CompactObliviousTransfer` handles messages on the bit level. Efficient composition and manipulation of arrays and enumerables of bits is provided by the classes in the `CompactOT.DataStructures` namespace. `CompactOT.DataStructures.BitSequence` is the high level common interface and represents a sequence of bits of an arbitrary length that can be concatenated or used in bitwise operations with other bit sequences.

Operations and compositions on `BitSequence`s are lazily evaluated, i.e., the result is only computed when it is required and intermediate values are not held in memory. To force evaluation of a `BitSequence` at any given point, convert it into a `CompactOT.DataStructures.BitArray` using `BitArray`'s constructor.

### Examples

Check the `Examples` folder to see how `CompactObliviousTransfer` can be used to perform

- compute a simple XOR gate where each of two parties contributes an input bit (`Examples/SimpleXor`),
- perform bit-wise addition where each of two parties contributes one input using a [garbled circuit](https://wiki.mpcalliance.org/garbled_circuit.html) (`Examples/GarbledCircuit`), or
- generation of Beaver Multiplication Triples for subsequent use in [GMW type secure two-party computation protocols](https://wiki.mpcalliance.org/beaver.html) (`Examples/BeaverTriples`).

## API Overview

Below is a brief description of the core components of the `CompactObliviousTransfer` library in the `CompactOT` root namespace:

- `IMessageChannel`: Represents a channel for passing arbitrary messages between two parties/computers. Used as the underyling communication channel for oblivious transfer protocols.
- `NetworkStreamMessageChannel`: An implementation of `IMessageChannel` using `System.Net.Sockets.NetworkStream`.
- `IObliviousTransferChannel`: Provides oblivious transfer functionality between two parties. Can be used to send or receive messages by either parties. Parties must coordinate setting up the exact protocol and when to send/expect which number of messages.
- `ICorrelatedObliviousTransferChannel`: As `IObliviousTransferChannel` but for the correlated oblivious transfer (C-OT) protocol [6].
- `IRandomObliviousTransferChannel`: As `IObliviousTransferChannel` but for the random oblivious transfer (R-OT) protocol [6].
- `ObliviousTransferChannelBuilder`: Class that instantiates oblivious transfer channels based on user-provided configuration options.
- `IBaseProtocolFactory`: Interface for factory classes that provided the base protocol used by `ObliviousTransferChannelBuilder` for the OT extension protocol [3].
- `ObliviousTransferOptions`: Input of the sending party to `IObliviousTransferChannel.SendAsync`. Holds the message options offered by the sender as `BitSequence`s.
- `ObliviousTransferResults`: Output obtained by the receiving party from `IObliviousTransferChannel.ReceiveAsync`. Holds the message selected by the receiver from the options offered by the sender as a `BitSequence`.

The following protocol implementations are currently available:

- `NaorPinkasObliviousTransfer`: Implements the Naor-Pinkas OT protocol [1]. However, the protocol was adapted along the lines of [4] to allow for 1-out-of-N oblivious transfers.
- `ExtendedObliviousTransferChannel`: Implements the 1-out-of-N OT extension protocol [3, 4].
- `CorrelatedObliviousTransferChannel`: Implements 1-out-of-N correlated OT based on OT extension [6, 4].
- `RandomObliviousTransferChannel`: Implements 1-out-of-N random OT based on OT extension [6, 4].

The `CompactOT.Adapters` namespace provides adapters to obtain random or correlated OT from standard OT implementations. Note that these are generally less communication efficient than the direct R-OT or C-OT protocol implementations.

- `CorrelatedFromStandardObliviousTransferChannel`: 
- `RandomFromCorrelatedObliviousTransferChannel`:
- `RandomFromStandardObliviousTransferChannel`:

The `CompactOT.DataStructures` namespace contains classes to create and manipulate sequences or arrays of bits and forms the basis for the `ObliviousTransferOptions` and `ObliviousTransferResults` classes that are used as inputs and outputs for the oblivious transfer API.

- `BitSequence`: Abstract base class representing any sequence of bits. Provides bitwise operations (and, or, xor, etc). Results of these are lazily evaluated, intermediate results are not kept in memory.
- `BitArray`: An array of bits stored in memory. Implements `BitSequence`.
- `BitArraySlice`: A view on a slice of a `BitArray`. Can be used to work with a continuous sub-block of a `BitArray` as a `BitSequence`.
- `BitMatrix`: A two-dimensional matrix of bits.
- `EnumeratedBitArrayView`: Allows interpreting an `IEnumerable<Bit>` as a `BitSequence`.


## License

`CompactObliviousTransfer` as a whole is licensed under the [GPLv3 license](LICENSES/GPL-3.0-or-later.txt) (or any later version) for general use. If you would like to use `CompactObliviousTransfer` under different terms, contact the authors. Some parts of the code base have been adapted from different sources under the [MIT license](LICENSES/MIT.txt).

`CompactObliviousTransfer` aims to be [REUSE Software](https://reuse.software/) compliant to facilitate easy reuse.

## Versioning

`CompactObliviousTransfer` version numbers adhere to [Semantic Versioning](https://semver.org/).

## References

The following are the main references used for the protocols implemented in `CompactObliviousTransfer`:

1: Moni Naor and Benny Pinkas: Efficient oblivious transfer protocols 2001. https://dl.acm.org/citation.cfm?id=365502

2: Secure Multi-Party Computation of Boolean Circuits with Applications to Privacy in On-Line Marketplaces. https://link.springer.com/chapter/10.1007/978-3-642-27954-6_26

3: Yuval Ishai, Joe Kilian, Kobbi Nissim and Erez Petrank: Extending Oblivious Transfers Efficiently. 2003. https://link.springer.com/content/pdf/10.1007/978-3-540-45146-4_9.pdf

4: Vladimir Kolesnikov, Ranjit Kumaresan: Improved OT Extension for Transferring Short Secrets. 2013. https://www.microsoft.com/en-us/research/wp-content/uploads/2017/03/otext_crypto13.pdf

5: Michele Orrù, Emmanuela Orsini, Peter Scholl: Actively Secure 1-out-of-N OT Extension with Application to Private Set Intersection. 2017. https://hal.archives-ouvertes.fr/hal-01401005/file/933.pdf

6: Asharov, Lindell, Schneider, Zohner: More Efficient Oblivious Transfer and Extensions for Faster Secure Computation. 2013. https://thomaschneider.de/papers/ALSZ13.pdf
