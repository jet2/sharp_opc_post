using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace Siemens.Opc.DaClient
{
    class StateRedundancyController
    {
        ManualResetEvent IsMaster;
        public StateRedundancyController(ManualResetEvent myevent)
        {
            IsMaster = myevent;
        }

        public void SetStateMaster()
        {
            this.IsMaster.Set();
        }

        public void SetStateSlave()
        {
            this.IsMaster.Reset();
        }
    }


    class UploaderByPost
    {

        private static string PrepareData(string filetext)
        {

            return filetext;
        }

        public static string UploadFile(string fn, string uriString)
        {

            string fileName = fn;
            string filetext;

            using (StreamReader sr = new StreamReader(fn))
            {
                //This allows you to do one Read operation.
                filetext = sr.ReadToEnd();
                filetext = PrepareData(filetext);
            }

            string responseFromServer;

            WebRequest request = WebRequest.Create(uriString);
            request.Method = "POST";
    
            string postData = "marker=" + fn + "&data=" + filetext;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            // The using block ensures the stream is automatically closed.
            using (dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                // Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();
            return responseFromServer;
        }
    }

}