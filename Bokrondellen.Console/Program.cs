using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bokrondellen.Console
{
    class Program
    {
        private readonly static char[] switchPrefix = new char[] { '/', '-' };
        private readonly static Dictionary<string, string> Args = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            Initialize(args);
        }

        private static void Initialize(string[] args)
        {
            ParseArgs(args);
        }

        static void ParseArgs(string[] args)
        {
            var switches = args.Select((s, i) => new { Index = i, Value = s })
                .Where(o => switchPrefix.Any(p => p == o.Value[0]));

            foreach (var sw in switches)
            {
                if (args.Length > sw.Index + 1)
                {
                    Args.Add(
                        sw.Value.Substring(1),
                        args[sw.Index + 1]);
                }
            }

        }
    }
}
