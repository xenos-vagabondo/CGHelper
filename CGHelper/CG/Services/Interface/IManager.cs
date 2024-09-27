using System;

namespace CGHelper.CG.Services.Interface
{
    internal interface IManager : IDisposable
    {
        void Start();
        void Stop();
    }
}
