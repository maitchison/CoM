
#define ZIP_NONE

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

#if ZIP_SHARP
using ICSharpCode.SharpZipLib.Zip;
#endif

#if ZIP_IONIC
using Ionic.Zip;
#endif


namespace Data
{
	public static class Compressor
	{
		/** Compressess given source string into a zip file formatted as base64 */
		public static string Compress(string source)
		{
			#if ZIP_NONE
			return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(source));	
			#endif
			#if ZIP_IONIC	
			using (var zip = new Ionic.Zip.ZipFile())
			{
				// compressed data, then converts back to a string again 
				zip.AddEntry("x",source);
				var zipOutput = new MemoryStream();
				zip.Save(zipOutput);

				// Base 64 encoding (so that I can embed it in an XML file. 
				return System.Convert.ToBase64String(zipOutput.ToArray());	
			}
			#endif
			#if ZIP_SHARP
			using (MemoryStream fsOut = new MemoryStream())
			{
				using (var zipStream = new ZipOutputStream(fsOut))
				{
					zipStream.SetLevel(3);
					
					var newEntry = new ZipEntry("X");
					zipStream.PutNextEntry(newEntry);
					
					var sourceBytes = Encoding.UTF8.GetBytes(source);
					Util.CopyStream(new MemoryStream(sourceBytes),zipStream);
					
					zipStream.CloseEntry();
					zipStream.IsStreamOwner = false;
					zipStream.Close();
				}
				
				// Base 64 encoding (so that I can embed it in an XML file. 
				return Convert.ToBase64String(fsOut.ToArray());	
			}
			#endif

		}

		
		/** Decompresses given source string into a normal string.  Source should be a zipfile encoded to base64. */
		public static string Decompress(string source)
		{
			#if ZIP_NONE
			return new StreamReader(new MemoryStream(System.Convert.FromBase64String(source))).ReadToEnd();	
			#endif
			#if ZIP_IONIC
			// convert from base64 back again
			var data = System.Convert.FromBase64String(source);	
			var memoryStream = new MemoryStream(data);
			
			using(var zip = Ionic.Zip.ZipFile.Read(memoryStream)) 
			{
				if (zip.Entries.Count != 1)
					throw new Exception("Expecting 1 entry in zip, but found "+zip.Entries.Count);
				
				foreach (var entry in zip.Entries)
				{
					var stream = new MemoryStream();
					entry.Extract(stream);
					
					stream.Position = 0;
					StreamReader sr = new StreamReader(stream);
					return sr.ReadToEnd();
				}
			#endif
			#if ZIP_SHARP
			// convert from base64 back again
			var data = Convert.FromBase64String(source);	
			var memoryStream = new MemoryStream(data);
			
			var zip = new ZipFile(memoryStream);
			
			var entry = zip.GetEntry("X");
			Stream zipStream = zip.GetInputStream(entry);
			
			var uncompressed = new MemoryStream();
			Util.CopyStream(zipStream,uncompressed);			
			
			uncompressed.Position = 0;
			var sr = new StreamReader(uncompressed);
			return sr.ReadToEnd();
			#endif
		}
	
	}
}
