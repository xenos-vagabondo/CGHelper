using System;
using System.Collections.Generic;
using System.Threading;

namespace CGHelper.CG
{
    public class QueueAction
    {
        private static Thread QueueThread { get; set; }

        private static Queue<RoleAction> Queue { get; set; }

        public QueueAction()
        {
            if (QueueThread == null)
            {
                QueueThread = new Thread(() =>
                {
                    while (true)
                    {
                        Action();
                        Common.Delay(100);
                    }
                });
                QueueThread.IsBackground = true;
                QueueThread.Priority = ThreadPriority.Lowest;
                QueueThread.Start();
            }
        }

        private void Action()
        {

        }
    }

    public class RoleAction
    {
        public IntPtr HandleWindow { get; set; }

        public string Action { get; set; }

        public RoleAction(IntPtr handleWindow, string action)
        {
            HandleWindow = handleWindow;
            Action = action;
        }
    }
}
