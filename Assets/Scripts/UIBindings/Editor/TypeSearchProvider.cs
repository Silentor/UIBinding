using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Editor
{
    public static class TypeSearchProvider
    {
        public const string Id = "types_search";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(Id, "C# types")
                   {
                           filterId           = "type:",
                           priority           = 99999, 
                           //showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
                           fetchItems         = (context, items, provider) => FetchItems(context, provider),
                           isExplicitProvider = true,
                           onEnable = () => AllTypesCache = TypeCache.GetTypesDerivedFrom<Object>(),
                           //showDetails = true,
                           //fetchThumbnail     = (item, context) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                           //fetchPreview       = (item, context, size, options) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                           //fetchLabel         = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id).name,
                           //fetchDescription   = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id).name,
                           //toObject           = (item, type) => AssetDatabase.LoadMainAssetAtPath(item.id),
                           //trackSelection     = TrackSelection,
                           //startDrag          = StartDrag
                   };
        }

        private static TypeCache.TypeCollection AllTypesCache;

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider )
        {
            var searchText = context.searchQuery;
            foreach ( var type in AllTypesCache )
            {
                var score = 0;
                if ( String.IsNullOrEmpty( searchText ) )
                    score = 1000;
                else
                {
                    var nameIndex = type.Name.IndexOf( searchText, StringComparison.OrdinalIgnoreCase );
                    if ( nameIndex >= 0 )
                        score = 100 + nameIndex + (type.Name.Length - searchText.Length);
                    else
                    {
                        var namespaceIndex = type.Namespace?.IndexOf( searchText, StringComparison.OrdinalIgnoreCase ) ?? -1;
                        if ( namespaceIndex >= 0 )
                        {
                            score = 200 + namespaceIndex;
                        }
                    }
                }

                if( score == 0 )
                    continue;

                if( typeof(INotifyPropertyChanged).IsAssignableFrom( type ) )
                    score /= 10;
                
                var item = provider.CreateItem( context, type.FullName );
                var propertiesCount = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Length;
                var methodsCount = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Length;
                var isNotifable = typeof(INotifyPropertyChanged).IsAssignableFrom(type);
                item.description = $"{type.FullName}{(isNotifable?" supports INotifyPropertyChanged,":"")} {propertiesCount} properties, {methodsCount} methods";
                item.label       = type.FullName;
                //item.options = SearchItemOptions.Highlight;
                item.score = score; 
                item.data = type;
                item.thumbnail = SearchUtils.GetTypeIcon( type );
                yield return item;
            }
        }
    }
}