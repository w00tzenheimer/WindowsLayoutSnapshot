using System;
using System.Threading;
using System.Windows.Forms;

namespace WindowsLayoutSnapshot
{
    internal static class ExceptionUtils
    {
        public static void Protected(Action action)
        {
            Exception? e = null;

            try
            {
                action();
            }
            catch (Exception ex) when (ex is not ThreadAbortException)
            {
                // StackOverflowException - uncatchable
                // OutOfMemoryException - doesn't matter, it will be re-thrown again soon enough.
                // ThreadAbortException
                e = ex;
            }

            if (e != null)
            {
                string msg = ExceptionToString(e);
                DialogResult res = MessageBox.Show(
                    msg,
                    "Unexpected Exception. Retry to copy in clipboard",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Exclamation);
                if (res == DialogResult.Retry)
                {
                    Clipboard.SetText(msg);
                }
            }
        }

        public static string ExceptionToString(Exception e, int nSkipFrames = 1)
        {
            if (e is null)
            {
                return "";
            }

            ++nSkipFrames; // by default skip this method
            if (nSkipFrames < 0)
            {
                nSkipFrames = 0;
            }

            return e.ToString() +
                Environment.NewLine +
                " --- Caller Stack: " + new System.Diagnostics.StackTrace(nSkipFrames, true);
        }
    }
}
