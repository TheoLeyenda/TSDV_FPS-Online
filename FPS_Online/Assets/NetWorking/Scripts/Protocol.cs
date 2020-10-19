using System;
using System.IO;

namespace Server
{
    class Protocol
    {
        private void InitWriter(int size)
        {
            m_buffer = new byte[size];
            m_stream = new MemoryStream(m_buffer);
            m_writer = new BinaryWriter(m_stream);
        }

        private void InitReader(byte[] buffer)
        {
            m_stream = new MemoryStream(buffer);
            m_reader = new BinaryReader(m_stream);
        }

        public byte[] Serialize(byte code, uint value)
        {
            const int bufSize = sizeof(byte) + sizeof(int);
            InitWriter(bufSize);
            m_writer.Write(code);
            m_writer.Write(value);
            return m_buffer;
        }

        public byte[] Serialize(byte code, uint value, 
                                float posX, float posY, float posZ, 
                                float rotX, float rotY, float rotZ,
                                float rotPivotWeaponX, float rotPivotWeaponY, float rotPivotWeaponZ)
        {
            const int bufSize = sizeof(byte) + sizeof(int) + (sizeof(float) * 9);
            InitWriter(bufSize);
            m_writer.Write(code);
            m_writer.Write(value);

            //POSICION
            m_writer.Write(posX);
            m_writer.Write(posY);
            m_writer.Write(posZ);

            //ROTACION
            m_writer.Write(rotX);
            m_writer.Write(rotY);
            m_writer.Write(rotZ);

            //ROTACION PIVOT
            m_writer.Write(rotPivotWeaponX);
            m_writer.Write(rotPivotWeaponY);
            m_writer.Write(rotPivotWeaponZ);

            return m_buffer;
        }

        public void Deserialize(byte[] buf, out byte code, out int value)
        {
            InitReader(buf);
            m_stream.Write(buf, 0, buf.Length);
            m_stream.Position = 0;
            code = m_reader.ReadByte();
            value = m_reader.ReadInt32();
        }

        private BinaryWriter m_writer;
        private BinaryReader m_reader;
        private MemoryStream m_stream;
        private byte[] m_buffer;
    }
}
