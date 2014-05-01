﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Happil.Expressions;
using Happil.Operands;

namespace Happil.Statements
{
	internal class CallStatement : StatementBase
	{
		private readonly MethodBase m_Method;
		private readonly IOperand[] m_Arguments;
		private IOperand m_Target;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public CallStatement(IOperand target, MethodBase method, params IOperand[] arguments)
		{
			var scope = StatementScope.Current;
	
			scope.Consume(target);

			foreach ( var argument in arguments )
			{
				scope.Consume(argument);			
			}

			m_Target = target;
			m_Method = method;
			m_Arguments = arguments;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region StatementBase Members

		public override void Emit(ILGenerator il)
		{
			Helpers.EmitCall(il, m_Target, m_Method, m_Arguments);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public override void AcceptVisitor(OperandVisitorBase visitor)
		{
			visitor.VisitOperand(ref m_Target);
			visitor.VisitOperandArray(m_Arguments);
		}

		#endregion
	}
}
