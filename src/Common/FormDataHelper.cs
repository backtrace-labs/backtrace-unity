using System;
using System.Collections.Generic;
using System.Text;

namespace Backtrace.Unity.Common
{
    internal class FormDataHelper
    {
        private static readonly Encoding _encoding = Encoding.UTF8;

        /// <summary>
        /// Get formatted string with boundary information 
        /// </summary>
        /// <param name="id">Boundary - request Id</param>
        /// <returns>Boundary string</returns>
        internal static string GetBoundary(Guid id)
        {
            return string.Format("----------{0:N}", id);
        }
        /// <summary>
        /// Get form data content type with boundary
        /// </summary>
        /// <param name="id">Boundary - request Id</param>
        /// <returns>Content type with boundary</returns>
        internal static string GetContentTypeWithBoundary(Guid id)
        {
            return "multipart/form-data; boundary=" + GetBoundary(id);
        }

        /// <summary>
        /// Get form data bytes
        /// </summary>
        /// <param name="json">Diagnostic JSON</param>
        /// <param name="attachments">file attachments</param>
        /// <param name="boundaryId">Current request id</param>
        /// <returns>Form data bytes</returns>
        //internal static byte[] GetFormData(string json, List<string> attachments, Guid boundaryId)
        //{
        //    return GetFormData(json, attachments, GetBoundary(boundaryId));
        //}

        ///// <summary>
        ///// Get form data bytes
        ///// </summary>
        ///// <param name="json">Diagnostic JSON</param>
        ///// <param name="attachments">file attachments</param>
        ///// <param name="boundary">Current request id</param>
        ///// <returns>Form data bytes</returns>
        //internal static byte[] GetFormData(string json, List<string> attachments, string boundary)
        //{
        //    Stream formDataStream = new MemoryStream();

        //    //write jsonfile to formData
        //    Write(formDataStream, Encoding.UTF8.GetBytes(json), "upload_file", boundary, false);

        //    foreach (var attachmentPath in attachments)
        //    {
        //        if (!File.Exists(attachmentPath))
        //        {
        //            continue;
        //        }
        //        Write(formDataStream, File.ReadAllBytes(attachmentPath), "attachment_" + Path.GetFileName(attachmentPath), boundary);
        //    }

        //    // Add the end of the request.  Start with a newline
        //    string footer = "\r\n--" + boundary + "--\r\n";
        //    formDataStream.Write(_encoding.GetBytes(footer), 0, _encoding.GetByteCount(footer));

        //    // Dump the Stream into a byte[]
        //    formDataStream.Position = 0;
        //    byte[] formData = new byte[formDataStream.Length];
        //    formDataStream.Read(formData, 0, formData.Length);
        //    formDataStream.Close();
        //    return formData;
        //}

        ///// <summary>
        ///// Write file row to formData
        ///// </summary>
        ///// <param name="formDataStream">Current form data stream</param>
        ///// <param name="data">data to write</param>
        ///// <param name="name">file name</param>
        ///// <param name="boundary">Boundary with request id</param>
        ///// <param name="clrf">Check if clear row required</param>
        //private static void Write(Stream formDataStream, byte[] data, string name, string boundary, bool clrf = true)
        //{
        //    // Add a CRLF to allow multiple parameters to be added.
        //    if (clrf)
        //    {
        //        formDataStream.Write(_encoding.GetBytes("\r\n"), 0, _encoding.GetByteCount("\r\n"));
        //    }
        //    string fileHeader = $"--{boundary}\r\nContent-Disposition: form-data;" +
        //        $" name=\"{name}\"; filename=\"{name}\"\r\n" +
        //        $"Content-Type: application/octet-stream\r\n\r\n";

        //    formDataStream.Write(_encoding.GetBytes(fileHeader), 0, _encoding.GetByteCount(fileHeader));

        //    // Write the file data directly to the Stream, rather than serializing it to a string.
        //    formDataStream.Write(data, 0, data.Length);
        //}
    }
}
