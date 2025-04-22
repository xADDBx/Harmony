using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib
{
	internal class Infixes
	{
		internal static Infixes Empty = new([], false);
		private List<Patch> SortedInnerPrefixes;
		private List<Patch> SortedInnerPostfixes;
		private List<Patch> SortedInnerFinalizers;
		internal Infixes(List<Patch> innerPrefixes, List<Patch> innerPostfixes, List<Patch> innerFinalizers)
		{
			SortedInnerPrefixes = innerPrefixes;
			SortedInnerPostfixes = innerPostfixes;
			SortedInnerFinalizers = innerFinalizers;
		}

		internal Infixes(Patch[] fixes, bool debug)
		{
			List<Patch> innerPrefixes = [];
			List<Patch> innerPostfixes = [];
			List<Patch> innerFinalizers = [];

			if (fixes.Length > 0)
			{
				foreach (var fix in fixes)
				{
					switch (fix.InnerFix)
					{
						case InnerPrefix pre: innerPrefixes.Add(fix); break;
						case InnerPostfix post: innerPostfixes.Add(fix); break;
						case InnerFinalizer final: innerFinalizers.Add(fix); break;
						default:
							throw new ArgumentOutOfRangeException(nameof(fix.InnerFix), $"Unexpected fix type: {fix.InnerFix.GetType().Name}");
					}
				}
				SortedInnerPrefixes = [.. new PatchSorter([.. innerPrefixes], debug).GetSortedPatchArray()];
				SortedInnerPostfixes = [.. new PatchSorter([.. innerPrefixes], debug).GetSortedPatchArray()];
				SortedInnerFinalizers = [.. new PatchSorter([.. innerPrefixes], debug).GetSortedPatchArray()];
			} else
			{
				innerPrefixes = [];
				innerPostfixes = [];
				innerFinalizers = [];
			}
		}
		private Dictionary<CodeInstruction, Infixes> m_Config = null;
		private static void AddFix(CodeInstruction inst, Dictionary<CodeInstruction, Infixes> config, Patch fix, InnerFixType type)
		{
			if (!config.TryGetValue(inst, out var fixesForInstruction))
			{
				switch (type)
				{
					case InnerFixType.Prefix: config[inst] = new([fix], [], []); break;
					case InnerFixType.Postfix: config[inst] = new([], [fix], []); break;
					case InnerFixType.Finalizer: config[inst] = new([], [], [fix]); break;
				}
			}
			else
			{
				switch (type)
				{
					case InnerFixType.Prefix: fixesForInstruction.SortedInnerPrefixes.Add(fix); break;
					case InnerFixType.Postfix: fixesForInstruction.SortedInnerPostfixes.Add(fix); break;
					case InnerFixType.Finalizer: fixesForInstruction.SortedInnerPrefixes.Add(fix); break;
				}

			}
		}
		internal List<Patch> GetFixesForType(InnerFixType type)
		{
			switch (type)
			{
				case InnerFixType.Prefix: return SortedInnerPrefixes;
				case InnerFixType.Postfix: return SortedInnerPostfixes;
				case InnerFixType.Finalizer: return SortedInnerFinalizers;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected fix type: {type}");
			}
		}
		// Do we need to care about thread safety here?
		internal Dictionary<CodeInstruction, Infixes> PrepareInfixes(IEnumerable<CodeInstruction> instructions)
		{
			Dictionary<MethodBase, List<CodeInstruction>> dict = [];
			foreach (var inst in instructions)
			{
				// Do we need to test for Calli too?
				if ((inst.opcode == OpCodes.Call || inst.opcode == OpCodes.Callvirt) && inst.operand is MethodBase mb)
				{
					if (!dict.TryGetValue(mb, out var callSites))
					{
						dict[mb] = [inst];
					}
					else
					{
						callSites.Add(inst);
					}
				}
			}
			Dictionary<CodeInstruction, Infixes> config = [];
			foreach (var fixType in new[] { InnerFixType.Prefix, InnerFixType.Postfix, InnerFixType.Finalizer })
			{
				foreach (var fix in GetFixesForType(fixType))
				{
					var target = fix.InnerFix.InnerMethod ?? fix.InnerFix.TargetFinder(instructions);
					if (dict.TryGetValue(target.Method, out var callSites))
					{
						if (target.positions.Length == 0)
						{
							foreach (var inst in callSites)
							{
								AddFix(inst, config, fix, fixType);
							}
						}
						else
						{
							foreach (var pos in target.positions)
							{
								CodeInstruction inst;
								if (pos > 0)
								{
									inst = callSites[pos - 1];
								}
								else
								{
									inst = callSites[callSites.Count + pos];
								}
								AddFix(inst, config, fix, fixType);
							}
						}
					}
				}
			}
			return config;
		}
	}
}
