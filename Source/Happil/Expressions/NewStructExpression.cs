﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Happil.Fluent;
using Happil.Statements;

//
// T  0A >0A
// L    
// A
// F
// P  

namespace Happil.Expressions
{
	internal class NewStructExpression<TStruct> : HappilExpression<TStruct>, IValueTypeInitializer
	{
		private readonly Type m_StructType;
		private readonly ConstructorInfo m_Constructor;
		private readonly IHappilOperand[] m_ConstructorArguments;
		private IHappilOperand m_Target;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public NewStructExpression(IHappilOperand[] constructorArguments = null)
			: base(ownerMethod: StatementScope.Current.OwnerMethod)
		{
			var statementScope = StatementScope.Current;

			m_StructType = TypeTemplate.Resolve<TStruct>();
			m_ConstructorArguments = (constructorArguments ?? new IHappilOperand[0]);

			if ( m_ConstructorArguments.Length > 0 )
			{
				var argumentTypes = m_ConstructorArguments.Select(arg => arg.OperandType).ToArray();
				m_Constructor = m_StructType.GetConstructor(argumentTypes);

				if ( m_Constructor == null )
				{
					throw new ArgumentException("Could not find constructor with specified argument types.");
				}

				foreach ( var argument in m_ConstructorArguments.Reverse() )
				{
					statementScope.Consume(argument);
				}
			}
			
			statementScope.RegisterExpressionStatement(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IValueTypeInitializer Members

		public IHappilOperand Target
		{
			set
			{
				m_Target = value;
			}
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitTarget(ILGenerator il)
		{
			// nothing
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitLoad(ILGenerator il)
		{
			HappilLocal<TStruct> tempLocal = (m_Target == null && m_Constructor == null ? OwnerMethod.Local<TStruct>() : null);
			var effectiveTarget = (m_Target ?? tempLocal);

			if ( effectiveTarget != null )
			{
				if ( m_Constructor != null )
				{
					Helpers.EmitCall(il, effectiveTarget, m_Constructor, m_ConstructorArguments);
				}
				else
				{
					effectiveTarget.EmitTarget(il);
					effectiveTarget.EmitAddress(il);

					il.Emit(OpCodes.Initobj, m_StructType);
				}

				if ( ShouldLeaveValueOnStack )
				{
					effectiveTarget.EmitTarget(il);
					effectiveTarget.EmitLoad(il);
				}
			}
			else
			{
				foreach ( var argument in m_ConstructorArguments )
				{
					argument.EmitTarget(il);
					argument.EmitLoad(il);
				}

				il.Emit(OpCodes.Newobj, m_Constructor);

				if ( !ShouldLeaveValueOnStack )
				{
					il.Emit(OpCodes.Pop);
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitStore(ILGenerator il)
		{
			throw new NotSupportedException();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected override void OnEmitAddress(ILGenerator il)
		{
			throw new NotSupportedException();
		}
	}
}