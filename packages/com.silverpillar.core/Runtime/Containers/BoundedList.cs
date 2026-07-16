using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public class BoundedList<T>
    {
        private T[] m_Items;

        // Index of the oldest active item.
        private int m_StartIndex;

        private int m_Count;

        public int Count => m_Count;
        public int Capacity => m_Items.Length;

        public BoundedList(int capacity)
        {
            capacity = Mathf.Max(0, capacity);

            m_Items = capacity > 0
                ? new T[capacity]
                : Array.Empty<T>();

            m_StartIndex = 0;
            m_Count = 0;
        }

        public T First()
        {
            if (m_Count == 0)
                throw new InvalidOperationException("BoundedList is empty.");

            return m_Items[m_StartIndex];
        }

        public T Last()
        {
            if (m_Count == 0)
                throw new InvalidOperationException("BoundedList is empty.");

            return m_Items[GetPhysicalIndex(m_Count - 1)];
        }

        public bool TryGetLast(out T item)
        {
            item = default;

            if (m_Count == 0)
                return false;

            item = m_Items[GetPhysicalIndex(m_Count - 1)];
            return !IsNull(item);
        }

        public T At(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return m_Items[GetPhysicalIndex(index)];
        }

        public void Push(T item)
        {
            if (IsNull(item))
                return;

            if (Capacity == 0)
                return;

            if (m_Count < Capacity)
            {
                int insertIndex = GetPhysicalIndex(m_Count);
                m_Items[insertIndex] = item;
                m_Count++;
                return;
            }

            // Buffer is full.
            // Overwrite the oldest item without shifting memory.
            m_Items[m_StartIndex] = item;

            // The next item becomes the new oldest.
            m_StartIndex = (m_StartIndex + 1) % Capacity;
        }

        public bool PushCopyOf(T source, Func<T, T, bool> copyTo)
        {
            if (IsNull(source))
                return false;

            if (copyTo == null)
                throw new ArgumentNullException(nameof(copyTo));

            if (Capacity == 0)
                return false;

            int targetIndex;

            if (m_Count < Capacity)
            {
                targetIndex = GetPhysicalIndex(m_Count);
            }
            else
            {
                // Buffer is full.
                // Reuse the oldest slot without replacing the object reference.
                targetIndex = m_StartIndex;
            }

            T target = m_Items[targetIndex];

            if (IsNull(target))
                throw new InvalidOperationException("BoundedList contains a null reusable slot.");

            bool copied = copyTo(source, target);

            if (!copied)
                return false;

            if (m_Count < Capacity)
            {
                m_Count++;
            }
            else
            {
                // The overwritten slot was the oldest one.
                m_StartIndex = (m_StartIndex + 1) % Capacity;
            }

            return true;
        }

        public T Pop()
        {
            if (m_Count == 0)
                return default;

            int lastIndex = GetPhysicalIndex(m_Count - 1);

            T item = m_Items[lastIndex];

            // Do not clear the slot.
            // This allows the item to stay allocated and reusable.
            m_Count--;

            if (m_Count == 0)
                m_StartIndex = 0;

            return item;
        }

        public bool PopDiscard()
        {
            if (m_Count == 0)
                return false;

            // Do not clear the slot.
            // The object remains allocated and reusable.
            m_Count--;

            if (m_Count == 0)
                m_StartIndex = 0;

            return true;
        }

        public void Fill(Func<T> createItem, bool markAsFull = true)
        {
            if (createItem == null)
                throw new ArgumentNullException(nameof(createItem));

            if (Capacity == 0)
                return;

            for (int i = 0; i < Capacity; i++)
            {
                if (IsNull(m_Items[i]))
                {
                    T newItem = createItem();

                    if (IsNull(newItem))
                        throw new InvalidOperationException("Fill factory returned null.");

                    m_Items[i] = newItem;
                }
            }

            m_StartIndex = 0;
            m_Count = markAsFull ? Capacity : 0;
        }

        public bool ForEachSlot(Func<T, bool> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bool result = true;

            for (int i = 0; i < Capacity; i++)
            {
                T item = m_Items[i];

                if (IsNull(item))
                {
                    result = false;
                    continue;
                }

                result &= action(item);
            }

            return result;
        }

        public void Clear(bool liberateMemory = false)
        {
            if (liberateMemory)
            {
                Array.Clear(m_Items, 0, m_Items.Length);
            }

            m_StartIndex = 0;
            m_Count = 0;
        }

        public void SetCapacity(int newCapacity)
        {
            newCapacity = Mathf.Max(0, newCapacity);

            if (newCapacity == Capacity)
                return;

            if (newCapacity == 0)
            {
                m_Items = Array.Empty<T>();
                m_StartIndex = 0;
                m_Count = 0;
                return;
            }

            T[] newItems = new T[newCapacity];

            int amountToKeep = Mathf.Min(m_Count, newCapacity);
            int firstItemToKeep = m_Count - amountToKeep;

            for (int i = 0; i < amountToKeep; i++)
            {
                int oldLogicalIndex = firstItemToKeep + i;
                int oldPhysicalIndex = GetPhysicalIndex(oldLogicalIndex);

                newItems[i] = m_Items[oldPhysicalIndex];
            }

            m_Items = newItems;
            m_StartIndex = 0;
            m_Count = amountToKeep;
        }

        public void SetCapacity(
            int newCapacity,
            Func<T> createItem,
            Func<T, T, bool> copyTo)
        {
            if (createItem == null)
                throw new ArgumentNullException(nameof(createItem));

            if (copyTo == null)
                throw new ArgumentNullException(nameof(copyTo));

            newCapacity = Mathf.Max(0, newCapacity);

            if (newCapacity == Capacity)
                return;

            T[] oldItems = m_Items;
            int oldCapacity = Capacity;
            int oldStartIndex = m_StartIndex;
            int oldCount = m_Count;

            if (newCapacity == 0)
            {
                m_Items = Array.Empty<T>();
                m_StartIndex = 0;
                m_Count = 0;
                return;
            }

            T[] newItems = new T[newCapacity];

            for (int i = 0; i < newCapacity; i++)
            {
                T newItem = createItem();

                if (IsNull(newItem))
                    throw new InvalidOperationException("Create item factory returned null.");

                newItems[i] = newItem;
            }

            int amountToKeep = Mathf.Min(oldCount, newCapacity);
            int firstItemToKeep = oldCount - amountToKeep;

            for (int i = 0; i < amountToKeep; i++)
            {
                int oldLogicalIndex = firstItemToKeep + i;
                int oldPhysicalIndex = (oldStartIndex + oldLogicalIndex) % oldCapacity;

                T source = oldItems[oldPhysicalIndex];
                T target = newItems[i];

                if (IsNull(source))
                    throw new InvalidOperationException("Old list contained a null active slot.");

                bool copied = copyTo(source, target);

                if (!copied)
                    throw new InvalidOperationException("Could not copy old item data into resized list.");
            }

            m_Items = newItems;
            m_StartIndex = 0;
            m_Count = amountToKeep;
        }

        private bool IsNull(T item)
        {
            return ReferenceEquals(item, null);
        }

        private int GetPhysicalIndex(int logicalIndex)
        {
            return (m_StartIndex + logicalIndex) % Capacity;
        }
    }
}