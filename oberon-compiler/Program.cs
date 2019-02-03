using System;
using System.Collections.Generic;

namespace oberon_compiler
{
    class Program
    {
        const string debug_directory = "../";


        static void Main(string[] args)
        {
            string oberonFile = debug_directory + DecodeArgs(args);

            string[] oberon_lines = System.IO.File.ReadAllLines(oberonFile);

            for(int x = 0; x < oberon_lines.Length; x++)
            {
                Console.WriteLine(oberon_lines[x]);
            }


            Console.Write("\nPress a key to end... ");
            Console.ReadKey();
        }


        // Read command line arguments and find the oberon file to compile
        static string DecodeArgs(string[] arguments)
        {
            string oberonFile;

            if(arguments.Length == 1)
            {
                oberonFile = arguments[0];
            }
            else
            {
                Console.Write("What file should I read from: ");
                oberonFile = Console.ReadLine();
            }

            Console.WriteLine("Gotcha, reading from: " + oberonFile + "\n");

            return oberonFile;
        }




    }
}



