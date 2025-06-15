using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    public class QuestVM : MonoBehaviour, INotifyPropertyChanged
    {
        public  String       QuestName => _quest.Data.Name;
        public bool       IsCompleted => _quest.IsCompleted;

        private Quest        _quest;
        private QuestManager _qManager;

        public void Init( Quest quest, QuestManager manager )
        {
            _quest = quest   ?? throw new ArgumentNullException( nameof( quest ) );
            _qManager  = manager ?? throw new ArgumentNullException( nameof( manager ) );
            //OnPropertyChanged( null );
        }

        public void CompleteQuest( )
        {
            if (_quest == null)
                throw new InvalidOperationException("Quest is not initialized.");

            _qManager.CompleteQuest(_quest.Data);
            OnPropertyChanged( nameof( IsCompleted ) );
        }

        public event Action<Object, String> PropertyChanged;

        private void OnPropertyChanged( String propertyName )
        {
            PropertyChanged?.Invoke( this, propertyName );
        }
    }

    public class QuestManager
    {
        public QuestManager( )
        {
            Quests = new []
                     {
                             new Quest(){Data = new QuestData { Name = "Quest 1" }}, 
                             new Quest(){Data = new QuestData { Name = "Quest 2" }}, 
                             new Quest(){Data = new QuestData { Name = "Quest 3" }}, 
                             new Quest(){Data = new QuestData { Name = "Quest 4" }}, 
                             new Quest(){Data = new QuestData { Name = "Quest 5" }}, 
                     };
        }

        public IReadOnlyList<Quest> Quests { get; }

        public void CompleteQuest( QuestData questData )
        {
            // Logic to complete the quest
            foreach (var quest in Quests)
            {
                if (quest.Data.Name == questData.Name && !quest.IsCompleted)
                {
                    quest.IsCompleted = true;
                    Debug.Log($"Quest '{questData.Name}' completed.");
                    QuestCompleted?.Invoke( questData );
                    return;
                }
            }
        }

        public event Action<QuestData> QuestCompleted;
    }

    public readonly struct QuestData
    {
        public String Name { get; init; }

        public override String ToString( ) => Name;
    }

    public record Quest
    {
        public QuestData Data;
        public bool IsCompleted { get; set; }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
