using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

using OWML.Common;
using UnityEngine;

namespace OWMixedInput;

// MethodBase objects that are important in our transpiler patches.
public static class Methods {
    public static MethodBase OWInput_UsingGamepad
        => AccessTools.Method(typeof(OWInput), nameof(OWInput.UsingGamepad));

    public static MethodBase OWInput_GetAxisValue
        => AccessTools.Method(typeof(OWInput), nameof(OWInput.GetAxisValue));

    public static MethodBase IInputCommands_GetAxisValue
        => AccessTools.Method(typeof(IInputCommands), nameof(IInputCommands.GetAxisValue));

    public static MethodBase Time_deltaTime
        => AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime));
}

// Useful tools for our transpiler methods.
// I was having trouble getting the CodeMatcher methods to work correctly.
// These work how I expect them to.
public static class TranspilerUtils {
    // Unit of the IEnumerable monad.
    public static IEnumerable<T> Yield<T>(this T t) { yield return t; }

    // Check if an opcode/instruction is a call or callvirt.
    extension (OpCode code) {
        public bool IsAnyCall => code == OpCodes.Call || code == OpCodes.Callvirt;
    }
    extension (CodeInstruction instruction) {
        public bool IsAnyCall => instruction.opcode.IsAnyCall;
    }

    // Create instructions from a MethodBase operand.
    extension (MethodBase method) {
        public CodeInstruction Callnovirt => new CodeInstruction(OpCodes.Call, method);
        public CodeInstruction Callvirt => new CodeInstruction(OpCodes.Callvirt, method);
        public CodeInstruction Call => new CodeInstruction(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
    }

    // Check if an instruction is a call (virtual or otherwise) to a method.
    public static bool MatchInstruction(this MethodBase method, CodeInstruction instruction)
        => instruction.IsAnyCall && (MethodBase)instruction.operand == method;

    // Extension methods for replacing instructions with other instructions in an instruction stream.
    // The methods take a stream of instructions, a proposition on instructions, and a stream of replacement instructions.
    // Any instruction in the original stream which matches the proposition is replaced with the stream of replacement instructions.
    // The originalMethod argument is just used for logging
    extension(IEnumerable<CodeInstruction> instructions) {
        public IEnumerable<CodeInstruction> ReplaceInstructions(
                Func<CodeInstruction, bool> matcher,
                IEnumerable<CodeInstruction> replacement,
                MethodBase originalMethod = null
        ) {
            foreach (var instruction in instructions) {
                if (matcher.Invoke(instruction)) {
                    // Clone the replacement instructions into a list.
                    var replacementInstructions = replacement
                        .Select(instruction => new CodeInstruction(instruction))
                        .ToList();

                    // Add the original labels into the beginning of the new instructions.
                    if (instruction.labels.Any()) {
                        if (!replacementInstructions.Any()) {
                            throw new Exception("Attempted to remove instruction with labels");
                        }
                        replacementInstructions.First().labels.AddRange(instruction.labels);

                        if (originalMethod is not null) {
                            OWMixedInput.WriteLine($"Patched labels in {originalMethod.DeclaringType}.{originalMethod.Name}",
                                    MessageType.Debug);
                        } else {
                            OWMixedInput.WriteLine($"Patched labels in unknown method.", MessageType.Warning);
                        }
                    }
                    // Add the exception blocks into the replacement instructions.
                    if (instruction.blocks.Any()) {
                        if (!replacementInstructions.Any()) {
                            throw new Exception("Attempted to remove instruction with blocks");
                        }

                        if (originalMethod is not null) {
                            OWMixedInput.WriteLine($"Patched IL exception blocks in {originalMethod.DeclaringType}.{originalMethod.Name}", 
                                    MessageType.Warning);
                        } else {
                            OWMixedInput.WriteLine($"Patched IL exception blocks in unknown method.", MessageType.Warning);
                        }

                        foreach (var block in instruction.blocks) {
                            switch (block.blockType) {
                            case ExceptionBlockType.BeginCatchBlock:
                            case ExceptionBlockType.BeginExceptFilterBlock:
                            case ExceptionBlockType.BeginExceptionBlock:
                            case ExceptionBlockType.BeginFaultBlock:
                            case ExceptionBlockType.BeginFinallyBlock:
                                // Begin blocks go at beginning of new instructions.
                                replacementInstructions.First().blocks.Add(block);
                                break;
                            case ExceptionBlockType.EndExceptionBlock:
                                // End blocks go at the end of new instructions.
                                replacementInstructions.Last().blocks.Add(block);
                                break;
                            default:
                                throw new Exception($"Unsupported ExceptionBlockType: {block.blockType}");
                            }
                        }
                    }

                    // Yield the new instructions.
                    foreach (var replacementInstruction in replacementInstructions) {
                        yield return replacementInstruction;
                    }
                } else {
                    yield return instruction;
                }
            }
        }

        public IEnumerable<CodeInstruction> ReplaceInstructions(
                Func<CodeInstruction, bool> matcher,
                CodeInstruction replacement,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(matcher, replacement.Yield(), originalMethod);

        public IEnumerable<CodeInstruction> ReplaceInstructions(
                Func<CodeInstruction, bool> matcher,
                MethodBase replacementMethod,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(matcher, replacementMethod.Call, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethod(
                MethodBase method,
                IEnumerable<CodeInstruction> replacement,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(method.MatchInstruction, replacement, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethod(
                MethodBase method,
                CodeInstruction replacement,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(method.MatchInstruction, replacement, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethod(
                MethodBase method,
                MethodBase replacementMethod,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(method.MatchInstruction, replacementMethod, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethods(
                IEnumerable<MethodBase> methods,
                IEnumerable<CodeInstruction> replacement,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(instruction => methods.Any(method => method.MatchInstruction(instruction)), replacement, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethods(
                IEnumerable<MethodBase> methods,
                CodeInstruction replacement,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(instruction => methods.Any(method => method.MatchInstruction(instruction)), replacement, originalMethod);

        public IEnumerable<CodeInstruction> ReplaceMethods(
                IEnumerable<MethodBase> methods,
                MethodBase replacementMethod,
                MethodBase originalMethod = null
        ) => instructions.ReplaceInstructions(instruction => methods.Any(method => method.MatchInstruction(instruction)), replacementMethod, originalMethod);
    }
}

