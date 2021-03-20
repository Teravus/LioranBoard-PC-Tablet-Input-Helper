using System.Windows.Forms;

namespace LioranBoardTabletInputStaller
{
    /// <summary>
    /// Subclassing MouseEventArgs so we can see what dwExtraInfo has assigned to it.
    /// </summary>
    public class APIMouseEventArgs : MouseEventArgs
    {
        public int dwExtraInfo { get; set; }
        public APIMouseEventArgs(MouseButtons b, int clicks, int x, int y, int delta, int dw) : base(b, clicks, x, y, delta)
        {
            dwExtraInfo = dw;
        }
    }
}
