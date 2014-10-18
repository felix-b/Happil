﻿using System;
using System.Collections.Generic;
using Hapil.Operands;
using Hapil.Statements;
using Hapil.Writers;

namespace Hapil.Decorators
{
	public class MethodDecorationBuilder
	{
		private readonly DecoratingMethodWriter m_OwnerWriter;
		private LinkedList<Action<MethodWriterBase>> m_OnBefore;
		private LinkedList<Action<MethodWriterBase, Argument<TypeTemplate.TArgument>>> m_OnInputArgument;
		private LinkedList<Action<MethodWriterBase>> m_OnReturnVoid;
		private LinkedList<Action<MethodWriterBase, Local<TypeTemplate.TReturn>>> m_OnReturnValue;
		private LinkedList<Action<MethodWriterBase, Argument<TypeTemplate.TArgument>>> m_OnOutputArgument;
		private LinkedList<Action<MethodWriterBase>> m_OnSuccess;
		private readonly Dictionary<Type, LinkedList<Action<MethodWriterBase, IHapilCatchSyntax>>> m_OnCatchExceptions;
		private LinkedList<Action<MethodWriterBase>> m_OnFailure;
		private LinkedList<Action<MethodWriterBase>> m_OnAfter;
		private bool m_IsEmpty;

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		internal MethodDecorationBuilder(DecoratingMethodWriter ownerWriter)
		{
			m_OwnerWriter = ownerWriter;
			m_OnCatchExceptions = new Dictionary<Type, LinkedList<Action<MethodWriterBase, IHapilCatchSyntax>>>();
			m_IsEmpty = true;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder Attribute<TAttribute>(Action<AttributeArgumentWriter<TAttribute>> values = null)
			where TAttribute : Attribute
		{
			m_OwnerWriter.Attribute<TAttribute>(values);
			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnBefore(Action<MethodWriterBase> decoration)
		{
			if ( m_OnBefore == null )
			{
				m_OnBefore = new LinkedList<Action<MethodWriterBase>>();
			}

			m_OnBefore.AddFirst(decoration);
			m_IsEmpty = false;
			
			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnInputArgument(Action<MethodWriterBase, Argument<TypeTemplate.TArgument>> decoration)
		{
			if ( m_OnInputArgument == null )
			{
				m_OnInputArgument = new LinkedList<Action<MethodWriterBase, Argument<TypeTemplate.TArgument>>>();
			}

			m_OnInputArgument.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnReturnVoid(Action<MethodWriterBase> decoration)
		{
			if ( m_OnReturnVoid == null )
			{
				m_OnReturnVoid = new LinkedList<Action<MethodWriterBase>>();
			}
			
			m_OnReturnVoid.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnReturnValue(Action<MethodWriterBase, Local<TypeTemplate.TReturn>> decoration)
		{
			if ( m_OnReturnValue == null )
			{
				m_OnReturnValue = new LinkedList<Action<MethodWriterBase, Local<TypeTemplate.TReturn>>>();
			}

			m_OnReturnValue.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnOutputArgument(Action<MethodWriterBase, Argument<TypeTemplate.TArgument>> decoration)
		{
			if ( m_OnOutputArgument == null )
			{
				m_OnOutputArgument = new LinkedList<Action<MethodWriterBase, Argument<TypeTemplate.TArgument>>>();
			}

			m_OnOutputArgument.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnSuccess(Action<MethodWriterBase> decoration)
		{
			if ( m_OnSuccess == null )
			{
				m_OnSuccess = new LinkedList<Action<MethodWriterBase>>();
			}

			m_OnSuccess.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnException<TException>(Action<MethodWriterBase, Operand<TException>> decoration)
			where TException : Exception
		{
			LinkedList<Action<MethodWriterBase, IHapilCatchSyntax>> handlerList;

			if ( !m_OnCatchExceptions.TryGetValue(typeof(TException), out handlerList) )
			{
				handlerList = new LinkedList<Action<MethodWriterBase, IHapilCatchSyntax>>();
				m_OnCatchExceptions.Add(typeof(TException), handlerList);
			}

			handlerList.AddLast((method, tryCatchStatement) => tryCatchStatement.Catch<TException>(excepion => decoration(method, excepion)));
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnFailure(Action<MethodWriterBase> decoration)
		{
			if ( m_OnFailure == null )
			{
				m_OnFailure = new LinkedList<Action<MethodWriterBase>>();
			}

			m_OnFailure.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public MethodDecorationBuilder OnAfter(Action<MethodWriterBase> decoration)
		{
			if ( m_OnAfter == null )
			{
				m_OnAfter = new LinkedList<Action<MethodWriterBase>>();
			}

			m_OnAfter.AddLast(decoration);
			m_IsEmpty = false;

			return this;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		internal void ApplyDecoration()
		{
			var successLocal = (m_OnFailure != null ? m_OwnerWriter.Local<bool>(initialValueConst: false) : null);

			if ( m_OnBefore != null )
			{
				m_OnBefore.ForEach(f => f(m_OwnerWriter));
			}

			InspectInputArguments();

			var tryBlock = m_OwnerWriter.Try(
				() => {
					var returnValueExpression = m_OwnerWriter.Proceed<TypeTemplate.TReturn>();

					if ( m_OnFailure != null )
					{
						successLocal.Assign(true);
					}

					InspectReturnValue(returnValueExpression);
					InspectOutputArguments();
				});

			foreach ( var exceptionHandler in m_OnCatchExceptions.Values )
			{
				exceptionHandler.ForEach(f => f(m_OwnerWriter, tryBlock));
			}

			tryBlock.Finally(() => {
				if ( m_OnFailure != null )
				{
					m_OwnerWriter.If(!successLocal).Then(() => m_OnFailure.ForEach(f => f(m_OwnerWriter)));
				}

				if ( m_OnAfter != null )
				{
					m_OnAfter.ForEach(f => f(m_OwnerWriter));
				}
			});
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		internal bool IsEmpty
		{
			get
			{
				return m_IsEmpty;
			}
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		private void InspectInputArguments()
		{
			if ( m_OnInputArgument != null )
			{
				m_OwnerWriter.ForEachArgument(arg => {
					if ( !arg.IsOut )
					{
						m_OnInputArgument.ForEach(f => f(m_OwnerWriter, arg));
					}
				});
			}
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		private void InspectReturnValue(Operand<TypeTemplate.TReturn> returnValueExpression)
		{
			if ( !ReferenceEquals(returnValueExpression, null) )
			{
				var returnValue = m_OwnerWriter.Local<TypeTemplate.TReturn>(initialValue: returnValueExpression);

				if ( m_OnReturnValue != null )
				{
					m_OnReturnValue.ForEach(f => f(m_OwnerWriter, returnValue));
				}

				if ( m_OnSuccess != null )
				{
					m_OnSuccess.ForEach(f => f(m_OwnerWriter));
				}

				m_OwnerWriter.AddReturnStatement(returnValue);
			}
			else
			{
				if ( m_OnReturnVoid != null )
				{
					m_OnReturnVoid.ForEach(f => f(m_OwnerWriter));
				}

				if ( m_OnSuccess != null )
				{
					m_OnSuccess.ForEach(f => f(m_OwnerWriter));
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void InspectOutputArguments()
		{
			if ( m_OnOutputArgument != null )
			{
				m_OwnerWriter.ForEachArgument(arg => {
					if ( arg.IsByRef || arg.IsOut )
					{
						m_OnOutputArgument.ForEach(f => f(m_OwnerWriter, arg));
					}
				});
			}
		}
	}
}