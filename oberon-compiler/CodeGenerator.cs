using System;
using System.Collections.Generic;
using System.Text;

namespace oberon_compiler
{
    class CodeGenerator
    {


        private string assembly_path;
        private string[] tac_lines;
        private int file_length;
        private int line_length;
        private SymbolTable symbol_table;
        private List<string> assembly_code = new List<string>();

        public CodeGenerator(string tac_file, SymbolTable symbol_table)
        {
            this.assembly_path = tac_file.Replace(".tac", ".asm");
            this.symbol_table = symbol_table;
            tac_lines = System.IO.File.ReadAllLines(tac_file);
            Array.Resize(ref tac_lines, tac_lines.Length + 1);
            tac_lines[tac_lines.Length - 1] = " ";
            file_length = tac_lines.Length;
            line_length = tac_lines[0].Length;
        }


        public void Generate()
        {
            GenerateBeginning();
            GenerateFromTAC();
            OutputAssemblyCode();
        }

        private void GenerateBeginning()
        {
            EmitCode("\t.model small");
            EmitCode("\t.stack 100h");
            EmitCode("\t.data");
            TableEntry[] global_entries = symbol_table.LookupDepth(1);
            foreach (TableEntry entry in global_entries)
            {
                if(entry.type_of_entry == TableEntry.EntryType.varEntry)
                {
                    EmitCode(entry.lexeme + " DW " + "?");
                    //EmitCode(FormatAssembly(entry.lexeme, "DW", " ", "?"));
                }
                else if(entry.type_of_entry == TableEntry.EntryType.constEntry)
                {
                    if(entry.entry_information.constant.type_of_constant == TableEntry.VarType.intType)
                    {
                        EmitCode(entry.lexeme + " DW " + entry.entry_information.constant.value.value.ToString());
                        //EmitCode(FormatAssembly(entry.lexeme, "DW", " ", entry.entry_information.constant.value.value.ToString()));
                    }
                    else if(entry.entry_information.constant.type_of_constant == TableEntry.VarType.floatType)
                    {
                        EmitCode(entry.lexeme + " DW " + entry.entry_information.constant.value.valueR.ToString());
                        //EmitCode(FormatAssembly(entry.lexeme, "DW", " ", entry.entry_information.constant.value.valueR.ToString()));
                    }
                }
                else if(entry.type_of_entry == TableEntry.EntryType.stringEntry)
                {
                    EmitCode(entry.lexeme + " DB " + "\"" + entry.entry_information.stringLiteral.value + "\",\"$\"");
                    //EmitCode(FormatAssembly(entry.lexeme, "DB", " ", "\"" + entry.entry_information.stringLiteral.value + "\",\"$\""));
                }
            }
            EmitCode("\t.code");
            EmitCode("\tinclude io.asm\n");
        }


        private void GenerateFromTAC()
        {
            for(int x = 0; x < file_length; x++)
            {
                string tac_line = tac_lines[x];

                // If the line is empty, skip it.
                if(CheckIfLineEmpty(tac_line))
                {
                    continue;
                }

                EmitCode("\t; " + tac_line);

                string[] tokens = tac_line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (tokens[0] == "Start")
                {
                    GenerateMainProc(tokens);
                }
                else if (tokens[0] == "proc")
                {
                    GenerateProcDeclCode(tokens);
                }
                else if(tokens[0] == "endp")
                {
                    GenerateProcEndCode(tokens);
                }
                else if (tokens[0] == "call")
                {
                    GenerateCallCode(tokens);
                }
                else if(tokens[0] == "push")
                {
                    GeneratePushCode(tokens);
                }
                else if(tokens[0] == "wrln")
                {
                    GenerateWriteLineCode();
                }
                else if(tokens[0] == "wrs")
                {
                    GenerateWriteStringCode(tokens);
                }
                else if(tokens[0] == "wri")
                {
                    GenerateWriteIntCode(tokens);
                }
                else if(tokens[0] == "rdi")
                {
                    GenerateReadIntCode(tokens);
                }
                // Assignment then
                else
                {
                    GenerateAssignmentCode(tokens);
                }
            }
        }

        private void GenerateAssignmentCode(string[] tokens)
        {
            // id1 = id2
            // id1 = numLiteral
            if(tokens.Length == 3)
            {
                GenerateCopyCode(CheckId(tokens[0]), CheckId(tokens[2]));
            }
            // id1 = id2 op id3
            else if(tokens.Length == 5)
            {
                string id1 = CheckId(tokens[0]);
                string id2 = CheckId(tokens[2]);
                string id3 = CheckId(tokens[4]);
                string op  = CheckId(tokens[3]);

                if(op == "+")
                {
                    GenerateAdditionCode(id1, id2, id3);
                }
                else if(op == "-")
                {
                    GenerateSubtractionCode(id1, id2, id3);
                }
                else if(op == "*")
                {
                    GenerateMultiplicationCode(id1, id2, id3);
                }
                else if(op == "/")
                {
                    GenerateDivisionCode(id1, id2, id3);
                }
            }
        }

