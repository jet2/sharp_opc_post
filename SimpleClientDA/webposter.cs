using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;

namespace Siemens.Opc.DaClient
{

    class ThreadedWebSenderX
    {

        private Queue SendFilenameQueue;
        private ManualResetEvent StopEvent = new ManualResetEvent(false);
        private string webAddr ;
        public ThreadedWebSenderX(Queue sendfilenamequeue, string webaddr)
        {
            SendFilenameQueue = sendfilenamequeue;
            webAddr = webaddr;
            Thread t = new Thread(new ThreadStart(this.Execute));
            t.Start();

        }

        private void Execute()
        {
            List<string> translist = new List<string>();
            while (!StopEvent.WaitOne(0))
            {
                try
                {
                    if (SendFilenameQueue == null)
                    {
                        continue;
                    }
                    else
                    {
                        var zzz = SendFilenameQueue.Count;
                        try
                        {
                            while (zzz > 0)
                            {
                                translist.Add(SendFilenameQueue.Dequeue().ToString());
                                zzz--;
                            }
                            if (translist.Count > 0)
                            {
                                foreach(string fn in translist)
                                {
                                    if (StopEvent.WaitOne(0))
                                    {
                                        break;
                                    }
                                    if (File.Exists(fn))
                                    {
                                        string uresult = "000";
                                        try
                                        {
                                            uresult = UploaderByPost.UploadFile(fn, webAddr);
                                        }
                                        catch (Exception ex)
                                        {
                                            using (StreamWriter we = File.AppendText("errors.log"))
                                            {
                                                try
                                                {
                                                    we.WriteLine(dtTools.GetNowString() + " ERROR " + ex.Message);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                        
                                        }
                                        if (uresult == "999")
                                        {
                                            int cnt = 0;
                                            while (cnt < 10)
                                            {
                                                if (StopEvent.WaitOne(0))
                                                {
                                                    break;
                                                }
                                                try
                                                {
                                                    File.Delete(fn);
                                                    break;
                                                }
                                                catch
                                                {
                                                    cnt++;
                                                    Thread.Sleep(200);
                                                };
                                            };
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            translist.Clear();
                        }

                    }
                }
                finally
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void StopSend()
        {
            StopEvent.Set();
        }


    }


    class UploaderByPost
    {

        private static string PrepareData(string filetext)
        {

            return filetext;
        }
    
        public static bool CheckServerAccessible(string URL)
        {
            bool result = false;
            
            Uri myUri = new Uri(URL);
            // Get host part (host name or address and port). Returns "server:8080".

            var client = new WebClient();


            // Specify that the DownloadStringCallback2 method gets called
            // when the download completes.
            string mystring = "000";

            // обработчик  результата обращения к серверу
            client.DownloadStringCompleted +=
                    (s, hdlr_e) => {
                        try
                        {
                            var resultX = hdlr_e.Result;
                            mystring = resultX.ToString();
                        }
                        catch
                        {
                            mystring = "000";
                        }

                        if (mystring == "777")
                        {
                            result = true;
                        }
                        
                    };


            try
            {
                client.DownloadStringAsync(new Uri("http://" + myUri.Authority));
            }
            catch
            {
//                LogText("Stopping data monitoring failed:\n\n" + ex.Message);
            };

            return result;
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

            string responseFromServer= "000";

            WebRequest request = WebRequest.Create(uriString);
            request.Method = "POST";

            string postData = "hostname=" + dtTools.MachineName + "&filename=" +  Path.GetFileName(fn) + "&data=" + filetext;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            try
            {
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
            }
            catch
            {

            }
            return responseFromServer;
        }
    }

}
