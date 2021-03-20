using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LioranBoardTabletInputStaller
{
    /// <summary>
    /// A queue processor that blocks the thread while the queue is empty.   
    /// When something is added to the queue, the item is processed and the thread blocks when it is done
    /// Posts mouse events through the windows API.
    /// Separate thread is necessary or this will freeze several programs in the messaging chain including the UI thread, 
    /// Lioranboard, and any app that is listening to the global windows mouse event queue upstream from this one.
    /// </summary>
    public class EventPostingThread
    {


        private Thread EvThread;

        /// <summary>
        /// Processing Queue that contains events to process
        /// </summary>
        private BlockingQueue<posteventdata> eventqueue = new BlockingQueue<posteventdata>();

        /// <summary>
        /// Is the Event posting thread running?
        /// </summary>
        public bool Running { get { return run; } }
        private bool run = false;

        public EventPostingThread()
        {
            EvThread = new Thread(EventThreadStart);
        }
        /// <summary>
        /// Start the event posting thread.
        /// </summary>
        public void Start()
        {
            run = true;
            EvThread.Start();
        }
        private void EventThreadStart(object data)
        {
            
            while (run)
            {
                var posteventdata = eventqueue.Dequeue();
                if (posteventdata.stop)
                {
                    run = false;
                    break;
                }
                postevents(posteventdata.delay);
            }

        }

        /// <summary>
        /// Posts the click
        /// </summary>
        /// <param name="delayp">Delay between mouse messages in Milliseconds</param>
        public void PostLeftClick(int delayp)
        {
            if (!run || eventqueue.Closed)
                return;

            var evt = new posteventdata()
            {
                
                stop = false, 
                delay = delayp
            };
            eventqueue.Enqueue(evt);
        }

        /// <summary>
        /// Stop the event poster thread
        /// </summary>
        public void Stop()
        {
            var evt = new posteventdata()
            {
                stop = true,
                delay = 0
            };
            eventqueue.Enqueue(evt);
        }

        // Mouse constant for Left Mouse Down
        private const int LM_DOWN = 1;
        // Mouse constant for no buttons held down
        private const int NONE = 0;

        /// <summary>
        /// Post the click sequence to the current mouse position using the specified delay in between the events.
        /// </summary>
        /// <param name="delay"></param>
        private void postevents(int delay)
        {
            try
            {
                Debug.WriteLine("I am posting the messages now!");

                // These coordinates are relative, that's why they're 0x and 0y
                Win32Api.mouse_event(MouseHook.MOUSEEVENTF_MOVE, 0, 0, LM_DOWN, 0);
                Thread.Sleep(delay);
                Win32Api.mouse_event(MouseHook.MOUSEEVENTF_LEFTDOWN, 0, 0, LM_DOWN, 0);
                Thread.Sleep(delay);
                Win32Api.mouse_event(MouseHook.MOUSEEVENTF_LEFTUP , 0, 0, NONE, 0);

                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine(error);
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("Cant post events when the thread is disposed.");
            }
        }

        /// <summary>
        /// Class to contain the event queue data that is processed by this thread.
        /// Currenly only 'delay' in milliseconds and whether or not to stop the event queue thread.
        /// </summary>
        public class posteventdata
        {

            public bool stop { get; set; }
            public int delay { get; set; }
        }

    }
}
