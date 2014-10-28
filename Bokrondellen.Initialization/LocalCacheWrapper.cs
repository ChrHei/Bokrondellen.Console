using EPiServer.Framework.Cache;

namespace Bokrondellen.Initialization
{
    internal class LocalCacheWrapper : HttpRuntimeCache, ISynchronizedObjectInstanceCache
    {
        public IObjectInstanceCache ObjectInstanceCache
        {
            get { return this; }
        }

        public void RemoveLocal(string key)
        {
            Remove(key);
        }

        public void RemoveRemote(string key)
        {
        }

        public FailureRecoveryAction SynchronizationFailedStrategy
        {
            get;
            set;
        }
    }
}
