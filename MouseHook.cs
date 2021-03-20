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
// somewhat based on ideas presented in API documentation and Microsoft examples.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LioranBoardTabletInputStaller
{
    /// <summary>
    /// Hooking Class that hooks to the Windows Event Queue for mouse activity and allows the caller to subscribe to them
    /// 
    /// </summary>
    public class MouseHook
    {
        private Point point;
        private int dwExtraInfo;
        private Point Point
        {
            get { return point; }
            set
            {
                if (point != value)
                {
                    point = value;
                    if (MouseMoveEvent != null)
                    {
                        var e = new APIMouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0, dwExtraInfo);
                        MouseMoveEvent(this, e);
                    }
                }
            }
        }
        private int hHook;

        //Constants from user32.dll so we don't have magic numbers in code
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;

        private const int WH_MOUSE_LL = 14;
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_MOVE = 0x01;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int WM_MOUSEMOVE = 0x0200;

        public Win32Api.HookProc hProc;
        public MouseHook()
        {
            this.Point = new Point();
        }

        /// <summary>
        /// Hook into the windows eventqueue for mouse events
        /// </summary>
        /// <returns></returns>
        public int SetHook()
        {
            hProc = new Win32Api.HookProc(MouseHookProc);
            hHook = Win32Api.SetWindowsHookEx(WH_MOUSE_LL, hProc, IntPtr.Zero, 0);
            return hHook;
        }

        /// <summary>
        /// When stopping, unhook to release resources.
        /// </summary>
        public void UnHook()
        {
            Win32Api.UnhookWindowsHookEx(hHook);
        }

        /// <summary>
        /// This is the code hooked into the windows event queue that receives the messages
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Win32Api.MouseHookStruct MyMouseHookStruct = (Win32Api.MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Api.MouseHookStruct));
            
            // Pass this message up the chain.  If false, eat the message.
            bool sendup = true;
            
            //Debug.WriteLine(string.Format("{0}", wParam));
            if (nCode < 0)
            {
                return Win32Api.CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            else
            {
                // send mouse events to subscribed code.
                // They tell us if we should eat it or pass it along.
                if (MouseClickEvent != null)
                {
                    MouseButtons button = MouseButtons.None;
                    int clickCount = 0;
                    switch ((Int32)wParam)
                    {
                        case WM_LBUTTONDOWN:
                            button = MouseButtons.Left;
                            clickCount = 1;
                            sendup = MouseDownEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                        case WM_RBUTTONDOWN:
                            button = MouseButtons.Right;
                            clickCount = 1;
                            sendup = MouseDownEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                        case WM_MBUTTONDOWN:
                            button = MouseButtons.Middle;
                            clickCount = 1;
                            sendup = MouseDownEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                        case WM_LBUTTONUP:
                            button = MouseButtons.Left;
                            clickCount = 1;
                            sendup = MouseUpEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                        case WM_RBUTTONUP:
                            button = MouseButtons.Right;
                            clickCount = 1;
                            sendup = MouseUpEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                        case WM_MBUTTONUP:
                            button = MouseButtons.Middle;
                            clickCount = 1;
                            sendup = MouseUpEvent(this, new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo));
                            break;
                    }

                    var e = new APIMouseEventArgs(button, clickCount, point.X, point.Y, 0, MyMouseHookStruct.dwExtraInfo);
                    MouseClickEvent(this, e);
                }
                if (sendup)
                {
                    this.Point = new Point(MyMouseHookStruct.pt.x, MyMouseHookStruct.pt.y);
                    this.dwExtraInfo = MyMouseHookStruct.dwExtraInfo;
                    return Win32Api.CallNextHookEx(hHook, nCode, wParam, lParam);
                }
                return 0;
            }
        }

        public delegate void MouseMoveHandler(object sender, APIMouseEventArgs e);
        /// <summary>
        /// Mouse has Moved
        /// </summary>
        public event MouseMoveHandler MouseMoveEvent;

        public delegate void MouseClickHandler(object sender, APIMouseEventArgs e);

        /// <summary>
        /// Mouse has Clicked
        /// </summary>
        public event MouseClickHandler MouseClickEvent;

        public delegate bool MouseDownHandler(object sender, APIMouseEventArgs e);
        /// <summary>
        /// Mouse button down
        /// </summary>
        public event MouseDownHandler MouseDownEvent;

        public delegate bool MouseUpHandler(object sender, APIMouseEventArgs e);
        /// <summary>
        /// Mouse button Up
        /// </summary>
        public event MouseUpHandler MouseUpEvent;

    }
}
