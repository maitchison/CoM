
using System;
using System.IO;

using Data;

namespace Mordor.Importers
	
{
	/**
	 * Ancestor class for importers of MDATA formated files 
	 */
	public abstract class ImporterTemplate<T, U> 
		where T : DataLibrary<U>, new()
		where U: NamedDataObject 
	{
		/** Used to read data from a MDATA file in a friendly way (i.e support for some VB strings and records) */
		protected MDRFileReader Data;

		protected int CountRecord;
		protected int FirstRecord;

		/** If true objects will be assigned a sequential ID as they are loaded */
		protected bool AutoAssignID = false;

		/**
		 * Creates, and configures a new ImporterTemplate.
		 * 
	 	 * @param source A stream referencing the MDATA file to import
		 * @param recordSize The size, in bytes, of each record in the source file.
		 * @param countRecord Index to the record containing the number of objects in this file. 
		 * @param firstRecord Index to the first record containing a valid object.
		 */
		public ImporterTemplate(Stream source, int recordSize, int countRecord, int firstRecord = -1)
		{
			CountRecord = countRecord;
			FirstRecord = firstRecord;
			SetupInput(source,recordSize);
		}

		/** 
		 * Configures the stream for input. 
		 * 
		 * @param source A stream referencing the MDATA file to import
		 * @param recordSize The size, in bytes, of each record in the source file
		 */
		protected void SetupInput(Stream source,int recordSize)
		{
			Data = new MDRFileReader(source);
			Data.RecordSize = recordSize;
		}

		/** 
		 * Imports a library of information from MDATA source, returns the library.
		 * 
		 * @returns The library.
		 */
		public T Import()
		{
			var result = new T();
			var maxID = 0;

			// find number of objects
			Data.RecordSeek(CountRecord);
			int count = Data.ReadWord();

			if ((count < 0) || (count > 10000))
				throw new Exception("Too many items in file, found "+count);

			// Read in each object.
			Data.RecordSeek(FirstRecord);
			for (var lp = 0; lp < count; lp ++)
			{
				Data.RecordSeek(FirstRecord+lp);
				U record = ReadObject();
				if (AutoAssignID)
					record.ID = lp;
				if (record.ID > maxID)
					maxID = record.ID;
				result.Add(record);
				Data.NextRecord();
			}

			result.CurrentID = maxID+1;
			
			return result;
		}

		/**
		 * Read and return a single object from source file 
		 */
		abstract protected U ReadObject();

	}
}