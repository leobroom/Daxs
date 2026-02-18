using System.Collections.Generic;

namespace Daxs
{


    public class UniqueQueue<T>
    {
        private readonly HashSet<T> _set = new HashSet<T>();
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _lock = new object();

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                // Add returns false if item already exists → ignore duplicate
                if (_set.Add(item))
                {
                    _queue.Enqueue(item);
                }
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    item = default;
                    return false;
                }

                item = _queue.Dequeue();
                _set.Remove(item);
                return true;
            }
        }

        //public T Dequeue()
        //{
        //    lock (_lock)
        //    {
        //        if (_queue.Count == 0)
        //            return default;

        //        var item = _queue.Dequeue();
        //        _set.Remove(item);
        //        return item;
        //    }
        //}

        public bool HasValues
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count > 0;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }
    }

}
