using System.Buffers.Binary;
using System.Text;

namespace Discord {
    class Message {
        public uint opcode { get; private set; }
        public uint length { get; private set; }
        public byte[] payload { get; private set; }

        internal Message(uint opcode, byte[] payload) {
            this.opcode = opcode;
            this.payload = payload;
            this.length = (uint)payload.Length;
        }

        internal Message(byte[] message) {
            this.opcode = BinaryPrimitives.ReadUInt32LittleEndian(message[..4]);
            this.length = BinaryPrimitives.ReadUInt32LittleEndian(message[4..8]);
            this.payload = message[8..];
        }

        internal byte[] GetMessage() {
            byte[] opcodeAsByteArray = new byte[sizeof(uint)];
            byte[] lengthAsByteArray = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(opcodeAsByteArray, opcode);
            BinaryPrimitives.WriteUInt32LittleEndian(lengthAsByteArray, length);

            byte[] message = opcodeAsByteArray.Concat(lengthAsByteArray).Concat(payload).ToArray();
            return message;
        }

        public string payloadAsString() {
            return Encoding.Default.GetString(payload);
        }

        public override string ToString() {
            return opcode + "," + length + "," + payloadAsString();
        }
    }
}