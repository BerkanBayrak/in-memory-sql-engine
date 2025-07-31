using System;
using System.Collections.Generic;
using SqlEngine.Utils;

namespace SqlEngine.Utils
{
    public class Tokenizer
    {
        private readonly string _source;
        private int _start = 0;
        private int _current = 0;

        private static readonly Dictionary<string, TokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["select"] = TokenType.Select,
            ["from"] = TokenType.From,
            ["where"] = TokenType.Where,
            ["join"] = TokenType.Join,
            ["inner"] = TokenType.Inner,
            ["left"] = TokenType.Left,
            ["right"] = TokenType.Right,
            ["full"] = TokenType.Full,
            ["outer"] = TokenType.Outer,
            ["on"] = TokenType.On,
            ["as"] = TokenType.As,
            ["order"] = TokenType.Order,
            ["by"] = TokenType.By,
            ["group"] = TokenType.Group,
            ["having"] = TokenType.Having,
            ["limit"] = TokenType.Limit,
            ["offset"] = TokenType.Offset,
            ["insert"] = TokenType.Insert,
            ["into"] = TokenType.Into,
            ["values"] = TokenType.Values,
            ["update"] = TokenType.Update,
            ["set"] = TokenType.Set,
            ["delete"] = TokenType.Delete,
            ["create"] = TokenType.Create,
            ["table"] = TokenType.Table,
            ["drop"] = TokenType.Drop,
            ["alter"] = TokenType.Alter,
            ["add"] = TokenType.Add,
            ["distinct"] = TokenType.Distinct,
            ["and"] = TokenType.And,
            ["or"] = TokenType.Or,
            ["not"] = TokenType.Not,
            ["in"] = TokenType.In,
            ["is"] = TokenType.Is,
            ["null"] = TokenType.Null,
            ["like"] = TokenType.Like,
            ["between"] = TokenType.Between,
            ["exists"] = TokenType.Exists,
            ["case"] = TokenType.Case,
            ["when"] = TokenType.When,
            ["then"] = TokenType.Then,
            ["else"] = TokenType.Else,
            ["end"] = TokenType.End,
            ["asc"] = TokenType.Asc,
            ["desc"] = TokenType.Desc,
            ["INDEX"] = TokenType.Index,

            ["union"] = TokenType.Union
        };

        public Tokenizer(string source)
        {
            _source = source;
        }
        

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd())
            {
                _start = _current;
                var token = NextToken();
                if (token != null)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(new Token(TokenType.EndOfFile, ""));
            return tokens;
        }


        private Token NextToken()
        {
            char c = Advance();

            if (char.IsWhiteSpace(c))
                return null;

            if (char.IsLetter(c))
                return ReadIdentifierOrKeyword();

            if (char.IsDigit(c))
                return ReadNumber();

            return c switch
            {
                '*' => new Token(TokenType.Asterisk, "*"),
                ',' => new Token(TokenType.Comma, ","),
                '.' => new Token(TokenType.Dot, "."),
                ';' => new Token(TokenType.Semicolon, ";"),
                '=' => new Token(TokenType.Equals, "="),
                '>' => Match('=') ? new Token(TokenType.GreaterEqual, ">=") : new Token(TokenType.GreaterThan, ">"),
                '<' => Match('=') ? new Token(TokenType.LessEqual, "<=") :
                       Match('>') ? new Token(TokenType.NotEqual, "<>") : new Token(TokenType.LessThan, "<"),
                '!' => Match('=') ? new Token(TokenType.NotEqual, "!=") : new Token(TokenType.Unknown, "!"),
                '(' => new Token(TokenType.LeftParen, "("),
                ')' => new Token(TokenType.RightParen, ")"),
                '\'' => ReadString(),
                _ => new Token(TokenType.Unknown, c.ToString())
            };
        }

        private Token ReadIdentifierOrKeyword()
        {
            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                Advance();

            string text = _source[_start.._current];

            if (_keywords.TryGetValue(text, out var type))
                return new Token(type, text);

            return new Token(TokenType.Identifier, text);
        }

        private Token ReadNumber()
        {
            bool hasDot = false;

            while (!IsAtEnd() && (char.IsDigit(Peek()) || (!hasDot && Peek() == '.')))
            {
                if (Peek() == '.')
                    hasDot = true;

                Advance();
            }

            return new Token(TokenType.Number, _source[_start.._current]);
        }

        private Token ReadString()
        {
            while (!IsAtEnd() && Peek() != '\'')
                Advance();

            if (!IsAtEnd()) Advance(); // consume closing '

            return new Token(TokenType.String, _source[(_start + 1)..(_current - 1)]);
        }

        private char Advance() => _source[_current++];

        private bool Match(char expected)
        {
            if (IsAtEnd() || _source[_current] != expected)
                return false;

            _current++;
            return true;
        }

        private char Peek() => IsAtEnd() ? '\0' : _source[_current];

        private bool IsAtEnd() => _current >= _source.Length;
    }
}
