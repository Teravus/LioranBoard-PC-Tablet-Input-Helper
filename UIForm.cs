/*
 * Copyright(c) 2021 teravus@gmail.com
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the teravus@gmail.com nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LioranBoardTabletInputStaller
{
    public partial class UIForm : Form
    {
        /// <summary>
        /// Handle for the Lioranboard window.  We use this to track the position of the window on the screen.
        /// </summary>
        private IntPtr LioranBoardWindowHandle = IntPtr.Zero;

        /// <summary>
        /// Hashset of click data.  We record clicks and releases in here so that when we send global clicks, we don't trigger our own click handlers
        /// We also store a touch square in here so touch release messages that are slightly different than the press message position still show as a touch.
        /// The appropriate square size will depend on the touch screen's accuracy.   Just about all are capable of an accuracy of at least a box of 16 pixels 
        /// surrounding the touch point.   Many are more accurate.
        /// </summary>
        private static HashSet<string> ignoreSet = new HashSet<string>();
        private static HashSet<string> padSet = new HashSet<string>();
        private EventPostingThread evtthd = null;
        public UIForm()
        {
            InitializeComponent();
        }

        MouseHook mh;

        private void Form1_Load(object sender, EventArgs e)
        {
            mh = new MouseHook();
            mh.SetHook();
            mh.MouseMoveEvent += mh_MouseMoveEvent;
            mh.MouseClickEvent += mh_MouseClickEvent;
            mh.MouseDownEvent += mh_MouseDownEvent;
            mh.MouseUpEvent += mh_MouseUpEvent;
            //evtthd = new EventPostingThread();

        }

        private bool mh_MouseDownEvent(object sender, APIMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {

                int padding = (int)(GetTouchAccuracy() * 0.5f);

                Win32Api.Rect lbpos = GetLioranBoardWindowLocation();

                if (lbpos.Top == 0 && lbpos.Bottom == 0 && lbpos.Left == 0 && lbpos.Right == 0)
                {
                    // The get Rectangle failed.  Likely because lioranboard is closed.
                    LioranBoardWindowHandle = IntPtr.Zero;
                }

                var outputX = e.X;
                var outputY = e.Y;


                string hashstr = string.Format("LD:({0},{1})", outputX, outputY);
                Debug.WriteLine(string.Format("Checking for Ignore Hash {0}", hashstr));
                if (!ignoreSet.Contains(hashstr))
                {
                    Debug.WriteLine(string.Format("Checking for Ignore Hash {0}", hashstr));
                    // We need location and placement because location returns the actual position (Aero snap included),
                    // GetWindowPlacement returns the minimized/maximized state but often returns the non-snapped dimensions

                    var placement = GetLBWindowPlacement();

                    if (LioranBoardWindowHandle != IntPtr.Zero && (e.X >= lbpos.Left && e.X < lbpos.Right) && (e.Y >= lbpos.Top && e.Y < lbpos.Bottom)
                        && (placement.showCmd == Win32Api.ShowWindowCommands.Normal || placement.showCmd == Win32Api.ShowWindowCommands.Maximized))
                    {
                        
                        ignoreSet.Add(hashstr);
                        Debug.WriteLine(string.Format("Added Ignore Hash {0}", hashstr));
                        // We are drawing a square in a hashtable the size of the padding around the touch point.
                        // We need this to dedupe our touch..   since we will also receive our own mouse click message. 
                        // We also need this to disambiguate our touch from a drag.
                        for (int x = (-1 * padding); x < padding; x++)
                        {
                            for (int y = (-1 * padding); y < padding; y++)
                            {
                                
                                string subhashstr = string.Format("LD:({0},{1})", outputX + x, outputY + y);
                                padSet.Add(subhashstr);
                                ignoreSet.Add(subhashstr);

                            }
                        }
                        richTextBox1.Text = string.Format("Setting release pad square:{0},{1}", e.X, e.Y);
                        return false;
                    }
                }
            }
            if (e.Button == MouseButtons.Right)
            {
                //richTextBox1.AppendText("Right Button Press\n");
            }
            return true;
        }

        private bool mh_MouseUpEvent(object sender, APIMouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {


                var outputX = e.X;//pointGlobal.x;
                var outputY = e.Y;//pointGlobal.y;

                string hashdown = string.Format("LD:({0},{1})", outputX, outputY);
                string hashup = string.Format("LU:({0},{1})", outputX, outputY);
                richTextBox1.Text = string.Format("({0},{1})Left Button Release\n", outputX, outputY);
                Debug.WriteLine(string.Format("({0},{1})Left Button Release\n", outputX, outputY));

                // Don't trigger ourselves with our own generated mouse messages.
                Debug.WriteLine(string.Format("Checking for Ignore Hash {0}", hashup));
                if (!ignoreSet.Contains(hashup))
                {
                    Debug.WriteLine(string.Format("Ignore Hash {0} Not found", hashup));
                    ignoreSet.Add(hashup);
                    int padding = (int)(GetTouchAccuracy() * 0.5f);
                    // We are drawing a square in a hashtable the size of the padding around the touch point.
                    // We need this to dedupe our touch..   since we will also receive our own mouse click message. 
                    // We also need this to disambiguate our touch from a drag.
                    for (int x = (-1 * padding); x < padding; x++)
                    {
                        for (int y = (-1 * padding); y < padding; y++)
                        {
                            string subhashstr = string.Format("LU:({0},{1})", outputX + x, outputY + y);
                            ignoreSet.Add(subhashstr);

                        }
                    }
                    // Only trigger a click if the touch release position is within the padding square
                    if (padSet.Contains(hashdown))
                    {
                        Win32Api.Rect lbpos = GetLioranBoardWindowLocation();
                        if (lbpos.Top == 0 && lbpos.Bottom == 0 && lbpos.Left == 0 && lbpos.Right == 0)
                        {
                            // The get Rectangle failed.  Likely because lioranboard is closed.
                            LioranBoardWindowHandle = IntPtr.Zero;
                        }
                        var placement = GetLBWindowPlacement();

                        // Filter clicks and releases to only over-top of the Lioranboard window.
                        if ((e.X >= lbpos.Left && e.X < lbpos.Right) && (e.Y >= lbpos.Top && e.Y < lbpos.Bottom)
                            && (placement.showCmd == Win32Api.ShowWindowCommands.Normal || placement.showCmd == Win32Api.ShowWindowCommands.Maximized) && evtthd != null && evtthd.Running)
                        {

                            richTextBox1.Text = string.Format("Triggering Delayed Click: {0},{1}", e.X, e.Y);

                            
                            evtthd.PostLeftClick(GetDelayMS());

                            return false;
                        }


                    }
                }
                else
                {
                    Debug.WriteLine(string.Format("Removing Ignore Hash {0}", hashup));
                    ignoreSet.Remove(hashup);
                }
            }
            if (e.Button == MouseButtons.Right)
            {
               // richTextBox1.Text = "Right Button Release\n";
            }
            return true;

        }
        private void mh_MouseClickEvent(object sender, APIMouseEventArgs e)
        {
            
            if (e.Button == MouseButtons.Left)
            {
                string sText = "(" + e.X.ToString() + "," + e.Y.ToString() + ")";
                label1.Text = sText;
            }
            
        }

        private void mh_MouseMoveEvent(object sender, APIMouseEventArgs e)
        {
            int x = e.Location.X;
            int y = e.Location.Y;
            textBox1.Text = x + "";
            textBox2.Text = y + "";
        }
        /// <summary>
        /// Event cleanup.   Unhook from the windows API.   Unhook our mouse events.  Stop our event posting thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            mh.UnHook();
            mh.MouseMoveEvent -= mh_MouseMoveEvent;
            mh.MouseClickEvent -= mh_MouseClickEvent;
            mh.MouseDownEvent -= mh_MouseDownEvent;
            mh.MouseUpEvent -= mh_MouseUpEvent;

            // already stopped.
            if (evtthd == null)
                return;
            if (!evtthd.Running)
                return;
            evtthd.Stop();
        }

        /// <summary>
        /// Button handler for when the user wants us to search for the LioranBoard window on screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetLioranBoardStreamDeckWindow_Click(object sender, EventArgs e)
        {
            IntPtr hWnd = IntPtr.Zero;
            var processList = Process.GetProcesses();
            
            // Look for LioranBoard 2
            foreach (Process pList in processList)
            {
                if (pList.MainWindowTitle.Contains("Lioranboard 2 Official Streamdeck"))
                {
                    hWnd = pList.MainWindowHandle;
                }

            }
            // If we found Lioranboard 2.    Go on.    Otherwise Look for LioranBoard 1
            foreach (Process pList in processList)
            {
                if (hWnd != IntPtr.Zero)
                {
                    break;
                }
                if (pList.MainWindowTitle.Contains("LioranBoard Stream Deck"))
                {
                    hWnd = pList.MainWindowHandle;
                }
                
            }
            LioranBoardWindowHandle = hWnd;
            if (LioranBoardWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("Lioranboard Window not found.  Please be sure it is open");
                pnl_WindowTracking.BackColor = Color.Red;
                return;
            }
            pnl_WindowTracking.BackColor = Color.Green;
        }
        /// <summary>
        /// Uses the Windows API to get the window positions in desktop space based on the position of the primary monitor as zero.
        /// We use this for filtering on clicks only over the Lioranboard window.
        /// </summary>
        /// <returns></returns>
        private Win32Api.Rect GetLioranBoardWindowLocation()
        {
            Win32Api.Rect rect = new Win32Api.Rect();

            if (!(LioranBoardWindowHandle == IntPtr.Zero))
            {
                Win32Api.GetWindowRect(LioranBoardWindowHandle, ref rect);
            }
            
            return rect;
        }

        /// <summary>
        /// Uses the windows API to get the minimized/Maximized status of the Lioranboard window.  
        /// We use this for filtering so we click only on the lioranboard window
        /// </summary>
        /// <returns></returns>
        private Win32Api.WINDOWPLACEMENT GetLBWindowPlacement()
        {
            Win32Api.WINDOWPLACEMENT placement = new Win32Api.WINDOWPLACEMENT();
            if (!(LioranBoardWindowHandle == IntPtr.Zero))
            {
                placement.length = Marshal.SizeOf(placement);
                Win32Api.GetWindowPlacement(LioranBoardWindowHandle, ref placement);
            }
            return placement;
        }

        /// <summary>
        /// Maintenance timer triggers this
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrCheckLioranBoardWindowHandle_Tick(object sender, EventArgs e)
        {
            // Notify the user when things are not active
            // Window handle isn't valid anymore so we can't track the window position.   Likely it is closed.
            if (pnl_WindowTracking.BackColor == Color.Green && LioranBoardWindowHandle == IntPtr.Zero)
            {
                pnl_WindowTracking.BackColor = Color.Red;
                MessageBox.Show("We lost communication with Lioranboard's window.  Complete Step 1 again.");
            }

            // We don't have an event posting thread.  Likely this program is disabled currently.
            if (evtthd == null)
            {
                pnl_delay_enabled.BackColor = Color.Red;
            } // Event posting thread is disabled.  Likely this program is disabled.
            else if (evtthd != null && !evtthd.Running)
            {
                pnl_delay_enabled.BackColor = Color.Red;
            }

            // House Cleaning
            ignoreSet.Clear();  // For memory (we don't always remove the touch square if the user drags outside of the padding before release)
        }

        /// <summary>
        /// Returns the Touch padding/Accuracy set in the UI after some validation and munging.
        /// </summary>
        /// <returns></returns>
        private int GetTouchAccuracy()
        {
            int touchpadding = 4;


            int.TryParse(tbTouchAccuracy.Text, out touchpadding);

            // negative touch padding is invalid.
            if (touchpadding < 0)
            {
                touchpadding = 0;
                tbTouchAccuracy.Text = "0";
            }

            // needs to be divisible by 2
            if (touchpadding % 2 != 0)
            {
                touchpadding += 1;
                tbTouchAccuracy.Text = touchpadding.ToString();
            }
            return touchpadding;
        }

        /// <summary>
        /// Gets the thread sleep delay between the mouse events Move (sleep), Left Down (sleep), Left Up from the UI.
        /// </summary>
        /// <returns></returns>
        private int GetDelayMS()
        {
            int delayms = 100;
            int.TryParse(tbMouseClickDelay.Text, out delayms);
            // Less than zero is invalid
            if (delayms < 0)
            {
                delayms = 1;
                tbMouseClickDelay.Text = "1";
            }
            // anything over 350ms isn't really responsive.
            if (delayms > 350)
            {
                delayms = 350;
                tbMouseClickDelay.Text = "350";
            }
            return delayms;
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            if (evtthd != null && evtthd.Running)
                return; // Already started and running
            
            // start our event posting thread.
            // These have to be in a different thread or they cause Lioranboard,
            // this program, and any other programs hooked into the global mouse
            // message queue to freeze.

            evtthd = new EventPostingThread();
            evtthd.Start();
            pnl_delay_enabled.BackColor = Color.Green;
        }

        private void btnDeactivate_Click(object sender, EventArgs e)
        {
            // already stopped.
            if (evtthd == null)
                return;
            if (!evtthd.Running)
                return;

            evtthd.Stop();
            pnl_delay_enabled.BackColor = Color.Red;
        }
    }

    

   
    
    
}
