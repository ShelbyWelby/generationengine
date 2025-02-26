using System;

namespace MyRpgEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Run as (s)erver or (c)lient?");
            string input = Console.ReadLine().ToLower();
            if (input == "s")
            {
                new Server().Run();
            }
            else
            {
                using (var game = new Game())
                {
                    game.Run();
                }
            }
        }
    }
}