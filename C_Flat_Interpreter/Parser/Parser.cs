/*
 *	Expression:
	*	<Expression>::=<Numeric-Expression> | <Logic-Expression>

 *	Numerical expressions:
	 *  <Numeric-Expression>::= <Term> {('+'|'-') <Term>}
	 *	<Term>::= <Value> {('*'|'/') <Value>}
	 *	<Value>::= '('<Numeric-Expression>')' | <Number> | '-'<Value>

 *	Logical expressions:
	 *	<Logic-Expression>::= <Boolean> {<Condition>}
	 *  <Condition>::= ('==' | '&' | '|') <Boolean>
	 *	<Boolean>::= '!’<Logic-Expression> | 'true' | 'false' | <Expression-Query> | '('<Logic-Expression>')'
	 *	<Expression-Query> ::= <Expression> ('=='|'>'|'<') <Expression>
	 	
 *	Statements:
	*	<Statement>::= <Declaration>';' | <Assignment>';' | <Flow-Control> | <Define> | <Call>
 
 *	Declaration and Assignment:
	*	<Declaration>::= 'double' <Identifier> ['=' <Numeric-Expression>] | 'bool' <Identifier> ['=' <Logic-Statement>] 
	*	<Identifier>::= 'azAZ'
	*	<Assignment>::= <Identifier>'='(<Numeric-Expression>|<Logic-Expression>)	
 
 *	Flow control
	*	<Flow-Control>::= <If-Statement> | <While-Statement>
	*	<If-Statement>::= 'if(' <Logic-Expression> ')' <Block>
	*	<While-Statement>::= 'while('<Logic-Expression> ')' <Block>
	*	<Block>::= '{'{<Statement>}'}'
	
 *	Define and Call
	*	<Define>::= ('void'|'double'|'bool') <Identifier>'('{<Declaration>}')'<Block>
	*	<Call>::<Identifier>'('{<Expression>}')'
 * */

using System.Data;
using C_Flat_Interpreter.Common;
using C_Flat_Interpreter.Common.Enums;
namespace C_Flat_Interpreter.Parser;

public class Parser : InterpreterLogger
{
	private TokenType _tokenType;
	private int _totalTokens;
	private StatementNode _statement;
	private int _statementIndex;
	//constructor
	public Parser()
	{
		GetLogger("Parser");
		//_tokens = new List<Token>();
	}

    //Helper Functions
    #region Helper Functions
	private bool CheckBoolLiteral()
	{
		var word = _statement.statement[_statementIndex].Word;//_tokens[_currentIndex].Word;
		if (word.Equals("true") || word.Equals("false")) return true;
		_logger.Error($"Bool parse error! Expected boolean literal, actual: \"{word}\"");
		return false;
	}
	private bool CheckString(string s)
    {
		var word = _statement.statement[_statementIndex].Word;
		if (word.Equals(s)) return true;
		_logger.Error($"parse error! Expected word, actual: \"{word}\"");
		return false;
	}
	private bool HasVariable()
    {
		var word = _statement.statement[_statementIndex].Word;
		if (_statement.HasVariable(word)) return true;
		return false;
	}
	private ValueType GetVariableType()
    {
		var word = _statement.statement[_statementIndex].Word;
		return _statement.parentBlock.GetVariableType(word);
	}
	private bool Match(TokenType tokenType)
	{
		if (_tokenType == TokenType.Null) //only on first call,  TODO - find better way to do this that means we don't need to set to null and/or we don't need this check
        {
			_tokenType = GetToken().Type;//_tokens[_currentIndex].Type;
		}

		if (tokenType == _tokenType)
		{
			_logger.Information("Token matches {tokenType}", tokenType);
		}
		else
		{
			_logger.Information("Token does NOT MATCH {tokenType}", tokenType);
			return false;
		}
		return true;
	}
	private Token GetToken()
    {
		return _statement.statement[_statementIndex];
    }
	private bool TryStatement(int level, Action<int> statementType)
    {
		int index = _statementIndex;
		try
		{
			statementType(level + 1);
			//if (!Match(TokenType.SemiColon)) return; //this is terminal
			return true;
		}
		catch (Exception e)
		{
			_logger.Warning(e.Message);
			_statementIndex = index;
			return false;
		}
	}
	private bool TryExpression(int level, Action<int> expressionType)
    {
		int index = _statementIndex;
		try
		{
			expressionType(level + 1);
			return true;
		}
		catch (Exception e)
		{
			_logger.Warning(e.Message);
			_statementIndex = index;
			return false;
		}
	}
	private void Reset()
	{
		_statementIndex = 0;
		_tokenType = TokenType.Null;
	}
	private void Advance(int level)
	{
		if (++_statementIndex >= _statement.statement.Count())
			//_totalTokens) 
			return; //todo - might be able to find a way to exit everything else quicker when we're at the end - EOF token!
		_tokenType = _statement.statement[_statementIndex].Type;//_tokens[_currentIndex].Type;
		_logger.Information("advance() called at level {level}. Next token is {@token}", level, _statement.statement[_statementIndex]);//_tokens[_currentIndex]);
	}
	private void AddVariable(ValueType type)
	{
		var word = _statement.statement[_statementIndex].Word;
		_statement.parentBlock.AddVariable(word, type);
	}
	#endregion


