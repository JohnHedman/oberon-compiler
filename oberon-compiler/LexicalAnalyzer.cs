using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oberon_compiler
{
    class LexicalAnalyzer
    {
        // Enumerated type for reserved words.
        enum ReservedWords { MODULE, PROCEDURE, VAR, BEGIN, END, IF, THEN, ELSE, ELSIF, WHILE, DO, ARRAY, RECORD, CONST, TYPE };

        // Arrays for alphabet and numerics (maybe could use a library instead
        char[] alpha_chars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        char[] num_chars = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        string[] reserved_words = { "MODULE", "PROCEDURE", "VAR", "BEGIN", "END", "IF", "THEN", "ELSE", "ELSEIF", "WHILE", "DO", "ARRAY", "RECORD", "CONST", "TYPE" };
        char[] operators_chars = { '=', '#', '<', '>', '*', '/', '&', '+', '-', ':' };

        // Global Variables for scanner
        public string token;
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
        char? current_char = ' ';


        // Constructor
        public LexicalAnalyzer(string oberon_file)
        {
            oberon_lines = System.IO.File.ReadAllLines(oberon_file);
            file_length = oberon_lines.Length;
            line_length = oberon_lines[0].Length;
            //ResetGlobalVariables();
        }

        public void GetNextToken()
        {
            // While the characters we are not whitespace
            while(isCurrentCharWhiteSpace() && token != "eoft")
            {
                GetNextChar();
            }
            if(token == "eoft")
            {
                return;
            }
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

        }

        private void GetNextChar()
        {
            // If we are out of characters in a line, go to the next
            if(char_index == line_length)
            {
                line_index++;

                // If we are out of lines with the file, they set the token to EOF and 
                // current character to whitepace and return
                if(line_index == file_length)
                {
                    token = "eoft";
                    current_char = null;
                    return;
                }

                char_index = 0;
                line_length = oberon_lines[line_index].Length;
            }

            current_char = oberon_lines[line_index][char_index];
            char_index++;
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

        static int ConvertToASCII(char? char_to_convert)
        {
            return char_to_convert;
        }


        //private void ResetGlobalVariables()
        //{
        //    token = "";
        //    lexeme = "";
        //    literal = "";
        //}
    }
}
