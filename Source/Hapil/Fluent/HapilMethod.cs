﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Hapil.Expressions;
using Hapil.Selectors;
using Hapil.Statements;

namespace Hapil.Fluent
{
	internal class HappilMethod : IHappilMethodBodyTemplate, IHappilMember
	{
		private readonly HappilClass m_HappilClass;
		private readonly MethodBuilder m_MethodBuilder;
		private readonly MethodInfo m_Declaration;
		private readonly List<IHappilStatement> m_Statements;
		private readonly Type[] m_ArgumentTypes;
		private readonly string[] m_ArgumentNames;
		private readonly bool[] m_ArgumentIsOut;
		private readonly bool m_IsStatic;
		private readonly List<Action> m_BodyDefinitions;
		private Type[] m_TemplateActualTypePairs = null;
		private HappilAttributes m_ReturnAttributes = null;
		private int m_CurrentBodyDefinitionIndex;
		private HappilLocal<TypeTemplate.TReturn> m_ReturnValueLocal = null;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilMethod(HappilClass happilClass, MethodInfo declaration)
			: this(happilClass, declaration, GetMethodAttributesFor(declaration))
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilMethod(HappilClass happilClass, MethodInfo declaration, MethodAttributes methodAttributes)
			: this(happilClass)
		{
			m_IsStatic = false;
			m_Declaration = declaration;
			m_MethodBuilder = happilClass.TypeBuilder.DefineMethod(
				happilClass.TakeMemberName(declaration.Name),
				methodAttributes,
				declaration.ReturnType, 
				declaration.GetParameters().Select(p => p.ParameterType).ToArray());

			//if ( !declaration.IsSpecialName )
			//{
				happilClass.TypeBuilder.DefineMethodOverride(m_MethodBuilder, declaration);
			//}

			m_ArgumentTypes = declaration.GetParameters().Select(p => p.ParameterType).ToArray();
			m_ArgumentNames = declaration.GetParameters().Select(p => p.Name).ToArray();
			m_ArgumentIsOut = declaration.GetParameters().Select(p => p.IsOut).ToArray();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilMethod(HappilClass happilClass, string name, Type returnType, Type[] argumentTypes)
			: this(happilClass)
		{
			m_IsStatic = true;
			m_MethodBuilder = happilClass.TypeBuilder.DefineMethod(
				happilClass.TakeMemberName(name),
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				returnType,
				argumentTypes);

			m_Declaration = m_MethodBuilder;
			m_ArgumentTypes = argumentTypes;
			m_ArgumentNames = CreateDefaultArgumentNames(argumentTypes);
			m_ArgumentIsOut = new bool[m_ArgumentTypes.Length]; // all false
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected HappilMethod(HappilClass happilClass)
		{
			m_HappilClass = happilClass;
			m_Statements = new List<IHappilStatement>();
			m_BodyDefinitions = new List<Action>(capacity: 16);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilMethodBodyBase Members

		public IHappilIfBody If(IHappilOperand<bool> condition)
		{
			return AddStatement(new IfStatement(condition));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilIfBody If(IHappilOperand<bool> condition, bool isTautology)
		{
			return AddStatement(new IfStatement(condition, isTautology));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> Iif<T>(IHappilOperand<bool> condition, IHappilOperand<T> onTrue, IHappilOperand<T> onFalse)
		{
			return new TernaryConditionalOperator<T>(condition, onTrue, onFalse);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> Iif<T>(IHappilOperand<bool> condition, bool isTautology, IHappilOperand<T> onTrue, IHappilOperand<T> onFalse)
		{
			if ( !isTautology )
			{
				return new TernaryConditionalOperator<T>(condition, onTrue, onFalse);
			}
			else
			{
				var scope = StatementScope.Current;
				scope.Consume(condition);
				scope.Consume(onFalse);
				
				return (HappilOperand<T>)onTrue;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilWhileSyntax While(IHappilOperand<bool> condition)
		{
			return AddStatement(new WhileStatement(condition));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilDoWhileSyntax Do(Action<IHappilLoopBody> body)
		{
			return AddStatement(new DoWhileStatement(body));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilForShortSyntax For(HappilConstant<int> from, IHappilOperand<int> to, int increment = 1)
		{
			return new HappilForShortSyntax(this, from, to.CastTo<int>(), increment);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilForShortSyntax For(IHappilOperand<int> from, HappilConstant<int> to, int increment = 1)
		{
			return new HappilForShortSyntax(this, from.CastTo<int>(), to, increment);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilForShortSyntax For(IHappilOperand<int> from, IHappilOperand<int> to, int increment = 1)
		{
			return new HappilForShortSyntax(this, from.CastTo<int>(), to.CastTo<int>(), increment);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilForWhileSyntax For(Action precondition)
		{
			return AddStatement(new ForStatement(precondition));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilForeachInSyntax<T> Foreach<T>(HappilLocal<T> element)
		{
			return AddStatement(new ForeachStatement<T>(element));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilForeachDoSyntax<T> ForeachElementIn<T>(IHappilOperand<IEnumerable<T>> collection)
		{
			var element = this.Local<T>();
			var statement = new ForeachStatement<T>(element);
			
			AddStatement(statement);
			statement.In(collection);

			return statement;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------
		
		public IHappilUsingSyntax Using(IHappilOperand<IDisposable> disposable)
		{
			return AddStatement(new UsingStatement(disposable));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilLockSyntax Lock(IHappilOperand<object> syncRoot, int millisecondsTimeout)
		{
			return AddStatement(new LockStatement(syncRoot, millisecondsTimeout));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilCatchSyntax Try(Action body)
		{
			return AddStatement(new TryStatement(body));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilSwitchSyntax<T> Switch<T>(IHappilOperand<T> value)
		{
			return AddStatement(new SwitchStatement<T>(value));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilThis<TBase> This<TBase>()
		{
			return new HappilThis<TBase>(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilLocal<T> Local<T>()
		{
			return new HappilLocal<T>(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilLocal<T> Local<T>(IHappilOperand<T> initialValue)
		{
			var local = new HappilLocal<T>(this);
			local.Assign(initialValue);
			return local;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilLocal<T> Local<T>(T initialValueConst)
		{
			return Local<T>(new HappilConstant<T>(initialValueConst));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilConstant<T> Const<T>(T value)
		{
			return new HappilConstant<T>(value);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<T> Default<T>()
		{
			var actualType = TypeTemplate.Resolve<T>();

			if ( actualType.IsPrimitive || !actualType.IsValueType )
			{
				return new HappilConstant<T>(default(T));
			}
			else
			{
				return new NewStructExpression<T>();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilOperand<TObject> New<TObject>(params IHappilOperand[] constructorArguments)
		{
			if ( TypeTemplate.Resolve<TObject>().IsValueType )
			{
				return new NewStructExpression<TObject>(constructorArguments);
			}
			else
			{
				return new NewObjectExpression<TObject>(constructorArguments);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilOperand<TElement[]> NewArray<TElement>(IHappilOperand<int> length)
		{
			return new HappilUnaryExpression<int, TElement[]>(
				ownerMethod: null, 
				@operator: new UnaryOperators.OperatorNewArray<TElement>(),
				operand: length);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilOperand<TElement[]> NewArray<TElement>(params TElement[] constantValues)
		{
			return NewArray<TElement>(constantValues.Select(v => new HappilConstant<TElement>(v)).ToArray());
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilOperand<TElement[]> NewArray<TElement>(params IHappilOperand<TElement>[] values)
		{
			var arrayLocal = Local<TElement[]>(NewArray<TElement>(Const(values.Length)));

			for ( int i = 0 ; i < values.Length ; i++ )
			{
				arrayLocal.ElementAt(i).Assign(values[i]);
			}

			return arrayLocal;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void RaiseEvent(string eventName, IHappilOperand<EventArgs> eventArgs)
		{
			var eventMember = m_HappilClass.FindMember<HappilEvent>(eventName);
			eventMember.RaiseEvent(this, eventArgs);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void Throw<TException>(string message) where TException : Exception
		{
			AddStatement(new ThrowStatement(typeof(TException), message));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void Throw()
		{
			AddStatement(new RethrowStatement());
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TReturn>> Delegate<TArg1, TReturn>(Action<IHappilMethodBody<TReturn>, HappilArgument<TArg1>> body)
		{
			return new HappilAnonymousDelegate<TArg1, TReturn>(m_HappilClass, body);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TReturn>> Delegate<TArg1, TReturn>(
			ref IHappilDelegate site, 
			Action<IHappilMethodBody<TReturn>, 
			HappilArgument<TArg1>> body)
		{
			if ( site == null )
			{
				site = (IHappilDelegate)Delegate(body);
			}
			
			return (HappilAnonymousDelegate<TArg1, TReturn>)site;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TArg2, TReturn>> Delegate<TArg1, TArg2, TReturn>(
			Action<IHappilMethodBody<TReturn>, HappilArgument<TArg1>, HappilArgument<TArg2>> body)
		{
			return new HappilAnonymousDelegate<TArg1, TArg2, TReturn>(m_HappilClass, body);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TArg2, TReturn>> Delegate<TArg1, TArg2, TReturn>(
			ref IHappilDelegate site,
			Action<IHappilMethodBody<TReturn>, HappilArgument<TArg1>, HappilArgument<TArg2>> body)
		{
			if ( site == null )
			{
				site = (IHappilDelegate)Delegate(body);
			}

			return (HappilAnonymousDelegate<TArg1, TArg2, TReturn>)site;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<TMethod> MakeDelegate<TTarget, TMethod>(IHappilOperand<TTarget> target, Expression<Func<TTarget, TMethod>> methodSelector)
		{
			var method = Helpers.ResolveMethodFromLambda(methodSelector);
			return new HappilDelegate<TMethod>(target, method);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TResult>> Lambda<TArg1, TResult>(Func<HappilOperand<TArg1>, IHappilOperand<TResult>> expression)
		{
			return Delegate<TArg1, TResult>((m, arg1) => m.Return(expression(arg1)));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TResult>> Lambda<TArg1, TResult>(
			ref IHappilDelegate site,
			Func<HappilOperand<TArg1>, IHappilOperand<TResult>> expression)
		{
			if ( site == null )
			{
				site = (IHappilDelegate)Lambda(expression);
			}

			return (HappilAnonymousDelegate<TArg1, TResult>)site;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TArg2, TResult>> Lambda<TArg1, TArg2, TResult>(
			ref IHappilDelegate site,
			Func<HappilOperand<TArg1>, HappilOperand<TArg2>, IHappilOperand<TResult>> expression)
		{
			if ( site == null )
			{
				site = (IHappilDelegate)Lambda(expression);
			}
			
			return (HappilAnonymousDelegate<TArg1, TArg2, TResult>)site;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilOperand<Func<TArg1, TArg2, TResult>> Lambda<TArg1, TArg2, TResult>(
			Func<HappilOperand<TArg1>, HappilOperand<TArg2>, IHappilOperand<TResult>> expression)
		{
			return Delegate<TArg1, TArg2, TResult>((m, arg1, arg2) => m.Return(expression(arg1, arg2)));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void EmitFromLambda(Expression<Action> lambda)
		{
			var callInfo = (MethodCallExpression)lambda.Body;
			var methodInfo = callInfo.Method;
			var arguments = callInfo.Arguments.Select(Helpers.GetLambdaArgumentAsConstant).ToArray();
			var happilExpression = new HappilUnaryExpression<object, object>(
				ownerMethod: this, 
				@operator: new UnaryOperators.OperatorCall<object>(methodInfo, arguments), 
				operand: null);

			//var arguments = new IHappilOperandInternals[callInfo.Arguments.Count];

			//for ( int i = 0 ; i < arguments.Length ; i++ )
			//{
			//	//var argument = callInfo.Arguments[i];


			//	//Expression<Func<object>> argumentLambda = Expression.Lambda<Func<object>>(argument);
			//	//var argumentValueFunc = argumentLambda.Compile();
			//	//var argumentValue = argumentValueFunc();

			//	arguments[i] = Helpers.GetLambdaArgumentAsConstant(callInfo.Arguments[i]);
			//}

			//m_HappilClass.CurrentScope.AddStatement();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void ForEachArgument(Action<HappilArgument<TypeTemplate.TArgument>> action)
		{
			var argumentTypes = GetArgumentTypes();
			var indexBase = (IsStatic ? 0 : 1);

			for ( byte i = 0 ; i < argumentTypes.Length ; i++ )
			{
				using ( TypeTemplate.CreateScope<TypeTemplate.TArgument>(argumentTypes[i]) )
				{
					var argument = this.Argument<TypeTemplate.TArgument>((byte)(i + indexBase));
					action(argument);
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilArgument<T> Argument<T>(byte index)
		{
			return new HappilArgument<T>(this, index);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public virtual MethodInfo MethodInfo
		{
			get
			{
				if ( m_Declaration != null )
				{
					return (MethodInfo)m_MethodBuilder;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public virtual MethodBuilder MethodBuilder
		{
			get
			{
				if ( m_Declaration != null )
				{
					return m_MethodBuilder;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public virtual int ArgumentCount
		{
			get
			{
				return GetArgumentTypes().Length;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public virtual Type ReturnType
		{
			get
			{
				if ( m_Declaration != null )
				{
					return m_Declaration.ReturnType;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IHappilAttributes ReturnAttributes
		{
			get
			{
				if ( m_ReturnAttributes == null )
				{
					m_ReturnAttributes = new HappilAttributes();
				}

				return m_ReturnAttributes;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilMethodBodyTemplate Members

		public HappilOperand<TypeTemplate.TReturn> Proceed()
		{
			if ( m_CurrentBodyDefinitionIndex < m_BodyDefinitions.Count - 1 )
			{
				m_CurrentBodyDefinitionIndex++;

				try
				{
					var innerDefinition = m_BodyDefinitions[m_CurrentBodyDefinitionIndex];
					innerDefinition();
				}
				finally
				{
					m_CurrentBodyDefinitionIndex--;
				}
			}
			
			return m_ReturnValueLocal;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------
		
		public void Return(IHappilOperand<TypeTemplate.TReturn> operand)
		{
			AddReturnStatement(operand);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected void AddReturnStatement(IHappilOperand<TypeTemplate.TReturn> returnValue)
		{
			if ( m_CurrentBodyDefinitionIndex > 0 )
			{
				if ( object.ReferenceEquals(m_ReturnValueLocal, null) )
				{
					m_ReturnValueLocal = this.Local<TypeTemplate.TReturn>();
				}

				m_ReturnValueLocal.Assign(returnValue.OrNullConstant());
			}
			else
			{
				//TODO: verify that current scope belongs to this method
				StatementScope.Current.AddStatement(new ReturnStatement<TypeTemplate.TReturn>(returnValue));
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void Return()
		{
			StatementScope.Current.AddStatement(new ReturnStatement());
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilMember Members

		void IHappilMember.DefineBody()
		{
			m_ReturnValueLocal = null;
			m_CurrentBodyDefinitionIndex = 0;
			
			var outermostDefinition = m_BodyDefinitions.First();

			using ( CreateTypeTemplateScope() )
			{
				using ( CreateBodyScope() )
				{
					outermostDefinition();
				}
			}

			DefineReturnAttributes();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		void IHappilMember.EmitBody()
		{
			var il = GetILGenerator();

			foreach ( var statement in m_Statements )
			{
				statement.Emit(il);
			}

			if ( IsVoidMethod() )
			{
				il.Emit(OpCodes.Ret);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IDisposable CreateTypeTemplateScope()
		{
			if ( m_TemplateActualTypePairs == null )
			{
				m_TemplateActualTypePairs = BuildTemplateActualTypePairs();
			}

			return TypeTemplate.CreateScope(m_TemplateActualTypePairs);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		MemberInfo IHappilMember.Declaration
		{
			get
			{
				return GetDeclaration();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		string IHappilMember.Name
		{
			get
			{
				return GetName();
			}
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region Overrides of Object

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append("{");

			foreach ( var statement in m_Statements )
			{
				text.Append(statement.ToString() + ";");
			}

			text.Append("}");
			return text.ToString();
		}

		#endregion

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public StatementScope CreateBodyScope()
		{
			return new StatementScope(m_HappilClass, this, m_Statements);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void SetAttributes(Func<IHappilMethodBodyBase, IHappilAttributes> attributes)
		{
			if ( attributes != null )
			{
				SetAttributes(attributes(this) as HappilAttributes);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public virtual void SetAttributes(HappilAttributes attributes)
		{
			if ( attributes != null )
			{
				foreach ( var attribute in attributes.GetAttributes() )
				{
					SetCustomAttribute(attribute);
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void AddBodyDefinition(Action bodyDefinition)
		{
			m_BodyDefinitions.Insert(0, bodyDefinition);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void DefineReturnAttributes()
		{
			if ( m_ReturnAttributes != null && m_MethodBuilder != null && m_Declaration.ReturnParameter != null )
			{
				var returnParameter = m_MethodBuilder.DefineParameter(0, m_Declaration.ReturnParameter.Attributes, m_Declaration.ReturnParameter.Name);

				foreach ( var attribute in m_ReturnAttributes.GetAttributes() )
				{
					returnParameter.SetCustomAttribute(attribute);
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilClass HappilClass
		{
			get
			{
				return m_HappilClass;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public bool IsStatic
		{
			get
			{
				return m_IsStatic;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected virtual ILGenerator GetILGenerator()
		{
			return m_MethodBuilder.GetILGenerator();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected virtual MethodBase GetDeclaration()
		{
			return m_MethodBuilder;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected virtual Type GetReturnType()
		{
			return m_MethodBuilder.ReturnType;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected virtual Type[] GetArgumentTypes()
		{
			return m_ArgumentTypes;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected virtual string[] GetArgumentNames()
		{
			return m_ArgumentNames;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal protected virtual bool[] GetArgumentIsOut()
		{
			return m_ArgumentIsOut;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal virtual ParameterBuilder DefineParameter(int position)
		{
			if ( m_Declaration == null || m_Declaration is MethodBuilder )
			{
				return m_MethodBuilder.DefineParameter(position, ParameterAttributes.None, strParamName: null);
			}
			else
			{
				var declaredParameter = m_Declaration.GetParameters()[position];
				return m_MethodBuilder.DefineParameter(position + 1, declaredParameter.Attributes, declaredParameter.Name);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected virtual string GetName()
		{
			return m_MethodBuilder.Name;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected virtual Type[] BuildTemplateActualTypePairs()
		{
			var parameterTypes = m_ArgumentTypes;// m_Declaration.GetParameters().Select(p => p.ParameterType).ToArray();
			var pairs = new Type[(1 + parameterTypes.Length) * 2];

			pairs[0] = typeof(TypeTemplate.TReturn);
			pairs[1] = m_MethodBuilder.ReturnType;

			TypeTemplate.BuildArgumentsTypePairs(parameterTypes, pairs, arrayStartIndex: 2);

			return pairs;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected virtual void SetCustomAttribute(CustomAttributeBuilder attribute)
		{
			m_MethodBuilder.SetCustomAttribute(attribute);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected bool IsVoidMethod()
		{
			var returnType = GetReturnType();
			return (returnType == null || returnType == typeof(void));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal TStatement AddStatement<TStatement>(TStatement statement) where TStatement : IHappilStatement
		{
			StatementScope.Current.AddStatement(statement);
			return statement;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal IHappilMethodBodyTemplate GetMethodBodyTemplate()
		{
			return new Hapil.StronglyTyped.MethodBody(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal IHappilMethodBody<TReturn> GetMethodBody<TReturn>()
		{
			return new Hapil.StronglyTyped.MethodBody<TReturn>(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		internal IVoidHappilMethodBody GetVoidMethodBody()
		{
			return new Hapil.StronglyTyped.VoidMethodBody(this);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private HappilOperand<T> CreateUnaryExpression<T>(IUnaryOperator<T> @operator, IHappilOperand<T> operand)
		{
			return new HappilUnaryExpression<T, T>(
				ownerMethod: null,
				@operator: @operator,
				operand: operand);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		protected static string[] CreateDefaultArgumentNames(Type[] argumentTypes)
		{
			var names = new string[argumentTypes.Length];

			for ( int i = 0 ; i < names.Length ; i++ )
			{
				names[i] = "p" + (i + 1).ToString();
			}

			return names;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private static MethodAttributes GetMethodAttributesFor(MethodInfo declaration)
		{
			var attributes =
				MethodAttributes.Final |
				MethodAttributes.HideBySig |
				MethodAttributes.Public |
				MethodAttributes.Virtual;

			if ( declaration != null && declaration.DeclaringType.IsInterface )
			{
				attributes |= MethodAttributes.NewSlot;
			}

			return attributes;
		}
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	internal class HappilMethod<TReturn> : HappilMethod, IHappilMethodBody<TReturn>
	{
		public HappilMethod(HappilClass happilClass, MethodInfo declaration)
			: base(happilClass, declaration)
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public HappilMethod(HappilClass happilClass, string name, Type returnType, Type[] argumentTypes)
			: base(happilClass, name, returnType, argumentTypes)
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		#region IHappilMethodBody<TReturn> Members

		public void Return(IHappilOperand<TReturn> operand)
		{
			AddReturnStatement(operand.OrNullConstant().CastTo<TypeTemplate.TReturn>());
			//StatementScope.Current.AddStatement(new ReturnStatement<TReturn>(operand.OrNullConstant()));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void ReturnConst(TReturn constantValue)
		{
			AddReturnStatement(new HappilConstant<TReturn>(constantValue).CastTo<TypeTemplate.TReturn>());
			////TODO: verify that current scope belongs to this method
			//StatementScope.Current.AddStatement(new ReturnStatement<TReturn>(new HappilConstant<TReturn>(constantValue)));
		}

		#endregion
	}

	//---------------------------------------------------------------------------------------------------------------------------------------------------------

	internal class VoidHappilMethod : HappilMethod, IVoidHappilMethodBody
	{
		public VoidHappilMethod(HappilClass happilClass, MethodInfo declaration)
			: base(happilClass, declaration)
		{
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------

		public VoidHappilMethod(HappilClass happilClass, MethodInfo declaration, MethodAttributes methodAttributes)
			: base(happilClass, declaration, methodAttributes)
		{

		}
	}

}
