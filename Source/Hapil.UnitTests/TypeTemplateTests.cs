﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hapil.Testing.NUnit;
using Hapil.Expressions;
using Hapil.Operands;
using NUnit.Framework;
using Repo = Hapil.UnitTests.AncestorRepository;
using TT = Hapil.TypeTemplate;
using System.IO;
using System.Reflection;
using Hapil.Members;

namespace Hapil.UnitTests
{
	[TestFixture]
	public class TypeTemplateTests : NUnitEmittedTypesTestBase
	{
      	[Test]
		public void CanDeriveFromTBase()
		{
			//-- Arrange

			OnDefineNewClass(key => DeriveClassFrom<TT.TBase>(key)
				.DefaultConstructor()
				.AllProperties().ImplementAutomatic()
			);

			//-- Act

			DefineClassByKey(new TypeKey(baseType: typeof(Repo.BaseTwo)));

			var obj = CreateClassInstanceAs<Repo.BaseTwo>().UsingDefaultConstructor();

			obj.FirstValue = 123;
			obj.SecondValue = "ABC";

			//-- Assert

			Assert.That(obj.FirstValue, Is.EqualTo(123));
			Assert.That(obj.SecondValue, Is.EqualTo("ABC"));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void CanImplementTPrimary()
		{
			//-- Arrange

			OnDefineNewClass(key => DeriveClassFrom<object>()
				.DefaultConstructor()
				.ImplementInterface<TT.TPrimary>()
				.AllProperties().ImplementAutomatic()
				.ImplementInterface<IEquatable<TT.TPrimary>>()
				.Method<TT.TPrimary, bool>(intf => intf.Equals).Implement((m, other) => {
					m.Return(true);
				})
			);

			//-- Act

			DefineClassByKey(new TypeKey(primaryInterface: typeof(Repo.IFewReadWriteProperties)));

			var obj = CreateClassInstanceAs<Repo.IFewReadWriteProperties>().UsingDefaultConstructor();
			obj.AnInt = 123;
			obj.AString = "ABC";

			var equatable = (IEquatable<Repo.IFewReadWriteProperties>)obj;
			var equalsResult = equatable.Equals(obj);

			//-- Assert

			Assert.That(obj.AnInt, Is.EqualTo(123));
			Assert.That(obj.AString, Is.EqualTo("ABC"));
			Assert.That(equalsResult, Is.True);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void CanHaveMembersOfTypeTBase()
		{
			//-- Arrange

			Field<TT.TBase> remoteField, localField;

			OnDefineNewClass(key => DeriveClassFrom<Repo.BaseTwo>()
				.Field<TT.TBase>("m_Remote", out remoteField)
				.Field<TT.TBase>("m_Local", out localField)
				.DefaultConstructor()
				.Constructor<TT.TBase, TT.TBase>((m, remote, local) => {
					remoteField.Assign(remote);
					localField.Assign(local);
				})
				.AllProperties().ImplementAutomatic()
				.ImplementInterface<Repo.IVersionControlled<TT.TBase>>()
				.Property(intf => intf.RemoteVersion).Implement(
					prop => prop.Get(m => m.Return(remoteField))
				)
				.Property(intf => intf.LocalVersion).Implement(
					prop => prop.Get(m => m.Return(localField))
				)
				.AllProperties().Implement(
					prop => prop.Get(m => m.Return(m.This<TT.TProperty>()))
				)
			);

			//-- Act

			DefineClassByKey(new TypeKey(
				baseType: typeof(Repo.BaseTwo),
				primaryInterface: typeof(Repo.IVersionControlled<Repo.BaseTwo>)));

			var remoteObj = CreateClassInstanceAs<Repo.BaseTwo>().UsingDefaultConstructor();
			var localObj = CreateClassInstanceAs<Repo.BaseTwo>().UsingDefaultConstructor();
			var workingObj = CreateClassInstanceAs<Repo.BaseTwo>()
				.UsingConstructor<Repo.BaseTwo, Repo.BaseTwo>(remoteObj, localObj, constructorIndex: 1);

			workingObj.FirstValue = 123;
			workingObj.SecondValue = "ABC";

			var versionedObj = (Repo.IVersionControlled<Repo.BaseTwo>)workingObj;

			//-- Assert

			Assert.That(remoteObj, Is.Not.SameAs(localObj));
			Assert.That(localObj, Is.Not.SameAs(workingObj));
			Assert.That(remoteObj, Is.Not.SameAs(workingObj));

			Assert.That(workingObj.FirstValue, Is.EqualTo(123));
			Assert.That(workingObj.SecondValue, Is.EqualTo("ABC"));
			Assert.That(versionedObj.RemoteVersion, Is.SameAs(remoteObj));
			Assert.That(versionedObj.LocalVersion, Is.SameAs(localObj));
			Assert.That(versionedObj.WorkingVersion, Is.SameAs(versionedObj));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void CanHaveMembersOfTypeTPrimary()
		{
			//-- Arrange

			Field<TT.TPrimary> remoteField, localField;

			OnDefineNewClass(key => DeriveClassFrom<object>()
				.Field<TT.TPrimary>("m_Remote", out remoteField)
				.Field<TT.TPrimary>("m_Local", out localField)
				.DefaultConstructor()
				.Constructor<TT.TPrimary, TT.TPrimary>((m, remote, local) => {
					remoteField.Assign(remote);
					localField.Assign(local);
				})
				.ImplementInterface<TT.TPrimary>()
				.AllProperties().ImplementAutomatic()
				.ImplementInterface<Repo.IVersionControlled<TT.TPrimary>>()
				.Property(intf => intf.RemoteVersion).Implement(
					prop => prop.Get(m => m.Return(remoteField))
				)
				.Property(intf => intf.LocalVersion).Implement(
					prop => prop.Get(m => m.Return(localField))
				)
				.AllProperties().Implement(
					prop => prop.Get(m => m.Return(m.This<TT.TProperty>()))
				)
			);

			//-- Act

			DefineClassByKey(new TypeKey(
				primaryInterface: typeof(Repo.IFewReadWriteProperties),
				secondaryInterfaces: typeof(Repo.IVersionControlled<Repo.IFewPropertiesWithIndexers>)));

			var remoteObj = CreateClassInstanceAs<Repo.IFewReadWriteProperties>().UsingDefaultConstructor();
			var localObj = CreateClassInstanceAs<Repo.IFewReadWriteProperties>().UsingDefaultConstructor();
			var workingObj = CreateClassInstanceAs<Repo.IFewReadWriteProperties>()
				.UsingConstructor<Repo.IFewReadWriteProperties, Repo.IFewReadWriteProperties>(remoteObj, localObj, constructorIndex: 1);

			workingObj.AnInt = 123;
			workingObj.AString = "ABC";

			var versionedObj = (Repo.IVersionControlled<Repo.IFewReadWriteProperties>)workingObj;

			//-- Assert

			Assert.That(remoteObj, Is.Not.SameAs(localObj));
			Assert.That(localObj, Is.Not.SameAs(workingObj));
			Assert.That(remoteObj, Is.Not.SameAs(workingObj));

			Assert.That(workingObj.AnInt, Is.EqualTo(123));
			Assert.That(workingObj.AString, Is.EqualTo("ABC"));
			Assert.That(versionedObj.RemoteVersion, Is.SameAs(remoteObj));
			Assert.That(versionedObj.LocalVersion, Is.SameAs(localObj));
			Assert.That(versionedObj.WorkingVersion, Is.SameAs(versionedObj));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void CanUseTemplateTypesInConstructorArguments()
		{
			//-- Arrange

			OnDefineNewClass(key => DeriveClassFrom<TT.TBase>()
				.Constructor<TT.TBase, TT.TPrimary>((m, @base, primary) => {
				})
				.ImplementInterface<TT.TPrimary>()
				.AllProperties().ImplementAutomatic()
			);

			//-- Act

			DefineClassByKey(new TypeKey(
				baseType: typeof(Repo.BaseOne),
				primaryInterface: typeof(Repo.IOneProperty)));

			var obj = CreateClassInstanceAs<Repo.IOneProperty>().UsingConstructor<Repo.BaseOne, Repo.IOneProperty>(null, null);
			var constructorInfo = obj.GetType().GetConstructor(new[] { typeof(Repo.BaseOne), typeof(Repo.IOneProperty) });

			//-- Assert

			Assert.That(obj, Is.Not.Null);
			Assert.That(constructorInfo, Is.Not.Null);
			Assert.That(constructorInfo.GetParameters().Length, Is.EqualTo(2));
			Assert.That(constructorInfo.GetParameters()[0].ParameterType, Is.EqualTo(typeof(Repo.BaseOne)));
			Assert.That(constructorInfo.GetParameters()[1].ParameterType, Is.EqualTo(typeof(Repo.IOneProperty)));
		}
    
        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanUseTemplateTypesWithInheritanceConstraints()
        {
            var type1 = typeof(AncestorRepository.GenericBaseWithConstraints<TT.TContract, TT.TImpl>);
            var type2 = typeof(AncestorRepository.GenericBaseWithConstraints<TT.TContract2, TT.TImpl2>);
            var type3 = typeof(AncestorRepository.GenericBaseWithConstraints<TT.TAbstract, TT.TConcrete>);
            var type4 = typeof(AncestorRepository.GenericBaseWithConstraints<TT.TAbstract2, TT.TConcrete2>);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanUseTemplateTypesWithStructConstraint()
        {
            var type1 = typeof(AncestorRepository.GenericBaseWithStructConstraint<TT.TStruct>);
            var type2 = typeof(AncestorRepository.GenericBaseWithStructConstraint<TT.TStruct2>);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanImplementGenericAncestorWithStructConstraint()
        {
            //-- Arrange

            using ( TT.CreateScope<TT.TStruct>(typeof(Int32)) )
            {
                DeriveClassFrom<AncestorRepository.GenericBaseWithStructConstraint<TT.TStruct>>()
                    .DefaultConstructor()
                    .Method<TT.TStruct, string>(cls => cls.FormatValue)
                    .Implement((m, value) => m.Return(m.Const("{0}={1}").Format(value.Func<Type>(x => x.GetType).Prop(x => x.Name), value)));
            }

            //-- Act

            var obj = CreateClassInstanceAs<AncestorRepository.GenericBaseWithStructConstraint<int>>().UsingDefaultConstructor();
            var formattedValue = obj.FormatValue(123);

            //-- Assert

            Assert.That(formattedValue, Is.EqualTo("Int32=123"));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanDeriveFromGenericBaseWithInheritanceConstraints()
        {
            //-- Arrange

            using ( TT.CreateScope<TT.TAbstract, TT.TConcrete>(typeof(Stream), typeof(MemoryStream)) )
            {
                var classWriter = DeriveClassFrom<AncestorRepository.GenericBaseWithConstraints<TT.TAbstract, TT.TConcrete>>();
                
                classWriter.Property(x => x.AsConcrete).ImplementAutomatic();

                var concreteProperty = classWriter.Property(x => x.AsConcrete).Single();
                var concreteField = classWriter.OwnerClass.GetPropertyBackingField(concreteProperty).AsOperand<TT.TConcrete>();

                classWriter.Property(x => x.AsAbstract).Implement(
                    p => p.Get(m => m.Return(concreteField)),
                    p => p.Set((m, value) => concreteField.Assign(value.CastTo<TT.TConcrete>())));

                classWriter.Constructor<TT.TConcrete>((m, arg) => concreteField.Assign(arg));
            }

            var memoryStream0 = new MemoryStream();
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();

            //-- Act

            var obj = 
                CreateClassInstanceAs<AncestorRepository.GenericBaseWithConstraints<Stream, MemoryStream>>().UsingConstructor(memoryStream0);

            var abstract0 = obj.AsAbstract;
            var concrete0 = obj.AsConcrete;

            obj.AsAbstract = memoryStream1;

            var abstract1 = obj.AsAbstract;
            var concrete1 = obj.AsConcrete;

            obj.AsConcrete = memoryStream2;

            var abstract2 = obj.AsAbstract;
            var concrete2 = obj.AsConcrete;

            //-- Assert

            Assert.That(abstract0, Is.SameAs(memoryStream0));
            Assert.That(concrete0, Is.SameAs(memoryStream0));
            
            Assert.That(abstract1, Is.SameAs(memoryStream1));
            Assert.That(concrete1, Is.SameAs(memoryStream1));
            
            Assert.That(abstract2, Is.SameAs(memoryStream2));
            Assert.That(concrete2, Is.SameAs(memoryStream2));
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanUseCustomTemplateTypes()
        {
            //-- Arrange

            using ( TT.CreateScope<CustomTT.TCustom>(typeof(CustomOne)) )
            {
                DeriveClassFrom<CustomContainerBase<CustomTT.TCustom>>().Constructor<CustomTT.TCustom>((cw, custom) => cw.Base(custom));
            }

            //-- Act

            var obj = CreateClassInstanceAs<CustomContainerBase<CustomOne>>().UsingConstructor(new CustomOne());
            var customString = obj.ToString();

            //-- Assert

            Assert.That(customString, Is.EqualTo("CS1"));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanUseThisOeprandOfTemplateType()
        {
            //-- Arrange

            var fiveMethod = TypeMemberCache.Of<AncestorRepository.IFewMethods>().SelectFuncs<int, string>(where: f => f.Name == "Five").Single();

            using ( TT.CreateScope<TT.TService, TT.TRequest, TT.TReply>(typeof(AncestorRepository.IFewMethods), typeof(int), typeof(string)) )
            {
                DeriveClassFrom<FewMethods>()
                    .DefaultConstructor()
                    .NewVirtualFunction<TT.TRequest, TT.TReply>("CallServiceMethod").Implement((w, request) => {
                        w.Return(w.This<TT.TService>().Func<TT.TRequest, TT.TReply>(fiveMethod, request));
                    });
            }

            //-- Act

            dynamic obj = CreateClassInstanceAs<object>().UsingDefaultConstructor();
            var result = obj.CallServiceMethod(123);

            //-- Assert

            Assert.That(result, Is.EqualTo("$123$"));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanResolveArrayTypes()
        {
            //-- Arrange

            using ( TT.CreateScope<TT.TItem>(typeof(DayOfWeek)) )
            {
                //-- Act

                var resolved1 = TT.Resolve<TT.TItem[]>();
                var resolved2 = TT.Resolve<IList<TT.TItem[]>>();
                var resolved3 = TT.Resolve<IList<TT.TItem>[]>();

                //-- Assert
                
                Assert.That(resolved1, Is.EqualTo(typeof(DayOfWeek[])));
                Assert.That(resolved2, Is.EqualTo(typeof(IList<DayOfWeek[]>)));
                Assert.That(resolved3, Is.EqualTo(typeof(IList<DayOfWeek>[])));
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanOverrideTemplateTypeInNestedScope()
        {
            //-- arrange

            Type resolved1;
            Type resolved2;
            Type resolved3;

            //-- act

            using ( TT.CreateScope<TT.TValue>(typeof(int)) )
            {
                resolved1 = TT.Resolve<TT.TValue>();

                using ( TT.CreateScope<TT.TValue>(typeof(string)) )
                {
                    resolved2 = TT.Resolve<TT.TValue>();
                }

                resolved3 = TT.Resolve<TT.TValue>();
            }

            //-- assert

            Assert.That(resolved1, Is.EqualTo(typeof(int)));
            Assert.That(resolved2, Is.EqualTo(typeof(string)));
            Assert.That(resolved3, Is.EqualTo(typeof(int)));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

	    [Test]
	    public void CanSaveAndRestoreOverriddenTemplateTypesInNestedScope()
	    {
	        //-- arrange

	        IDisposable saved1, saved2, saved3;

	        using ( TT.CreateScope<TT.TValue>(typeof(int)) )
	        {
	            saved1 = TT.Save();

	            using ( TT.CreateScope<TT.TValue>(typeof(string)) )
	            {
	                saved2 = TT.Save();
	            }

	            saved3 = TT.Save();
	        }

	        //-- act

	        Type resolved1, resolved2, resolved3;

	        using ( TT.Restore(saved1) )
	        {
	            resolved1 = TT.Resolve<TT.TValue>();
	        }

	        using ( TT.Restore(saved2) )
	        {
	            resolved2 = TT.Resolve<TT.TValue>();
	        }

	        using ( TT.Restore(saved3) )
	        {
	            resolved3 = TT.Resolve<TT.TValue>();
	        }

	        //-- assert

	        Assert.That(resolved1, Is.EqualTo(typeof(int)));
	        Assert.That(resolved2, Is.EqualTo(typeof(string)));
	        Assert.That(resolved3, Is.EqualTo(typeof(int)));
	    }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [TestCase(typeof(ICollection<int>), false)]
        [TestCase(typeof(ICollection<TT.TItem>), true)]
        //igonre: [TestCase(typeof(TT.TCollection<int>), true)]
        [TestCase(typeof(TT.TCollection<TT.TItem>), true)]
        [TestCase(typeof(ICollection<>), false)]
        //[TestCase(typeof(TT.TCollection<>), true)]
        public void CanIdentifyCollectionTemplateTypes(Type type, bool expectedOutcome)
        {
            Assert.That(TT.IsTemplateType(type), Is.EqualTo(expectedOutcome));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test, Ignore("TBD")]
        public void CanResolveOpenGenericCollectionTemplateTypes()
        {
            //-- arrange

            Type resolvedAbstract;
            Type resolvedConcrete;

            //-- act
            
            using ( TT.CreateScope<TT.TAbstract, TT.TConcrete>(typeof(Stream), typeof(MemoryStream)) )
            {
                using ( TT.CreateScope<TT.TCollection<TT.TAbstract>, TT.TCollection<TT.TConcrete>>(typeof(ICollection<>), typeof(HashSet<>)) )
                {
                    resolvedAbstract = TT.Resolve<TT.TCollection<TT.TAbstract>>();
                    resolvedConcrete = TT.Resolve<TT.TCollection<TT.TConcrete>>();
                }
            }

            //-- assert

            Assert.That(resolvedAbstract, Is.EqualTo(typeof(ICollection<Stream>)));
            Assert.That(resolvedConcrete, Is.EqualTo(typeof(HashSet<FileStream>)));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [Test]
        public void CanResolveClosedGenericCollectionTemplateTypes()
        {
            //-- arrange

            Type resolvedAbstract;
            Type resolvedConcrete;

            //-- act

            using ( TT.CreateScope<TT.TCollection<TT.TAbstract>, TT.TCollection<TT.TConcrete>>(typeof(int[]), typeof(HashSet<string>)) )
            {
                resolvedAbstract = TT.Resolve<TT.TCollection<TT.TAbstract>>();
                resolvedConcrete = TT.Resolve<TT.TCollection<TT.TConcrete>>();
            }

            //-- assert

            Assert.That(resolvedAbstract, Is.EqualTo(typeof(int[])));
            Assert.That(resolvedConcrete, Is.EqualTo(typeof(HashSet<string>)));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

	    public class FewMethods : AncestorRepository.IFewMethods
	    {
            public void One()
            {
                throw new NotImplementedException();
            }
            public void Two(int n)
            {
                throw new NotImplementedException();
            }
            public int Three()
            {
                throw new NotImplementedException();
            }
            public int Four(string s)
            {
                throw new NotImplementedException();
            }
            public string Five(int n)
            {
                return "$" + n + "$";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

	    public interface ICustom
	    {
	        string GetCustomString();
	    }
	    public abstract class CustomContainerBase<T> 
            where T : class, ICustom, new()
	    {
	        private readonly T _custom ;
            protected CustomContainerBase(T custom)
            {
                _custom = custom;
            }
	        public override string ToString()
	        {
	            return _custom.GetCustomString();
	        }
	    }
	    public class CustomOne : ICustom
	    {
            public string GetCustomString()
	        {
	            return "CS1";
	        }
	    }
        public class CustomTwo : ICustom
        {
            public string GetCustomString()
            {
                return "CS2";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public static class CustomTT
	    {
	        // ReSharper disable once InconsistentNaming
	        public class TCustom : ICustom, TypeTemplate.ITemplateType<TCustom>
	        {
                public string GetCustomString()
                {
                    throw new NotImplementedException();
                }
            }
	    }
	}
}