	//End Helper Functions

	public int Parse(List<Token> tokens)
	{
		//_tokens = tokens;
		_totalTokens = tokens.Count;
		_statement = null;
		_statementIndex = 0;
		BlockNode block = new BlockNode();
		block.Load(tokens, 0);
		ParseTree(block, 0);
		//if (_indexBase >= _totalTokens)
		return 0;
	}
	private void ParseTree(BlockNode block, int level)
    {
		_logger.Information("ParseTree() called at level {level}", level);
		for (int i = 0; i < block.statements.Count; i++)
		{
			_statement = block.statements[i];
			Reset();
            try
            {
				Statement(level + 1);
            }
            catch (Exception e)
            {
				_logger.Error("Syntax error! Could not parse Token: {@token}", GetToken().Word);
				throw new Exception("Could not parse statement");
            }
		}
	}
	//EBNF Functions
	private void Statement(int level)
	{
		_logger.Information( "Statement() called at level {level}", level);
		if (TryExpression(level, Declare))
			return;
		else if (TryExpression(level, Assign))
			return;
		else if (TryExpression(level, IfStatement))
			return;
		else if (TryExpression(level, WhileStatement))
			return;
		else throw new Exception("Could not parse expression");
	}
	
	private void Expression(int level)
    {
		_logger.Information("Expression() called at level {level}", level);

		if (TryExpression(level, Numerical_Expression))
			return;
		else if (TryExpression(level, LogicStatement))
			return;
		else throw new Exception("Could not parse expression");
	}
    #region Numerical expressions
	private void Numerical_Expression(int level)
	{
		_logger.Information( "expression() called at level {level}", level);
		Term(level + 1);
		if (!Match(TokenType.Add) && !Match(TokenType.Sub)) return; //this is terminal
		Advance(level + 1);
		Term(level + 1);
	}

	private void Term(int level)
	{
		_logger.Information( "term() called at level {level} ", level);
		Factor(level + 1);
		if (!Match(TokenType.Multi) && !Match(TokenType.Divide)) return; //this is terminal
		Advance(level + 1);
		Factor(level + 1);
	}

