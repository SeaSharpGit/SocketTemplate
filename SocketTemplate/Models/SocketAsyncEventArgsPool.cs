using System;
using System.Collections.Generic;
using System.Net.Sockets;

public class SocketAsyncEventArgsPool
{
    private Stack<SocketAsyncEventArgs> _Pool;

    public SocketAsyncEventArgsPool(int capacity)
    {
        _Pool = new Stack<SocketAsyncEventArgs>(capacity);
    }

    public void Push(SocketAsyncEventArgs item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
        }
        lock (_Pool)
        {
            _Pool.Push(item);
        }
    }

    public SocketAsyncEventArgs Pop()
    {
        lock (_Pool)
        {
            return _Pool.Pop();
        }
    }

    public int Count => _Pool.Count;

}