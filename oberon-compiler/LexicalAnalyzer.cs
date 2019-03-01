using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{

    // Class for Lexical Analyzer and scanner
    class LexicalAnalyzer
    {
        // Enumerated type for reserved words.
        public enum ReservedWords { MODULE, PROCEDURE, VAR, BEGIN, END, IF, THEN, ELSE, ELSIF, WHILE, DO, ARRAY, RECORD, CONST, TYPE, INTEGER, REAL, CHAR  };

        // Enumerated type for the different tokens that are possible.
        public enum Token
        {
            // Reserved words
            modulet, proceduret, vart, begint, endt, ift, thent, elset, elsift, whilet, dot, arrayt, recordt, constt, typet, integert, realt, chart,

            relopt, addopt, mulopt, assignopt, periodt, lpart, rpart, lbrackt, rbrackt, lsqbrackt, rsqbrackt, commat, semicolt, colt, gravet, tildet,

            stringt, intt, decimalt, idt,

            eoft, unknownt, errort
        };

        // Arrays for alphabet and numerics, and reserved words
        char[] alpha_chars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        char[] num_chars = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        string[] reserved_words = { "MODULE", "PROCEDURE", "VAR", "BEGIN", "END", "IF", "THEN", "ELSE", "ELSIF", "WHILE", "DO", "ARRAY", "RECORD", "CONST", "TYPE", "INTEGER", "REAL", "CHAR" };
        string[] mulop_words = { "DIV", "MOD" };
        string[] addop_words = { "OR" };
        char[] operators_chars = { '=', '#', '<', '>', '*', '/', '&', '+', '-', ':' };

        // Global Variables for scanner
        public Token token;
        public string lexeme;
        public int value;
        public double valueR;
        public string literal;
        public (int line, int character) token_start;   //token_start for outputing errors to user.
        public (int line, int character) token_end;

        // Private variables
        string[] oberon_lines;
        int file_length;
        int line_length;
        int line_index = 0;
        int char_index = 0;
        char current_char = ' ';
        char next_char = ' ';
        


        // Constructor
        public LexicalAnalyzer(string oberon_file)
        {
            oberon_lines = System.IO.File.ReadAllLines(oberon_file);
            Array.Resize(ref oberon_lines, oberon_lines.Length + 1);
            oberon_lines[oberon_lines.Length - 1] = " ";
            file_length = oberon_lines.Length;
            line_length = oberon_lines[0].Length;
            GetNextChar();  // Construct the object so that it has a current_char
        }

        //Function: DisplayToken
        //Purpose:  Prints the token in a specified format based on the type of token
        public void DisplayToken()
        {
            string input_lines = oberon_lines[token_start.line].TrimEnd().Replace("\t", "    ");
            if(input_lines.Length > 50)
            {
                input_lines.Substring(0, 50);
            }
            if (token == Token.intt)
            {
                Console.WriteLine("{0, -50} {1, -11} {2, -17} {3, -20}", input_lines, token, lexeme, value);
            }
            else if(token == Token.decimalt)
            {
                Console.WriteLine("{0, -50} {1, -11} {2, -17} {3, -20}", input_lines, token, lexeme, valueR);
            }
            else if(token == Token.stringt)
            {
                Console.WriteLine("{0, -50} {1, -11} {2, -17} {3, -20}", input_lines, token, "\"...\"", literal);
            }
            else if(token == Token.errort)
            {
                return;
            }
            else if(token == Token.eoft)
            {
                Console.WriteLine("{0, -50} {1, -11} {2, -17} {3, -20}", " ", token, lexeme, " ");
            }
            else
            {
                Console.WriteLine("{0, -50} {1, -11} {2, -17} {3, -20}", input_lines, token, lexeme, " ");
            }
            
        }

        //Function: GetNextToken
        //Purpose:  Gets the next token from the input file and sets it as the public variable
        public void GetNextToken()
        {
            // Skip over the whitespace until we find the beginning of the next token
            while(isCurrentCharWhiteSpace() && token != Token.eoft)
            {
                GetNextChar();
            }
            if(token == Token.eoft)
            {
                return;
            }
            // We have found the beginning character of the next token, let's processes it!
            else
            {
                ProcessToken();
            }
        }

        //Function: ProcessToken
        //Purpose:  Takes the first few symbols of a token and figures out which type of token
        //          it can be.
        private void ProcessToken()
        {
            token_start = (line_index, char_index - 1);  // Keep track of where the token started for logging and errors
            lexeme = current_char.ToString();
            GetNextChar();
            lexeme += current_char;

            if (alpha_chars.Contains(lexeme[0]))
            {
                ProcessWordToken();
            }
            else if(num_chars.Contains(lexeme[0]))
            {
                ProcessNumToken();
            }
            else if(lexeme[0] == '"' || lexeme[0] == '\'')
            {
                ProcessStringLiteral();
                // Load the next character so GetNextToken doesn't read the ')' comment character
                GetNextChar();
            }
            else if(lexeme == "(*" )
            {
                ProcessComment();
                GetNextChar();
                // Get the next token since comments are whitespace.
                GetNextToken();
            }
            else if(operators_chars.Contains(lexeme[0]))
            {
                if(lexeme == "<=" || lexeme == ">=" || lexeme == ":=")
                {
                    ProcessDoubleToken();
                    GetNextChar();
                }
                else
                {
                    ProcessSingleToken();
                }
            }
            else
            {
                ProcessSingleToken();
            }
        }

        //Function: ProcessWordToken
        //Purpose:  Processes the word type tokens and figures out exactly what token it is.
        //          Sets the token type as public variable.
        private void ProcessWordToken()
        {
            lexeme = lexeme[0].ToString();

            while (Char.IsLetterOrDigit(current_char))
            {
                lexeme += current_char;
                GetNextChar();
            }

            // We now have our lexeme, let's check to see if it is the right size!
            if(lexeme.Length > 17)
            {
                Console.WriteLine("Error at {0},{1}: the identifier token '{2}' can only be 17 characters long!", token_start.line+1, token_start.character+1, lexeme);
                token = Token.errort;
                return;
            }

            // Now, let's check if it is a mulop token.
            // Note: all our reserved words are case sensitive, don't upper case the string.
            if (mulop_words.Contains(lexeme))
            {
                token = Token.mulopt;
            }
            // Um, maybe it is a addopt?
            else if(addop_words.Contains(lexeme))
            {
                token = Token.addopt;
            }
            // Darn, maybe a reserved word!?
            else if(reserved_words.Contains(lexeme))
            {
                // Add 't' to the end of the lexeme and convert it to its enumerated value.
                Enum.TryParse((lexeme + 't').ToLower(), out token);
            }
            // Oh, it's got to be a identifier, right? (Hint: it does)
            else
            {
                token = Token.idt;
            }
        }

        //Function: ProcessNumToken
        //Purpose:  Process integer and decimal tokens
        private void ProcessNumToken()
        {
            lexeme = lexeme[0].ToString();

            // While we are still getting numbers, added them to the lexeme
            while(Char.IsNumber(current_char))
            {
                lexeme += current_char;
                GetNextChar();
            }

            // If we have a decimal after our number
            if(current_char == '.')
            {
                // Check to see if the next character is a number or the lexeme is invalid
                if(Char.IsNumber(PeekNextChar()))
                {
                    GetNextChar();
                    token = Token.decimalt;
                    lexeme += ".";
                }
                else
                {
                    token = Token.intt;
                    value = int.Parse(lexeme);
                    return;
                }

                // Again, while we are still getting numbers, added them to the lexeme
                while (Char.IsNumber(current_char))
                {
                    lexeme += current_char.ToString();
                    GetNextChar();
                }

                valueR = double.Parse(lexeme);
            }
            else
            {
                token = Token.intt;
                value = int.Parse(lexeme);
            }


        }

        //Function: ProcessComment
        //Purpose:  Process tokens that are comments
        private void ProcessComment()
        {
            // Keep track of the last two characters until we find the closing comment token.
            string temp_string = "  ";
            while(temp_string != "*)")
            {
                GetNextChar();
                temp_string = temp_string[1].ToString() + current_char.ToString();
                
                // We support nested comments, recursively call the ProcessComment function if we find another.
                if(temp_string == "(*")
                {
                    ProcessComment();
                }
            }
        }

        //Function: ProcessStringLiteral
        //Purpose:  Process tokens that are string literals
        private void ProcessStringLiteral()
        {
            // Keep track of what character started the literal
            char literal_character = lexeme[0];
            lexeme = lexeme[0].ToString();

            while(current_char != literal_character)
            {
                if(current_char == '\n')
                {
                    Console.WriteLine("Error at {0},{1}: newline encountered when expecting end of string literal", token_start.line+1, token_start.character+1);
                    token = Token.errort;
                    return;
                }
                else if(current_char == '\0')
                {
                    Console.WriteLine("Error at {0},{1}: end of file encountered when expecting end of string literal", token_start.line+1, token_start.character+1);
                    token = Token.errort;
                    return;
                }
                lexeme += current_char;
                GetNextChar();
            }

            token = Token.stringt;
            lexeme += current_char;

            literal = lexeme;
        }

        //Function: ProcessSingleToken
        //Purpose:  Process predefined tokens with a single character
        private void ProcessSingleToken()
        {
            // Only a single token, so lets just use the first character 
            lexeme = lexeme[0].ToString();

            switch (lexeme)
            {
                case "=":
                case "#":
                case "<":
                case ">":
                    token = Token.relopt;
                    break;
                case "+":
                case "-":
                    token = Token.addopt;
                    break;
                case "*":
                case "/":
                case "&":
                    token = Token.mulopt;
                    break;
                case "(":
                    token = Token.lpart;
                    break;
                case ")":
                    token = Token.rpart;
                    break;
                case "{":
                    token = Token.lbrackt;
                    break;
                case "}":
                    token = Token.rbrackt;
                    break;
                case "[":
                    token = Token.lsqbrackt;
                    break;
                case "]":
                    token = Token.rsqbrackt;
                    break;
                case ":":
                    token = Token.colt;
                    break;
                case ";":
                    token = Token.semicolt;
                    break;
                case ".":
                    token = Token.periodt;
                    break;
                case ",":
                    token = Token.commat;
                    break;
                case "`":
                    token = Token.gravet;
                    break;
                case "~":
                    token = Token.tildet;
                    break;
                default:
                    token = Token.unknownt;
                    break;
            }
        }

        //Function: ProcessSingleToken
        //Purpose:  Process predefined tokens with two characters
        private void ProcessDoubleToken()
        {
            switch (lexeme)
            {
                case "<=":
                case ">=":
                    token = Token.relopt;
                    break;
                case ":=":
                    token = Token.assignopt;
                    break;
            }
        }

        //Function: PeekNextChar
        //Purpose:  Look at the next character in the file without changing the current character.
        private char PeekNextChar()
        {
            if(char_index == line_length)
            {
                return '\n';
            }
            else
            {
                return oberon_lines[line_index][char_index];
            }
        }

        //Function: GetNextChar
        //Purpose:  Get the next character in the input file and set the
        //          scanner's index to point to the character
        private void GetNextChar()
        {
            // If we are out of characters in a line, go to the next
            if(char_index == line_length)
            {
                GetNextLine();
            }
            else
            {
                // Read next character and increment so we know which character on the line we should read next.
                current_char = oberon_lines[line_index][char_index];
                char_index++;
            }

            return;
        }

        //Function: GetNextLine
        //Purpose:  Function to go to the next line in the input file
        private void GetNextLine()
        {
            // Increment what line we are on and update character index and line length.
            line_index++;
            char_index = 0;

            // If we are out of lines with the file, then set the token to EOF and 
            // current character to the null character and return
            if (line_index == file_length)
            {
                token = Token.eoft;
                lexeme = "";
                current_char = '\0';
                return;
            }

            line_length = oberon_lines[line_index].Length;

            //if(line_length != 0)
            //{
            //    Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            //    Console.WriteLine("{0}", oberon_lines[line_index]);

            //}

            // Set the current char to newline so Processes know when to cut tokens.
            current_char = '\n';
            return;
        }
        //Function: isCurrentCharWhiteSpace
        //Purpose:  Returns if the current char is white space based on if it has a higher ascii value
        // then the space key.
        private bool isCurrentCharWhiteSpace()
        {
            if(ConvertToASCII(current_char) > 33)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //Function: ConvertToASCII
        //Purpose:  Returns the integer representation of the character that is inputed in the function.
        public static int ConvertToASCII(char char_to_convert)
        {

            return (int)char_to_convert;
        }
    }
}
