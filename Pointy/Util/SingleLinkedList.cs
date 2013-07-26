using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pointy.Util
{
    /// <summary>
    /// I can't believe I had to write this, but since .NET's LinkedList doesn't
    /// allow modifying through nodes... here we are.
    /// 
    /// Flavor:
    ///  * This is singly linked
    ///  * Elements are prepended for performance (maintain a pointer to the end?  pfft)
    /// </summary>
    class SingleLinkedList<T> : ICollection<T>
    {
        public SingleLinkedListNode<T> First
        {
            get;
            set;
        }

        #region ICollection

        public void Add(T item)
        {
            First = new SingleLinkedListNode<T>(item, First);
        }
        public void Clear()
        {
            First = null;
        }
        public bool Contains(T item)
        {
            foreach (T x in this)
                if (Comparer<T>.Default.Compare(x, item) == 0)
                    return true;
            return false;
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            var i = 0;
            foreach (T x in this)
            {
                if (array.Length <= arrayIndex + i)
                    throw new ArgumentException();
                array[arrayIndex + i++] = x;
            }
        }
        public int Count
        {
            get
            {
                int c = 0;
                for (var l = First; l != null; l = l.Next)
                    c++;
                return c;
            }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public bool Remove(T item)
        {
            SingleLinkedListNode<T> previous = null;
            for (var node = First; node != null; node = node.Next)
            {
                if (Comparer<T>.Default.Compare(node.Value, item) == 0)
                {
                    // If it's the first node, change the First pointer
                    if (previous == null)
                        First = node.Next;
                    // Otherwise just cut the node out of the list
                    else
                        previous.Next = node.Next;

                    // No need to keep iterating
                    return true;
                }

                // Since this isn't a doubly linked list, we need
                // to keep track of the previous node manually.
                previous = node;
            }

            return false;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        class Enumerator : System.Collections.IEnumerator, IEnumerator<T>
        {
            SingleLinkedListNode<T> Node;
            SingleLinkedListNode<T> Start;

            public Enumerator(SingleLinkedList<T> list)
            {
                Node = list.First;
                Start = Node;
            }

            T IEnumerator<T>.Current
            {
                get { return Node == null ? default(T) : Node.Value; }
            }
            void IDisposable.Dispose()
            {
                Node = null;
            }
            object System.Collections.IEnumerator.Current
            {
                get { return this.Node.Value; }
            }
            bool System.Collections.IEnumerator.MoveNext()
            {
                if (Node == null)
                    return false;

                Node = Node.Next;
                return Node == null;
            }
            void System.Collections.IEnumerator.Reset()
            {
                Node = Start;
            }
        }
    }

    class SingleLinkedListNode<T>
    {
        public T Value
        {
            get;
            set;
        }
        public SingleLinkedListNode<T> Next
        {
            get;
            set;
        }

        public SingleLinkedListNode(T value) : this(value, null)
        {
            
        }
        public SingleLinkedListNode(T value, SingleLinkedListNode<T> next)
        {
            Value = value;
            Next = next;
        }
    }
}
