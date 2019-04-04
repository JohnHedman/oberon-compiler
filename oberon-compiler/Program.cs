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

            //Begin testing for symbol_table
            //SymbolTable symbol_table = new SymbolTable();

            //symbol_table.Insert("John", LexicalAnalyzer.Token.idt, 0);
            //symbol_table.Insert("Bryan", LexicalAnalyzer.Token.idt, 0);
            //symbol_table.Insert("Pam", LexicalAnalyzer.Token.idt, 0);
            //symbol_table.Insert("Jamison", LexicalAnalyzer.Token.idt, 1);
            //symbol_table.Insert("Ande", LexicalAnalyzer.Token.idt, 2);
            //symbol_table.WriteTable(0);

            //TableEntry node = symbol_table.Lookup("John");
            //Stack<TableEntry> stack = new Stack<TableEntry>();
            //stack.Push(node);
            //TableEntry test = stack.Pop();
            //test.lexeme = "Jacob";
            //Console.WriteLine("\nAfter:");
            //symbol_table.WriteTable(0);

            //Console.WriteLine("\nPrint depth = 1:");
            //symbol_table.WriteTable(1);

            //Console.WriteLine("\nPrint depth = 2:");
            //symbol_table.WriteTable(2);

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



