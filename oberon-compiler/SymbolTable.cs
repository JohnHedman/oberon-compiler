using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/*
 * Name: Jonathan Hedman
 * Assignment 4 
 * 03/22/2019
 */
namespace oberon_compiler
{
    class SymbolTable
    {
        // Public Variable
        public const int TableSize = 211;


        // Private Variables
        private LinkedList<TableEntry>[] hash_table;

        // Constructors 
        public SymbolTable()
        {
            hash_table = new LinkedList<TableEntry>[TableSize];  // Create array of linked list for hash table.
            for (int i = 0; i < TableSize; i++)                  // Set the linked lists to empty in the array.  
            {
                hash_table[i] = new LinkedList<TableEntry>();
            }
        }


        // Methods
        public void Insert(string lexeme, LexicalAnalyzer.Token token, int depth, LexicalAnalyzer lex_analyzer)
        {
            // Create the entry/symbol to put inside the symbol table.
            TableEntry symbol = new TableEntry(lexeme, token, depth);

            // Now, insert the symbol inside the table.
            uint index = hash(lexeme);

            TableEntry node = this.Lookup(lexeme);

            if(node != null)
            {
                if(node.symbol_depth == depth)
                {
                    Console.WriteLine("Error at {0},{1}: The identifier '{2}' has already been declared in the current scope!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1,  lexeme);

                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }

            hash_table[index].AddFirst(symbol);                       // Going to the last element in the list waists time, just add to front.
        }


        public TableEntry Lookup(string lexeme)
        {
            // Find the index the lexeme should be in
            uint index = hash(lexeme);

            ref LinkedList<TableEntry> list =  ref hash_table[index];

            if(list != null)
            {
                LinkedListNode<TableEntry> node = list.First;
                while(node != null)
                {
                    if(node.Value.GetLexeme() == lexeme)
                    {
                        return node.Value;
                    }
                    else
                    {
                        node = node.Next;
                    }
                }
            }

            // If we couldn't find the lexeme in the hash table, then return null.
            return null;
        }

        public TableEntry[] LookupDepth(int depth)
        {
            List<TableEntry> entry_list = new List<TableEntry>();

            for(uint x = 0; x < TableSize; x++)
            {
                List<TableEntry> entries = LookupDepthInList(depth, x);
                foreach (TableEntry entry in entries)
                {
                    entry_list.Add(entry);
                }
            }

            return entry_list.ToArray();
        }

        private List<TableEntry> LookupDepthInList(int depth, uint hash_index)
        {
            List<TableEntry> entry_list = new List<TableEntry>();
            LinkedList<TableEntry> list = hash_table[hash_index];

            if(list != null)
            {
                LinkedListNode<TableEntry> node = list.First;

                while(node != null)
                {
                    if(node.Value.GetSymbolDepth() == depth)
                    {
                        entry_list.Add(node.Value);
                    }

                    node = node.Next;
                }
            }

            return entry_list;
        }


        public void DeleteDepth(int depth)
        {
            // Interate through the symbol table and grab each linked list in the table.
            foreach (LinkedList<TableEntry> list in hash_table)
            {
                // Go through each node in the list
                LinkedListNode<TableEntry> node = list.First;
                while(node != null)
                {
                    // If the node has the specified depth, let's delete it.
                    if(node.Value.GetSymbolDepth() == depth)
                    {
                        // Hold onto next node in the list and delete the node with the specified depth.
                        LinkedListNode<TableEntry> temp = node.Next;
                        list.Remove(node);
                        node = temp;
                    }
                    // If the node is not specified depth, let's move onto the next node;
                    else
                    {
                        node = node.Next;
                    }
                }
            }
        }

        public void WriteTable(int depth)
        {
            PrintEntryHeader();

            // Interate through the symbol table and grab each linked list so we can print values of each node.
            foreach (LinkedList<TableEntry> list in hash_table)
            {
                // Go through each node in the list and print information on the node.
                LinkedListNode<TableEntry> node = list.First;
                while(node != null)
                {
                    // If the entry has the depth, then print it.
                    if(node.Value.GetSymbolDepth() == depth)
                    {
                        TableEntry entry = node.Value;
                        PrintEntryInformation(entry);
                    }
                    node = node.Next;
                }
            }

            Console.WriteLine("\nPress any continue to continue...");
            Console.ReadKey();

        }

        public void PrintEntryHeader()
        {
            Console.WriteLine("Symbol            Token     Type      Depth    Entry Information");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------");
        }

        public void PrintEntryInformation(TableEntry entry)
        {
            string entryInformation;

            switch (entry.type_of_entry)
            {
                case TableEntry.EntryType.constEntry:
                    TableEntry.Constant const_info = entry.entry_information.constant;
                    entryInformation = String.Format("Offset : {0, -5} Type : {1, -10} Value : {2, -10}", const_info.offset, varToString(const_info.type_of_constant), const_info.type_of_constant == TableEntry.VarType.intType ? const_info.value.value : const_info.value.valueR);
                    Console.WriteLine("{0, -17} {1, -9} {2, -9} {3, -8} {4}", entry.lexeme, entry.token, "Constant", entry.symbol_depth, entryInformation);
                    break;
                case TableEntry.EntryType.varEntry:
                    TableEntry.Variable var_info = entry.entry_information.variable;
                    if(var_info.is_parameter == true)
                    {
                        entryInformation = String.Format("Offset : {0, -5} Type : {1, -10} Size : {2, -5} Param: True", var_info.offset, varToString(var_info.type_of_variable), var_info.size);
                    }
                    else
                    {
                        entryInformation = String.Format("Offset : {0, -5} Type : {1, -10} Size : {2, -5} Param: False", var_info.offset, varToString(var_info.type_of_variable), var_info.size);
                    }
                    Console.WriteLine("{0, -17} {1, -9} {2, -9} {3, -8} {4}", entry.lexeme, entry.token, "Variable", entry.symbol_depth, entryInformation);
                    break;
                case TableEntry.EntryType.functionEntry:
                    TableEntry.Function fun_info = entry.entry_information.function;
                    entryInformation = String.Format("Params Num : {0, -5} Param Size : {1, -5} Local Size : {2, -5}", fun_info.number_of_parameters, fun_info.size_of_params, fun_info.size_of_local);
                    Console.WriteLine("{0, -17} {1, -9} {2, -9} {3, -8} {4}", entry.lexeme, entry.token, "Function", entry.symbol_depth, entryInformation);
                    LinkedList<TableEntry.ParameterNode> parameter_list = fun_info.paramter_list;
                    foreach (TableEntry.ParameterNode node in parameter_list)
                    {
                        Console.WriteLine("{0, -47}Argument Type: {1, -10} Passed By: {2, -10}", " ", varToString(node.type_of_parameter), passToString(node.pass_type));
                    }
                    break;
                case TableEntry.EntryType.moduleEntry:
                    TableEntry.Function mod_info = entry.entry_information.function;
                    entryInformation = String.Format("Local Size : {0, -5}", mod_info.size_of_local);
                    Console.WriteLine("{0, -17} {1, -9} {2, -9} {3, -8} {4}", entry.lexeme, entry.token, "Module", entry.symbol_depth, entryInformation);
                    break;
                default:
                    break;
            }
        }

        public string passToString(TableEntry.PassType passType)
        {
            switch (passType)
            {
                case TableEntry.PassType.passByValue:
                    return "Pass by Value";
                case TableEntry.PassType.passByReference:
                    return "Pass by Reference";
                default:
                    return "ERROR";
            }
        }

        public string varToString(TableEntry.VarType varType)
        {
            switch (varType)
            {
                case TableEntry.VarType.charType:
                    return "Character";
                case TableEntry.VarType.intType:
                    return "Integer";
                case TableEntry.VarType.floatType:
                    return "Float";
                default:
                    return "ERROR";
            }
        }


        private uint hash(string lexeme)
        {
            uint h = 0, g = 0;
            foreach (char p in lexeme)
            {
                h = (h << 4) + p;
                if(g == (h & 0xf0000000))
                {
                    h = h ^ (g >> 24);
                    h = h ^ g;
                }
            }

            return h % TableSize;
        }
    }

    class TableEntry
    {
        // Public Variables
        public enum VarType { charType, intType, floatType };
        public enum PassType { passByValue, passByReference };
        public enum EntryType { constEntry, varEntry, functionEntry, moduleEntry, stringEntry };
        public LexicalAnalyzer.Token token;
        public string lexeme;
        public int symbol_depth;
        public EntryType type_of_entry;
        public EntryInformation entry_information;

        // Need to make ParameterNode class since C# struct doesn't allow for next pointers.
        public class ParameterNode
        {
            public VarType type_of_parameter;
            public PassType pass_type;
        }

        public struct Variable
        {
            public VarType type_of_variable;
            public int offset;
            public int size;
            public bool is_parameter;
            public PassType pass_type;
        }

        public struct Constant
        {
            public VarType type_of_constant;
            public int offset;
            public ValueUnion value;
            public int size;
        }

        public struct Function
        {
            public int size_of_params;
            public int size_of_local;
            public int number_of_parameters;
            // There is no return type in this grammar
            //public VarType return_type;
            public LinkedList<ParameterNode> paramter_list;
        }

        // Added this for assembly code generation, not needed to make TAC
        public struct StringLiteral
        {
            public string value;
        }

        public struct EntryInformation
        {
            public Variable variable;
            public Constant constant;
            public Function function;
            public StringLiteral stringLiteral;
        }

        public struct ValueUnion
        {
            public int value;
            public double valueR;
        }

        // Constructors
        public TableEntry(string lexeme, LexicalAnalyzer.Token token, int depth)
        {
            this.lexeme = lexeme;
            this.token = token;
            this.symbol_depth = depth;
        }

        // Setters for private variables

        public void SetEntryType(EntryType entry_type)
        {
            this.type_of_entry = entry_type;
        }


        // Getters for private variables
        public string GetLexeme()
        {
            return this.lexeme;
        }

        public int GetSymbolDepth()
        {
            return this.symbol_depth;
        }

        public LexicalAnalyzer.Token GetToken()
        {
            return this.token;
        }

        public EntryType GetEntryType()
        {
            return this.type_of_entry;
        }
    }
}
