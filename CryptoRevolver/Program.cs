using System;

namespace CryptoRevolver
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Initialize();
            Listener listener = new Listener();
            listener.Start();
        }
    }
}
