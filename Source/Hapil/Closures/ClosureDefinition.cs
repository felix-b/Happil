﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Hapil.Expressions;
using Hapil.Members;
using Hapil.Operands;
using Hapil.Statements;
using Hapil.Writers;

namespace Hapil.Closures
{
	internal class ClosureDefinition
	{
		private readonly StatementBlock m_HostScopeBlock;
		private readonly ClassType m_OwnerClass;
		private readonly MethodMember m_HostMethod;
		private readonly HashSet<IAnonymousMethodOperand> m_AnonymousMethodsToHoist;
		private readonly HashSet<OperandCapture> m_Captures;
		private readonly List<ClosureDefinition> m_Children;
		private readonly Dictionary<OperandCapture, IOperand> m_RewrittenOperands;
		private bool m_ParentCapturesPulled;
		private ClosureDefinition m_Parent;
		private ClassType m_ClosureClass;
		private ConstructorBuilder m_ClosureClassConstructor;
		private FieldMember m_ParentField;
		private IOperand m_ClosureInstanceReference;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ClosureDefinition(StatementBlock scopeBlock)
		{
			m_HostScopeBlock = scopeBlock;
			m_HostMethod = scopeBlock.OwnerMethod;
			m_AnonymousMethodsToHoist = new HashSet<IAnonymousMethodOperand>();
			m_OwnerClass = scopeBlock.OwnerMethod.OwnerClass;
			m_Captures = new HashSet<OperandCapture>();
			m_Parent = null;
			m_Children = new List<ClosureDefinition>();
			m_RewrittenOperands = new Dictionary<OperandCapture, IOperand>();
			m_ClosureClass = null;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void AttachToParent(ClosureDefinition parent)
		{
			ValidateMutability();

			if ( m_Parent != null )
			{
				throw new InvalidOperationException("Current closure already has a parent closure attached.");
			}

			m_Parent = parent;

			if ( parent != null )
			{
				parent.m_Children.Add(this);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void AddCapture(OperandCapture capture)
		{
			ValidateMutability();

			if ( !m_Captures.Any(c => c.SourceOperand == capture.SourceOperand) )
			{
				m_Captures.Add(capture);
				capture.HoistInClosure(this);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void AddAnonymousMethod(IAnonymousMethodOperand anonymousMethod)
		{
			m_AnonymousMethodsToHoist.Add(anonymousMethod);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void PullCapturesFromParent()
		{
			if ( m_Parent == null || m_ParentCapturesPulled )
			{
				return;
			}

			ValidateMutability();

			foreach ( var parentCapture in m_Parent.Captures )
			{
				m_Captures.Add(parentCapture);
			}

			m_ParentCapturesPulled = true;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void ImplementClosure()
		{
			if ( m_ClosureClass == null )
			{
				CompileClosureClass();
				WriteClosureInstanceInitialization();
				WriteAnonymousMethodCallSites();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void RewriteOperandIfCaptured(ref IOperand operand)
		{
			RewriteOperandIfCaptured(ref operand, closureInstanceReference: m_ClosureInstanceReference);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public void RewriteOperandIfCaptured(ref IOperand operand, IOperand closureInstanceReference)
		{
			var sourceOperand = operand;
			var capture = m_Captures.FirstOrDefault(c => object.ReferenceEquals(c.SourceOperand, sourceOperand));

			if ( capture != null )
			{
				operand = GetRewrittenOperand(capture, closureInstanceReference);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ClosureDefinition Parent
		{
			get
			{
				return m_Parent;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ClosureDefinition[] Children
		{
			get
			{
				return m_Children.ToArray();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public int ChildCount
		{
			get
			{
				return m_Children.Count;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public StatementBlock HostScopeBlock
		{
			get
			{
				return m_HostScopeBlock;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public OperandCapture[] Captures
		{
			get
			{
				return m_Captures.ToArray();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IAnonymousMethodOperand[] AnonymousMethodsToHoist
		{
			get
			{
				return m_AnonymousMethodsToHoist.ToArray();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ClassType ClosureClass
		{
			get
			{
				return m_ClosureClass;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public ConstructorBuilder ClosureClassConstructor
		{
			get
			{
				return m_ClosureClassConstructor;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public IOperand ClosureInstanceReference
		{
			get
			{
				return m_ClosureInstanceReference;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private IOperand GetRewrittenOperand(OperandCapture capture, IOperand target)
		{
			using ( TypeTemplate.CreateScope<TypeTemplate.TField>(capture.OperandType) )
			{
				if ( capture.HoistingClosure == this )
				{
					return new Field<TypeTemplate.TField>(target, capture.HoistedField);
				}
				else if ( m_Parent != null )
				{
					return m_Parent.GetRewrittenOperand(capture, m_ParentField.AsOperand<TypeTemplate.TField>(target));
				}
				else
				{
					Debug.Fail("Captured operand is not hoisted.");
					throw new Exception();
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void CompileClosureClass()
		{
			m_ClosureClass = new NestedClassType(
				containingClass: m_OwnerClass, 
				classFullName: m_HostMethod.Name + "<Closure>", 
				baseType: typeof(object),
				closureDefinition: this);

			m_ClosureClassConstructor = m_ClosureClass.TypeBuilder.DefineDefaultConstructor(
				MethodAttributes.Public |
				MethodAttributes.ReuseSlot |
				MethodAttributes.SpecialName |
				MethodAttributes.RTSpecialName |
				MethodAttributes.HideBySig);

			var closureWriter = new ImplementationClassWriter<object>(m_ClosureClass);

			if ( m_Parent != null )
			{
				m_ParentField = closureWriter.DefineField(
					name: "Parent", 
					isStatic: false, 
					isPublic: true, 
					fieldType: m_Parent.ClosureClass.TypeBuilder);
			}

			foreach ( var capture in m_Captures.Where(c => c.HoistingClosure == this) )
			{
				capture.DefineHoistedField(closureWriter);
			}

			foreach ( var anonymousMethodOperand in m_AnonymousMethodsToHoist )
			{
				anonymousMethodOperand.CreateAnonymousMethod(m_ClosureClass, closure: this, isStatic: false, isPublic: true);
				anonymousMethodOperand.AnonymousMethod.AcceptVisitor(new ClosureHoistedMethodRewritingVisitor(anonymousMethodOperand.AnonymousMethod, this));
			}

			m_ClosureClass.Compile();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void WriteClosureInstanceInitialization()
		{
			using ( TypeTemplate.CreateScope<TypeTemplate.TClosure>(m_ClosureClass.TypeBuilder) )
			{
				using ( var scope = new StatementScope(m_HostScopeBlock, StatementScope.RewriteMode.On) )
				{
					var scopeRewriter = m_HostMethod.TransparentWriter;
					var closureInstance = scopeRewriter.Local<TypeTemplate.TClosure>();
					closureInstance.Assign(new NewObjectExpression<TypeTemplate.TClosure>(m_ClosureClassConstructor, new IOperand[0]));
					
					m_ClosureInstanceReference = closureInstance;

					if ( m_Parent != null )
					{
						WriteParentFieldInitialization();
					}

					foreach ( var capture in m_Captures.Where(c => c.HoistingClosure == this) )
					{
						WriteCaptureFieldInitialization(capture);
					}
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void WriteCaptureFieldInitialization(OperandCapture capture)
		{
			using ( TypeTemplate.CreateScope<TypeTemplate.TField>(capture.OperandType) )
			{
				if ( capture.SourceOperand.ShouldInitializeHoistedField )
				{
					capture.HoistedField.AsOperand<TypeTemplate.TField>(m_ClosureInstanceReference).Assign(capture.SourceOperand.CastTo<TypeTemplate.TField>());
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void WriteParentFieldInitialization()
		{
			using ( TypeTemplate.CreateScope<TypeTemplate.TField>(m_Parent.ClosureClass.TypeBuilder) )
			{
				m_ParentField.AsOperand<TypeTemplate.TField>(m_ClosureInstanceReference).Assign(m_Parent.m_ClosureInstanceReference.CastTo<TypeTemplate.TField>());
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		private void WriteAnonymousMethodCallSites()
		{
			foreach ( var anonymousMethodOperand in m_AnonymousMethodsToHoist )
			{
				anonymousMethodOperand.WriteCallSite();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------
		
		private void ValidateMutability()
		{
			if ( m_ClosureClass != null )
			{
				throw new InvalidOperationException("Cannot change closure definition because closure class was already created.");
			}
		}
	}
}
