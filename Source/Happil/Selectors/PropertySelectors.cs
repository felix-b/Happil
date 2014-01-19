﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Happil.Fluent;

namespace Happil.Selectors
{
	public static class PropertySelectors
	{
		public abstract class Base<TBase> : IEnumerable<PropertyInfo>
		{
			internal Base(HappilClassBody<TBase> ownerBody, IEnumerable<PropertyInfo> selectedProperties)
			{
				this.OwnerBody = ownerBody;
				this.SelectedProperties = selectedProperties.ToArray();
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			#region IEnumerable<PropertyInfo> Members

			public IEnumerator<PropertyInfo> GetEnumerator()
			{
				return SelectedProperties.AsEnumerable().GetEnumerator();
			}

			#endregion

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return SelectedProperties.GetEnumerator();
			}

			#endregion

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> ForEach(Action<PropertyInfo> action)
			{
				foreach ( var property in SelectedProperties )
				{
					action(property);
				}

				return OwnerBody;
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			internal IHappilClassBody<TBase> DefineMembers<TProperty>(Action<HappilProperty<TProperty>.BodyBase> invokeAccessorBodyDefinitions)
			{
				var propertiesToImplement = OwnerBody.HappilClass.TakeNotImplementedMembers(SelectedProperties);

				foreach ( var declaration in propertiesToImplement )
				{
					var propertyMember = new HappilProperty<TProperty>(OwnerBody.HappilClass, declaration);
					OwnerBody.HappilClass.RegisterMember(propertyMember, propertyMember.BodyDefinitionAction);
					
					invokeAccessorBodyDefinitions(propertyMember.Body);
				}

				return OwnerBody;
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			internal IHappilClassBody<TBase> DefineAutomaticImplementation<TProperty>()
			{
				DefineMembers<TProperty>(body => {
					ValidateAutomaticImplementation(body.Declaration);
					
					((IHappilPropertyBody<TProperty>)body).Get(m => m.Return(body.BackingField));
					((IHappilPropertyBody<TProperty>)body).Set((m, value) => body.BackingField.Assign(value));
				});

				return OwnerBody;
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			internal HappilClassBody<TBase> OwnerBody { get; private set; }
			internal PropertyInfo[] SelectedProperties { get; private set; }

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			private void ValidateAutomaticImplementation(PropertyInfo declaration)
			{
				if ( !declaration.CanRead || !declaration.CanWrite )
				{
					throw new NotSupportedException(string.Format(
						"Property '{0}' cannot have automatic implementation because it is not read-write.", declaration.Name));
				}

				if ( declaration.IsIndexer() )
				{
					throw new NotSupportedException(string.Format(
						"Property '{0}' cannot have automatic implementation because it is an indexer.", declaration.Name));
				}
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public class Untyped<TBase> : Base<TBase>
		{
			internal Untyped(HappilClassBody<TBase> ownerBody, IEnumerable<PropertyInfo> selectedProperties)
				: base(ownerBody, selectedProperties)
			{
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> Implement(
				Func<IHappilPropertyBody<TypeTemplate>, IHappilPropertyGetter> getter,
				Func<IHappilPropertyBody<TypeTemplate>, IHappilPropertySetter> setter = null)
			{
				DefineMembers<TypeTemplate>(body => {
					if ( getter != null )
					{
						getter((IHappilPropertyBody<TypeTemplate>)body);
					}
					if ( setter != null )
					{
						setter((IHappilPropertyBody<TypeTemplate>)body);
					}
				});

				return OwnerBody;
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> ImplementAutomatic()
			{
				return DefineAutomaticImplementation<TypeTemplate>();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public class Typed<TBase, TProperty> : Base<TBase>
		{
			internal Typed(HappilClassBody<TBase> ownerBody, IEnumerable<PropertyInfo> selectedProperties)
				: base(ownerBody, selectedProperties)
			{
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> Implement(
				Func<IHappilPropertyBody<TProperty>, IHappilPropertyGetter> getter,
				Func<IHappilPropertyBody<TProperty>, IHappilPropertySetter> setter = null)
			{
				DefineMembers<TProperty>(body => {
					if ( getter != null )
					{
						getter((IHappilPropertyBody<TProperty>)body);
					}
					if ( setter != null )
					{
						setter((IHappilPropertyBody<TProperty>)body);
					}
				});

				return OwnerBody;
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> ImplementAutomatic()
			{
				return DefineAutomaticImplementation<TypeTemplate>();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public class Indexer1Arg<TBase, TIndex, TProperty> : Base<TBase>
		{
			internal Indexer1Arg(HappilClassBody<TBase> ownerBody, IEnumerable<PropertyInfo> selectedProperties)
				: base(ownerBody, selectedProperties)
			{
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> Implement(
				Func<IHappilPropertyBody<TIndex, TProperty>, IHappilPropertyGetter> getter,
				Func<IHappilPropertyBody<TIndex, TProperty>, IHappilPropertySetter> setter = null)
			{
				DefineMembers<TProperty>(body => {
					if ( getter != null )
					{
						getter((IHappilPropertyBody<TIndex, TProperty>)body);
					}
					if ( setter != null )
					{
						setter((IHappilPropertyBody<TIndex, TProperty>)body);
					}
				});

				return OwnerBody;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		public class Indexer2Args<TBase, TIndex1, TIndex2, TProperty> : Base<TBase>
		{
			internal Indexer2Args(HappilClassBody<TBase> ownerBody, IEnumerable<PropertyInfo> selectedProperties)
				: base(ownerBody, selectedProperties)
			{
			}

			//-------------------------------------------------------------------------------------------------------------------------------------------------

			public IHappilClassBody<TBase> Implement(
				Func<IHappilPropertyBody<TIndex1, TIndex2, TProperty>, IHappilPropertyGetter> getter,
				Func<IHappilPropertyBody<TIndex1, TIndex2, TProperty>, IHappilPropertySetter> setter = null)
			{
				DefineMembers<TProperty>(body => {
					if ( getter != null )
					{
						getter((IHappilPropertyBody<TIndex1, TIndex2, TProperty>)body);
					}
					if ( setter != null )
					{
						setter((IHappilPropertyBody<TIndex1, TIndex2, TProperty>)body);
					}
				});

				return OwnerBody;
			}
		}
	}
}