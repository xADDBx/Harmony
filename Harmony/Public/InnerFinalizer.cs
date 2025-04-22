using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>An inner finalizer that is applied inside some method call inside a method</summary>
	/// 
	public class InnerFinalizer : InnerFix
	{
		internal override InnerFixType Type
		{
			get => InnerFixType.Finalizer;
			set => throw new NotImplementedException();
		}

		/// <summary>Creates an infix for an implicit defined method call</summary>
		/// <param name="innerMethod">The method call to apply the fix to</param>
		/// 
		public InnerFinalizer(InnerMethod innerMethod) : base(InnerFixType.Finalizer, innerMethod) { }

		/// <summary>Creates an infix for an indirectly defined method call</summary>
		/// <param name="targetFinder">Calculates Target from a given methods content</param>
		/// 
		public InnerFinalizer(Func<IEnumerable<CodeInstruction>, InnerMethod> targetFinder) : base(InnerFixType.Finalizer, targetFinder) { }

		internal override IEnumerable<CodeInstruction> Apply(MethodBase original, IEnumerable<CodeInstruction> instructions)
		{
			// TODO: implement
			_ = original;
			foreach (var instruction in instructions) yield return instruction;
		}
	}
}
