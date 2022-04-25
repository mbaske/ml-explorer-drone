using System.Collections.Generic;

namespace DroneProject
{
    /// <summary>
    /// A very basic generic pool.
    /// </summary>
    /// <typeparam name="T">Type implementing IPoolable</typeparam>
    public class Pool<T> where T : IPoolable, new()
    {
        /// <summary>
        /// Initial pool stack capacity.
        /// We do NOT pre-populate the pool, objects are instantiated on demand.
        /// </summary>
        public static int InitCapacity = 64;
        
        /// <summary>
        /// Singleton accessor and factory.
        /// </summary>
        public static Pool<T> Instance
        {
            get
            {
                s_Instance ??= new Pool<T>();
                return s_Instance;
            }
        }
        
        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static Pool<T> s_Instance;
        
        /// <summary>
        /// Number of pooled items.
        /// </summary>
        public int Count => m_Stack.Count;
        
        /// <summary>
        /// Pooled items stack.
        /// </summary>
        private readonly Stack<T> m_Stack;

        /// <summary>
        /// Constructor is private because we're using
        /// a singleton instance for each pool type.
        /// </summary>
        private Pool()
        {
            m_Stack = new Stack<T>(InitCapacity);
        }

        /// <summary>
        /// Lookup or factory. Retrieves pooled item 
        /// if available, creates a new one otherwise.
        /// </summary>
        /// <returns></returns>
        public T RetrieveItem()
        {
            return m_Stack.Count > 0 ? m_Stack.Pop() : new T();
        }

        /// <summary>
        /// Returns item to the pool.
        /// </summary>
        /// <param name="item"></param>
        public void ReturnItem(T item)
        {
            m_Stack.Push(item);
        }

        /// <summary>
        /// Removes all pooled items.
        /// </summary>
        public void Clear()
        {
            m_Stack.Clear();  
        }
    }
}