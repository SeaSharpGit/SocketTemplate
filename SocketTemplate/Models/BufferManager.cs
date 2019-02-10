//暴露出去的方法并不是线程安全的
//只在初始化的时候进行设置就可以忽略线程安全问题
using System.Collections.Generic;
using System.Net.Sockets;

public class BufferManager
{
    private readonly int _TotalBytes;
    private readonly int _BufferSize = 0;
    private readonly byte[] _Buffer = null;
    private Stack<int> _FreeIndexPool = new Stack<int>();
    private int m_currentIndex = 0;

    public BufferManager(int totalBytes, int bufferSize)
    {
        _TotalBytes = totalBytes;
        _Buffer = new byte[_TotalBytes];
        _BufferSize = bufferSize;
    }

    // Assigns a buffer from the buffer pool to the 
    // specified SocketAsyncEventArgs object
    //
    // <returns>true if the buffer was successfully set, else false</returns>
    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (_FreeIndexPool.Count > 0)
        {
            args.SetBuffer(_Buffer, _FreeIndexPool.Pop(), _BufferSize);
        }
        else
        {
            if ((_TotalBytes - _BufferSize) < m_currentIndex)
            {
                return false;
            }
            args.SetBuffer(_Buffer, m_currentIndex, _BufferSize);
            m_currentIndex += _BufferSize;
        }
        return true;
    }
    
    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        _FreeIndexPool.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }

}