using Energize.Interfaces.Services.Eval;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Eval
{
    //0 -> bad
    //1 -> ok
    //2 -> warning
    [Service("Evaluator")]
    public class CSharpEvaluationService : ServiceImplementationBase, ICSharpEvaluationService
    {
        public async Task<(int, string)> Eval(string code, object ctx)
        {
            if (code[code.Length - 1] != ';')
                code += ";";

            string[] imports = {
                "System",
                "System.IO",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Reflection",
                "System.Text",
                "System.Threading.Tasks",
                "Discord",
                "Discord.Net",
                "Discord.Rest",
                "Discord.WebSocket",
                "Energize",
                "Energize.Essentials",
                "Energize.Services",
                "Energize.Interfaces",
                "System.Text.RegularExpressions",
                "System.Diagnostics"
            };

            ScriptOptions options = ScriptOptions.Default
                .WithImports(imports)
                .WithReferences(AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

            try
            {
                ScriptState state = await CSharpScript.RunAsync(code, options, ctx);
                if (state?.ReturnValue != null)
                {
                    string ret = state.ReturnValue.ToString();
                    if (!string.IsNullOrWhiteSpace(ret))
                    {
                        if (ret.Length > 2000)
                            ret = $"{ret.Substring(0, 1980)}... \n**[{(ret.Length - 2020)}\tCHARS\tLEFT]**";

                        return (1, ret);
                    }
                    else
                    {
                        return (2, "⚠ (string was null or empty)");
                    }
                }
                else
                {
                    return (1, "👌 (nothing or null was returned)");
                }
            }
            catch (Exception ex)
            {
                return (0, $"```\n{ex.Message.Replace("`", "")}```");
            }
        }
    }
}
