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
        private static List<String> names = new List<string>();
        // Reference: https://stackoverflow.com/a/1344242/11567632
        public static string random_string(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string name = "";
            do {
                name = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (names.Contains(name));

            return name;
        }

        /// <summary>
        /// fixes discrepancies in IL such as maxstack errors that occur due to instruction insertion
        /// </summary>
        public static void clean_asm(ModuleDef md)
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

        // reference: https://github.com/CodeOfDark/Tutorials-StringEncryption
        public static void obfuscate_strings(ModuleDef md)
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
                            method.Body.Instructions[i].OpCode = OpCodes.Nop; // errors occur if instruction not replaced with Nop
                            method.Body.Instructions.Insert(i + 1,new Instruction(OpCodes.Call, md.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8", new Type[] { })))); // Load string onto stack
                            method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Ldstr, encString)); // Load string onto stack
                            method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Call, md.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) })))); // call method FromBase64String with string parameter loaded from stack, returned value will be loaded onto stack
                            method.Body.Instructions.Insert(i + 4, new Instruction(OpCodes.Callvirt, md.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) })))); // call method GetString with bytes parameter loaded from stack 
                            i += 4; //skip the Instructions as to not recurse on them
                        }
                    }
                    //method.Body.KeepOldMaxStack = true;
                }
            }

        }

        public static void obfuscate_methods(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                // create method to obfuscation map
                foreach (MethodDef method in type.Methods)
                {
                    // empty method check
                    if (!method.HasBody) continue;
                    // method is a constructor
                    if (method.IsConstructor) continue;
                    // method overrides another
                    if (method.HasOverrides) continue;
                    // method has a rtspecialname, VES needs proper name
                    if (method.IsRuntimeSpecialName) continue;
                    // method foward declaration
                    if (method.DeclaringType.IsForwarder) continue;

                    string encName = random_string(10);
                    Console.WriteLine($"{method.Name} -> {encName}");
                    method.Name = encName;
                }
            }
        }

        public static void obfuscate_resources(ModuleDef md, String name, String encName)
        {
            if (name == "") return;
            foreach (var resouce in md.Resources)
            {
                String newName = resouce.Name.Replace(name, encName);
                if(resouce.Name != newName)Console.WriteLine($"{resouce.Name} -> {newName}");
                resouce.Name = newName;
            }
        }

        public static void obfuscate_classes(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = random_string(10);
                Console.WriteLine($"{type.Name} -> {encName}");
                obfuscate_resources(md, type.Name, encName);
                type.Name = encName;
            }

        }

        public static void obfuscate_namespace(ModuleDef md)
        {
            foreach (var type in md.GetTypes())
            {
                string encName = random_string(10);
                Console.WriteLine($"{type.Namespace} -> {encName}");
                obfuscate_resources(md, type.Namespace, encName);
                type.Namespace = encName;
            }

        }

        public static void obfuscate_assembly_info(ModuleDef md)
        {
            // obfuscate assembly name
            string encName = random_string(10);
            Console.WriteLine($"{md.Assembly.Name} -> {encName}");
            md.Assembly.Name = encName;

            // obfuscate Assembly Attributes(AssemblyInfo) .rc file
            string[] attri = { "AssemblyDescriptionAttribute", "AssemblyTitleAttribute", "AssemblyProductAttribute", "AssemblyCopyrightAttribute", "AssemblyCompanyAttribute","AssemblyFileVersionAttribute"};
            // "GuidAttribute", and assembly version can also be changed
            foreach (CustomAttribute attribute in md.Assembly.CustomAttributes) {
                if (attri.Any(attribute.AttributeType.Name.Contains)) {
                    string encAttri = random_string(10);
                    Console.WriteLine($"{attribute.AttributeType.Name} = {encAttri}");
                    attribute.ConstructorArguments[0] = new CAArgument(md.CorLibTypes.String, new UTF8String(encAttri));
                }
            }
        }

        /// <summary>
        /// Obfuscate ECMA CIL (.NET IL) assemblies by obfuscating names of methods, classes, namespaces, assemblyInfo and encoding strings
        /// </summary>
        /// <param name="inFile">The .Net assembly path you want to obfuscate</param>
        /// <param name="outFile">Path to the newly obfuscated file, default is "inFile".obfuscated</param>
        static void obfuscate(string inFile, string outFile)
        {
            if (inFile == "" || outFile == "") return;

            string pathExec = inFile;

            // cecil moduel ref(similar to dnlib): https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/faq/
            // load ECMA CIL (.NET IL)
            ModuleDef md = ModuleDefMD.Load(pathExec);
            md.Name = random_string(10);

            obfuscate_strings(md);
            obfuscate_methods(md);
            obfuscate_classes(md);
            obfuscate_namespace(md);
            obfuscate_assembly_info(md);
            //obfuscateVariables(md); // md.Write already simplifies variable names to there type in effect mangling them i.e: aesSetup -> aes1, aesRun -> aes2
            //obfuscateComments(md); // comments are stripped during compile opitmization

            clean_asm(md);

            md.Write(outFile);
        }

        /// <summary>
        /// Obfuscate ECMA CIL (.NET IL) assemblies to evade Windows Defender AMSI 
        /// </summary>
        /// <param name="inFile">The .Net assembly path you want to obfuscate</param>
        /// <param name="outFile">Path to the newly obfuscated file, default is "inFile".obfuscated</param>
        static void Main(string inFile = @"", string outFile = @"")
        {
            if(outFile == "")
            {
                outFile = inFile + ".Obfuscated";
            }
            obfuscate(inFile,outFile);
        }
    }
}
