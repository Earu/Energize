using System;
using System.Collections.Generic;

namespace Victoria.Queue
{
    /// <summary>
    /// Queue based on <see cref="LinkedList{T}" />. Follows FIFO.
    /// </summary>
    /// <typeparam name="T">
    /// <see cref="IQueueObject" />
    /// </typeparam>
    public sealed class LavaQueue<T> where T : IQueueObject
    {
        private readonly LinkedList<T> _linked;
        private readonly Random _random;
        private readonly object _lockObj;

        /// <inheritdoc cref="LavaQueue{T}" />
        public LavaQueue()
        {
            this._random = new Random();
            this._linked = new LinkedList<T>();
            this._lockObj = new object();
        }

        /// <summary>
        /// Returns the total count of items.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this._lockObj)
                {
                    return this._linked.Count;
                }
            }
        }

        /// <inheritdoc cref="IEnumerable{T}" />
        public IEnumerable<T> Items
        {
            get
            {
                lock (this._lockObj)
                {
                    for (var node = this._linked.First; node != null; node = node.Next)
                        yield return node.Value;
                }
            }
        }

        /// <summary>
        /// Adds an object.
        /// </summary>
        /// <param name="value">
        /// <see cref="IQueueObject" />
        /// </param>
        public void Enqueue(T value)
        {
            lock (this._lockObj)
            {
                this._linked.AddLast(value);
            }
        }

        /// <summary>
        /// Removes the first item from queue.
        /// </summary>
        /// <returns>
        /// <see cref="IQueueObject" />
        /// </returns>
        public T Dequeue()
        {
            lock (this._lockObj)
            {
                var result = this._linked.First.Value;
                this._linked.RemoveFirst();
                return result;
            }
        }

        /// <summary>
        /// Safely removes the first item from queue.
        /// </summary>
        /// <param name="value">
        /// <see cref="IQueueObject" />
        /// </param>
        /// <returns><see cref="bool" /> based on if dequeue-ing was successful.</returns>
        public bool TryDequeue(out T value)
        {
            lock (this._lockObj)
            {
                if (this._linked.Count < 1)
                {
                    value = default;
                    return false;
                }

                var result = this._linked.First.Value;
                if (result == null)
                {
                    value = default;
                    return false;
                }

                this._linked.RemoveFirst();
                value = result;
                return true;
            }
        }

        /// <summary>
        /// Sneaky peaky the first time in list.
        /// </summary>
        /// <returns>
        /// <see cref="IQueueObject" />
        /// </returns>
        public T Peek()
        {
            lock (this._lockObj)
            {
                return this._linked.First.Value;
            }
        }

        /// <summary>
        /// Removes an item from queue.
        /// </summary>
        /// <param name="value">
        /// <see cref="IQueueObject" />
        /// </param>
        public void Remove(T value)
        {
            lock (this._lockObj)
            {
                this._linked.Remove(value);
            }
        }

        /// <summary>
        /// Clears the queue.
        /// </summary>
        public void Clear()
        {
            lock (this._lockObj)
            {
                this._linked.Clear();
            }
        }

        /// <summary>
        /// Shuffles the queue.
        /// </summary>
        public void Shuffle()
        {
            lock (this._lockObj)
            {
                if (this._linked.Count < 2)
                    return;

                var shadow = new T[this._linked.Count];
                var i = 0;
                for (var node = this._linked.First; !(node is null); node = node.Next)
                {
                    var j = this._random.Next(i + 1);
                    if (i != j)
                        shadow[i] = shadow[j];
                    shadow[j] = node.Value;
                    i++;
                }

                this._linked.Clear();
                foreach (var value in shadow)
                    this._linked.AddLast(value);
            }
        }

        /// <summary>
        /// Removes an item based on the given index.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// <returns>
        /// <see cref="IQueueObject" />
        /// </returns>
        public T RemoveAt(int index)
        {
            lock (this._lockObj)
            {
                var currentNode = this._linked.First;

                for (var i = 0; i <= index && currentNode != null; i++)
                {
                    if (i != index)
                    {
                        currentNode = currentNode.Next;
                        continue;
                    }

                    this._linked.Remove(currentNode);
                    break;
                }

                return currentNode.Value;
            }
        }

        /// <summary>
        /// Removes a item from given range.
        /// </summary>
        /// <param name="from">Start index.</param>
        /// <param name="to">End index.</param>
        public void RemoveRange(int from, int to)
        {
            lock (this._lockObj)
            {
                var currentNode = this._linked.First;
                for (var i = 0; i <= to && currentNode != null; i++)
                {
                    if (from <= i)
                    {
                        this._linked.Remove(currentNode);
                        currentNode = currentNode.Next;
                        continue;
                    }

                    this._linked.Remove(currentNode);
                }
            }
        }
    }
}