using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{
    /*Class: RDParser
     *Description: Uses the LexicalAnalyzer class to parse through a file a given file
     *             using the Oberon-0 context-free grammar rules 
     */
    class RDParser
    {
        //Private variables that the class uses
        private LexicalAnalyzer lex_analyzer;
        private SymbolTable symbol_table;
        private int current_depth;
        private TableEntry current_node;
        private TableEntry current_procedure;
        private List<string> identifier_list = new List<string>();
        private Stack<TableEntry> prog_procedures = new Stack<TableEntry>();
        private int current_offset = 0;
        private TableEntry.EntryType current_entry_type;
        private TableEntry.PassType current_passing_type;
        private List<string> three_address_code = new List<string>();           // Holds the Three Address Code that we will write to file
        private string tac_path;
        private int parameter_depth = 0;

        // Holder for NewTemp()
        private static int i = 1;



        /* Function: RDParser
         * Description: Constructor for the RDParser class
         * Input: LexicalAnalyzer to use as a reference for parsing
         */
        public RDParser(LexicalAnalyzer analyzer)
        {
            lex_analyzer = analyzer;
            symbol_table = new SymbolTable();
            current_depth = 0;
        }

        public RDParser(string oberon_file)
        {
            lex_analyzer = new LexicalAnalyzer(oberon_file);
            symbol_table = new SymbolTable();
            current_depth = 0;
            tac_path = oberon_file.Replace(".obr", ".tac");
        }

        /* Function: Parser
         * Description: Function to begin parsing through the given file, only public function of the class
         * Input:
         */
        public void Parse()
        {
            // Load initial token
            lex_analyzer.GetNextToken();
            Prog();
            match(LexicalAnalyzer.Token.eoft);
            OutputThreeAddressCode();
        }

        /* Function: Prog
         * Description: Implements the following CFG rule:
         *              Prog -> modulet idt;
         *                      DeclarativePart
         *                      StatementPart
         *                      endt idt.
         */
        private void Prog()
        {
            switch (lex_analyzer.token)
            {
                case LexicalAnalyzer.Token.modulet:
                    match(LexicalAnalyzer.Token.modulet);

                    current_depth = 0;

                    // Insert the identifier token in the symbol table as a function/module type.
                    symbol_table.Insert(lex_analyzer.lexeme, lex_analyzer.token, current_depth, lex_analyzer);

                    // Look up the new symbol in the table and add the default values to it
                    current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                    current_node.type_of_entry = TableEntry.EntryType.moduleEntry;
                    current_node.entry_information.function.size_of_local = 0;
                    current_node.entry_information.function.size_of_params = 0;
                    current_node.entry_information.function.number_of_parameters = 0;
                    current_node.entry_information.function.paramter_list = new LinkedList<TableEntry.ParameterNode>();
                    current_offset = 2;
                    // Push the module one the program procedure stack.
                    prog_procedures.Push(current_node);

                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.semicolt);
                    current_depth++;
                    DeclarativePart();

                    // We are about to encounter statements, let's print the "proc idt" three address code.
                    current_procedure = prog_procedures.Peek();
                    EmitCode(FormatTAC("proc", current_procedure.lexeme));

                    StatementPart();
                    match(LexicalAnalyzer.Token.endt);

                    Console.WriteLine("\nPrinting Depth 1:");
                    symbol_table.WriteTable(1);
                    symbol_table.DeleteDepth(1);

                    TableEntry module = prog_procedures.Peek();

                    // If the module identifiers do not match, tell the user and exit.
                    if(lex_analyzer.lexeme != module.lexeme)
                    {
                        Console.WriteLine("Error at {0},{1}: expecting the module identifier '{2}' but recieved the identifier '{3}' instead!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, module.lexeme, lex_analyzer.lexeme);
                        Console.Write("\nPress a key to exit... ");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }

                    // We are about to end the function, let's print the "Endp idt" three address code.
                    EmitCode(FormatTAC("endp", module.lexeme));
                    EmitCode(FormatTAC());

                    EmitCode(FormatTAC("Start proc", module.lexeme));

                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.periodt);

                    // We have finished parsing the file, print the last depth and delete from symbol table.
                    Console.WriteLine("\nPrinting Depth 0:");
                    symbol_table.WriteTable(0);
                    symbol_table.DeleteDepth(0);

                    break;
                default:
                    Console.WriteLine("Error at {0},{1}: expecting modulet", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    break;
            }
        }

        /* Function: DeclarativePart
         * Description: Implements the following CFG rule:
         *              DeclarativePart -> ConstPart
         *                                 VarPart
         *                                 ProcPart
         */
        private void DeclarativePart()
        {
            ConstPart();
            VarPart();
            ProcPart();
        }

        /* Function: ConstPart
         * Description: Implements the following CFG rule:
         *              ConstPart -> constt ConstTail | Lambda
         */
        private void ConstPart()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.constt)
            {
                match(LexicalAnalyzer.Token.constt);
                ConstTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: ConstTail
         * Description: Implements the following CFG rule:
         *              ConstTail -> idt = Value ; ConstTail | Lambda
         */
        private void ConstTail()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                // Add the lexeme to the symbol table and then look up the lexeme to get a reference to the node.
                symbol_table.Insert(lex_analyzer.lexeme, lex_analyzer.token, current_depth, lex_analyzer);
                current_entry_type = TableEntry.EntryType.constEntry;

                // Lookup the node in the symbol table and add the default constant information
                current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                current_node.type_of_entry = TableEntry.EntryType.constEntry;
                
                match(LexicalAnalyzer.Token.idt);
                matchLexeme("=");
                Value();
                match(LexicalAnalyzer.Token.semicolt);
                ConstTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: Value
         * Description: Implements the following CFG rule:
         *              Value -> NumericalLiteral
         */
        private void Value()
        {
            // Pop the current procedure off the stack so we can update its information.
            TableEntry current_procedure = prog_procedures.Pop();

            if (lex_analyzer.token == LexicalAnalyzer.Token.intt)
            {
                current_procedure.entry_information.function.size_of_local += 2;
                current_node.entry_information.constant.offset = current_offset;
                current_node.entry_information.constant.type_of_constant = TableEntry.VarType.intType;
                current_node.entry_information.constant.value.value = lex_analyzer.value;
                current_offset += 2;
                match(LexicalAnalyzer.Token.intt);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
            {
                current_procedure.entry_information.function.size_of_local += 4;
                current_node.entry_information.constant.offset = current_offset;
                current_node.entry_information.constant.type_of_constant = TableEntry.VarType.floatType;
                current_node.entry_information.constant.value.valueR = lex_analyzer.valueR;
                current_offset += 4;
                match(LexicalAnalyzer.Token.decimalt);
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting numerical literal", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1); 
            }

            prog_procedures.Push(current_procedure);
        }

        /* Function: VarPart
         * Description: Implements the following CFG rule:
         *              VarPart -> vart  VarTail | Lambda
         */
        private void VarPart()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.vart)
            {
                match(LexicalAnalyzer.Token.vart);
                VarTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: VarTail
         * Description: Implements the following CFG rule:
         *              VarTail -> IdentifierList : TypeMark ; VarTail  | Lambda
         */
        private void VarTail()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                current_entry_type = TableEntry.EntryType.varEntry;
                // Get a list of all the variables that are being declared.
                identifier_list = new List<string>();
                IdentifierList();
                match(LexicalAnalyzer.Token.colt);
                TypeMark();
                match(LexicalAnalyzer.Token.semicolt);
                VarTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: IdentifierList
         * Description: Implements the following CFG rule:
         *              IdentifierList -> idt | IdentifierList , idt
         */
        private void IdentifierList()
        {
            // Sinced this is LL(1), we can not look ahead to see the variable/parameter type.
            // So we will save lexeme to list to add the type information to the node later.
            identifier_list.Add(lex_analyzer.lexeme);

            match(LexicalAnalyzer.Token.idt);
            if(lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);

                // Recursivly call IdentifierList to add all the identifiers to the list.
                IdentifierList();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: TypeMark
         * Description: Implements the following CFG rule:
         *              TypeMark -> integert | realt | chart  
         */
        private void TypeMark()
        {
            TableEntry.VarType var_type;
            int var_size;

            switch (lex_analyzer.token)
            {
                case LexicalAnalyzer.Token.integert:
                    match(LexicalAnalyzer.Token.integert);
                    var_type = TableEntry.VarType.intType;
                    var_size = 2;
                    break;
                case LexicalAnalyzer.Token.realt:
                    match(LexicalAnalyzer.Token.realt);
                    var_type = TableEntry.VarType.floatType;
                    var_size = 4;
                    break;
                case LexicalAnalyzer.Token.chart:
                    match(LexicalAnalyzer.Token.chart);
                    var_type = TableEntry.VarType.charType;
                    var_size = 1;
                    break;
                default:
                    Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting variable type (INTEGER, REAL, or CHAR)", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    return;
            }

            // Pop the current procedure off the stack so we can update its information.
            current_procedure = prog_procedures.Pop();

            // If the current entry is a variable, then we know all the identifiers in the list are variables.
            if (current_entry_type == TableEntry.EntryType.varEntry)
            {
                // Go through the identifier list and add the variable to the symbol table along with other important information.
                foreach (string identifier in identifier_list)
                {
                    // Add the varible to the symbol table.
                    symbol_table.Insert(identifier, LexicalAnalyzer.Token.idt, current_depth, lex_analyzer);

                    // Lookup the symbol and add information about the variable.
                    current_node = symbol_table.Lookup(identifier);
                    current_node.type_of_entry = TableEntry.EntryType.varEntry;
                    current_node.entry_information.variable.type_of_variable = var_type;
                    current_node.entry_information.variable.is_parameter = false;
                    current_node.entry_information.variable.offset = current_offset;
                    current_node.entry_information.variable.size = var_size;
                    current_offset += var_size;

                    // Update the local size of the current procedure.
                    current_procedure.entry_information.function.size_of_local += var_size;
                }
            }
            // If the current entry is a function, then we know all the identifiers in the list are arguments.
            else if(current_entry_type == TableEntry.EntryType.functionEntry)
            {
                if(current_passing_type == TableEntry.PassType.passByReference)
                {
                    // Change the var_size to 4 in order to hold 32 bit addresses.
                    var_size = 4;
                }
                foreach (string identifier in identifier_list)
                {
                    // Add the argument to the symbol table.
                    symbol_table.Insert(identifier, LexicalAnalyzer.Token.idt, current_depth, lex_analyzer);

                    // Lookup the symbol and add more information for the parameter
                    current_node = symbol_table.Lookup(identifier);
                    current_node.type_of_entry = TableEntry.EntryType.varEntry;
                    current_node.entry_information.variable.type_of_variable = var_type;
                    current_node.entry_information.variable.is_parameter = true;
                    current_node.entry_information.variable.offset = current_offset;
                    current_node.entry_information.variable.size = var_size;
                    current_node.entry_information.variable.pass_type = current_passing_type;
                    current_offset += var_size;

                    TableEntry.ParameterNode node = new TableEntry.ParameterNode();
                    node.type_of_parameter = var_type;
                    node.pass_type = current_passing_type;
                    current_procedure.entry_information.function.paramter_list.AddLast(node);
                    current_procedure.entry_information.function.number_of_parameters += 1;
                    current_procedure.entry_information.function.size_of_params += var_size;
                }
            }

            // Push the current procedure back on the stack.
            prog_procedures.Push(current_procedure);
        }

        /* Function: ProcPart
         * Description: Implements the following CFG rule:
         *              ProcPart -> ProcedureDecl  ProcPart | Lambda
         */
        private void ProcPart()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.proceduret)
            {
                ProcedureDecl();
                ProcPart();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: ProcedureDecl
         * Description: Implements the following CFG rule:
         *              ProcedureDecl -> ProcHeading ; ProcBody  idt ;
         */
        private void ProcedureDecl()
        {
            ProcHeading();
            match(LexicalAnalyzer.Token.semicolt);
            ProcBody();
            match(LexicalAnalyzer.Token.idt);
            match(LexicalAnalyzer.Token.semicolt);

            // We are done with the procedure declaration, pop it off the stack and get rid of variables at the depth in the hash table!
            prog_procedures.Pop();
            Console.WriteLine("\nPrinting Depth " + current_depth.ToString() + ":");
            symbol_table.WriteTable(current_depth);
            symbol_table.DeleteDepth(current_depth);
            current_depth -= 1;

            // The current offset will equal the size of local variables.
            // This is not necessary since you must declare all variables inside a procedure before declaring a procedure.
            current_offset = prog_procedures.Peek().entry_information.function.size_of_local;
        }

        /* Function: ProcHeading
         * Description: Implements the following CFG rule:
         *              ProcHeading -> proct idt Args 
         */
        private void ProcHeading()
        {
            match(LexicalAnalyzer.Token.proceduret);

            current_entry_type = TableEntry.EntryType.functionEntry;
            symbol_table.Insert(lex_analyzer.lexeme, LexicalAnalyzer.Token.idt, current_depth, lex_analyzer);
            current_node = symbol_table.Lookup(lex_analyzer.lexeme);
            current_node.type_of_entry = TableEntry.EntryType.functionEntry;
            current_node.entry_information.function.size_of_local = 0;
            current_node.entry_information.function.size_of_params = 0;
            current_node.entry_information.function.number_of_parameters = 0;
            current_node.entry_information.function.paramter_list = new LinkedList<TableEntry.ParameterNode>();

            // Push the procedure onto the stack so that we can keep track of the procedures & local variables
            prog_procedures.Push(current_node);
            current_depth += 1;
            current_offset = 0;

            match(LexicalAnalyzer.Token.idt);
            Args();


            current_offset = 2;
        }

        /* Function: Args
         * Description: Implements the following CFG rule:
         *              Args -> ( ArgList ) | Lambda
         */
        private void Args()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.lpart)
            {
                match(LexicalAnalyzer.Token.lpart);
                ArgList();
                match(LexicalAnalyzer.Token.rpart);
            }
            else
            {
                Lambda();
            }
        }

        /* Function: ArgList
         * Description: Implements the following CFG rule:
         *              ArgList -> Mode IdentifierList : TypeMark MoreArgs
         */
        private void ArgList()
        {
            Mode();
            identifier_list = new List<string>();
            IdentifierList();
            match(LexicalAnalyzer.Token.colt);
            TypeMark();
            MoreArgs();
        }

        /* Function: Mode
         * Description: Implements the following CFG rule:
         *              Mode -> vart | Lambda
         */
        private void Mode()
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.vart)
            {
                current_passing_type = TableEntry.PassType.passByReference;
                match(LexicalAnalyzer.Token.vart);
            }
            else
            {
                current_passing_type = TableEntry.PassType.passByValue;
                Lambda();
            }
        }

        /* Function: MoreArgs
         * Description: Implements the following CFG rule:
         *              MoreArgs -> ; ArgList | Lambda
         */
        private void MoreArgs()
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.semicolt)
            {
                match(LexicalAnalyzer.Token.semicolt);
                ArgList();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: ProcBody
         * Description: Implements the following CFG rule:
         *              ProcBody -> DeclarativePart StatementPart  endt
         */
        private void ProcBody()
        {
            DeclarativePart();

            // We are about to encounter statements, let's print the "proc idt" three address code.
            current_procedure = prog_procedures.Peek();
            EmitCode(FormatTAC("proc", current_procedure.lexeme));

            StatementPart();

            // We are about to end the function, let's print the "Endp idt" three address code.
            current_procedure = prog_procedures.Peek();
            EmitCode(FormatTAC("endp", current_procedure.lexeme));
            EmitCode(FormatTAC());

            match(LexicalAnalyzer.Token.endt);
        }

        /* Function: StatementPart
         * Description: Implements the following CFG rule:
         *              StatementPart -> begint SeqOfStatements | Lambda
         */
        private void StatementPart()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.begint)
            {
                match(LexicalAnalyzer.Token.begint);
                SeqOfStatements();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: SeqOfStatements
         * Description: Implements the following CFG rule:
         *              SeqOfStatements -> Statement ; StatTail | Lambda
         */
        private void SeqOfStatements()
        {
            switch (lex_analyzer.token)
            {
                case (LexicalAnalyzer.Token.idt):

                case (LexicalAnalyzer.Token.readt):

                case (LexicalAnalyzer.Token.writet):

                case (LexicalAnalyzer.Token.writelnt):
                    Statement();
                    match(LexicalAnalyzer.Token.semicolt);
                    StatTail();
                    break;

                default:
                    Lambda();

                    break;
            }
        }

        /* Function: StatTail
         * Description: Implements the following CFG rule:
         *              StatTail	-> Statement ; StatTail | Lambda
         */
        private void StatTail()
        {
            switch (lex_analyzer.token)
            {
                case (LexicalAnalyzer.Token.idt):
                    
                case (LexicalAnalyzer.Token.readt):

                case (LexicalAnalyzer.Token.writet):

                case (LexicalAnalyzer.Token.writelnt):
                    Statement();
                    match(LexicalAnalyzer.Token.semicolt);
                    StatTail();
                    break;

                default:
                    Lambda();
                    break;
            }
        }

        /* Function: Statement
         * Description: Implements the following CFG rule:
         *              Statement	-> AssignStat	| IOStat
         */
        private void Statement()
        {
            switch (lex_analyzer.token)
            {
                case (LexicalAnalyzer.Token.idt):
                    AssignStat();
                    break;

                case (LexicalAnalyzer.Token.readt):

                case (LexicalAnalyzer.Token.writet):

                case (LexicalAnalyzer.Token.writelnt):
                    IOStat();
                    break;

                default:
                    Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting start of statement (identifier, read, write, writeln)!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    break;
            }
        }

        /* Function: AssignStat
         * Description: Implements the following CFG rule:
         *              AssignStat	->	idt := Expr
         */
        private void AssignStat()
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                // Look up the identifier in the symbol table and check to see if it has been declared.
                current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                CheckIfEmpty(current_node);

                if(current_node.type_of_entry == TableEntry.EntryType.functionEntry)
                {
                    ProcCall();
                }
                else if(current_node.type_of_entry == TableEntry.EntryType.varEntry)
                {
                    TableEntry id_ptr = symbol_table.Lookup(lex_analyzer.lexeme);
                    TableEntry expr_ptr = null;

                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.assignopt);
                    expr_ptr = Expr();

                    EmitCode(FormatTAC(ConvertVar(id_ptr), "=", ConvertVar(expr_ptr)));
                }
                // If identifer is a constant, tell the user taht it is not allowed to be assigned to or called.
                else if(current_node.type_of_entry == TableEntry.EntryType.constEntry)
                {
                    Console.WriteLine("Error at {0},{1}: '{2}' is a constant identifier which can not be assigned to or called!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                // If identifier is a module, tell the user that it is not allowed to be assigned to or called.
                else if(current_node.type_of_entry == TableEntry.EntryType.moduleEntry)
                {
                    Console.WriteLine("Error at {0},{1}: '{2}' is a module identifier which can not be assigned to or called!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting an identifier token!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        /* Function: IOStat
         * Description: Implements the following CFG rule:
         *              IOStat	->	InStat | OutStat
         */
        private void IOStat()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.readt)
            {
                InStat();
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.writelnt || lex_analyzer.token == LexicalAnalyzer.Token.writet)
            {
                OutStat();
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting a IO command (read, write, writeln)!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }


        private void InStat()
        {
            match(LexicalAnalyzer.Token.readt);
            match(LexicalAnalyzer.Token.lpart);
            IdList();
            match(LexicalAnalyzer.Token.rpart);
        }

        private void OutStat()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.writet)
            {
                match(LexicalAnalyzer.Token.writet);
                match(LexicalAnalyzer.Token.lpart);
                WriteList();
                match(LexicalAnalyzer.Token.rpart);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.writelnt)
            {
                match(LexicalAnalyzer.Token.writelnt);
                match(LexicalAnalyzer.Token.lpart);
                WriteList();
                match(LexicalAnalyzer.Token.rpart);
                EmitCode(FormatTAC("wrln"));
            }
        }

        private void IdList()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                // Look up the identifier in the symbol table and check to see if it has been declared.
                current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                CheckIfEmpty(current_node);
                // See if the variable provided is a variable and if it is a integer.
                if (current_node.type_of_entry == TableEntry.EntryType.varEntry && current_node.entry_information.variable.type_of_variable == TableEntry.VarType.intType)
                {
                    EmitCode(FormatTAC("rdi", ConvertVar(current_node)));
                }
                match(LexicalAnalyzer.Token.idt);
                IdListTail();
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found as a parameter inside of the read() function, which can only have identifiers as parameters!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        private void IdListTail()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);
                if (lex_analyzer.token == LexicalAnalyzer.Token.idt)
                {
                    // Look up the identifier in the symbol table and check to see if it has been declared.
                    current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                    CheckIfEmpty(current_node);
                    // See if the variable provided is a variable and if it is a integer.
                    if (current_node.type_of_entry == TableEntry.EntryType.varEntry && current_node.entry_information.variable.type_of_variable == TableEntry.VarType.intType)
                    {
                        EmitCode(FormatTAC("rdi", ConvertVar(current_node)));
                    }
                    match(LexicalAnalyzer.Token.idt);
                    IdListTail();
                }
                else
                {
                    Console.WriteLine("Error at {0},{1}: '{2}' was found as a parameter inside of the read() function, which can only have identifiers as parameters!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                match(LexicalAnalyzer.Token.idt);
                IdListTail();
            }
            else
            {
                Lambda();
            }
        }

        private void WriteList()
        {
            WriteToken();
            WriteListTail();
        }

        private void WriteListTail()
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);
                WriteToken();
                WriteListTail();
            }
            else
            {
                Lambda();
            }
        }

        private void WriteToken()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                // Look up the identifier in the symbol table and check to see if it has been declared.
                current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                CheckIfEmpty(current_node);
                // See if the variable provided is a variable and if it is a integer.
                if (current_node.type_of_entry == TableEntry.EntryType.varEntry && current_node.entry_information.variable.type_of_variable == TableEntry.VarType.intType)
                {
                    EmitCode(FormatTAC("wri", ConvertVar(current_node)));
                }
                match(LexicalAnalyzer.Token.idt);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.intt)
            {
                EmitCode(FormatTAC("wri", lex_analyzer.lexeme));
                match(LexicalAnalyzer.Token.intt);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
            {
                EmitCode(FormatTAC("wrd", lex_analyzer.lexeme));
                match(LexicalAnalyzer.Token.decimalt);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.stringt)
            {
                // If it is a string, just print wrs followed by the string litteral
                EmitCode(FormatTAC("wrs", lex_analyzer.lexeme));
                match(LexicalAnalyzer.Token.stringt);
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found as a parameter inside of the write()/writeln() function, which only supports identifiers, numeric literals, or string literals as parameters!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        /* Function: Expr
         * Description: Implements the following CFG rule:
         *              Expr	->	Relation
         */
        private TableEntry Expr()
        {
            TableEntry rel_ptr = null;

            rel_ptr = Relation();
            return rel_ptr;
        }

        /* Function: Relation
         * Description: Implements the following CFG rule:
         *              Relation	->	SimpleExpr
         */
        private TableEntry Relation()
        {
            TableEntry simple_expr_ptr = null;

            simple_expr_ptr = SimpleExpr();
            return simple_expr_ptr;
        }


        /* Function: SimpleExpr
         * Description: Implements the following CFG rule:
         *              SimpleExpr	->	Term MoreTerm
         */
        private TableEntry SimpleExpr()
        {
            TableEntry T_ptr = null;

            T_ptr = Term();
            T_ptr = MoreTerm(T_ptr);

            return T_ptr;
        }

        /* Function: MoreTerm
         * Description: Implements the following CFG rule:
         *              MoreTerm	->	Addop Term MoreTerm | Lambda
         */
        private TableEntry MoreTerm(TableEntry more_term_place)
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.addopt)
            {
                TableEntry tmp_ptr = NewTemp();
                TableEntry T_ptr = null;

                // Add offset information to the temp variable.
                tmp_ptr.entry_information.variable.offset = current_offset;
                tmp_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                tmp_ptr.entry_information.variable.size = 2;
                tmp_ptr.entry_information.variable.is_parameter = false;
                current_offset += 2;

                string op = lex_analyzer.lexeme;
                match(LexicalAnalyzer.Token.addopt);
                T_ptr = Term();
                // CalculateTempSize is a type checking part of the compiler that does not
                // need to be used for assignment 7!
                //CalculateTempSize(ref tmp_ptr, more_term_place, T_ptr);
                EmitCode(FormatTAC(ConvertVar(tmp_ptr), "=", ConvertVar(more_term_place), op, ConvertVar(T_ptr)));

                more_term_place = tmp_ptr;
                return MoreTerm(more_term_place);
            }
            else
            {
                Lambda();
                return more_term_place;
            }
        }

        /* Function: Term
         * Description: Implements the following CFG rule:
         *              Term	->	Factor MoreFactor
         */
        private TableEntry Term()
        {
            TableEntry F_ptr = null;

            F_ptr = Factor(F_ptr);
            F_ptr = MoreFactor(F_ptr);

            return F_ptr;
        }

        /* Function: MoreFactor
         * Description: Implements the following CFG rule:
         *              MoreFactor	-> Mulop Factor MoreFactor | Lambda
         */
        private TableEntry MoreFactor(TableEntry more_factor_place)
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.mulopt)
            {
                TableEntry tmp_ptr = NewTemp();
                TableEntry F_ptr = null;

                // Add offset information to the temp variable.
                tmp_ptr.entry_information.variable.offset = current_offset;
                tmp_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                tmp_ptr.entry_information.variable.size = 2;
                tmp_ptr.entry_information.variable.is_parameter = false;
                current_offset += 2;

                string op = lex_analyzer.lexeme;
                match(LexicalAnalyzer.Token.mulopt);
                F_ptr = Factor(F_ptr);

                ChangeTempFactor(tmp_ptr, F_ptr, more_factor_place);
                EmitCode(FormatTAC(ConvertVar(tmp_ptr), "=", ConvertVar(more_factor_place), op, ConvertVar(F_ptr)));

                more_factor_place = tmp_ptr;
                return MoreFactor(more_factor_place);
            }
            else
            {
                Lambda();
                return more_factor_place;
            }
        }

        /* Function: Factor
         * Description: Implements the following CFG rule:
         *              Factor	->	idt      |
         *                          numt     |
         *                          ( Expr ) |
         *                          ~ Factor |
         *                          SignOp Factor
         */
        private TableEntry Factor(TableEntry F_ptr)
        {
            TableEntry temp_ptr;

            switch (lex_analyzer.token)
            {
                case LexicalAnalyzer.Token.idt:
                    // Look up the identifier in the symbol table and check to see if it has been declared.
                    F_ptr = symbol_table.Lookup(lex_analyzer.lexeme);
                    CheckIfEmpty(F_ptr);
                    match(LexicalAnalyzer.Token.idt);
                    break;

                case LexicalAnalyzer.Token.intt:
                    F_ptr = NewTemp();
                    F_ptr.entry_information.variable.size = 2; F_ptr.entry_information.variable.is_parameter = false;
                    F_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                    current_offset += 2;
                    EmitCode(FormatTAC(ConvertVar(F_ptr), "=", lex_analyzer.lexeme));
                    match(LexicalAnalyzer.Token.intt);
                    break;

                case LexicalAnalyzer.Token.decimalt:
                    F_ptr = NewTemp();
                    F_ptr.entry_information.variable.size = 4; F_ptr.entry_information.variable.is_parameter = false;
                    F_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                    current_offset += 4;
                    EmitCode(FormatTAC(ConvertVar(F_ptr), "=", lex_analyzer.lexeme));
                    match(LexicalAnalyzer.Token.decimalt);
                    break;

                case LexicalAnalyzer.Token.lpart:
                    match(LexicalAnalyzer.Token.lpart);
                    F_ptr = Expr();
                    match(LexicalAnalyzer.Token.rpart);
                    break;

                case LexicalAnalyzer.Token.tildet:

                    // Create a temp variable to hold negated Factor
                    temp_ptr = NewTemp();

                    temp_ptr.entry_information.variable.size = 2;
                    temp_ptr.entry_information.variable.is_parameter = false;
                    temp_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                    temp_ptr.entry_information.variable.offset = current_offset; current_offset += 2;

                    match(LexicalAnalyzer.Token.tildet);
                    F_ptr = Factor(F_ptr);

                    // Generate the TAC code for the ~Factor
                    EmitCode(FormatTAC(ConvertVar(temp_ptr), "=", ConvertVar(F_ptr) , "~"));

                    // We need to return the 1's complement Factor value instead of the Factor value!
                    F_ptr = temp_ptr;

                    break;

                default: 
                    if(lex_analyzer.lexeme == "-")
                    {
                        SignOp();

                        // Create a temp variable to hold negated Factor
                        temp_ptr = NewTemp();

                        temp_ptr.entry_information.variable.size = 2;
                        temp_ptr.entry_information.variable.is_parameter = false;
                        temp_ptr.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                        temp_ptr.entry_information.variable.offset = current_offset; current_offset += 2;

                        // Get the Factor we need to negate
                        F_ptr = Factor(F_ptr);

                        // Generate the TAC code for the -Factor
                        EmitCode(FormatTAC(ConvertVar(temp_ptr), "=", "-1", "*", ConvertVar(F_ptr)));

                        // We need to return the negated Factor value instead of the Factor value!
                        F_ptr = temp_ptr;
                    }
                    else
                    {
                        Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting a Factor token (idt, numt, (Expr), ~ Factor, or SignOp)", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                        Console.Write("\nPress a key to exit... ");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }

                    break;
            }

            return F_ptr;
        }

        /* Function: Addop
         * Description: Implements the following CFG rule:
         *              Addop	->	+ |
         *                          - |
         *                          OR
         */
        public void Addop()
        {
            match(LexicalAnalyzer.Token.addopt);
        }

        /* Function: Mulop
         * Description: Implements the following CFG rule:
         *              Mulop	-> *    |
         *                          /   |
         *                          DIV |
         *                          MOD |
         *                          &
         */
        public void Mulop()
        {
            match(LexicalAnalyzer.Token.mulopt);
        }

        /* Function: SignOp
         * Description: Implements the following CFG rule:
         *              SignOp	->	-
         */
        public void SignOp()
        {
            matchLexeme("-");
        }

        /* Function: checkIfEmpty
         * Description: This function recieves a TableEntry reference and checks if the reference
         *              is to a null pointer (meaning the identifier isn't in the symbol table).
         *              If it isn't in the table, tell the user that they must declare it before
         *              using it.  If it is in the table, then carry on with parsing the input file.
         */
        public void CheckIfEmpty(TableEntry node)
        {
            // If the identifier was not found inside of the symbol table, then tell the user and exit.
            if (current_node == null)
            {
                Console.WriteLine("Error at {0},{1}: the identifier '{2}' was found in an assignment statement, but the identifier has not been declared yet!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            else
            {
                return;
            }
        }


        public void ProcCall()
        {
            string procedure_identifier = lex_analyzer.lexeme;
            current_procedure = symbol_table.Lookup(procedure_identifier);

            match(LexicalAnalyzer.Token.idt);
            match(LexicalAnalyzer.Token.lpart);

            parameter_depth = 0;
            Params();
            match(LexicalAnalyzer.Token.rpart);

            EmitCode(FormatTAC("call", procedure_identifier));
        }


        public void Params()
        {
            TableEntry.ParameterNode parameter;

            // Try to find the expected parameter in the procedure paramater list.
            try
            {
                parameter = current_procedure.entry_information.function.paramter_list.ElementAt(parameter_depth);
            }
            // If we get a "ArgumentOutOfRangeException, then we know that there is no other parameters.
            catch (ArgumentOutOfRangeException)
            {
                parameter = null;
            }
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt || lex_analyzer.token == LexicalAnalyzer.Token.intt || lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
            {
                if(parameter == null)
                {
                    Console.WriteLine("Error at {0},{1}: the argument '{2}' was found inside of a call to '{3}' which does not support any parameters!",
                        lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme, current_procedure.lexeme, current_procedure.entry_information.function.number_of_parameters);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }

                if (lex_analyzer.token == LexicalAnalyzer.Token.idt)
                {
                    TableEntry argument = symbol_table.Lookup(lex_analyzer.lexeme);
                    CheckIfEmpty(argument);
                    CheckArgumentType(argument, parameter);
                    if(parameter.pass_type == TableEntry.PassType.passByValue)
                    {
                        EmitCode(FormatTAC("push", lex_analyzer.lexeme));
                    }
                    else
                    {
                        EmitCode(FormatTAC("push", "@" + lex_analyzer.lexeme));
                    }
                    match(LexicalAnalyzer.Token.idt);
                    ParamsTail();
                }
                else if (lex_analyzer.token == LexicalAnalyzer.Token.intt)
                {
                    EmitCode(FormatTAC("Push", lex_analyzer.lexeme));
                    match(LexicalAnalyzer.Token.intt);
                    ParamsTail();
                }
                else if (lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
                {
                    EmitCode(FormatTAC("Push", lex_analyzer.lexeme));
                    match(LexicalAnalyzer.Token.decimalt);
                    ParamsTail();
                }

            }
            else
            {
                Lambda();
            }
        }


        public void ParamsTail()
        {
            parameter_depth++;
            TableEntry.ParameterNode parameter;

            try
            {
                parameter = current_procedure.entry_information.function.paramter_list.ElementAt(parameter_depth);
            }
            catch (ArgumentOutOfRangeException)
            {
                parameter = null;
            }

            if (lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);

                if (lex_analyzer.token == LexicalAnalyzer.Token.idt || lex_analyzer.token == LexicalAnalyzer.Token.intt || lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
                {
                    if (parameter == null)
                    {
                        Console.WriteLine("Error at {0},{1}: At least {2} arguments were found inside of a call to '{3}' which only supports {4} arguments!",
                            lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, current_procedure.entry_information.function.number_of_parameters + 1, current_procedure.lexeme, current_procedure.entry_information.function.number_of_parameters);
                        Console.Write("\nPress a key to exit... ");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }

                    if (lex_analyzer.token == LexicalAnalyzer.Token.idt)
                    {
                        TableEntry argument = symbol_table.Lookup(lex_analyzer.lexeme);
                        CheckIfEmpty(argument);
                        CheckArgumentType(argument, parameter);
                        if (parameter.pass_type == TableEntry.PassType.passByValue)
                        {
                            EmitCode(FormatTAC("push", lex_analyzer.lexeme));
                        }
                        else
                        {
                            EmitCode(FormatTAC("push", "@" + lex_analyzer.lexeme));
                        }
                        match(LexicalAnalyzer.Token.idt);
                        ParamsTail();
                    }
                    else if (lex_analyzer.token == LexicalAnalyzer.Token.intt)
                    {
                        if(parameter.pass_type == TableEntry.PassType.passByReference)
                        {
                            Console.WriteLine("Error at {0},{1}: The numeric literal {2} as found in the call to '{3}' which expects a pass by reference.",
                                lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme, current_procedure.lexeme);
                            Console.WriteLine("Numeric literals can not be passed by reference!");
                            Console.Write("\nPress a key to exit... ");
                            Console.ReadKey();
                            Environment.Exit(-1);
                        }
                        else
                        {
                            EmitCode(FormatTAC("Push", lex_analyzer.lexeme));
                            match(LexicalAnalyzer.Token.intt);
                            ParamsTail();
                        }
                    }
                    else if (lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
                    {
                        if (parameter.pass_type == TableEntry.PassType.passByReference)
                        {
                            Console.WriteLine("Error at {0},{1}: The numeric literal {2} as found in the call to '{3}' which expects a pass by reference.",
                                lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme, current_procedure.lexeme);
                            Console.WriteLine("Numeric literals can not be passed by reference!");
                            Console.Write("\nPress a key to exit... ");
                            Console.ReadKey();
                            Environment.Exit(-1);
                        }
                        else
                        {
                            EmitCode(FormatTAC("Push", lex_analyzer.lexeme));
                            match(LexicalAnalyzer.Token.decimalt);
                            ParamsTail();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error at {0},{1}: the lexeme '{2}' was found when expecting a parameter (identifier, integer, or real)!",
                            lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                        Console.Write("\nPress a key to exit... ");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }

                }
            }
            else
            {
                Lambda();
            }
        }

        /* Function: Lambda
        * Description: Function that is a place holder for better readability when looking at the code
        */
        private void Lambda()
        {
            return;
        }

        /* Function: match
        * Description: Function to check if the current token is a certain token that is inputed.
        *              If the token is not correct, the program prints out an error of with the lexeme and the start
        *              of the token.
        * Inputs: token enumerated value to check with the current token
        */
        private void match(LexicalAnalyzer.Token token)
        {
            if(lex_analyzer.token == token)
            {
                lex_analyzer.GetNextToken();
                return;
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting {3} token", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme, token);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        /* Function: matchLexeme
        * Description: Function to check if the current lexeme is a certain lexeme that is inputed.
        *              If the lexeme is not correct, the program prints out an error of with the lexeme and the start
        *              of the token.
        * Inputs: lexeme string value to check with the current lexeme
        */
        private void matchLexeme(string lexeme)
        {
            if (lex_analyzer.lexeme == lexeme)
            {
                lex_analyzer.GetNextToken();
                return;
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting '{3}'", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme, lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }
        }

        private string ConvertVar(TableEntry var)
        {
            // If the variable is further then depth 1, we need to convert it to the BP stack offset.
            if(var.symbol_depth > 1)
            {
                if(var.type_of_entry == TableEntry.EntryType.varEntry)
                {
                    // If the variable is a parameter in a function.
                    if (var.entry_information.variable.is_parameter)
                    {
                        current_procedure = prog_procedures.Peek();

                        if(var.entry_information.variable.pass_type == TableEntry.PassType.passByValue)
                        {
                            return "_bp+" + (current_procedure.entry_information.function.size_of_params - var.entry_information.variable.offset - var.entry_information.variable.size + 4).ToString();
                        }
                        else
                        {
                            return "@_bp+" + (current_procedure.entry_information.function.size_of_params - var.entry_information.variable.offset - var.entry_information.variable.size + 4).ToString();
                        }
                    }
                    else
                    {
                        return "_bp-" + var.entry_information.variable.offset;
                    }
                }
                else if(var.type_of_entry == TableEntry.EntryType.constEntry)
                {
                    return "_bp-" + var.entry_information.constant.offset;
                }
            }
            // If the variable is in depth 1, then we can use the global data segment.
            // This means that we can just use the actual symbol lexeme.
            else
            {
                return var.lexeme;
            }

            return " ";

        }

        private void CalculateTempSize(ref TableEntry temp_var, TableEntry var1, TableEntry var2)
        {
            int var1_size = 0;
            TableEntry.VarType var1_type = TableEntry.VarType.intType;

            int var2_size = 0;
            TableEntry.VarType var2_type = TableEntry.VarType.intType;

            temp_var.type_of_entry = TableEntry.EntryType.varEntry;

            if (var1.type_of_entry == TableEntry.EntryType.varEntry)
            {
                var1_size = var1.entry_information.variable.size;
                var1_type = var1.entry_information.variable.type_of_variable;
            }
            else if(var1.type_of_entry == TableEntry.EntryType.constEntry)
            {
                var1_size = var1.entry_information.constant.size;
                var1_type = var1.entry_information.constant.type_of_constant;
            }
            else
            {
                Console.WriteLine("ERROR in calculate temp size!");
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            if (var2.type_of_entry == TableEntry.EntryType.varEntry)
            {
                var2_size = var2.entry_information.variable.size;
                var2_type = var2.entry_information.variable.type_of_variable;
            }
            else if (var2.type_of_entry == TableEntry.EntryType.constEntry)
            {
                var2_size = var2.entry_information.constant.size;
                var2_type = var2.entry_information.constant.type_of_constant;
            }
            else
            {
                Console.WriteLine("ERROR in calculate temp size!");
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            if (var1_size > var2_size)
            {
                temp_var.entry_information.variable.size = var1_size;
            }
            else
            {
                temp_var.entry_information.variable.size = var2_size;
            }

            switch (var1_type)
            {
                case TableEntry.VarType.charType:
                    switch (var2_type)
                    {
                        case TableEntry.VarType.charType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.charType;
                            break;
                        case TableEntry.VarType.intType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                            break;
                        case TableEntry.VarType.floatType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                            break;
                        default:
                            break;
                    }
                    break;
                case TableEntry.VarType.intType:
                    switch (var2_type)
                    {
                        case TableEntry.VarType.charType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                            break;
                        case TableEntry.VarType.intType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.intType;
                            break;
                        case TableEntry.VarType.floatType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                            break;
                        default:
                            break;
                    }
                    break;
                case TableEntry.VarType.floatType:
                    switch (var2_type)
                    {
                        case TableEntry.VarType.charType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                            break;
                        case TableEntry.VarType.intType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                            break;
                        case TableEntry.VarType.floatType:
                            temp_var.entry_information.variable.type_of_variable = TableEntry.VarType.floatType;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            temp_var.symbol_depth = current_depth; temp_var.entry_information.variable.is_parameter = false;
            temp_var.entry_information.variable.offset = current_offset;
            current_offset += temp_var.entry_information.variable.size;
        }

        private void CheckArgumentType(TableEntry argument, TableEntry.ParameterNode parameter)
        {
            if(argument.type_of_entry == TableEntry.EntryType.constEntry)
            {
                if(parameter.pass_type == TableEntry.PassType.passByReference)
                {
                    Console.WriteLine("Error at {0},{1}: the constant '{2}' was found as a argument to a call to '{3}' which requires a pass by reference.  Constants can not be passed by reference!",
                        lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, argument.lexeme, current_procedure.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
        }

        private void ChangeTempFactor(TableEntry tmp, TableEntry fac1, TableEntry fac2)
        {
            switch (fac1.type_of_entry)
            {
                case TableEntry.EntryType.constEntry:
                    break;
                case TableEntry.EntryType.varEntry:
                    break;
                case TableEntry.EntryType.functionEntry:
                    Console.WriteLine("Error at line {0}: the procedure '{2}' was found in an arithmetic expression, which is not allowed!",
                        lex_analyzer.token_start.line + 1, fac1.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    break;
                case TableEntry.EntryType.moduleEntry:
                    break;
                default:
                    break;
            }
        }

        //private void ProcedureInArithmeticExpression(TableEntry entry)
        //{
        //    Console.WriteLine("Error at line {0}: the procedure '{2}' was found in an arithmetic expression, which is not allowed!",
        //                lex_analyzer.token_start.line + 1, fac1.lexeme);
        //    Console.Write("\nPress a key to exit... ");
        //    Console.ReadKey();
        //    Environment.Exit(-1);
        //}

        private TableEntry NewTemp()
        {
            string temp = "_t" + i.ToString();
            symbol_table.Insert(temp, LexicalAnalyzer.Token.idt, current_depth, lex_analyzer);

            TableEntry entry = symbol_table.Lookup(temp);
            entry.type_of_entry = TableEntry.EntryType.varEntry;
            entry.entry_information.variable.offset = current_offset;

            i++;
            return entry;
        }

        private void EmitCode(string code)
        {
            three_address_code.Add(code);
        }


        private void PrintHeader()
        {
            Console.WriteLine("{0, -10} {1, -1} {2, -10} {3, -1} {4, -10}", "REG/VAR", " ", "REG/VAR", "OP", "REG/VAR");
        }

        private string FormatTAC(string id1 = " ", string equals = " ", string id2= " ", string op = " ", string id3 = " ")
        {
            return String.Format("{0, -10} {1, -5} {2, -10} {3, -5} {4, -10}", id1, equals, id2, op, id3);
        }


        private void OutputThreeAddressCode()
        {
            // Convert the list of three address code statements that we have to an array
            // and print them to the TAC file.
            string[] tac_lines = three_address_code.ToArray();
            System.IO.File.WriteAllLines(tac_path, tac_lines);

            int line_number = 1;
            Console.WriteLine("\nThree Address Code:");
            Console.WriteLine("-------------------");

            foreach (string line in tac_lines)
            {
                if (line_number % 20 == 0)
                {
                    Console.WriteLine("\nPress any continue to continue...");
                    Console.ReadKey();
                    Console.WriteLine("\nMore Three Address Code:");
                    Console.WriteLine("------------------------");
                }

                Console.WriteLine(line);
                line_number++;
            }

            Console.WriteLine("\n\nTAC has been written to: " + tac_path.Replace("../", ""));
        }

        private void PrintTACHeader()
        {
            Console.WriteLine("\n{0, -10} {1, -5} {2, -10} {3, -5} {4, -10}", "Variable", "", "operand", "operation", "operand");
            Console.WriteLine("----------------------------------------------------------------------------------------------------------");
        }
    }
}
