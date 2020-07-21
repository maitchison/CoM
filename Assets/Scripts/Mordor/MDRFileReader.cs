using System;
using System.IO;

namespace Mordor
{
    /// <summary>
    /// Helper wrapper for Stream class to allow the reading and writing 16 bit words
    /// </summary>
    public class MDRFileReader : IDisposable
    {

		/** Creates a new MDR file helper */
        public MDRFileReader(Stream source)
        { 
			_stream = source;
		}

		/** The stream to load data from */
		private Stream _stream;

		/** The size of the records used in this filestream */
		private int _recordSize = 20;

		public int RecordSize { get { return _recordSize;} set {_recordSize = value;} }

		/** Reads a 'prefixed' string from the file.  Prefix strings have their length recorded at the begining as a 16bit word */
		public string ReadPrefixString()
		{
			string result = "";
			int characters = ReadWord();

			if (characters < 0) throw new Exception("String too short: "+characters);

			if (characters > 256) throw new Exception("String too long: "+characters);

			for (int lp = 0; lp < characters; lp ++)
			{
				if (!_stream.CanRead)
					throw new Exception("Read error.");
				if (_stream.Position >= _stream.Length)
					throw new Exception("End of file.");

				byte b = (byte)ReadByte();

				//strip out non ascii characters
				if ((b >= 16) && (b <= 128))
					result += (char)b;
			}

			return result;
		}

		/** Reads a string of fixed length from the file.  */
		public string ReadFixedString(int length)
		{
			string result = "";

			for (int lp = 0; lp < length; lp ++)
			{
				result += (char)ReadByte();
			}

			return result.TrimEnd(' ');
		}


		/** Skips given number of bytes */
		public void Skip(int bytes)
		{
			_stream.Position += bytes;
		}

		/** Skips given number of records, will seek to the start of the new record. */
		public void SkipRecords(int delta)
		{
			RecordSeek(CurrentRecord+delta);
		}

        /// <summary>
        /// reads a 16bit word from file
        /// </summary>
        public int ReadWord()
        {
            int value = ReadByte();
            value = value + ReadByte()*256;
			return (UInt16)value;
        }

		public string DisplayWords(int count)
		{
			string result = "";
			for (int lp = 1; lp <= count; lp++)
				result += " "+lp+":"+ReadWord();
			return result;
		}

		/// <summary>
		/// reads a 16bit signed integer from file
		/// </summary>
		public int ReadShort()
		{
			byte[] data = new byte[2];
			Read(data, 0, 2);
			return BitConverter.ToInt16(data, 0);
		}

		/** reads a float from file */
		public float ReadFloat()
		{
			byte[] data = new byte[4];
			Read(data, 0, 4);
			return BitConverter.ToSingle(data, 0);
		}

		/// <summary>
		/// reads a 32bit signed integer from file
		/// </summary>
		public int ReadInt32()
		{
			byte[] data = new byte[4];
			Read(data, 0, 4);
			return BitConverter.ToInt32(data, 0);
	
		}

		/// <summary>
		/// reads an unsigned 32bit integer from file
		/// </summary>
		public UInt32 ReadUInt32()
		{
			byte[] data = new byte[4];
			Read(data, 0, 4);
			return BitConverter.ToUInt32(data, 0);	
		}

		/// <summary>
		/// reads an unsigned 16bit integer from file
		/// </summary>
		public UInt16 ReadUInt16()
		{
			byte[] data = new byte[2];
			Read(data, 0, 2);
			return BitConverter.ToUInt16(data, 0);
			
		}

        /// <summary>
        /// Reads a 16bit word from file, and advances to next record. 
        /// This is required because the MRD format uses 20 bytes for each 2 byte word
        /// </summary>
        public int ReadMDRWord()
        {
            int value = ReadWord();
			if (RecordSize > 2)
				NextRecord();
            return value;           
        }

        /// <summary>
        /// Seeks to given  record, begining with 1 as the first record of the file.
		/// Record size can be adjusted at the files creation 
        /// </summary>
        /// <param name="recordNumber"></param>
        public void RecordSeek(int recordNumber)
        {
            Seek((recordNumber-1)*RecordSize,SeekOrigin.Begin);
        }

		/**
		 * Moves to next record in file 
		 */
		public void NextRecord()
		{
			RecordSeek(CurrentRecord+1);
		}

		/**
		 * Returns the current record number we are in
		 */
		public int CurrentRecord
		{
			get 
			{
				return (int)(Position/RecordSize)+1;
			}
			set
			{
				RecordSeek(value);
			}
		}

        /// <summary>
        /// Reads an 8 byte currency type from next location in file.
        /// Note: The currency format was replaced by decimal a long time ago so the decimal equivilent is returned,
        /// but the format read from the file is the origional currency type.
        /// </summary>
        /// <returns></returns>
        public Decimal ReadCurrency()
        {
            byte[] data = new byte[8];
            Read(data, 0, 8);
            long cy = BitConverter.ToInt64(data, 0);
            return Decimal.FromOACurrency(cy);

        }

		public int ReadByte()
		{
			return _stream.ReadByte();
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer,offset,count);
		}

		public long Seek(long offset,SeekOrigin origin)
		{
			return _stream.Seek(offset,origin);
		}

		public long Position { get { return _stream.Position; }  set { _stream.Position = value; } }

		public void Close()
		{
			_stream.Close();
		}

		#region IDisposable implementation
		void IDisposable.Dispose()
		{
			Close();
		}
		#endregion
    }
}
