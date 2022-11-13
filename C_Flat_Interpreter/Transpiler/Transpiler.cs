using C_Flat_Interpreter.Common;
using C_Flat_Interpreter.Common.Enums;
using Serilog;

namespace C_Flat_Interpreter.Transpiler;

public class Transpiler : InterpreterLogger
{
    private readonly string[] _defaultProgramString =
    {
        "// See https://aka.ms/new-console-template for more information",
        @"Console.WriteLine(""Hello, World!"");",
    };

    public Transpiler()
    {
        GetLogger("Transpiler");
    }

    public void Transpile(List<Token> tokens)
    {
        //Retrieve program.cs file
        var writer = File.CreateText(GetProgramPath());
        string prog = $@"Console.Out.WriteLine(";
        string vari = $@"";
        int index = 0;
        bool declaring = false; 
        foreach (var tok in tokens)
        {
           

            // check to see if token is declaring a variable
            if (string.Equals(tok.Word, "double") || declaring == true )
            {
                declaring = true;
                vari += (tok.Word + " ");
                if (string.Equals(tok.Word, ";"))
                {
                    declaring = false;
                    vari += System.Environment.NewLine;
                }
                //if ((tokens[index + 2].Type == TokenType.Equals) && (tokens[index + 3].Type == TokenType.Num))
                //{
                //    vari += ("double " + tokens[1].Word + " = " + tokens[3].Word + ";" + System.Environment.NewLine);
                //}
                //else
                //{
                //    vari += ("double " + tokens[1].Word + ";" + System.Environment.NewLine);
                //}
                
            }
            //
            else
            {
                //TODO: Refactor this if needed
                if (tok.Type is TokenType.Sub && prog.EndsWith('-'))
                    prog += ' ';
                prog += (tok.Value ?? tok.Word);
            }

            index++;
        }
        prog += @");";
        //vari = ("double " + tokens[1].Word + " = " + tokens[3].Word + ";" + System.Environment.NewLine);
        writer.Write(vari);
        writer.Write(prog);
        writer.Close();
    }

    public string GetProgramPath()
    {
        return Path.GetFullPath("../../../../C_Flat_Output/Program.cs");
    }

    public void ResetOutput()
    {
        //Writes the microsoft console application template to program.cs to prevent build errors.
        var writer = File.CreateText(GetProgramPath());
        foreach (var line in _defaultProgramString)
        {
            writer.WriteLine(line);
        }
        writer.Close();
    }
}