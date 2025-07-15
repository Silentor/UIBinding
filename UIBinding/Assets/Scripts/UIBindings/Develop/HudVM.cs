using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    public class HudVM : MonoBehaviour, INotifyPropertyChanged
    {
        private QuestManager _questManager;
        private Quest[]      _testQuestArray;

        public  ViewCollection Quests { get; private set; }

        [CollectionBinding(bindMethodName: nameof(BindItem), processMethodName: nameof(ProcessList))]
        public IReadOnlyList<Quest> Quests2 => _questManager.Quests;
        //public Quest[] Quests2 => _questManager.Quests.ToArray();
        //public Quest[] Quests2 => _testQuestArray;
        //public IEnumerable<Quest> Quests2 => _testQuestArray;
        //public IEnumerable Quests2 => _testQuestArray;

        private void BindItem( Object item, GameObject view )
        {
            var questVM = view.GetComponent<QuestVM>();
            if ( questVM != null )
            {
                questVM.Init( (Quest)item, _questManager );
            }
        }

        private void ProcessList( List<Object> viewList )
        {
            //Sort completed quest to the end of the list
            viewList.Sort( (x, y) =>
            {
                var questX = (Quest)x;
                var questY = (Quest)y;
                var isCompletedX = questX.IsCompleted ? 1 : 0;
                var isCompletedY = questY.IsCompleted ? 1 : 0;
                return isCompletedX == isCompletedY ? 
                    string.Compare(questX.Data.Name, questY.Data.Name, StringComparison.Ordinal) : 
                    isCompletedX.CompareTo(isCompletedY);
            } );
        }


        private void Awake( )
        {
            //Inject this dependency in a real application
            _questManager = new QuestManager();
            _questManager.QuestCompleted += QuestManagerOnQuestCompleted;

            Quests = new ViewCollection( _questManager.Quests, ProcessList, BindItem );
        }

        private void Start( )
        {
            _testQuestArray = _questManager.Quests.ToArray();
        }

        private void QuestManagerOnQuestCompleted( QuestData questData )
        {
            // Notify UI about quest completion
            PropertyChanged?.Invoke( this, nameof(Quests2) );
        }

        private void OnDestroy( )
        {
            _questManager.QuestCompleted -= QuestManagerOnQuestCompleted;
        }

        public event Action<Object, String> PropertyChanged;
    }
}
