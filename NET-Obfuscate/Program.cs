using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.DragonFruit;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Obfuscate
{
    class Obfuscate
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // obfuscates method names
        public static void obfuscateMethods(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                // create method to obfuscation map
                foreach (MethodDef method in type.Methods)
                {
                    // empty method check
                    if (!method.HasBody) continue;

                    // Obfuscate constructor
                    if (method.IsConstructor) continue;

                    string encName = RandomString(10);
                    Console.WriteLine($"{method.Name} -> {encName}");
                    method.Name = encName;
                }
            }
        }

        public static void obfuscateClasses(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = RandomString(10);
                Console.WriteLine($"{type.Name} -> {encName}");
                type.Name = encName;
            }

        }

        // fixes errors that occur due to instruction insertion
        public static void cleanAsm(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    // empty method check
                    if (!method.HasBody) continue;

                    method.Body.SimplifyBranches();
                    method.Body.OptimizeBranches(); // negates simplifyBranches
                    //method.Body.OptimizeMacros();
                }
            }
        }

        // object are default pass by ref
        // TODO: implement custom decryptor
        public static void obfuscateStrings(ModuleDef md)
        {
            //foreach (var type in md.Types) // only gets parent(non-nested) classes

            // types(namespace.class) in module
            foreach (var type in md.GetTypes())
            {
                // methods in type
                foreach(MethodDef method in type.Methods)
                {
                    // empty method check
                    if (!method.HasBody) continue;
                    // iterate over instructions of method
                    for(int i = 0; i < method.Body.Instructions.Count(); i++)
                    {
                        // check for LoadString opcode
                        // CIL is Stackbased (data is pushed on stack rather than register)
                        // ref: https://en.wikipedia.org/wiki/Common_Intermediate_Language
                        // ld = load (push onto stack), st = store (store into variable)
                        if(method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            // c# variable has for loop scope only
                            String regString = method.Body.Instructions[i].Operand.ToString();
                            String encString = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(regString));
                            Console.WriteLine($"{regString} -> {encString}");
                            // methodology for adding code: write it in plain c#, compile, then view IL in dnspy
                            method.Body.Instructions[i].OpCode = OpCodes.Nop; // erase orginal string instruction
                            method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, md.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8", new Type[] { })))); // call Method (get_UTF8) from Type (System.Text.Encoding) no parameters
                            method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Ldstr, encString)); // Load string onto stack
                            method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Call, md.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) })))); // call method FromBase64String with string parameter loaded from stack, returned value will be loaded onto stack
                            method.Body.Instructions.Insert(i + 4, new Instruction(OpCodes.Callvirt, md.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) })))); // call method GetString with bytes parameter loaded from stack 
                            i += 4; //skip the Instructions as to not recurse on them
                        }
                    }
                }
            }

        }

        public static void obfuscateNamespace(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = RandomString(10);
                Console.WriteLine($"{type.Namespace} -> {encName}");
                type.Namespace = encName;
            }

        }

        public static void obfuscateAssemblyInfo(ModuleDef md)
        {
            // obfuscate assembly name
            string encName = RandomString(10);
            Console.WriteLine($"{md.Assembly.Name} -> {encName}");
            md.Assembly.Name = encName;

            // should "GuidAttribute" be changed aswell? or assembly version
            // obfuscate Assembly Attributes(AssemblyInfo) .rc file
            string[] attri = { "AssemblyDescriptionAttribute", "AssemblyTitleAttribute", "AssemblyProductAttribute", "AssemblyCopyrightAttribute", "AssemblyCompanyAttribute","AssemblyFileVersionAttribute"};
            foreach (CustomAttribute attribute in md.Assembly.CustomAttributes) {
                if (attri.Any(attribute.AttributeType.Name.Contains)) {
                    string encAttri = RandomString(10);
                    Console.WriteLine($"{attribute.AttributeType.Name} = {encAttri}");
                    attribute.ConstructorArguments[0] = new CAArgument(md.CorLibTypes.String, new UTF8String(encAttri));
                }
            }
        }

        static void obfuscate(string inFile, string outFile)
        {
            if (inFile == "" || outFile == "") return;

            string pathExec = inFile;

            // cecil moduel ref(similar to dnlib): https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/faq/
            // load ECMA CIL (.NET IL)
            ModuleDef md = ModuleDefMD.Load(pathExec);

            obfuscateStrings(md);
            obfuscateMethods(md);
            obfuscateClasses(md);
            obfuscateNamespace(md);
            obfuscateAssemblyInfo(md);
            //obfuscateVariables(md); // md.Write already simplifies variable names to there type in effect obfuscating them from antivirus i.e: aesSetup -> aes1, aesRun -> aes2
            // comments are also stripped during compile
            cleanAsm(md);

            md.Write(outFile);
        }

        // TODO: GUI and CLI
        // TODO: obfuscate assembly icon
        // TODO: obfuscate parameters
        // TODO: obfuscate fields, do I need to?
        // TODO: keep list of random strings, make sure no names overlap

        /// <summary>
        /// Obfuscate ECMA CIL (.NET IL) assemblies to evade Windows Defender AMSI 
        /// </summary>
        /// <param name="inFile">The .Net assembly path you want to obfuscate</param>
        /// <param name="outFile">Path to the newly obfuscated file, default is "inFile".obfuscated</param>
        static void Main(string inFile = "", string outFile = "")
        {
            if(outFile == "")
            {
                outFile = inFile + ".Obfuscated";
            }
            obfuscate(inFile,outFile);
        }
    }
}
