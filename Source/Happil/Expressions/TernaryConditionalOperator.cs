﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Happil.Operands;
using Happil.Statements;

namespace Happil.Expressions
{
	internal class TernaryConditionalOperator<T> : Operand<T>
	{
		private readonly IOperand<bool> m_Condition;
		private readonly IOperand<T> m_OnTrue;
		private readonly IOperand<T> m_OnFalse;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public TernaryConditionalOperator(IOperand<bool> condition, IOperand<T> onTrue, IOperand<T> onFalse) 
		{
			m_Condition = condition;
			m_OnTrue = onTrue;
			m_OnFalse = onFalse;

			var statements = StatementScope.Current;
			
			statements.Consume(onFalse);
			statements.Consume(onTrue);
			statements.Consume(condition);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitTarget(ILGenerator il)
		{
			// no target
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitLoad(ILGenerator il)
		{
			var falseLabel = il.DefineLabel();
			var endLabel = il.DefineLabel();

			m_Condition.EmitTarget(il);
			m_Condition.EmitLoad(il);

			il.Emit(OpCodes.Brfalse, falseLabel);

			m_OnTrue.EmitTarget(il);
			m_OnTrue.EmitLoad(il);
	
			il.Emit(OpCodes.Br, endLabel);
			il.MarkLabel(falseLabel);

			m_OnFalse.EmitTarget(il);
			m_OnFalse.EmitLoad(il);

			il.MarkLabel(endLabel);
			il.Emit(OpCodes.Nop);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitStore(ILGenerator il)
		{
			throw new NotSupportedException("This expression cannot be assigned a value.");
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitAddress(ILGenerator il)
		{
			throw new NotSupportedException("This expression cannot be passed by ref.");
		}
	}
}
