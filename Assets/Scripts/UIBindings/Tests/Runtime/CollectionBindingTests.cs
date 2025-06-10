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
            ClearState();

            TestCollectionAdd( list, binding, () => new object() ); 
        }

        [Test]
        public void TestCollectionAddValue( )
        {
            var (list, binding) = GetStructListBinding();
            list.Add( Vector3.one );                       //Start with 1-object list
            binding.ManuallyCheckChanges();
            ClearState();

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
            ClearState();

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
            ClearState();

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
            ClearState();

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
            ClearState();

            TestCollectionMove(list, binding);
        }

        [Test]
        public void TestCollectionChangeReference()
        {
            var (list, binding) = GetObjectListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(new Object());
            binding.ManuallyCheckChanges();
            ClearState();
            TestCollectionChange(list, binding, () => new Object());
        }

        [Test]
        public void TestCollectionChangeValue()
        {
            var (list, binding) = GetStructListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(UnityEngine.Random.onUnitSphere);
            binding.ManuallyCheckChanges();
            ClearState();
            TestCollectionChange(list, binding, () => UnityEngine.Random.onUnitSphere);
        }

        [Test]
        public void TestCollectionComplexChangeReference()
        {
            var (list, binding) = GetObjectListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(new Object());
            binding.ManuallyCheckChanges();
            ClearState();
            TestCollectionComplexChange(list, binding, () => new Object());
        }

        [Test]
        public void TestCollectionComplexChangeValue()
        {
            var (list, binding) = GetStructListBinding();
            for (int i = 0; i < 5; i++)
                list.Add(UnityEngine.Random.onUnitSphere);
            binding.ManuallyCheckChanges();
            ClearState();
            TestCollectionComplexChange(list, binding, () => UnityEngine.Random.onUnitSphere);
        }

        private void TestCollectionChange<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue)
        {
            // Change one element
            list[2] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_changedCount, Is.EqualTo(1));
            Assert.That(_changedIndex, Is.EqualTo(2));
            Assert.That(_changedItem, Is.EqualTo(list[2]));

            // Change several elements
            ClearState();
            list[1] = getNewValue();
            list[3] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_changedCount, Is.EqualTo(2));
            Assert.That(_changedIndex, Is.EqualTo(3)); // Last changed index
        }

        private void TestCollectionAdd<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue ) where T : new()
        {
            // Act add plain object
            list.Add( getNewValue() ); 
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_addedCount, Is.EqualTo(1));
            Assert.That( _addedIndex, Is.EqualTo( 1 ) );

            //Act add null object
            ClearState();
            list.Add( default );   
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_addedCount, Is.EqualTo(1));
            Assert.That( _addedItem, Is.EqualTo( default(T) ) ); 

            //Act insert 3 object 
            ClearState();
            list.Insert( 0, getNewValue() );
            list.Insert( 1, getNewValue() );
            list.Insert( 2, getNewValue() );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_addedCount, Is.EqualTo(3));

            //Act add two null objects
            ClearState();
            list.Insert( 0, default );
            list.Add( default );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That( _resetCount, Is.EqualTo( 1 ) );        //No add events, just reset, null objects prevents diff calculation. Avoid nulls
        }

        private void TestCollectionRemove<T>(List<T> list, CollectionBinding binding )
        {
            // Act remove first object
            list.RemoveAt( 0 );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_removedCount, Is.EqualTo(1));

            //Act remove two objects
            ClearState();
            list.Remove( list.Last() );
            list.Remove( list.First() );
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_removedCount, Is.EqualTo(2));

            //Act remove all objects
            ClearState();
            list.Clear();
            binding.ManuallyCheckChanges();
            // Assert
            Assert.That(_resetCount, Is.EqualTo(1));           //No remove events, clear fires reset event
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
            Assert.That( _resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
            // Assert.That(_movedCount, Is.EqualTo(1));
            // Assert.That(_movedFromIndex, Is.EqualTo(fromIndex));
            // Assert.That(_movedToIndex, Is.EqualTo(toIndex));
            // Assert.That(_movedItem, Is.EqualTo(item));

            // Move two items at the same time (simulate by moving two sequentially before checking changes)
            ClearState();
            var item1 = list[1]; // index after previous move
            var item2 = list[2];
            list.RemoveAt(1);
            list.Insert(3, item1);
            list.RemoveAt(2); // after previous insert, index shifts
            list.Insert(0, item2);
            binding.ManuallyCheckChanges();
            Assert.That( _resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
            //Assert.That(_movedCount, Is.EqualTo(2));

            ClearState();
            list.Reverse();
            binding.ManuallyCheckChanges();
            //Assert.That( _movedCount, Is.EqualTo( 2) ); // 2 moves: 0->4, 1->3
            Assert.That( _resetCount, Is.EqualTo( 1 ) ); // No detection for permutations now, just reset
        }

        private void TestCollectionComplexChange<T>(List<T> list, CollectionBinding binding, Func<T> getNewValue)
        {
            // Add item + change another item
            list.Add(getNewValue());
            list[1] = getNewValue();
            binding.ManuallyCheckChanges();
            Assert.That(_resetCount, Is.EqualTo(1));
            Assert.That(_addedCount, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(0));
            Assert.That(_removedCount, Is.EqualTo(0));
            Assert.That(_movedCount, Is.EqualTo(0));

            // Clear list
            ClearState();
            list.Clear();
            binding.ManuallyCheckChanges();
            Assert.That(_resetCount, Is.EqualTo(1));
            Assert.That(_removedCount, Is.EqualTo(0));
            Assert.That(_addedCount, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(0));
            Assert.That(_movedCount, Is.EqualTo(0));
        }

        private TestSource _source;
        private int _addedCount = 0;
        private int _addedIndex = 0;
        private object _addedItem;
        private int _removedCount = 0;
        private int _movedCount = 0;
        private int _movedFromIndex = 0;
        private int _movedToIndex = 0;
        private object _movedItem;
        private int _changedCount = 0;
        private int _changedIndex = 0;
        private object _changedItem;
        private int _resetCount = 0;

        private (List<object>, CollectionBinding) GetObjectListBinding( )
        {
            _source = ScriptableObject.CreateInstance<TestSource>();
            var binding = new CollectionBinding();
            binding.Source = _source;
            binding.Path = nameof(TestSource.ObjectsList);
            PrepareBinding( binding );
            ClearState();

            return (_source.ObjectsList, binding);
        }

        private (List<Vector3>, CollectionBinding) GetStructListBinding( )
        {
            _source = ScriptableObject.CreateInstance<TestSource>();
            var binding = new CollectionBinding();
            binding.Source            =  _source;
            binding.Path              =  nameof(TestSource.StructsList);
            PrepareBinding( binding );
            ClearState();

            return (_source.StructsList, binding);
        }

        private void PrepareBinding(CollectionBinding binding )
        {
            binding.ItemAdded         += (sender, index, item) => { _addedCount++; _addedIndex = index; _addedItem = item; };
            binding.ItemRemoved       += (sender, index, item) => { _removedCount++; };
            binding.ItemMoved         += (sender, index, index2, item) => { _movedCount++; _movedFromIndex = index; _movedToIndex = index2; _movedItem = item; };
            binding.ItemChanged       += (sender, index, item) => { _changedCount++; _changedIndex = index; _changedItem = item; };
            binding.CollectionChanged += (sender, list) => { _resetCount++; };
            binding.Init();
            binding.Subscribe();
        }

        private void ClearState( )
        {
            _addedCount = 0;
            _addedIndex = 0;
            _addedItem = null;
            _removedCount = 0;
            _movedCount = 0;
            _movedFromIndex = 0;
            _movedToIndex = 0;
            _movedItem = null;
            _changedCount = 0;
            _changedIndex = 0;
            _changedItem = null;
            _resetCount = 0;
        }
    }

    public class TestSource : ScriptableObject
    {
        public List<Object>     ObjectsList { get; set; } = new List<Object>();
        public List<Vector3>     StructsList { get; set; } = new List<Vector3>();
    }
}
