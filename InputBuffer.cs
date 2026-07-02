using System.Text;
public static class InputBuffer
{
    private static StringBuilder _currectInput = new();
    private static readonly object _lock = new object();

    public static string GetCurrentText()
    {
        lock (_lock)
        {
            return _currectInput.ToString();
        }
    }

    public static string ReadLineCustom(string prompt)
    {
        lock (_lock)
        {
            _currectInput.Clear();
        }

        Console.Write(prompt);

        while (true)
        {
            var KeyInfo = Console.ReadKey(true);

            lock (_lock)
            {
                if (KeyInfo.Key == ConsoleKey.Enter)
                {
                    string result = _currectInput.ToString();
                    Console.WriteLine();
                    _currectInput.Clear();
                    return result;
                }

                if (KeyInfo.Key == ConsoleKey.Backspace)
                {
                    if (_currectInput.Length > 0)
                    {
                        _currectInput.Remove(_currectInput.Length -1, 1);

                        Console.Write("\b \b");
                    }
                    continue;
                }

                if (char.IsControl(KeyInfo.KeyChar)) continue;

                _currectInput.Append(KeyInfo.KeyChar);
                Console.Write(KeyInfo.KeyChar);
            }
        }
    }
}