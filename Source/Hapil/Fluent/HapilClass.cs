﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Hapil.Statements;

namespace Hapil.Fluent
{
	internal class HappilClass
	{
		private readonly HappilModule m_OwnerModule;
		private readonly TypeBuilder m_TypeBuilder;
		private readonly List<IHappilMember> m_Members;
		private readonly List<MethodInfo> m_FactoryMethods;
		private readonly Dictionary<Type, IHappilClassDefinition> m_BodiesByBaseType;
		private readonly HashSet<MemberInfo> m_NotImplementedMembers;
		private readonly Dictionary<MemberInfo, IHappilMember> m_ImplementedMembers;
		private Type m_BuiltType = null;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilClass(HappilModule ownerModule, TypeBuilder typeBuilder)
		{
			m_OwnerModule = ownerModule;
            m_TypeBuilder = typeBuilder;
			m_Members = new List<IHappilMember>();
			m_FactoryMethods = new List<MethodInfo>();
			m_BodiesByBaseType = new Dictionary<Type, IHappilClassDefinition>();
			m_NotImplementedMembers = new HashSet<MemberInfo>();
			m_ImplementedMembers = new Dictionary<MemberInfo, IHappilMember>();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal HappilClassBody<TBase> GetBody<TBase>()
		{
			IHappilClassDefinition body;

			if ( !m_BodiesByBaseType.TryGetValue(typeof(TBase), out body) )
			{
				body = CreateNewBody<TBase>();
				m_BodiesByBaseType.Add(typeof(TBase), body);
			}

			return (HappilClassBody<TBase>)body;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void ImplementInterface(Type interfaceType)
		{
			m_TypeBuilder.AddInterfaceImplementation(TypeTemplate.Resolve(interfaceType));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public string TakeMemberName(string proposedName)
		{
			//TODO: check for duplicate names and add suffix if necessary
			return proposedName;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void AddUndeclaredMember(IHappilMember member)
		{
			m_Members.Add(member);
			//RegisterMemberBodyDefinition(member, bodyDefinition);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public TMember GetOrAddDeclaredMember<TMember>(MemberInfo declaration, Func<TMember> memberFactory)
			where TMember : IHappilMember
		{
			IHappilMember member;

			if ( !m_ImplementedMembers.TryGetValue(declaration, out member) )
			{
				member = memberFactory();
				m_Members.Add(member);
				m_ImplementedMembers.Add(declaration, member);
			}

			return (TMember)member;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public TInfo[] TakeNotImplementedMembers<TInfo>(TInfo[] members) where TInfo : MemberInfo
		{
			var membersToImplement = new HashSet<MemberInfo>(members);
			membersToImplement.IntersectWith(m_NotImplementedMembers);
			
			m_NotImplementedMembers.ExceptWith(membersToImplement);
			
			return membersToImplement.Cast<TInfo>().ToArray();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public bool WasMemberImplemented(MemberInfo member)
		{
			return m_ImplementedMembers.ContainsKey(member);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public TMember FindMember<TMember>(string name) where TMember : IHappilMember
		{
			var member = m_Members.OfType<TMember>().Where(m => m.Name == name).FirstOrDefault();

			if ( object.ReferenceEquals(member, null) )
			{
				throw new ArgumentException(string.Format("{0} member named '{1}' was not defined.", typeof(TMember).Name, name));
			}

			return member;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilField GetPropertyBackingField(PropertyInfo declaration)
		{
			var property = m_Members.OfType<IHappilProperty>().FirstOrDefault(p => p.Declaration == declaration);

			if ( object.ReferenceEquals(property, null) )
			{
				throw new ArgumentException(string.Format("Property '{0}' was not implemented by this class.", declaration.Name));
			}

			return property.BackingField;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void DefineFactoryMethod(ConstructorInfo constructor, Type[] parameterTypes)
		{
			var resolvedParameterTypes = parameterTypes.Select(TypeTemplate.Resolve).ToArray();
			var factoryMethodName = TakeMemberName("FactoryMethod" + (m_FactoryMethods.Count + 1).ToString());
			var factoryMethod = m_TypeBuilder.DefineMethod(
				factoryMethodName,
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
				CallingConventions.Standard,
				typeof(object),
				resolvedParameterTypes);

			var il = factoryMethod.GetILGenerator();
			il.DeclareLocal(typeof(object));

			for ( int i = 0 ; i < parameterTypes.Length ; i++ )
			{
				il.Emit(OpCodes.Ldarg, i);
			}

			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ret);

			m_FactoryMethods.Add(factoryMethod);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Called by HappilFactoryBase.TypeEntry constructor.
		/// </summary>
		public Type CreateType()
		{
			DefineMemberBodies();
			EmitMemberBodies();

			m_BuiltType = m_TypeBuilder.CreateType();

			FixupFactoryMethods();
			
			return m_BuiltType;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Called by HappilFactoryBase.TypeEntry constructor.
		/// </summary>
		public Delegate[] GetFactoryMethods()
		{
			return m_FactoryMethods.Select(CreateFactoryMethodDelegate).ToArray();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilModule OwnerModule
		{
			get
			{
				return m_OwnerModule;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public TypeBuilder TypeBuilder
		{
			get
			{
				return m_TypeBuilder;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private HappilClassBody<TBase> CreateNewBody<TBase>()
		{
			var newBody = new HappilClassBody<TBase>(happilClass: this);
			var implementableMembers = newBody.GetImplementableMembers();

			foreach ( var member in implementableMembers )
			{
				if ( !m_ImplementedMembers.ContainsKey(member) )
				{
					m_NotImplementedMembers.Add(member);
				}
			}

			return newBody;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private Delegate CreateFactoryMethodDelegate(MethodInfo factoryMethod)
		{
			var parameters = factoryMethod.GetParameters();
			var openDelegateType = s_DelegatePrototypesByArgumentCount[parameters.Length];
			var delegateTypeParameters = parameters.Select(p => TypeTemplate.Resolve(p.ParameterType)).Concat(new[] { factoryMethod.ReturnType });
			var closedDelegateType = openDelegateType.MakeGenericType(delegateTypeParameters.ToArray());

			return Delegate.CreateDelegate(closedDelegateType, factoryMethod);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void FixupFactoryMethods()
		{
			for ( int i = 0 ; i < m_FactoryMethods.Count ; i++ )
			{
				var runtimeMethod = m_BuiltType.GetMethod(m_FactoryMethods[i].Name, BindingFlags.Public | BindingFlags.Static);
				m_FactoryMethods[i] = runtimeMethod;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		//private void RegisterMemberBodyDefinition(IHappilMember member, Action bodyDefinition)
		//{
		//	if ( bodyDefinition != null )
		//	{
		//		Stack<Action> bodyDefinitionStack;

		//		if ( !m_MemberBodyDefinitions.TryGetValue(member, out bodyDefinitionStack) )
		//		{
		//			bodyDefinitionStack = new Stack<Action>();
		//			m_MemberBodyDefinitions.Add(member, bodyDefinitionStack);
		//		}

		//		bodyDefinitionStack.Push(bodyDefinition);
		//	}
		//}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void EmitMemberBodies()
		{
			foreach ( var member in m_Members )
			{
				using ( member.CreateTypeTemplateScope() )
				{
					member.EmitBody();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void DefineMemberBodies()
		{
			for ( int index = 0 ; index < m_Members.Count ; index++ )
			{
				var member = m_Members[index];
				//Stack<Action> bodyDefinitionStack;

				//if ( !m_MemberBodyDefinitions.TryGetValue(member, out bodyDefinitionStack) )
				//{
				//	continue;
				//}

				using ( member.CreateTypeTemplateScope() )
				{
					member.DefineBody();
					//while ( bodyDefinitionStack.Count > 0 )
					//{
					//	var action = bodyDefinitionStack.Pop();
					//	action();
						
					//	if ( StatementScope.Exists )
					//	{
					//		throw new InvalidOperationException(
					//			string.Format("Scope stack is not empty after body definition of member '{0}' ({1}).", member.Name, member.GetType().Name));
					//	}
					//}
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private static readonly Type[] s_DelegatePrototypesByArgumentCount = new Type[] {
			typeof(Func<>), 
			typeof(Func<,>), 
			typeof(Func<,,>), 
			typeof(Func<,,,>), 
			typeof(Func<,,,,>), 
			typeof(Func<,,,,,>), 
			typeof(Func<,,,,,,>), 
			typeof(Func<,,,,,,,>), 
			typeof(Func<,,,,,,,,>)
		};
	}
}
