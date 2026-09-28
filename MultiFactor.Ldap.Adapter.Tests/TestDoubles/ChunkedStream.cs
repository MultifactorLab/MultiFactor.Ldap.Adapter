using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Tests.TestDoubles
{
    internal class ChunkedStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _chunkSize;
        private int _position;

        public int ReadCallCount { get; private set; }

        public ChunkedStream(byte[] data, int chunkSize)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _chunkSize = chunkSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;

            var available = _data.Length - _position;
            if (available <= 0) return 0;

            var toCopy = Math.Min(Math.Min(count, _chunkSize), available);
            Buffer.BlockCopy(_data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;

        public override long Position
        {
            get { return _position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
