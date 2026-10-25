using Ballance2.Services.Debug;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Threading.Tasks;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ZipUtils.cs
* 
* Purpose:
* ZIP archive helpers.
*
* Author:
* mengyu
*
*/
namespace Ballance2.Utils
{
  /// <summary>
  /// ZIP archive helpers.
  /// </summary>
  public class ZipUtils
  {
    /// <summary>
    /// Add a file to a ZIP archive.
    /// </summary>
    /// <param name="zipStream">ZIP stream.</param>
    /// <param name="file">File path to add.</param>
    /// <param name="subIndex">Index at which to slice the path into the archive filename.</param>
    /// <param name="crc">CRC32 checksum.</param>
    public static void AddFileToZip(ZipOutputStream zipStream, string file, int subIndex, ref Crc32 crc)
    {
      string fileName = file.Substring(subIndex);
      AddFileToZip(zipStream, file, fileName, ref crc);
    }
    /// <summary>
    /// Add a file to a ZIP archive.
    /// </summary>
    /// <param name="zipStream">ZIP stream.</param>
    /// <param name="file">File path to add.</param>
    /// <param name="inZipFilePath">Path inside the ZIP.</param>
    /// <param name="crc">CRC32 checksum.</param>
    public static void AddFileToZip(ZipOutputStream zipStream, string file, string inZipFilePath, ref Crc32 crc)
    {
      FileStream fileStream = File.OpenRead(file);
      byte[] buffer = new byte[fileStream.Length];
      fileStream.Read(buffer, 0, buffer.Length);

      ZipEntry entry = new ZipEntry(inZipFilePath);
      entry.DateTime = DateTime.Now;
      entry.Size = fileStream.Length;
      fileStream.Close();
      crc.Reset();
      crc.Update(buffer);
      entry.Crc = crc.Value;
      zipStream.PutNextEntry(entry);
      zipStream.Write(buffer, 0, buffer.Length);
    }
    /// <summary>
    /// Create a ZIP archive.
    /// </summary>
    /// <param name="file">ZIP file path.</param>
    /// <returns>ZIP stream handle.</returns>
    public static ZipOutputStream CreateZipFile(string file)
    {
      ZipOutputStream zipStream = new ZipOutputStream(File.Create(file));
      zipStream.SetLevel(0);  // Compression level 0-9.
      return zipStream;
    }
    /// <summary>
    /// Close a ZIP archive.
    /// </summary>
    /// <param name="file">ZIP file path.</param>
    /// <returns>ZIP stream handle.</returns>
    public static void CloseZipFile(ZipOutputStream file)
    {
      file.Close();
      file.Dispose();
    }
    /// <summary>
    /// Open an existing ZIP archive.
    /// </summary>
    /// <param name="file">ZIP file path.</param>
    /// <returns>ZIP stream handle.</returns>
    public static ZipInputStream OpenZipFile(string file)
    {
      ZipInputStream zipStream = null;
      try
      {
        zipStream = new ZipInputStream(File.OpenRead(file));
      }
      catch (Exception e)
      {
        Log.E("ZipUtils", "Load Zip file {0} failed! {1}", file, e.ToString());
        GameErrorChecker.LastError = GameError.FileReadFailed;
      }
      return zipStream;
    }
    /// <summary>
    /// Open a stream as a ZIP archive.
    /// </summary>
    /// <param name="stream">File stream.</param>
    /// <returns>ZIP stream handle.</returns>
    public static ZipInputStream OpenZipStream(Stream stream)
    {
      ZipInputStream zipStream = null;
      try
      {
        zipStream = new ZipInputStream(stream);
      }
      catch (Exception e)
      {
        Log.E("ZipUtils", "Load Zip stream failed! {1}", e.ToString());
        GameErrorChecker.LastError = GameError.FileReadFailed;
      }
      return zipStream;
    }
    /// <summary>
    /// Read a ZIP entry into memory.
    /// </summary>
    /// <param name="zip">ZIP stream.</param>
    /// <returns></returns>
    public static MemoryStream ReadZipFileToMemory(ZipInputStream zip)
    {
      int size;
      MemoryStream ms = new MemoryStream();
      byte[] buffer = new byte[4096];
      while (true)
      {
        size = zip.Read(buffer, 0, 4096);
        if (size > 0)
          ms.Write(buffer, 0, size);
        else break;
      }
      return ms;
    }
    /// <summary>
    /// Read a ZIP entry into memory asynchronously.
    /// </summary>
    /// <param name="zip">ZIP stream.</param>
    /// <returns></returns>
    public static async Task<MemoryStream> ReadZipFileToMemoryAsync(ZipInputStream zip)
    {
      MemoryStream ms = new MemoryStream();
      byte[] buffer = new byte[1048576];
      while (true)
      {
        // SharpZipLib's Stream.ReadAsync uses worker threads, unavailable on Web.
        int i = zip.Read(buffer, 0, buffer.Length);
        if (i > 0)
          ms.Write(buffer, 0, i);
        else break;
        await Task.Yield();
      }
      return ms;
    }
  }
}
