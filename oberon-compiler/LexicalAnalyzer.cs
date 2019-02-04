using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{
    class LexicalAnalyzer
    {
        // Enumerated type for reserved words.
        public enum ReservedWords { MODULE, PROCEDURE, VAR, BEGIN, END, IF, THEN, ELSE, ELSIF, WHILE, DO, ARRAY, RECORD, CONST, TYPE };

        // Enumerated type for the different tokens that are possible.
        public enum Token
        {
            // Reserved words
            modulet, proceduret, vart, begint, endt, ift, thent, elset, elseift, whilet, dot, arrayt, recordt, constt, typet,

            stringt, relopt, addopt, mulopt, assignopt,

            eoft, unknown
        };

        // Arrays for alphabet and numerics, and reserved words
        char[] alpha_chars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        char[] num_chars = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        string[] reserved_words = { "MODULE", "PROCEDURE", "VAR", "BEGIN", "END", "IF", "THEN", "ELSE", "ELSEIF", "WHILE", "DO", "ARRAY", "RECORD", "CONST", "TYPE" };
        string[] mulop_words = { "DIV", "MOD" };
        string[] addop_words = { "OR" };
        char[] operators_chars = { '=', '#', '<', '>', '*', '/', '&', '+', '-', ':' };

        // Global Variables for scanner
        public Token token;
        public string lexeme;
        public int value;
        public double valueR;
        public string literal;

        // Private variables
        string[] oberon_lines;
        int file_length;
        int line_length;
        int line_index = 0;
        int char_index = 0;
        char current_char = ' ';
        char next_char = ' ';
        (int line, int character) token_start;   //token_start for outputing errors to user.
        (int line, int character) token_end;


        // Constructor
        public LexicalAnalyzer(string oberon_file)
        {
            oberon_lines = System.IO.File.ReadAllLines(oberon_file);
            file_length = oberon_lines.Length;
            line_length = oberon_lines[0].Length;
            GetNextChar();
        }

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

        public void DisplayToken()
        {

        }

        private void ProcessToken()
        {
            token_start = (line_index, char_index - 1);
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
            else if(lexeme == "(*" )
            {
                ProcessComment();
            }
            else if(operators_chars.Contains(lexeme[0]))
            {
                if(lexeme == "<=" || lexeme == ">=" || lexeme == ":=")
                {
                    ProcessDoubleToken();
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
                Console.WriteLine("Error starting at {0},{1}: the lexeme '{2}' is too long.\n" +
                                  "    Identifier tokens can only be 17 characters long!", token_start.line, token_start.character, lexeme);
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
                Enum.TryParse((lexeme + 't').ToLower(), out Token this_token);
            }
            // Oh, it's got to be a identifier, right? (Hint: it does)
            else
            {
                token = Token.ift;
            }
        }

        private void ProcessNumToken()
        {

        }

        private void ProcessComment()
        {

        }

        private void ProcessDoubleToken()
        {

        }

        private void ProcessSingleToken()
        {

        }

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

        // Function to handle going to the next Oberon file line.
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
                current_char = '\0';
                return;
            }

            line_length = oberon_lines[line_index].Length;

            // Set the current char to newline so Processes know when to cut tokens.
            current_char = '\n';
            return;
        }

        // Returns if the current char is white space based on if it has a higher ascii value
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

        public static int ConvertToASCII(char char_to_convert)
        {

            return (int)char_to_convert;
        }
    }
}