        private void GenerateDivisionCode(string id1, string id2, string id3)
        {
            // Pass by reference variable
            if(id2[0] == '[' && id2[1] == '[')
            {
                EmitCode("\tmov bx, " + id2.Substring(1, id2.Length - 2));
                EmitCode("\tmov ax, [bx]");
            }
            else
            {
                EmitCode("\tmov ax, " + id2);
            }

            if (id3[0] == '[' && id3[1] == '[')
            {
                EmitCode("\tmov bx, " + id3.Substring(1, id3.Length - 2));
                EmitCode("\tmov cx, [bx]");
                EmitCode("\tmov bx, cx");
            }
            else
            {
                EmitCode("\tmov bx, " + id3);
            }

            EmitCode("\tidiv bx");
            GenerateCopyCode(id1, "ax");
        }

        private void GenerateMultiplicationCode(string id1, string id2, string id3)
        {
            // Pass by reference variable
            if (id2[0] == '[' && id2[1] == '[')
            {
                EmitCode("\tmov bx, " + id2.Substring(1, id2.Length - 2));
                EmitCode("\tmov ax, [bx]");
            }
            else
            {
                EmitCode("\tmov ax, " + id2);
            }

            if (id3[0] == '[' && id3[1] == '[')
            {
                EmitCode("\tmov bx, " + id3.Substring(1, id3.Length - 2));
                EmitCode("\tmov cx, [bx]");
                EmitCode("\tmov bx, cx");
            }
            else
            {
                EmitCode("\tmov bx, " + id3);
            }

            EmitCode("\timul bx");

            GenerateCopyCode(id1, "ax");
        }

        private void GenerateSubtractionCode(string id1, string id2, string id3)
        {
            // Pass by reference variable
            if (id2[0] == '[' && id2[1] == '[')
            {
                EmitCode("\tmov bx, " + id2.Substring(1, id2.Length - 2));
                EmitCode("\tmov ax, [bx]");
            }
            else
            {
                EmitCode("\tmov ax, " + id2);
            }

            if (id3[0] == '[' && id3[1] == '[')
            {
                EmitCode("\tmov bx, " + id3.Substring(1, id3.Length - 2));
                EmitCode("\tmov cx, [bx]");
                EmitCode("\tmov bx, cx");
            }
            else
            {
                EmitCode("\tmov bx, " + id3);
            }

            EmitCode("\tsub ax, bx");
            GenerateCopyCode(id1, "ax");
        }

        private void GenerateAdditionCode(string id1, string id2, string id3)
        {
            // Pass by reference variable
            if (id2[0] == '[' && id2[1] == '[')
            {
                EmitCode("\tmov bx, " + id2.Substring(1, id2.Length - 2));
                EmitCode("\tmov ax, [bx]");
            }
            else
            {
                EmitCode("\tmov ax, " + id2);
            }

            if (id3[0] == '[' && id3[1] == '[')
            {
                EmitCode("\tmov bx, " + id3.Substring(1, id3.Length - 2));
                EmitCode("\tmov cx, [bx]");
                EmitCode("\tmov bx, cx");
            }
            else
            {
                EmitCode("\tmov bx, " + id3);
            }
            EmitCode("\tadd ax, bx");

            GenerateCopyCode(id1, "ax");
        }

        // Generate code for copy statement id1 = id2
        private void GenerateCopyCode(string id1, string id2)
        {
            if(id2 == "ax")
            {
                if (id1[0] == '[' && id1[1] == '[')
                {
                    EmitCode("\tmov bx, " + id1.Substring(1, id1.Length - 2));
                    EmitCode("\tmov [bx], ax");
                }
                else
                {
                    EmitCode("\tmov " + id1 + ", ax");
                }

                return; // We are done, don't need to do anything else.
            }
            else if(id2[0] == '[' && id2[1] == '[')
            {
                EmitCode("\tmov bx, " + id2.Substring(1, id2.Length - 2));
                EmitCode("\tmov ax, [bx]");
            }
            else
            {
                EmitCode("\tmov ax, " + id2);
            }

            if (id1[0] == '[' && id1[1] == '[')
            {
                EmitCode("\tmov bx, " + id1.Substring(1, id1.Length - 2));
                EmitCode("\tmov [bx], ax");
            }
            else
            {
                EmitCode("\tmov " + id1 + ", ax");
            }


        }

