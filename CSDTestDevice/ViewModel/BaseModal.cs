using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSDTestDevice.ViewModel
{
    public abstract class DisposableViewModel : IDisposable
    {
        ~DisposableViewModel()
        {
            //Debug.Assert(Disposed, "WARNING: Object finalized without being disposed!");
            Dispose(false);
        }

        #region IDisposable
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                }

                DisposeUnmanagedResources();
                Disposed = true;
            }
        }

        /// <summary>
        /// method for managed objects cleanup and managed objects disposal
        /// </summary>
        protected virtual void DisposeManagedResources() { }

        /// <summary>
        /// method for unmanaged objects cleanup and unmanaged objects disposal
        /// </summary>
        protected virtual void DisposeUnmanagedResources() { }
        #endregion
    }
}
