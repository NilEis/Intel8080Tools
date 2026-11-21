namespace Intel8080Tools.Assembler;

public class MemoryBuffer<T>
{
    private readonly List<T> _internalBuffer = [];

    public T this[int index]
    {
        get => _internalBuffer[index];
        set
        {
            if (index < _internalBuffer.Count)
            {
                _internalBuffer[index] = value;
            }
            else
            {
                _internalBuffer.AddRange(Enumerable.Repeat(default(T), 1 + (index - _internalBuffer.Count)).ToArray()!);
                _internalBuffer[index] = value;
            }
        }
    }

    public void AddRange(IEnumerable<T> collection) => _internalBuffer.AddRange(collection);

    public void AddRange(IEnumerable<T> collection, int index)
    {
        var i = 0;
        foreach (var v in collection)
        {
            this[index + i] = v;
            i++;
        }
    }

    public void Add(T v) => _internalBuffer.Add(v);

    public T[] ToArray()
    {
        return _internalBuffer.ToArray();
    }
}