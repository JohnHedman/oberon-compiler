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
        private bool isCorrect;

        /* Function: RDParser
         * Description: Constructor for the RDParser class
         * Input: LexicalAnalyzer to use as a reference for parsing
         */
        public RDParser(LexicalAnalyzer analyzer)
        {
            lex_analyzer = analyzer;
            isCorrect = true;
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
            Console.WriteLine("File successfully parsed!");
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
                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.semicolt);
                    DeclarativePart();
                    StatementPart();
                    match(LexicalAnalyzer.Token.endt);
                    match(LexicalAnalyzer.Token.idt);
                    match(LexicalAnalyzer.Token.periodt);
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
            if(lex_analyzer.token == LexicalAnalyzer.Token.intt)
            {
                match(LexicalAnalyzer.Token.intt);
            }
            else if(lex_analyzer.token == LexicalAnalyzer.Token.decimalt)
            {
                match(LexicalAnalyzer.Token.decimalt);
            }
            else
            {
                Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting numerical literal", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                Console.Write("\nPress a key to exit... ");
                Console.ReadKey();
                Environment.Exit(-1); 
            }

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
            match(LexicalAnalyzer.Token.idt);
            if(lex_analyzer.token == LexicalAnalyzer.Token.commat)
            {
                match(LexicalAnalyzer.Token.commat);
                IdentifierList();
            }
        }

        /* Function: TypeMark
         * Description: Implements the following CFG rule:
         *              TypeMark -> integert | realt | chart  
         */
        private void TypeMark()
        {
            switch (lex_analyzer.token)
            {
                case LexicalAnalyzer.Token.integert:
                    match(LexicalAnalyzer.Token.integert);
                    break;
                case LexicalAnalyzer.Token.realt:
                    match(LexicalAnalyzer.Token.realt);
                    break;
                case LexicalAnalyzer.Token.chart:
                    match(LexicalAnalyzer.Token.chart);
                    break;
                default:
                    Console.WriteLine("Error at {0},{1}: '{2}' was found when expecting variable type (INTEGER, REAL, or CHAR)", lex_analyzer.token_start.line + 1, lex_analyzer.token_start.character + 1, lex_analyzer.lexeme);
                    Console.Write("\nPress a key to exit... ");
                    Console.ReadKey();
                    Environment.Exit(-1);
                    break;
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
        }

        /* Function: ProcHeading
         * Description: Implements the following CFG rule:
         *              ProcHeading -> proct idt Args 
         */
        private void ProcHeading()
        {
            match(LexicalAnalyzer.Token.proceduret);
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
                match(LexicalAnalyzer.Token.vart);
            }
            else
            {
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
         *              SeqOfStatements -> Lambda
         */
        private void SeqOfStatements()
        {
            Lambda();
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