	private void Factor(int level) //todo - see if factor prime is needed
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
			Numerical_Expression(level + 1);
			if (Match(TokenType.RightParen)) Advance(level + 1);
			else
			{
				throw new SyntaxErrorException("Syntax Error! Mismatched parentheses");
			}
		}
		else
		{
			throw new SyntaxErrorException("Syntax Error! Unexpected token");
		}
	}
    #endregion

    #region Boolean expressions
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
		else
		{
			var index = _statementIndex;//_currentIndex;
			try
			{
				ExpressionQuery(level+1);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message);
				_statementIndex = index;
				//_currentIndex = index;
				_tokenType = TokenType.Null;
				if (Match(TokenType.LeftParen))
				{
					Advance(level + 1);
					LogicStatement(level + 1);
					if (Match(TokenType.RightParen)) Advance(level + 1);
					else
					{
						_logger.Error("Syntax Error! Mismatched parentheses");
					}
				}
			}
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
				_logger.Error("Syntax Error! Mismatched equality operator, expected \"=\" actual: {@word} ", _statement.statement[_statementIndex].Word);
				//_tokens[_currentIndex].Word);
				return;
			}
		}
		Advance(level+1);
		Boolean(level+1);
	}

	private void ExpressionQuery(int level)
	{
		_logger.Information( "ExpressionQuery() called at level {level}", level);
		Numerical_Expression(level + 1);
		if (!(Match(TokenType.Equals) || Match(TokenType.More) || Match(TokenType.Less)))
			return;
		if (Match(TokenType.Equals))
		{
			Advance(level);
			if (!Match(TokenType.Equals))
			{
				_logger.Error("Syntax Error! Mismatched equality operator, expected \"=\" actual: {@word} ",
					_statement.statement[_statementIndex].Word);//_tokens[_currentIndex].Word);
				return;
			}
		}
		Advance(level + 1);
		Numerical_Expression(level + 1);
	}
	#endregion

	#region Declaration and Assignment
	void Declare(int level)
	{
		_logger.Information("Declare() called at level {level}", level);
		//value type
		ValueType type;
		if (Match(TokenType.String) && CheckString("double"))
			type = ValueType.RealNumber;
		else if (Match(TokenType.String) && CheckString("bool"))
			type = ValueType.Boolean;
		else
			throw new Exception("Cannot recognise value type");
		Advance(level + 1);
		//identifier
		if (Match(TokenType.String) && !HasVariable())
		{
			AddVariable(type);
		}
		else
			throw new Exception("Missing new identifier");
		//try assignment
		if (TryStatement(level, Assign))
			return;
		else
			Advance(level + 1);
		//Otherwise finish
		if (Match(TokenType.SemiColon))
			Advance(level + 1);
		else throw new Exception("Missing semicolon");
	}
	void Assign(int level)
	{
		_logger.Information("Assign() called at level {level}", level);
		ValueType type;
		if (Match(TokenType.String) && HasVariable())
		{
			type = GetVariableType();
			Advance(level + 1);
		}
		else throw new Exception("Missing variable to assign to");
		if (Match(TokenType.Equals))
			Advance(level + 1);
		else throw new Exception("Missing =");
		if (type.Equals(ValueType.RealNumber))
		{
			if (!TryExpression(level, Numerical_Expression))
				throw new Exception("Invalid numeric expression");
		}
		else if (type.Equals(ValueType.Boolean))
		{
			if (!TryExpression(level, LogicStatement))
				throw new Exception("Invalid logic expression");
		}
		if (Match(TokenType.SemiColon))
			Advance(level + 1);
		else throw new Exception("Missing semicolon");
	}
	#endregion

	#region Conditions and iterators
	private void IfStatement(int level)
    {
		_logger.Information("if-statement() called at level {level}", level);
		if (Match(TokenType.String) && CheckString("if")) 
			Advance(level + 1);
        else throw new Exception("Missing if");
		if (Match(TokenType.LeftParen)) Advance(level + 1);
        else
        {
            _logger.Error("Syntax Error! Expected \"(\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			throw new Exception("Missing (");
        }
		LogicStatement(level + 1);
		if (Match(TokenType.RightParen)) Advance(level + 1);
		else
        {
			_logger.Error("Syntax Error! Expected \"(\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			throw new Exception("Missing )");
		}
		SubBlock(level + 1);
		try
		{
			ElseStatements(level + 1);
		}
		catch (Exception e)
		{
			_logger.Warning(e.Message);
		}
	}
	private void ElseStatements(int level) {
		if (Match(TokenType.String) && CheckString("else"))
		{
			Advance(level + 1);
			SubBlock(level + 1);
		}
        else
        {
			throw new Exception("Missing else");
		}
	}
	private void WhileStatement(int level)
	{
		_logger.Information("while-statement() called at level {level}", level);
		if (Match(TokenType.String) && CheckString("while"))
			Advance(level + 1);
		else throw new Exception("Missing while");
		if (Match(TokenType.LeftParen)) Advance(level + 1);
		else
		{
			_logger.Error("Syntax Error! Expected \"(\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			//_tokens[_currentIndex].Word);
			throw new Exception("Missing (");
		}
		LogicStatement(level + 1);
		if (Match(TokenType.RightParen)) Advance(level + 1);
		else
		{
			_logger.Error("Syntax Error! Expected \"(\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			//_tokens[_currentIndex].Word);
			throw new Exception("Missing )");
		}
		SubBlock(level + 1);
	}
	private void SubBlock(int level) {
		if (Match(TokenType.LeftCurlyBrace)) Advance(level + 1);
		else
		{
			_logger.Error("Syntax Error! Expected \"{\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			//_tokens[_currentIndex].Word);
			throw new Exception("Missing {");
		}
		if (Match(TokenType.RightCurlyBrace)) Advance(level + 1);
		else
		{
			_logger.Error("Syntax Error! Expected \"}\" actual: {@word} ", _statement.statement[_statementIndex].Word);
			//_tokens[_currentIndex].Word);
			throw new Exception("Missing }");
		}
		try
		{
			ParseTree(_statement.subBlock, level + 1);
		}
		catch (Exception e)
		{
			_logger.Warning(e.Message);
		}
	}
    #endregion

    
    //EBNF Functions
}
