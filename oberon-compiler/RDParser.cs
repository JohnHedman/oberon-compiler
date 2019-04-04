using System;
using System.Collections.Generic;
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
        private List<string> identifier_list = new List<string>();
        private Stack<TableEntry> prog_procedures = new Stack<TableEntry>();
        private int current_offset = 0;
        private TableEntry.EntryType current_entry_type;
        private TableEntry.PassType current_passing_type;

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
            Console.WriteLine("\nFile successfully parsed!");
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
                    current_offset = 0;

                    prog_procedures.Push(current_node);

                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.semicolt);

                    current_depth++;

                    DeclarativePart();
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

            // If the current entry is a variable, then we know all the identifiers in the list are variables.
            if (current_entry_type == TableEntry.EntryType.varEntry)
            {
                // Pop the current procedure off the stack so we can update its information.
                TableEntry current_procedure = prog_procedures.Pop();

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

                    // Let's debug!!!
                    //Console.WriteLine("Added " + identifier + " as variable");
                }

                // Push the current procedure back on the stack.
                prog_procedures.Push(current_procedure);
            }
            // If the current entry is a function, then we know all the identifiers in the list are arguments.
            else if(current_entry_type == TableEntry.EntryType.functionEntry)
            {
                // Pop the current procedure off the stack so we can update its information.
                TableEntry current_procedure = prog_procedures.Pop();

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
                    current_offset += var_size;

                    TableEntry.ParameterNode node = new TableEntry.ParameterNode();
                    node.type_of_parameter = var_type;
                    node.pass_type = current_passing_type;
                    current_procedure.entry_information.function.paramter_list.AddLast(node);
                    current_procedure.entry_information.function.number_of_parameters += 1;
                    current_procedure.entry_information.function.size_of_params += var_size;

                    // Let's Debug!
                    //Console.WriteLine("Added " + identifier + " as a parameter");
                }

                prog_procedures.Push(current_procedure);
            }
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
            StatementPart();
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
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                Statement();
                match(LexicalAnalyzer.Token.semicolt);
                StatTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: StatTail
         * Description: Implements the following CFG rule:
         *              StatTail	-> Statement ; StatTail | Lambda
         */
        private void StatTail()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                Statement();
                match(LexicalAnalyzer.Token.semicolt);
                StatTail();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: Statement
         * Description: Implements the following CFG rule:
         *              Statement	-> AssignStat	| IOStat
         */
        private void Statement()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                AssignStat();
            }
            else
            {
                IOStat();
            }
        }

        /* Function: AssignStat
         * Description: Implements the following CFG rule:
         *              AssignStat	->	idt := Expr
         */
        private void AssignStat()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                // Look up the identifier in the symbol table and check to see if it has been declared.
                current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                checkIfEmpty(current_node);

                if(current_node.type_of_entry == TableEntry.EntryType.functionEntry)
                {
                    ProcCall();
                }
                else if(current_node.type_of_entry == TableEntry.EntryType.varEntry)
                {
                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.assignopt);
                    Expr();
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
         *              IOStat	->	Lambda
         */
        private void IOStat()
        {
            Lambda();
        }

        /* Function: Expr
         * Description: Implements the following CFG rule:
         *              Expr	->	Relation
         */
        private void Expr()
        {
            Relation();
        }

        /* Function: Relation
         * Description: Implements the following CFG rule:
         *              Relation	->	SimpleExpr
         */
        private void Relation()
        {
            SimpleExpr();
        }


        /* Function: SimpleExpr
         * Description: Implements the following CFG rule:
         *              SimpleExpr	->	Term MoreTerm
         */
        private void SimpleExpr()
        {
            Term();
            MoreTerm();
        }

        /* Function: MoreTerm
         * Description: Implements the following CFG rule:
         *              MoreTerm	->	Addop Term MoreTerm | Lambda
         */
        private void MoreTerm()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.addopt)
            {
                match(LexicalAnalyzer.Token.addopt);
                Term();
                MoreTerm();
            }
            else
            {
                Lambda();
            }
        }

        /* Function: Term
         * Description: Implements the following CFG rule:
         *              Term	->	Factor MoreFactor
         */
        private void Term()
        {
            Factor();
            MoreFactor();
        }

        /* Function: MoreFactor
         * Description: Implements the following CFG rule:
         *              MoreFactor	-> Mulop Factor MoreFactor | Lambda
         */
        private void MoreFactor()
        {
            if (lex_analyzer.token == LexicalAnalyzer.Token.mulopt)
            {
                match(LexicalAnalyzer.Token.mulopt);
                Factor();
                MoreFactor();
            }
            else
            {
                Lambda();
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
        private void Factor()
        {
            switch (lex_analyzer.token)
            {
                case LexicalAnalyzer.Token.idt:
                    // Look up the identifier in the symbol table and check to see if it has been declared.
                    current_node = symbol_table.Lookup(lex_analyzer.lexeme);
                    checkIfEmpty(current_node);

                    match(LexicalAnalyzer.Token.idt);
                    break;

                case LexicalAnalyzer.Token.intt:
                    match(LexicalAnalyzer.Token.intt);
                    break;

                case LexicalAnalyzer.Token.decimalt:
                    match(LexicalAnalyzer.Token.decimalt);
                    break;

                case LexicalAnalyzer.Token.lpart:
                    match(LexicalAnalyzer.Token.lpart);
                    Expr();
                    match(LexicalAnalyzer.Token.rpart);
                    break;

                case LexicalAnalyzer.Token.tildet:
                    match(LexicalAnalyzer.Token.tildet);
                    Factor();
                    break;

                default:
                    if(lex_analyzer.lexeme == "-")
                    {
                        SignOp();
                        Factor();
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
        public void checkIfEmpty(TableEntry node)
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
            match(LexicalAnalyzer.Token.idt);
            match(LexicalAnalyzer.Token.lpart);
            Params();
            match(LexicalAnalyzer.Token.rpart);
        }


        public void Params()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.idt)
            {
                match(LexicalAnalyzer.Token.idt);
                ParamsTail();
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.intt)
            {
                match(LexicalAnalyzer.Token.intt);
                ParamsTail();
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
            {
                match(LexicalAnalyzer.Token.decimalt);
                ParamsTail();
            }
            else
            {
                Lambda();
            }
        }


        public void ParamsTail()
        {
            if(lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);

                if (lex_analyzer.token == LexicalAnalyzer.Token.idt)
                {
                    match(LexicalAnalyzer.Token.idt);
                    ParamsTail();
                }
                else if (lex_analyzer.token == LexicalAnalyzer.Token.intt)
                {
                    match(LexicalAnalyzer.Token.intt);
                    ParamsTail();
                }
                else if(lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
                {
                    match(LexicalAnalyzer.Token.decimalt);
                    ParamsTail();
                }
                else
                {
                    Console.WriteLine("Error at {0},{1}: the lexeme '{2}' was found when expecting a parameter (identifier, integer, or real number)!", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
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
    }
}
