namespace DroneProject
{
    /// <summary>
    /// Interface for poolable items.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Clears item data and returns item back to the pool.
        /// </summary>
        void Recycle();
    }
}