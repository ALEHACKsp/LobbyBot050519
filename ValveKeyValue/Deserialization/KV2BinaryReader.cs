using System;
using System.IO;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KV2BinaryReader : IVisitingReader
    {
        public KV2BinaryReader(Stream stream, IVisitationListener listener)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(listener, nameof(listener));

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            this.stream = stream;
            this.listener = listener;
            reader = new BinaryReader(stream);
        }

        readonly Stream stream;
        readonly BinaryReader reader;
        readonly IVisitationListener listener;
        bool disposed;

        public void ReadObject()
        {
            Require.NotDisposed(nameof(KV1TextReader), disposed);

            try
            {
                ReadObjectCore();
            }
            catch (IOException ex)
            {
                throw new KeyValueException("Error while reading binary KeyValues.", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new KeyValueException("Error while parsing binary KeyValues.", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new KeyValueException("Error while parsing binary KeyValues.", ex);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                reader.Dispose();
                stream.Dispose();
                disposed = true;
            }
        }

        void ReadObjectCore()
        {
            var type = ReadNextNodeType();
            var name = ReadNullTerminatedString();
            ReadValue(name, type);
        }

        void ReadValue(string name, KV2BinaryNodeType type)
        {
            KVValue value;

            switch (type)
            {
                case KV2BinaryNodeType.ChildObject:
                    {
                        listener.OnObjectStart(name);
                        do
                        {
                            ReadObjectCore();
                        }
                        while (PeekNextNodeType() != KV2BinaryNodeType.End);
                        listener.OnObjectEnd();
                        return;
                    }

                case KV2BinaryNodeType.String:
                    value = new KVObjectValue<string>(ReadNullTerminatedString(), KVValueType.String);
                    break;

                case KV2BinaryNodeType.WideString:
                    throw new NotSupportedException("Wide String is not supported.");

                case KV2BinaryNodeType.Int32:
                case KV2BinaryNodeType.Color:
                case KV2BinaryNodeType.Pointer:
                    value = new KVObjectValue<int>(reader.ReadInt32(), KVValueType.Int32);
                    break;

                case KV2BinaryNodeType.UInt64:
                    value = new KVObjectValue<ulong>(reader.ReadUInt64(), KVValueType.UInt64);
                    break;

                case KV2BinaryNodeType.Float32:
                    var floatValue = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    value = new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            listener.OnKeyValuePair(name, value);
        }

        string ReadNullTerminatedString()
        {
            var sb = new StringBuilder();
            byte nextByte;
            while ((nextByte = reader.ReadByte()) != 0)
            {
                sb.Append((char)nextByte);
            }

            return sb.ToString();
        }

        KV2BinaryNodeType ReadNextNodeType()
            => (KV2BinaryNodeType)reader.ReadByte();

        KV2BinaryNodeType PeekNextNodeType()
        {
            var value = ReadNextNodeType();
            stream.Seek(-1, SeekOrigin.Current);
            return value;
        }
    }
}
