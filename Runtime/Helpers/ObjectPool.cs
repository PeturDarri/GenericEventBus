using System;
using System.Collections.Generic;

namespace GenericEventBus.Helpers
{
	internal class ObjectPool<T> where T : class, new()
	{
		private readonly Stack<T> _pool;

		public ObjectPool(int capacity = 8, int prewarmCount = 4)
		{
			capacity = Math.Max(4, Math.Max(capacity, prewarmCount));

			_pool = new Stack<T>(capacity);

			for (var i = 0; i < prewarmCount; i++)
			{
				_pool.Push(new T());
			}
		}

		public T Get() => _pool.Count == 0 ? new T() : _pool.Pop();
		public void Release(T obj) => _pool.Push(obj);
	}
}