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
            string oberon_file = debug_directory + DecodeArguments(args);

            RDParser parser = new RDParser(oberon_file);
            parser.Parse();

            // Hold the output until the user wants to end the program.
            Console.Write("Press a key to end... ");
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



