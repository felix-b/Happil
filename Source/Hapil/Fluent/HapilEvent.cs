﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using Hapil.Expressions;

namespace Hapil.Fluent
{
	public class HappilEvent : IHappilMember
	{
		private const MethodAttributes ContainedMethodAttributes = 
			MethodAttributes.Public | 
			MethodAttributes.NewSlot | 
			MethodAttributes.Final | 
			MethodAttributes.HideBySig | 
			MethodAttributes.SpecialName | 
			MethodAttributes.Virtual;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private readonly HappilClass m_HappilClass;
		private readonly EventInfo m_Declaration;
		private readonly EventBuilder m_EventBuilder;
		private readonly HappilField m_BackingField;
		private readonly VoidHappilMethod m_AddMethod;
		private readonly VoidHappilMethod m_RemoveMethod;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal HappilEvent(HappilClass happilClass, EventInfo declaration)
		{
			m_HappilClass = happilClass;
			m_Declaration = declaration;
			m_EventBuilder = happilClass.TypeBuilder.DefineEvent(declaration.Name, declaration.Attributes, declaration.EventHandlerType);

			using ( CreateTypeTemplateScope() )
			{
				m_BackingField = new HappilField(happilClass, "m_" + declaration.Name + "EventHandler", typeof(TypeTemplate.TEventHandler));

				m_AddMethod = new VoidHappilMethod(happilClass, declaration.GetAddMethod(), ContainedMethodAttributes);
				m_EventBuilder.SetAddOnMethod(m_AddMethod.MethodBuilder);

				m_RemoveMethod = new VoidHappilMethod(happilClass, declaration.GetRemoveMethod(), ContainedMethodAttributes);
				m_EventBuilder.SetRemoveOnMethod(m_RemoveMethod.MethodBuilder);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilMember Members

		void IHappilMember.DefineBody()
		{
			DefineDefaultAddRemoveMethods();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		void IHappilMember.EmitBody()
		{
			((IHappilMember)m_AddMethod).EmitBody();
			((IHappilMember)m_RemoveMethod).EmitBody();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IDisposable CreateTypeTemplateScope()
		{
			return TypeTemplate.CreateScope(typeof(TypeTemplate.TEventHandler), m_Declaration.EventHandlerType);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		MemberInfo IHappilMember.Declaration
		{
			get
			{
				return m_Declaration;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public string Name
		{
			get
			{
				return m_Declaration.Name;
			}
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilEvent Set<TAttribute>(Action<IHappilAttributeBuilder<TAttribute>> values = null)
			where TAttribute : Attribute
		{
			var builder = new HappilAttributeBuilder<TAttribute>(values);
			m_EventBuilder.SetCustomAttribute(builder.GetAttributeBuilder());
			return this;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void RaiseEvent(IHappilMethodBodyBase m, IHappilOperand args)
		{
			using ( CreateTypeTemplateScope() )
			{
				m.If(m_BackingField.AsOperand<TypeTemplate.TEventHandler>() != m.Const<TypeTemplate.TEventHandler>(null)).Then(() => {
					m_BackingField.AsOperand<TypeTemplate.TEventHandler>().Invoke(m.This<TypeTemplate.TBase>(), args);
				});
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public FieldAccessOperand<TypeTemplate.TEventHandler> BackingField
		{
			get
			{
				return m_BackingField.AsOperand<TypeTemplate.TEventHandler>();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal void SetAttributes(Func<HappilAttributes> attributes)
		{
			if ( attributes != null )
			{
				foreach ( var attribute in attributes().GetAttributes() )
				{
					m_EventBuilder.SetCustomAttribute(attribute);
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void DefineDefaultAddRemoveMethods()
		{
			using ( CreateTypeTemplateScope() )
			{
				using ( m_AddMethod.CreateBodyScope() )
				{
					DefineAddMethodBody(m_AddMethod, new HappilArgument<TypeTemplate.TEventHandler>(m_AddMethod, 1));
				}

				using ( m_RemoveMethod.CreateBodyScope() )
				{
					DefineRemoveMethodBody(m_RemoveMethod, new HappilArgument<TypeTemplate.TEventHandler>(m_RemoveMethod, 1));
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void DefineAddMethodBody(IVoidHappilMethodBody m, HappilArgument<TypeTemplate.TEventHandler> value)
		{
			var oldHandler = m.Local<TypeTemplate.TEventHandler>();
			var newHandler = m.Local<TypeTemplate.TEventHandler>();
			var lastHandler = m.Local<TypeTemplate.TEventHandler>(initialValue: m_BackingField.AsOperand<TypeTemplate.TEventHandler>());

			m.Do(loop => {
				oldHandler.Assign(lastHandler);

				newHandler.Assign(Static.Func(Delegate.Combine,
					oldHandler.CastTo<Delegate>(),
					value.CastTo<Delegate>()).CastTo<TypeTemplate.TEventHandler>());

				lastHandler.Assign(Static.GenericFunc((x, y, z) => Interlocked.CompareExchange(ref x, y, z),
					m_BackingField.AsOperand<TypeTemplate.TEventHandler>(),
					newHandler,
					oldHandler));
			}).While(lastHandler != oldHandler);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void DefineRemoveMethodBody(IVoidHappilMethodBody m, HappilArgument<TypeTemplate.TEventHandler> value)
		{
			var oldHandler = m.Local<TypeTemplate.TEventHandler>();
			var newHandler = m.Local<TypeTemplate.TEventHandler>();
			var lastHandler = m.Local<TypeTemplate.TEventHandler>(initialValue: m_BackingField.AsOperand<TypeTemplate.TEventHandler>());

			m.Do(loop => {
				oldHandler.Assign(lastHandler);

				newHandler.Assign(Static.Func(Delegate.Remove, 
					oldHandler.CastTo<Delegate>(), 
					value.CastTo<Delegate>()).CastTo<TypeTemplate.TEventHandler>());

				lastHandler.Assign(Static.GenericFunc((x, y, z) => Interlocked.CompareExchange(ref x, y, z),
					m_BackingField.AsOperand<TypeTemplate.TEventHandler>(),
					newHandler,
					oldHandler));
			}).While(lastHandler != oldHandler);
		}
	}
}
