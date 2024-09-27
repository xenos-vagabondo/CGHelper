using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace CGHelper.CG
{
    public static class UsingGameWindow
    {
        public static ArrayList GameWindows { get; set; } = new ArrayList();

        private static CancellationTokenSource CTS { get; set; }
        private static Task WorkTask { get; set; }
    }
}
