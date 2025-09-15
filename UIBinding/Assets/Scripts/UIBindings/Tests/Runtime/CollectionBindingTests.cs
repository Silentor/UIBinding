using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Tests.Runtime
{
    public class CollectionBindingTests
    {
        [Test]
        public void TestCollectionAddReference( )
        {
            var (list, binding) = GetObjectListBinding();
            list.Add( new object() );                       //Start with 1-object list
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionAdd( list, binding, () => new object() ); 
        }

        [Test]
        public void TestCollectionAddValue( )
        {
            var (list, binding) = GetStructListBinding();
            list.Add( Vector3.one );                       //Start with 1-object list
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionAdd( list, binding, () => UnityEngine.Random.onUnitSphere );
        }

        [Test]
        public void TestCollectionRemoveReference( )
        {
            var (list, binding) = GetObjectListBinding();
            for ( int i = 0; i < 10; i++ )
            {
                list.Add( new Object() );
            }
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionRemove( list, binding );
        }

        [Test]
        public void TestCollectionRemoveValue( )
        {
            var (list, binding) = GetStructListBinding();
            for ( int i = 0; i < 10; i++ )
            {
                list.Add( UnityEngine.Random.onUnitSphere );
            }
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionRemove( list, binding );
        }

        [Test]
        public void TestCollectionMoveReference()
        {
            var (list, binding) = GetObjectListBinding();
            for (int i = 0; i < 5; i++)
            {
                list.Add(new Object());
            }
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionMove(list, binding);
        }

        [Test]
        public void TestCollectionMoveValue()
        {
            var (list, binding) = GetStructListBinding();
            for (int i = 0; i < 5; i++)
            {
                list.Add(UnityEngine.Random.onUnitSphere);
            }
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionMove(list, binding);
        }

        [Test]
        public void TestCollectionChangeReference()
        {
            var (list, binding) = GetObjectListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(new Object());
            binding.ManuallyCheckChanges();
            _testable.ClearState();
            TestCollectionChange(list, binding, () => new Object());
        }

        [Test]
        public void TestCollectionChangeValue()
        {
            var (list, binding) = GetStructListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(UnityEngine.Random.onUnitSphere);
            binding.ManuallyCheckChanges();
            _testable.ClearState();
            TestCollectionChange(list, binding, () => UnityEngine.Random.onUnitSphere);
        }

        [Test]
        public void TestCollectionMassiveChangeReference()
        {
            var (list, binding) = GetObjectListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(new Object());
            binding.ManuallyCheckChanges();
            _testable.ClearState();
            TestCollectionMassiveChange(list, binding, () => new Object());
        }

        [Test]
        public void TestCollectionMassiveChangeValue()
        {
            var (list, binding) = GetStructListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(UnityEngine.Random.onUnitSphere);
            binding.ManuallyCheckChanges();
            _testable.ClearState();
            TestCollectionMassiveChange(list, binding, () => UnityEngine.Random.onUnitSphere);
        }

        [Test]
        public void TestCollectionComplexPathAddReference( )
        {
            var (list, binding) = GetObjectListComplexBinding();
            list.Add( new object() );                       //Start with 1-object list
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionAdd( list, binding, () => new object() ); 
        }

        [Test]
        public void TestCollectionComplexPathAddValue( )
        {
            var (list, binding) = GetStructListBinding();
            list.Add( Vector3.one );                       //Start with 1-object list
            binding.ManuallyCheckChanges();
            _testable.ClearState();

            TestCollectionAdd( list, binding, () => UnityEngine.Random.onUnitSphere );
        }

        private void TestCollectionChange<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue)
        {
            // Change one element
            list[2] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_testable._changedCount, Is.EqualTo(1));
            Assert.That(_testable._changedIndex, Is.EqualTo(2));
            Assert.That(_testable._changedItem, Is.EqualTo(list[2]));

            // Change several elements
            _testable.ClearState();
            list[1] = getNewValue();
            list[3] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_testable._changedCount, Is.EqualTo(2));
            Assert.That(_testable._changedIndex, Is.EqualTo(3)); // Last changed index
        }

        private void TestCollectionAdd<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue ) where T : new()
        {
            // Act add plain object
            list.Add( getNewValue() ); 
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._addedCount, Is.EqualTo(1));
            Assert.That( _testable._addedIndex, Is.EqualTo( 1 ) );

            //Act add null object
            _testable.ClearState();
            list.Add( default );   
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._addedCount, Is.EqualTo(1));
            Assert.That( _testable._addedItem, Is.EqualTo( default(T) ) ); 

            //Act insert 3 object 
            _testable.ClearState();
            list.Insert( 0, getNewValue() );
            list.Insert( 1, getNewValue() );
            list.Insert( 2, getNewValue() );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._addedCount, Is.EqualTo(3));

            //Act add two null objects
            _testable.ClearState();
            list.Insert( 0, default );
            list.Add( default );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That( _testable._resetCount, Is.EqualTo( 1 ) );        //No add events, just reset, null objects prevents diff calculation. Avoid nulls
        }

        private void TestCollectionRemove<T>(List<T> list, CollectionBinding binding )
        {
            // Act remove first object
            list.RemoveAt( 0 );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._removedCount, Is.EqualTo(1));

            //Act remove two objects
            _testable.ClearState();
            list.Remove( list.Last() );
            list.Remove( list.First() );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._removedCount, Is.EqualTo(2));

            //Act remove all objects
            _testable.ClearState();
            list.Clear();
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_testable._resetCount, Is.EqualTo(1));           //No remove events, clear fires reset event
        }

        private void TestCollectionMove<T>(List<T> list, CollectionBinding binding)
        {
            // Move item from index 0 to 4
            var fromIndex = 0;
            var toIndex = 4;
            var item = list[fromIndex];
            list.RemoveAt(fromIndex);
            list.Insert(toIndex, item);
            binding.ManuallyCheckChanges();
            Assert.That( _testable._resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
            // Assert.That(_movedCount, Is.EqualTo(1));
            // Assert.That(_movedFromIndex, Is.EqualTo(fromIndex));
            // Assert.That(_movedToIndex, Is.EqualTo(toIndex));
            // Assert.That(_movedItem, Is.EqualTo(item));

            // Move two items at the same time (simulate by moving two sequentially before checking changes)
            _testable.ClearState();
            var item1 = list[1]; // index after previous move
            var item2 = list[2];
            list.RemoveAt(1);
            list.Insert(3, item1);
            list.RemoveAt(2); // after previous insert, index shifts
            list.Insert(0, item2);
            binding.ManuallyCheckChanges();
            Assert.That( _testable._resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
            //Assert.That(_movedCount, Is.EqualTo(2));

            _testable.ClearState();
            list.Reverse();
            binding.ManuallyCheckChanges();
            //Assert.That( _movedCount, Is.EqualTo( 2) ); // 2 moves: 0->4, 1->3
            Assert.That( _testable._resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
        }

        private void TestCollectionMassiveChange<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue)
        {
            // Add item + change another item
            list.Add(getNewValue());
            list[1] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_testable._resetCount, Is.EqualTo(1));
            Assert.That(_testable._addedCount, Is.EqualTo(0));
            Assert.That(_testable._changedCount, Is.EqualTo(0));
            Assert.That(_testable._removedCount, Is.EqualTo(0));
            Assert.That(_testable._movedCount, Is.EqualTo(0));

            // Clear list
            _testable.ClearState();
            list.Clear();
            binding.ManuallyCheckChanges();
            Assert.That(_testable._resetCount, Is.EqualTo(1));
            Assert.That(_testable._removedCount, Is.EqualTo(0));
            Assert.That(_testable._addedCount, Is.EqualTo(0));
            Assert.That(_testable._changedCount, Is.EqualTo(0));
            Assert.That(_testable._movedCount, Is.EqualTo(0));
        }

        private TestSource _testable;

        private (List<object>, CollectionBinding) GetObjectListBinding( )
        {
            _testable = new TestSource();
            var binding = new CollectionBinding();
            binding.Path = nameof(TestSource.ObjectsList);
            PrepareBinding( binding, _testable, _testable );
            _testable.ClearState();

            return (_testable.ObjectsList, binding);
        }

        private (List<object>, CollectionBinding) GetObjectListComplexBinding( )
        {
            var outer = new TestSource();
            outer.Internal = new TestSource();
            _testable = outer.Internal;
            var binding = new CollectionBinding();
            binding.Path = $"{nameof(TestSource.Internal)}.{nameof(TestSource.ObjectsList)}";
            PrepareBinding( binding, outer, _testable );
            _testable.ClearState();

            return (_testable.ObjectsList, binding);
        }


        private (List<Vector3>, CollectionBinding) GetStructListBinding( )
        {
            _testable = new TestSource();
            var binding = new CollectionBinding();
            binding.Path              =  nameof(TestSource.StructsList);
            PrepareBinding( binding, _testable, _testable );
            _testable.ClearState();

            return (_testable.StructsList, binding);
        }

        private (List<Vector3>, CollectionBinding) GetStructListComplexBinding( )
        {
            var outer = new TestSource();
            outer.Internal = new TestSource();
            _testable      = outer.Internal;
            var binding = new CollectionBinding();
            binding.Path = $"{nameof(TestSource.Internal)}.{nameof(TestSource.StructsList)}";
            PrepareBinding( binding, outer, _testable );
            _testable.ClearState();

            return (_testable.StructsList, binding);
        }


        private void PrepareBinding(CollectionBinding binding, TestSource source, Testable testable )
        {
            binding.ItemAdded         += (sender, index, item) => { testable._addedCount++; testable._addedIndex = index; testable._addedItem = item; };
            binding.ItemRemoved       += (sender, index, item) => { testable._removedCount++; };
            binding.ItemMoved         += (sender, index, index2, item) => { testable._movedCount++; testable._movedFromIndex = index; testable._movedToIndex = index2; testable._movedItem = item; };
            binding.ItemChanged       += (sender, index, item) => { testable._changedCount++; testable._changedIndex = index; testable._changedItem = item; };
            binding.CollectionChanged += (sender, list) => { testable._resetCount++; };
            binding.Init( source );
            binding.Subscribe();
        }
    }

    public class Testable
    {
        public int    _addedCount = 0;
        public int    _addedIndex = 0;
        public object _addedItem;
        public int    _removedCount   = 0;
        public int    _movedCount     = 0;
        public int    _movedFromIndex = 0;
        public int    _movedToIndex   = 0;
        public object _movedItem;
        public int    _changedCount = 0;
        public int    _changedIndex = 0;
        public object _changedItem;
        public int    _resetCount = 0;

        public void ClearState( )
        {
            _addedCount     = 0;
            _addedIndex     = 0;
            _addedItem      = null;
            _removedCount   = 0;
            _movedCount     = 0;
            _movedFromIndex = 0;
            _movedToIndex   = 0;
            _movedItem      = null;
            _changedCount   = 0;
            _changedIndex   = 0;
            _changedItem    = null;
            _resetCount     = 0;
        }


    }

    public class TestSource : Testable
    {
        public List<Object>     ObjectsList { get; set; } = new List<Object>();
        public List<Vector3>     StructsList { get; set; } = new List<Vector3>();

        public TestSource Internal { get; set; }
    }
}
