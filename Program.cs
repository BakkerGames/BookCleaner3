using System;
using System.IO;

namespace BookCleaner3
{
    class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Syntax: path");
                return 1;
            }
            int fileCount = 0;
            int changedCount = 0;
            int errorCount = 0;
            foreach (string filename in Directory.GetFiles(args[0], "*.txt"))
            {
                fileCount++;
                Console.Write($"\r{fileCount}");
                try
                {
                if (BookCleaner.FixEbook(filename))
                {
                    Console.Write("\r");
                    Console.WriteLine(filename);
                    changedCount++;
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    errorCount++;
                }
            }
            Console.Write("\r");
            if (changedCount > 0)
            {
                Console.WriteLine();
            }
            Console.WriteLine($"Files found  : {fileCount}");
            Console.WriteLine($"Files changed: {changedCount}");
            Console.WriteLine($"Errors found : {errorCount}");
            Console.WriteLine();
            Console.Write("Press enter to continue...");
            Console.ReadLine();
            return 0;
        }
    }
}
