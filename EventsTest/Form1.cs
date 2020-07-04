using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using System.IO;


namespace EventsTest
{
    public partial class Form1 : Form
    {
        ManualResetEvent mre;
        Thread t;
        Queue myQ;
        // Creates a synchronized wrapper around the Queue.
        Queue mySyncdQ;
        public Form1()
        {
            
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Creates and initializes a new Queue.
            myQ = new Queue();
            // Creates a synchronized wrapper around the Queue.
            mySyncdQ = Queue.Synchronized(myQ);
            rtb1.AppendText(String.Format("myQ is {0}.\n", myQ.IsSynchronized ? "synchronized" : "not synchronized"));
            // Displays the sychronization status of both Queues.
            rtb1.AppendText(String.Format("mySyncdQ is {0}.\n", mySyncdQ.IsSynchronized ? "synchronized" : "not synchronized"));
            // Supply the state information required by the task.
            ThreadWithState tws = new ThreadWithState(mySyncdQ);

            // Create a thread to execute the task, and then
            // start the thread.
            t = new Thread(new ThreadStart(tws.ThreadProc));
            t.Start();

            mre = new ManualResetEvent(false);
            timer1.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, System.EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                mre.Reset(); 
            }
            else
            {
                mre.Set();
            }
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            checkBox2.Checked = mre.WaitOne(0);
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
              //t.Join();
              rtb1.AppendText("append\n");
              mySyncdQ.Enqueue("The");
              mySyncdQ.Enqueue("quick");
              mySyncdQ.Enqueue("brown");
              mySyncdQ.Enqueue("fox");
              //MessageBox.Show("123");

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            t.Abort("Information from Main.");

            // Wait for the thread to terminate.
            t.Join();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int intValue = 100500;
            string hexValue = intValue.ToString("X8");
            int myNewInt = Convert.ToInt32(hexValue, 16);
            MessageBox.Show(hexValue+" / "+ myNewInt.ToString());
        }
    }

    #region Threading
    #region tools
    public class Tools
    {
        public static string GetNowString()
        {
            string DateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff ");
            return DateTime;
        }


        public static string GetTimeDateFile()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm").Substring(0, 15);
        }

    #endregion
    }
    public class ThreadWithState
    {
        // State information used in the task.
        private Queue myQ;

        // The constructor obtains the state information.
        public ThreadWithState(Queue myQx)
        {
            myQ = myQx;
        }

        // The thread procedure performs the task, such as formatting
        // and printing a document.
        public void ThreadProc()
        {

            while (true)
            {

                var zzz = myQ.Count;
                using (StreamWriter w = File.AppendText(Tools.GetTimeDateFile() + ".txt"))
                {
                    while (zzz > 0)
                    {
                        w.WriteLine(Tools.GetNowString() + myQ.Dequeue().ToString());
                        zzz = myQ.Count;
                    }
                }
                Thread.Sleep(1);
            }
        }
    }

    #endregion

    
    #region callbacker


// The ThreadWithState class contains the information needed for
// a task, the method that executes the task, and a delegate
// to call when the task is complete.
//
public class ThreadWithState2
{
    // State information used in the task.
    private string boilerplate;
    private int numberValue;

    // Delegate used to execute the callback method when the
    // task is complete.
    private ExampleCallback callback;

    // The constructor obtains the state information and the
    // callback delegate.
    public ThreadWithState2(string text, int number,
        ExampleCallback callbackDelegate)
    {
        boilerplate = text;
        numberValue = number;
        callback = callbackDelegate;
    }

    // The thread procedure performs the task, such as
    // formatting and printing a document, and then invokes
    // the callback delegate with the number of lines printed.
    public void ThreadProc()
    {
        Console.WriteLine(boilerplate, numberValue);
        if (callback != null)
            callback(1);
    }
}

// Delegate that defines the signature for the callback method.
//
public delegate void ExampleCallback(int lineCount);

// Entry point for the example.
//
public class Example
{
    public Example()
    {
        // Supply the state information required by the task.
        ThreadWithState2 tws = new ThreadWithState2(
            "This report displays the number {0}.",
            42,
            new ExampleCallback(ResultCallback)
        );

        Thread t = new Thread(new ThreadStart(tws.ThreadProc));
        t.Start();
        Console.WriteLine("Main thread does some work, then waits.");
        t.Join();
        Console.WriteLine(
            "Independent task has completed; main thread ends.");
    }

    // The callback method must match the signature of the
    // callback delegate.
    //
    public static void ResultCallback(int lineCount)
    {
        Console.WriteLine(
            "Independent task printed {0} lines.", lineCount);
    }
}
// The example displays the following output:
//       Main thread does some work, then waits.
//       This report displays the number 42.
//       Independent task printed 1 lines.
//       Independent task has completed; main thread ends.
#endregion
}
