using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{
    class Program
    {
        //File for where the test files will be located
        const string debug_directory = "../";


        //Function: Main
        //Purpose:  Main function of the program.  Also serves as the driver for the program
        static void Main(string[] args)
        {
            string oberon_file;
            int token_count = 0;

            // Use the oberon file in the argument to build the Lexical Analyzer object.
            oberon_file = debug_directory + DecodeArguments(args);
            LexicalAnalyzer oberon_lex = new LexicalAnalyzer(oberon_file);
            RDParser rd_parser = new RDParser(oberon_lex);

            rd_parser.Parse();

            //PrintTokenHeader();

            //// While we haven't reached the end of the file, keep finding tokens
            //while (oberon_lex.token != LexicalAnalyzer.Token.eoft)
            //{
            //    oberon_lex.GetNextToken();
            //    oberon_lex.DisplayToken();
            //    token_count++;

            //    // Make it so the user can read 20 tokens at a time.
            //    if(token_count % 25 == 0)
            //    {
            //        Console.Write("\nPress any button to continue...");
            //        Console.ReadKey();
            //        Console.Clear();
            //        PrintTokenHeader();
            //    }
            //}

            // Hold the output until the user wants to end the program.
            Console.Write("\nPress a key to end... ");
            Console.ReadKey();
        }

        //Function: PrintTokenHeader
        //Purpose:  Print the header for the program to label the output.
        static void PrintTokenHeader()
        {
            Console.WriteLine("Input                                              Token       Lexeme            Attribute");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------");
        }

        //Function: DecodeArguments
        //Purpose:  Decodes the arguments to find the Oberon file to compile
        static string DecodeArguments(string[] args)
        {
            // Make sure user put Oberon file in command line arguments
            if (args.Length != 1)
            {
                Console.WriteLine("Error: Please put file location as first argument!");
                Console.Write("\nPress a key to end... ");
                Console.ReadKey();
                Environment.Exit(1);
            }

            return args[0];
        }

    }
}



