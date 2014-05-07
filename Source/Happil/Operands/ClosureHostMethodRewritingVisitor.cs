﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Happil.Statements;

namespace Happil.Operands
{
	internal class ClosureHostMethodRewritingVisitor : OperandVisitorBase
	{
		private readonly IClosureIdentification m_Identification;
		private readonly Stack<ClosureDefinition> m_EffectiveClosures;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ClosureHostMethodRewritingVisitor(IClosureIdentification identification)
		{
			m_Identification = identification;
			m_EffectiveClosures = new Stack<ClosureDefinition>();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public override void VisitStatementBlock(StatementBlock statementBlock)
		{
			var effectiveClosure = m_Identification.ClosuresOuterToInner.FirstOrDefault(c => c.ScopeBlock == statementBlock);

			if ( effectiveClosure != null )
			{
				m_EffectiveClosures.Push(effectiveClosure);
			}

			try
			{
				base.VisitStatementBlock(statementBlock);
			}
			finally
			{
				if ( effectiveClosure != null )
				{
					m_EffectiveClosures.Pop();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override bool OnFilterOperand(IOperand operand)
		{
			return (operand is IScopedOperand);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnVisitOperand(ref IOperand operand)
		{
			var effectiveClosure = GetEffectiveClosure();

			if ( effectiveClosure != null )
			{
				effectiveClosure.RewriteOperandIfCaptured(ref operand);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private ClosureDefinition GetEffectiveClosure()
		{
			if ( m_EffectiveClosures.Count > 0 )
			{
				return m_EffectiveClosures.Peek();
			}
			else
			{
				return null;
			}
		}
	}
}