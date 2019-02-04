using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{
    class Program
    {
        //Global variables for the compiler
        const string debug_directory = "../";

        static void Main(string[] args)
        {
            string oberon_file;
            int token_count = 0;

            char test = '\0';

            Console.WriteLine("Int: " + LexicalAnalyzer.ConvertToASCII(test));

            // Make sure user put Oberon file in command line arguments
            if (args.Length != 1)
            {
                Console.WriteLine("Error: Please put file location as first argument!");
                Environment.Exit(1);
            }

            // Use the oberon file in the argument to build the Lexical Analyzer object.
            oberon_file = debug_directory + args[0];
            LexicalAnalyzer oberon_lex = new LexicalAnalyzer(oberon_file);

            // While we haven't reached the end of the file, keep finding tokens
            while(oberon_lex.token != LexicalAnalyzer.Token.eoft)
            {
                oberon_lex.GetNextToken();
                oberon_lex.DisplayToken();
                token_count++;

                // Make it so the user can read 20 tokens at a time.
                if(token_count % 20 == 0)
                {
                    Console.WriteLine("Press any button to continue...");
                    Console.ReadKey();
                }
            }

            // Hold the output until the user wants to end the program.
            Console.Write("\nPress a key to end... ");
            Console.ReadKey();
        }

        static string[] TakeOutEmptyStrings(string[] string_array)
        {
            return string_array.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

    }
}



