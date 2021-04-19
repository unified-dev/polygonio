using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PolygonIo.WebSocket.Contracts;
using PolygonIo.WebSocket.Deserializers;
using PolygonIo.WebSocket.Factory;
using System;
using System.Buffers;
using System.Text;

namespace PolygonIo.WebSocket.Tests
{
    public class BufferTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CanProcessData()
        {
            Chunk<char> startChnk;
            var currentChnk = startChnk = new Chunk<char>(new ReadOnlyMemory<char>("\"hello\".\"mu".ToCharArray()));
            currentChnk = currentChnk.Add(new ReadOnlyMemory<char>("ch\".".ToCharArray()));
            currentChnk = currentChnk.Add(new ReadOnlyMemory<char>("\"fun\"".ToCharArray()));
            currentChnk = currentChnk.Add(new ReadOnlyMemory<char>("\n\"done\"\n".ToCharArray()));
            var seq = new ReadOnlySequence<char>(startChnk, 0, currentChnk, currentChnk.Memory.Length);

            Span<char> localBuffer = new char[(int)seq.Length];
            seq.FirstSpan.CopyTo(localBuffer);

            // TODO: Allow arbitary number of frames.
            var otherStart = localBuffer.Slice(seq.FirstSpan.Length, localBuffer.Length - seq.FirstSpan.Length);
            var enumerator = seq.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Span.CopyTo(otherStart);
        }

        class Chunk<T> : ReadOnlySequenceSegment<T>
        {
            public Chunk(ReadOnlyMemory<T> memory) => Memory = memory;

            public Chunk<T> Add(ReadOnlyMemory<T> mem)
            {
                var segment = new Chunk<T>(mem) { RunningIndex = RunningIndex + Memory.Length };
                Next = segment;
                return segment;
            }
        }
    }
}