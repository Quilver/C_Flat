/*
 *	Statements:
 *	<statement>::=<Expression> | <Logic-Statement>

 *	Numerical expressions:
	 *  <Expression>::= <Term> {('+'|'-') <Term>}
	 *	<Term>::= <Value> {('*'|'/') <Value>}
	 *	<Value>::= '('<Expression>')' | <Number> | '-'<Value>

 *	Logical expressions:
	 *	<Logic-Statement>::= <Boolean> {<Condition>}
	 *  <Condition>::= ('==' | '&' | '|') <Boolean>
	 *	<Boolean>::= '!’<Logic-Statement> | 'true' | 'false' | <Expression-Query> | '('<Logic-Statement>')'
	 *	<Expression-Query> ::= <Expression> ('=='|'>'|'<') <expression>
 
 * */

using C_Flat_Interpreter.Common;
using C_Flat_Interpreter.Common.Enums;

namespace C_Flat_Interpreter.Parser;

public class Parser : InterpreterLogger
{
	private TokenType _tokenType;
	private int _currentIndex;
	private int _totalTokens;
	private List<Token> _tokens; //TODO - define max with the group

	//constructor
	public Parser()
	{
		GetLogger("Parser");
	}

	//Helper Functions
	
	private bool CheckBoolLiteral()
	{
		var word = _tokens[_currentIndex].Word;
		if (word.Equals("true") || word.Equals("false")) return true;
		_logger.Error($"Bool parse error! Expected boolean literal, actual: \"{word}\"");
		return false;
	}
	private bool Match(TokenType tokenType)
	{
		if (_tokenType == TokenType.Null) //only on first call,  TODO - find better way to do this that means we don't need to set to null and/or we don't need this check
        {
			_tokenType = _tokens[_currentIndex].Type;
		}

		if (tokenType == _tokenType)
		{
			_logger.Information("Token at index {index} matches {tokenType}", _currentIndex, tokenType);
		}
		else
		{
			_logger.Information("Token at index {index} does NOT MATCH {tokenType}", _currentIndex, tokenType);
			return false;
		}
		return true;
	}

	private void Reset()
	{
		_currentIndex = 0;
		_tokenType = TokenType.Null;
	}
	private void Advance(int level)
	{
		if (++_currentIndex == _totalTokens) return; //todo - might be able to find a way to exit everything else quicker when we're at the end - EOF token!
		_tokenType = _tokens[_currentIndex].Type;
		_logger.Information("advance() called at level {level}. Next token is {@token}", level, _tokens[_currentIndex]);
	}
	
	//End Helper Functions

	public int Parse(List<Token> tokens)
	{
		_tokens = tokens;
		_totalTokens = tokens.Count;
		Reset();
		Statement(0);
		if (_currentIndex >= _totalTokens) return 0;
		_logger.Error("Syntax error! Could not parse Token: {@token}", _tokens[_currentIndex]); //todo - Create test for this!
		return 1;
	}
	
	//EBNF Functions

	private void Statement(int level)
	{
		_logger.Information( "Statement() called at level {level}", level);
		Expression(level+1);
		if(_currentIndex >= _totalTokens) //if we've successfully parsed an expression early exit
			return;
		Reset();
		LogicStatement(level+1);

	}
	private void Expression(int level)
	{
		_logger.Information( "expression() called at level {level}", level);
		Term(level + 1);
		expression_p(level + 1);
	}

	private void expression_p(int level)
	{
		while (true)
		{
			_logger.Information("expression_p() called at level {level}", level);
			if (!Match(TokenType.Add) && !Match(TokenType.Sub)) return; //this is terminal
			Advance(level + 1);
			Term(level + 1);
			level = level + 1;
		}
	}
	
	private void Term(int level)
	{
		_logger.Information( "term() called at level {level} ", level);
		Factor(level + 1);
		term_p(level + 1);
	}

	private void term_p(int level)
	{
		_logger.Information("term_p() called at level {level}", level);
		if (!Match(TokenType.Multi) && !Match(TokenType.Divide)) return; //this is terminal
		Advance(level + 1);
		Factor(level + 1);
		term_p(level + 1);
	}
	
	private void Factor(int level)
	{
		_logger.Information( "factor() called at level {level}", level);
		if (Match(TokenType.Num))
		{
			Advance(level + 1);
		}
		else if (Match(TokenType.Sub))
		{
			Advance(level+1);
			Factor(level+1);
		}
		else if (Match(TokenType.LeftParen))
		{
			Advance(level + 1);
			Expression(level + 1);
			if (Match(TokenType.RightParen)) Advance(level + 1);
			else
			{
				_logger.Error("Syntax Error! Mismatched parentheses at token {index}", _currentIndex);
			}
		}
	}

	private void LogicStatement(int level)
	{
		_logger.Information( "LogicStatement() called at level {level}", level);
		Boolean(level+1);
		Condition(level + 1);
	}

	private void Boolean(int level)
	{
		_logger.Information( "Boolean() called at level {level}", level);

		if (Match(TokenType.Not))
		{
			Advance(level+1);
			LogicStatement(level+1);
		}
		else if (Match(TokenType.String))
		{
			if(CheckBoolLiteral())
				Advance(level+1);
		}
		else if (Match(TokenType.LeftParen))
		{
			Advance(level + 1);
			LogicStatement(level + 1);
			if (Match(TokenType.RightParen)) Advance(level + 1);
			else
			{
				_logger.Error("Syntax Error! Mismatched parentheses at token {index}", _currentIndex);
			}
		}
		else
		{
			ExpressionQuery(level+1);
		}
	}

	private void Condition(int level)
	{
		_logger.Information( "Condition() called at level {level}", level);

		if (!(Match(TokenType.Equals) || Match(TokenType.And) || Match(TokenType.Or)))
			return;
		if (Match(TokenType.Equals))
		{
			Advance(level);
			if (!Match(TokenType.Equals))
			{
				_logger.Error("Syntax Error! Mismatched equality operator, expected \"=\" actual: {@word} ", _tokens[_currentIndex].Word);
				return;
			}
		}
		Advance(level+1);
		Boolean(level+1);
	}

	private void ExpressionQuery(int level)
	{
		_logger.Information( "ExpressionQuery() called at level {level}", level);
		Expression(level+1);
		if (!(Match(TokenType.Equals) || Match(TokenType.More) || Match(TokenType.Less)))
			return;
		if (Match(TokenType.Equals))
		{
			Advance(level);
			if (!Match(TokenType.Equals))
			{
				_logger.Error("Syntax Error! Mismatched equality operator, expected \"=\" actual: {@word} ", _tokens[_currentIndex].Word);
				return;
			}
		}
		Advance(level+1);
		Expression(level+1);
	}
	//EBNF Functions
}