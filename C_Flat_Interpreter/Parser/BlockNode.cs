using C_Flat_Interpreter.Common;
using C_Flat_Interpreter.Common.Enums;
namespace C_Flat_Interpreter.Parser
{
    public enum ValueType
    {
        RealNumber,
        Boolean,
        Invalid
    }
    public class BlockNode
    {
        private StatementNode parent;
        private Dictionary<string, ValueType> _identifiers = new Dictionary<string, ValueType>();
        public List<StatementNode> statements;
        public BlockNode()
        {
            statements = new List<StatementNode>();
        }
        public int Load(List<Token> tokens, int index)
        {
            while (tokens.Count > index)
            {
                Token token = tokens[index];
                if (tokens[index].Type == TokenType.RightCurlyBrace)
                {
                    return index;
                }
                else
                {
                    StatementNode statement = new StatementNode(this);
                    index = statement.Load(tokens, index);
                    statements.Add(statement);
                }
            }
            return index;
        }
        public int Length
        {
            get
            {
                int length = 0;
                foreach (StatementNode statement in statements)
                    length+=statement.Length;
                return length;
            }
        }
        public bool HasVariable(string identifier)
        {
            if(_identifiers.ContainsKey(identifier))
                return true;
            else if(parent != null)
                return parent.HasVariable(identifier);
            else return false;
        }
        public ValueType GetVariableType(string identifier)
        {
            if (_identifiers.ContainsKey(identifier))
                return _identifiers[identifier];
            else if (parent != null)
                return parent.GetVariableType(identifier);
            else return ValueType.Invalid;
        }
        public void AddVariable(string identifier, ValueType type)
        {
            _identifiers.Add(identifier, type);
        }
    }
}