        private void GenerateReadIntCode(string[] tokens)
        {
            EmitCode("\tpush bx");
            EmitCode("\tcall readint");
            GenerateCopyCode(CheckId(tokens[1]), "bx");
            EmitCode("\tpop bx");
        }

        private void GenerateWriteIntCode(string[] tokens)
        {
            EmitCode("\tpush ax");
            EmitCode("\tpush bx");
            EmitCode("\tpush cx");
            EmitCode("\tpush dx");
            GenerateCopyCode("ax", CheckId(tokens[1]));
            EmitCode("\tcall writeint");
            EmitCode("\tpop dx");
            EmitCode("\tpop cx");
            EmitCode("\tpop bx");
            EmitCode("\tpop ax");
        }

        private void GenerateWriteStringCode(string[] tokens)
        {
            EmitCode("\tmov dx, OFFSET " + tokens[1]);
            EmitCode("\tcall writestr");
        }

        private void GenerateWriteLineCode()
        {
            EmitCode("\tcall writeln");
        }

        private void GenerateCallCode(string[] tokens)
        {
            EmitCode("\tcall " + tokens[1]);
        }

        private void GeneratePushCode(string[] tokens)
        {
            string id = tokens[1];
            if(id[0] == '@')
            {
                id = id.Substring(1);
                EmitCode("\tmov ax, OFFSET " + id);
                EmitCode("\tpush ax");
            }
            else
            {
                EmitCode("\tpush " + id);
            }
        }

        private void GenerateMainProc(string[] tokens)
        {
            EmitCode("main" + "\tPROC");
            EmitCode("\tmov ax, @data");
            EmitCode("\tmov ds, ax");
            EmitCode("\tcall " + tokens[2]);
            EmitCode("\tmov ah, 04ch");
            EmitCode("\tint 21h");
            EmitCode("main" + "\tENDP");
            EmitCode("\tEND main");
        }

        private void GenerateProcEndCode(string[] tokens)
        {
            string function_name = tokens[1];
            TableEntry function = symbol_table.Lookup(function_name);

            EmitCode("\tadd sp," + function.entry_information.function.size_of_local.ToString());
            EmitCode("\tpop bp");
            EmitCode("\tret " + function.entry_information.function.size_of_params);
            EmitCode(function_name + "\tENDP");
        }

        private void GenerateProcDeclCode(string[] tokens)
        {
            string function_name = tokens[1];
            TableEntry function = symbol_table.Lookup(function_name);

            EmitCode(function_name + "\tPROC");
            EmitCode("\tpush bp");
            EmitCode("\tmov bp,sp");
            EmitCode("\tsub sp," + function.entry_information.function.size_of_local.ToString());

        }

        private string CheckId(string id)
        {
            if(id.Contains("_bp-") || id.Contains("_bp+"))
            {
                id = id.Replace("_bp", "bp");
                id = "[" + id + "]";
            }
            
            if(id.Length > 1)
            {
                if (id[1] == '@')
                {
                    id = id.Substring(0, 1) + '[' + id.Substring(2) + ']';
                }
            }

            return id;
        }

        private bool CheckIfLineEmpty(string line)
        {
            string rest = line.Replace(" ", "");

            if(rest == "")
            {
                return true;
            }


            return false;
        }

        private string FormatAssembly(string instruction = " ", string id1 = " ", string comma = " ", string id2 = " ")
        {
            return String.Format("{0, -10} {1, 10}{2, 1} {3, -10}", instruction, id1, comma, id2);
        }


        private void EmitCode(string code)
        {
            assembly_code.Add(code);
        }

        private void OutputAssemblyCode()
        {
            // Convert the list of three address code statements that we have to an array
            // and print them to the TAC file.
            string[] assembly_lines = assembly_code.ToArray();
            System.IO.File.WriteAllLines(assembly_path, assembly_lines);

            //int line_number = 1;
            //Console.WriteLine("\nAssembly Code:");
            //Console.WriteLine("-------------------");

            //foreach (string line in assembly_lines)
            //{
            //    if (line_number % 20 == 0)
            //    {
            //        Console.WriteLine("\nPress any continue to continue...");
            //        Console.ReadKey();
            //        Console.WriteLine("\nMore Three Assembly:");
            //        Console.WriteLine("------------------------");
            //    }

            //    Console.WriteLine(line);
            //    line_number++;
            //}

            Console.WriteLine("\n\nAssembly has been written to: " + assembly_path.Replace("../", ""));
        }
    }
}
